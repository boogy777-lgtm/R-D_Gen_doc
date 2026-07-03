namespace AvaloniaApp.Models;

/// <summary>
/// Подразделение / функциональное направление.
/// </summary>
public class Department
{
    public string Code { get; set; } = string.Empty;

    public string NameRu { get; set; } = string.Empty;

    public string? ParentCode { get; set; }

    public bool IsRoot { get; set; }

    /// <summary>
    /// Формат отображения в UI: (CODE) Название.
    /// Единый источник для всех ComboBox, DataTemplate и логов.
    /// </summary>
    public string DisplayName => $"({Code}) {NameRu}";

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
