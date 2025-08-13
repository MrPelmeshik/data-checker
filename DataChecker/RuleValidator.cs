using System.Data;
using Microsoft.Extensions.Logging;

namespace DataChecker;

public class RuleValidator(ILogger<RuleValidator> logger, ExplainAnalyzer explainAnalyzer)
{
    public async Task Validate(IDbConnection connection, Rules rule)
    {
        if (rule.IsValid is null)
        {
            if (!TryValidateSql(rule.Sql, out var failResult))
            {
                rule.IsValid = false;
                logger.LogWarning("Rule {RuleId} is invalid: {FailResult}", rule.Id, failResult);
            }
            else
            {
                try
                {
                    var explain = await explainAnalyzer.ExplainAsync(
                        connection,
                        DataValidator.ConstructSql(rule),
                        param: rule,
                        analyze: false,
                        buffers: false);

                    if (!explain.IsSafe)
                    {
                        rule.IsValid = false;
                        logger.LogWarning("Rule {RuleId} rejected by EXPLAIN security checks: {Violations}", rule.Id,
                            string.Join(", ", explain.Violations));
                    }
                    else
                    {
                        rule.IsValid = true;
                    }

                    rule.ValidationResult = @$"
Проверка запроса: {(string.IsNullOrEmpty(failResult) ? "Успех" : $"Неудача: {failResult}")}
Анализ запроса: {(explain.IsSafe ? "Успех" : $"Неудача: {string.Join(", ", explain.Violations)}")}
{explain.ToString()}
                ";
                }
                catch (Exception ex)
                {
                    rule.IsValid = false;
                    rule.ValidationResult = @$"
Проверка запроса: {(string.IsNullOrEmpty(failResult) ? "Успех" : $"Неудача: {failResult}")}
Анализ запроса: Неудача: Невозможно выполнить запрос
                ";
                    rule.ValidationRuleStackTrace = ex.ToString();
                }
            }
            logger.LogInformation("Rule {RuleId} is valid: {IsValid}", rule.Id, rule.IsValid);
        }
        else
        {
            logger.LogWarning("Rule {RuleId} is already validated", rule.Id);
        }
    }
    
    private static bool TryValidateSql(string sql, out string failResult)
    {
        failResult = String.Empty;
        return true;
    }
}