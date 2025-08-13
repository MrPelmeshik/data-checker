namespace DataChecker;

/// <summary>
/// Модель для таблицы rules (см. 001.schema.sql)
/// </summary>
public class Rules
{
    /// <summary>
    /// Уникальный идентификатор правила (id serial primary key)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SQL запрос для проверки (sql text not null)
    /// </summary>
    public required string Sql { get; set; }

    /// <summary>
    /// Шаблон сообщения об ошибке (fail_template text not null)
    /// </summary>
    public required string FailTemplate { get; set; }

    /// <summary>
    /// Дата и время создания правила (created_at timestamp not null default now())
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Флаг валидности правила (is_valid boolean)
    /// </summary>
    public bool? IsValid { get; set; }

    /// <summary>
    /// Результат валидации правила (validation_result text)
    /// </summary>
    public string? ValidationResult { get; set; }
    
    /// <summary>
    /// Стек вызовов правил (validation_rule_stack_trace text)
    /// </summary>
    public string? ValidationRuleStackTrace { get; set; } = string.Empty;

    /// <summary>
    /// Стек вызовов данных (validation_data_stack_trace text)
    /// </summary>
    public string? ValidationDataStackTrace { get; set; } = string.Empty;
}