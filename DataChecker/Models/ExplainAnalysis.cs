using System;
using System.Collections.Generic;

namespace DataChecker;

public record ExplainAnalysis
{
	public double TotalCost { get; init; }
	public long PlanRows { get; init; }
	public double? ActualTotalTime { get; init; }
	public double PlanningTime { get; init; }
	public double ExecutionTime { get; init; }

	public long SharedHitBlocks { get; init; }
	public long SharedReadBlocks { get; init; }
	public long SharedDirtiedBlocks { get; init; }
	public long SharedWrittenBlocks { get; init; }

	public long LocalHitBlocks { get; init; }
	public long LocalReadBlocks { get; init; }
	public long LocalDirtiedBlocks { get; init; }
	public long LocalWrittenBlocks { get; init; }

	public long TempReadBlocks { get; init; }
	public long TempWrittenBlocks { get; init; }

	public string NodeType { get; init; } = string.Empty;
	public string? RelationName { get; init; }
	public string? IndexName { get; init; }

	// Безопасность запроса по результатам EXPLAIN
	public bool IsSafe { get; init; }
	public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();

	public override string ToString()
	{
		var safety = IsSafe ? "безопасен" : "НЕБЕЗОПАСЕН";
		var violations = Violations.Count > 0 ? ("\nНарушения: " + string.Join("; ", Violations)) : string.Empty;
		return
			"=== Анализ выполнения запроса ===\n" +
			$"Безопасность: {safety}{violations}\n" +
			$"Тип узла (NodeType): {NodeType}\n" +
			$"Имя отношения (RelationName): {RelationName}\n" +
			$"Имя индекса (IndexName): {IndexName}\n" +
			$"Общая стоимость (TotalCost): {TotalCost}\n" +
			$"Строк в плане (PlanRows): {PlanRows}\n" +
			$"Фактическое общее время (ActualTotalTime): {ActualTotalTime}\n" +
			$"Время планирования (PlanningTime): {PlanningTime}\n" +
			$"Время выполнения (ExecutionTime): {ExecutionTime}\n" +
			$"Общие блоки попаданий (SharedHitBlocks): {SharedHitBlocks}\n" +
			$"Общие блоки чтения (SharedReadBlocks): {SharedReadBlocks}\n" +
			$"Общие блоки грязных страниц (SharedDirtiedBlocks): {SharedDirtiedBlocks}\n" +
			$"Общие записанные блоки (SharedWrittenBlocks): {SharedWrittenBlocks}\n" +
			$"Локальные блоки попаданий (LocalHitBlocks): {LocalHitBlocks}\n" +
			$"Локальные блоки чтения (LocalReadBlocks): {LocalReadBlocks}\n" +
			$"Локальные блоки грязных страниц (LocalDirtiedBlocks): {LocalDirtiedBlocks}\n" +
			$"Локальные записанные блоки (LocalWrittenBlocks): {LocalWrittenBlocks}\n" +
			$"Временные блоки чтения (TempReadBlocks): {TempReadBlocks}\n" +
			$"Временные записанные блоки (TempWrittenBlocks): {TempWrittenBlocks}\n" +
			"===============================================";
	}
}


