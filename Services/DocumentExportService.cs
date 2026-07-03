using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <inheritdoc />
public sealed class DocumentExportService : IDocumentExportService
{
    /// <inheritdoc />
    public Task ExportDocxAsync(DocumentMetadata metadata, DocumentBody body, string path)
    {
        return Task.Run(() =>
        {
            // Единый путь: DocumentBodyToMarkdown.Convert(metadata, body) сам подбирает шаблон
            // формы и применяет Placeholder/TemplateDefault к пустым полям.
            // MarkdownService.Compose дополняет YAML-фронтматтер теми же fallback'ами.
            // DocxGenerator получает ту же строку, что и preview / файл на диске.
            var markdown = MarkdownService.Compose(
                metadata,
                DocumentBodyToMarkdown.Convert(metadata, body));
            DocxGenerator.Generate(markdown, path);
        });
    }
}
