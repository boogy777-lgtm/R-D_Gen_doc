using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис построения Markdown-предпросмотра из метаданных и тела документа.
/// Изолирует ViewModel от парсеров/композиторов Markdown.
/// </summary>
public interface IMarkdownPreviewBuilder
{
    /// <summary>
    /// Компонует полный Markdown: YAML frontmatter + структурированное тело.
    /// Пустые поля всегда заменяются placeholder/TemplateDefault — Markdown
    /// является единым источником правды для диска, preview и DOCX.
    /// </summary>
    string Build(DocumentMetadata metadata, DocumentBody body);

    /// <summary>
    /// Компонует полный Markdown с учётом <see cref="FieldPermission"/>
    /// из шаблона формы. Пустые поля всегда заменяются placeholder
    /// или TemplateDefault — Markdown является единым источником правды
    /// для диска, preview и DOCX.
    /// </summary>
    string Build(DocumentMetadata metadata, DocumentBody body, FormTemplate formTemplate);
}
