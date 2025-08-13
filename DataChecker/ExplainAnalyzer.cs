using System.Data;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Logging;

namespace DataChecker;

public class ExplainAnalyzer(ILogger<ExplainAnalyzer> logger)
{
	private static readonly string[] DisallowedSchemas =
	[
		"pg_catalog",
		"information_schema"
	];

	private static readonly string[] DisallowedSchemaPrefixes =
	[
		"pg_toast",
		"pg_temp"
	];

	private static readonly string[] BlacklistedFunctions =
	[
		"pg_sleep",
		"pg_read_file",
		"pg_read_binary_file",
		"pg_stat_file",
		"pg_ls_dir",
		"pg_logdir_ls",
		"pg_file_settings",
		"pg_reload_conf",
		"pg_write_file",
		"pg_terminate_backend",
		"pg_cancel_backend",
		"dblink",
		"dblink_exec",
		"lo_import",
		"lo_export"
	];
    public async Task<ExplainAnalysis> ExplainAsync(IDbConnection connection, string sql, object? param = null, bool analyze = true, bool buffers = true)
	{
        var options = new List<string> { "VERBOSE", "FORMAT JSON" };
        if (analyze) options.Insert(0, "ANALYZE");
        if (buffers) options.Insert(1, "BUFFERS");

		var explainSql = $"EXPLAIN ({string.Join(", ", options)}) {sql}";
		logger.LogInformation("Running EXPLAIN with options: {Options}", string.Join(", ", options));

		var json = await connection.QuerySingleAsync<string>(explainSql, param);

		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement[0];
		var plan = root.GetProperty("Plan");

		var totals = new IoTotals();
		Accumulate(plan, ref totals);

		var violations = new List<string>();
		ScanSecurity(plan, violations);
		var isSafe = violations.Count == 0;

		var actualTotalTime = TryGetDouble(plan, "Actual Total Time");

		return new ExplainAnalysis
		{
			TotalCost = GetDouble(plan, "Total Cost"),
			PlanRows = (long)GetDouble(plan, "Plan Rows"),
			ActualTotalTime = actualTotalTime,
			PlanningTime = TryGetDouble(root, "Planning Time") ?? 0,
			ExecutionTime = TryGetDouble(root, "Execution Time") ?? 0,
			NodeType = TryGetString(plan, "Node Type") ?? string.Empty,
			RelationName = TryGetString(plan, "Relation Name"),
			IndexName = TryGetString(plan, "Index Name"),

			SharedHitBlocks = totals.SharedHitBlocks,
			SharedReadBlocks = totals.SharedReadBlocks,
			SharedDirtiedBlocks = totals.SharedDirtiedBlocks,
			SharedWrittenBlocks = totals.SharedWrittenBlocks,

			LocalHitBlocks = totals.LocalHitBlocks,
			LocalReadBlocks = totals.LocalReadBlocks,
			LocalDirtiedBlocks = totals.LocalDirtiedBlocks,
			LocalWrittenBlocks = totals.LocalWrittenBlocks,

			TempReadBlocks = totals.TempReadBlocks,
			TempWrittenBlocks = totals.TempWrittenBlocks,

			IsSafe = isSafe,
			Violations = violations
		};
	}

	private static void Accumulate(JsonElement node, ref IoTotals totals)
	{
		totals.SharedHitBlocks += (long)(TryGetDouble(node, "Shared Hit Blocks") ?? 0);
		totals.SharedReadBlocks += (long)(TryGetDouble(node, "Shared Read Blocks") ?? 0);
		totals.SharedDirtiedBlocks += (long)(TryGetDouble(node, "Shared Dirtied Blocks") ?? 0);
		totals.SharedWrittenBlocks += (long)(TryGetDouble(node, "Shared Written Blocks") ?? 0);

		totals.LocalHitBlocks += (long)(TryGetDouble(node, "Local Hit Blocks") ?? 0);
		totals.LocalReadBlocks += (long)(TryGetDouble(node, "Local Read Blocks") ?? 0);
		totals.LocalDirtiedBlocks += (long)(TryGetDouble(node, "Local Dirtied Blocks") ?? 0);
		totals.LocalWrittenBlocks += (long)(TryGetDouble(node, "Local Written Blocks") ?? 0);

		totals.TempReadBlocks += (long)(TryGetDouble(node, "Temp Read Blocks") ?? 0);
		totals.TempWrittenBlocks += (long)(TryGetDouble(node, "Temp Written Blocks") ?? 0);

		if (!node.TryGetProperty("Plans", out var children) || children.ValueKind != JsonValueKind.Array) return;
		foreach (var child in children.EnumerateArray())
			Accumulate(child, ref totals);
	}

