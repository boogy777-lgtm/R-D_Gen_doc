using System.IO;
using System.Text;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <inheritdoc />
public sealed class MarkdownFileService : IMarkdownFileService
{
    /// <inheritdoc />
    public async Task<(DocumentMetadata Metadata, DocumentBody Body)> OpenAsync(string path)
    {
        var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var (metadata, bodyMarkdown) = MarkdownService.Parse(text);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        // Гарантируем стартовые поля для всех Repeating* коллекций — даже если .md файл
        // был создан до Phase 4 (например sample.md без Terms/ChangeLog), пользователь
        // увидит пустые строки для заполнения.
        DocumentBodyFactory.EnsureRequiredFields(body, metadata);

        return (metadata, body);
    }

    /// <inheritdoc />
    public async Task SaveAsync(string path, DocumentMetadata metadata, DocumentBody body)
    {
        // Markdown — единый источник правды: DocumentBodyToMarkdown.Convert(metadata, body)
        // сам подберёт шаблон и применит Placeholder/TemplateDefault для пустых полей.
        var markdown = MarkdownService.Compose(
            metadata,
            DocumentBodyToMarkdown.Convert(metadata, body));
        await File.WriteAllTextAsync(path, markdown, Encoding.UTF8).ConfigureAwait(false);
    }
}
