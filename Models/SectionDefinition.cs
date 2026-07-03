namespace AvaloniaApp.Models;

/// <summary>
/// Тип содержимого раздела шаблона.
/// </summary>
public enum SectionKind
{
    FreeText,
    Table,
    Mermaid,
    Raci,
    Jsa,
    Checklist,
}

/// <summary>
/// Описание одного раздела шаблона (нативный порядок сохраняется).
/// </summary>
public class SectionDefinition
{
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MarkdownTemplate { get; set; } = string.Empty;

    public SectionKind Kind { get; set; } = SectionKind.FreeText;

    public bool IsRequired { get; set; } = true;
}
