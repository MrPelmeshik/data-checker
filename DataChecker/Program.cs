using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataChecker;

public static class Program
{
    public static void Main(string[] args)
    {
        // Create logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger("DataChecker");
        
        logger.LogInformation("Application started");
        
        var explainAnalyzer = new ExplainAnalyzer(loggerFactory.CreateLogger<ExplainAnalyzer>());
        var ruleValidator = new RuleValidator(loggerFactory.CreateLogger<RuleValidator>(), explainAnalyzer);
        var ruleProvider = new RuleProvider(loggerFactory.CreateLogger<RuleProvider>());
        var dataValidator = new DataValidator(loggerFactory.CreateLogger<DataValidator>());
        var validationResultProvider = new ValidationResultProvider(loggerFactory.CreateLogger<ValidationResultProvider>());
        
        var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
        var postgresDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
        var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? string.Empty;

        var connectionString = $"Host={postgresHost};Port=5432;Database={postgresDb};Username={postgresUser};Password={postgresPassword};";
        
        Main(connectionString,
            ruleProvider,
            ruleValidator, 
            dataValidator,
            validationResultProvider).GetAwaiter().GetResult();

        logger.LogInformation("Application completed");
    }

    private static async Task Main(
        string connectionString,
        RuleProvider ruleProvider, 
        RuleValidator ruleValidator, 
        DataValidator dataValidator,
        ValidationResultProvider validationResultProvider)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        foreach (var newRule in await ruleProvider.GetNewRulesAsync(connection))
        { 
            await ruleValidator.Validate(connection, newRule);
            await ruleProvider.UpdateValidateRuleAsync(connection, newRule);
        }

        foreach (var rule in await ruleProvider.GetLastRulesAsync(connection))
        {
            var results = await dataValidator.ValidateAsync(connection, rule);
            await validationResultProvider.InsertValidationResultAsync(connection, results);
        }
    }
}