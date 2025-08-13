using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace DataChecker;

public class ValidationResultProvider(ILogger<ValidationResultProvider> logger)
{
    public async Task InsertValidationResultAsync(IDbConnection connection, List<ValidationResults> validationResults)
    {
        logger.LogInformation("Insert validation result list {Count}", validationResults.Count);
        foreach (var validationResult in validationResults)
        {
            await InsertValidationResultAsync(connection, validationResult);
        }
    }

    
    
    public async Task InsertValidationResultAsync(IDbConnection connection, ValidationResults validationResult)
    {
        logger.LogInformation("Insert validation result");
        await connection.ExecuteAsync(@$"
insert into validation_results (rule_id
,                               object_id
,                               is_fail
,                               description
,                               date)
values (    @{nameof(ValidationResults.RuleId)}
,           @{nameof(ValidationResults.ObjectId)}
,           @{nameof(ValidationResults.IsFail)}
,           @{nameof(ValidationResults.Description)}
,           @{nameof(ValidationResults.Date)})
", validationResult);
    }
}