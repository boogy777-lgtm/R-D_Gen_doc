namespace AvaloniaApp.Models;

/// <summary>
/// Справочник типа документа ИСМ.
/// </summary>
public class DocType
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string LabelRu { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets требуемый уровень согласования (проверки).
    /// </summary>
    public string ApprovalReview { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets требуемый уровень утверждения.
    /// </summary>
    public string ApprovalApprove { get; init; } = string.Empty;

    /// <summary>
    /// Формат отображения в UI: (CODE) Название.
    /// Единый источник для всех ComboBox, DataTemplate и логов.
    /// </summary>
    public string DisplayName => $"({Code}) {LabelRu}";

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