	private static double GetDouble(JsonElement e, string name) => e.GetProperty(name).GetDouble();
	private static double? TryGetDouble(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : (double?)null;
	private static string? TryGetString(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static IEnumerable<string> TryGetStringArray(JsonElement e, string name)
    {
	    if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
            yield break;
	    foreach (var i in v.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String))
	    {
		    yield return i.GetString() ?? string.Empty;
	    }
    }

	private static void ScanSecurity(JsonElement node, List<string> violations)
	{
		var nodeType = TryGetString(node, "Node Type")?.Replace(" ", string.Empty).ToLowerInvariant();
		switch (nodeType)
		{
			case "modifytable":
				violations.Add("DML операция (ModifyTable) обнаружена");
				break;
			case "lockrows":
				violations.Add("Блокировка строк (FOR UPDATE/SHARE) обнаружена");
				break;
			case "functionscan":
				violations.Add("Function Scan обнаружен (возможен вызов небезопасной функции)");
				break;
			case "customscan":
				violations.Add("Custom Scan обнаружен (расширение/кастомный планировщик)");
				break;
			case "foreignscan":
				violations.Add("Foreign Scan обнаружен (FDW доступ к внешним данным)");
				break;
			case "projectset":
				violations.Add("ProjectSet обнаружен (set-returning функции в списке вывода)");
				break;
		}

		// Запрет системных схем (если схема известна в узле)
		var schema = TryGetString(node, "Schema")?.ToLowerInvariant();
		if (!string.IsNullOrEmpty(schema))
		{
			violations.AddRange(from s in DisallowedSchemas where schema == s select $"Доступ к системной схеме: {schema}");
			violations.AddRange(from p in DisallowedSchemaPrefixes where schema.StartsWith(p) select $"Доступ к временной/системной схеме: {schema}");
		}

		// Поиск опасных функций в явных списках функций узла
		if (node.TryGetProperty("Functions", out var funcs) && funcs.ValueKind == JsonValueKind.Array)
		{
			violations.AddRange(from f in funcs.EnumerateArray() select TryGetString(f, "Function Name")?.ToLowerInvariant() into fn where !string.IsNullOrEmpty(fn) from b in BlacklistedFunctions where fn.Contains(b) select $"Запрещенная функция: {fn}");
		}
		else
		{
			var singleFn = TryGetString(node, "Function Name")?.ToLowerInvariant();
			if (!string.IsNullOrEmpty(singleFn))
			{
				violations.AddRange(from b in BlacklistedFunctions where singleFn.Contains(b) select $"Запрещенная функция: {singleFn}");
			}
		}

		// Поиск опасных функций в строковых выражениях плана
        var exprFields = new[] { "Filter", "Index Cond", "Recheck Cond", "Hash Cond", "Join Filter", "Merge Cond", "TID Cond", "One-Time Filter" };
		violations.AddRange(from field in exprFields select TryGetString(node, field)?.ToLowerInvariant() into expr where !string.IsNullOrEmpty(expr) from b in BlacklistedFunctions where expr.Contains(b + "(") || expr.Contains("." + b + "(") select $"Запрещенная функция в выражении: {b}");
		// Массивы выражений (Output, Sort Key, Group Key)
		var arrayExprFields = new[] { "Output", "Sort Key", "Group Key" };
		violations.AddRange(from field in arrayExprFields from expr in TryGetStringArray(node, field) select expr.ToLowerInvariant() into e from b in BlacklistedFunctions where e.Contains(b + "(") || e.Contains("." + b + "(") select $"Запрещенная функция в выражении: {b}");

		if (node.TryGetProperty("Plans", out var children) && children.ValueKind == JsonValueKind.Array)
		{
			foreach (var child in children.EnumerateArray())
				ScanSecurity(child, violations);
		}

		// Некоторые вложенные планы могут быть в других массивах объектов (например, CTE/InitPlan), попытаемся просканировать поля-объекты с под-планом
		foreach (var prop in node.EnumerateObject())
		{
			switch (prop.Value.ValueKind)
			{
				case JsonValueKind.Object:
					if (prop.Value.TryGetProperty("Plan", out var subPlan))
						ScanSecurity(subPlan, violations);
					break;
				case JsonValueKind.Array:
				{
					foreach (var el in prop.Value.EnumerateArray())
						if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("Plan", out var subPlan2))
							ScanSecurity(subPlan2, violations);
					break;
				}
				default:
					break;
			}
		}
	}

	private struct IoTotals
	{
		public long SharedHitBlocks;
		public long SharedReadBlocks;
		public long SharedDirtiedBlocks;
		public long SharedWrittenBlocks;
		public long LocalHitBlocks;
		public long LocalReadBlocks;
		public long LocalDirtiedBlocks;
		public long LocalWrittenBlocks;
		public long TempReadBlocks;
		public long TempWrittenBlocks;
	}
}


