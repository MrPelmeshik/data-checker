namespace DataChecker;

/// <summary>
/// Результаты валидации
/// </summary>
public class ValidationResults
{
    /// <summary>
    /// Уникальный идентификатор результата
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор правила
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// Идентификатор проверяемого объекта
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// Флаг неуспешной проверки
    /// </summary>
    public bool IsFail { get; set; }

    /// <summary>
    /// Описание результата проверки
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время выполнения проверки
    /// </summary>
    public DateTime Date { get; set; }
}