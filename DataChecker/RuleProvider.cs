using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace DataChecker;

public class RuleProvider(ILogger<RuleProvider> logger)
{
    public async Task<List<Rules>> GetLastRulesAsync(IDbConnection connection)
    {
        logger.LogInformation("Get last rules");
        return (await GetAllRulesAsync(connection))
            .Where(rule => rule.IsValid is true)
            .GroupBy(rule => rule.Id)
            .Select(group => group.OrderByDescending(rule => rule.CreatedAt).First())
            .OrderBy(rule => rule.Id)
            .ToList();
    }
    
    public async Task<List<Rules>> GetNewRulesAsync(IDbConnection connection)
    {
        logger.LogInformation("Get new rules");
        return (await GetAllRulesAsync(connection))
            .Where(rule => rule.IsValid is null)
            .OrderBy(rule => rule.Id)
            .ToList();
    }

    private async Task<List<Rules>> GetAllRulesAsync(IDbConnection connection)
    {
        return (await connection
                .QueryAsync<Rules>(@$"
select  id as {nameof(Rules.Id)}
,       sql as {nameof(Rules.Sql)}
,       fail_template as {nameof(Rules.FailTemplate)}
,       created_at as {nameof(Rules.CreatedAt)}
,       is_valid as {nameof(Rules.IsValid)}
,       validation_result as {nameof(Rules.ValidationResult)}
,       validation_rule_stack_trace as {nameof(Rules.ValidationRuleStackTrace)}
,       validation_data_stack_trace as {nameof(Rules.ValidationDataStackTrace)}
from rules
"))
            .AsList();
    }
    
    public async Task UpdateValidateRuleAsync(IDbConnection connection, Rules rule)
    {
        logger.LogInformation("Save rule {RuleId}", rule.Id);
        await connection.ExecuteAsync(@$"
update rules 
set is_valid = @{nameof(Rules.IsValid)}
,   validation_result = @{nameof(Rules.ValidationResult)}
,   validation_rule_stack_trace = @{nameof(Rules.ValidationRuleStackTrace)}
,   validation_data_stack_trace = @{nameof(Rules.ValidationDataStackTrace)}
where id = @{nameof(Rules.Id)}
", rule);
    }
}