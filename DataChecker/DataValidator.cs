using System.Data;
using Microsoft.Extensions.Logging;
using Dapper;

namespace DataChecker;

public class DataValidator(ILogger<DataValidator> logger)
{
    public static string ConstructSql(Rules rule)
    {
        return $@"
select  @{nameof(Rules.Id)} as {nameof(ValidationResults.RuleId)}
,       object_id as {nameof(ValidationResults.ObjectId)}
,       is_fail as {nameof(ValidationResults.IsFail)}
,       case    when is_fail    
                then @{nameof(Rules.FailTemplate)}
                else null
        end as {nameof(ValidationResults.Description)}
,       now() as {nameof(ValidationResults.Date)}
from (
    select  object_id
    ,       bool_or(is_fail) as is_fail
    from (
        {rule.Sql}
    ) t1
    group by object_id
)t2
";
    }
    
    public async Task<List<ValidationResults>> ValidateAsync(IDbConnection connection, Rules rule)
    {
        logger.LogInformation("Validate rule {RuleId}", rule.Id);
        try
        {
            using var tx = connection.BeginTransaction(); // READ ONLY ниже
            await connection.ExecuteAsync(@"
SET LOCAL transaction_read_only = on;
SET LOCAL statement_timeout = '5s';
SET LOCAL lock_timeout = '2s';
SET LOCAL idle_in_transaction_session_timeout = '10s';
SET LOCAL work_mem = '64MB';
SET LOCAL temp_file_limit = '100MB';
", transaction: tx);

            var results = (await connection.QueryAsync<ValidationResults>(
                DataValidator.ConstructSql(rule), rule, transaction: tx, commandTimeout: 5)).AsList();

            tx.Rollback(); // транзакция только для SET LOCAL
            
            return results;
        }
        catch (Exception ex)
        {
            rule.ValidationDataStackTrace = ex.ToString();
            logger.LogError(ex, "Error validating rule {RuleId}", rule.Id);
            return [];
        }
    }
}
