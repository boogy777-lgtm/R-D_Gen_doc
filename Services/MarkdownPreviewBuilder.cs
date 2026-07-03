using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <inheritdoc />
public sealed class MarkdownPreviewBuilder : IMarkdownPreviewBuilder
{
    /// <inheritdoc />
    public string Build(DocumentMetadata metadata, DocumentBody body)
        => MarkdownService.Compose(
            metadata,
            DocumentBodyToMarkdown.Convert(metadata, body));

    /// <inheritdoc />
    public string Build(DocumentMetadata metadata, DocumentBody body, FormTemplate formTemplate)
        => Build(metadata, body);
}
