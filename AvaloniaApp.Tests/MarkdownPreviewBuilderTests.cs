using AvaloniaApp.Models;
using AvaloniaApp.Services;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для MarkdownPreviewBuilder: проверяем, что композиция фронтматтера
/// и тела не падает и возвращает непустую строку.
/// </summary>
public class MarkdownPreviewBuilderTests
{
    [Fact]
    public void Build_ComposesFrontmatterAndBody()
    {
        var builder = new MarkdownPreviewBuilder();
        var meta = new DocumentMetadata { DocId = "PV-1", Title = "Превью" };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body);

        var result = builder.Build(meta, body);

        Assert.Contains("doc_id: PV-1", result);
        Assert.Contains("title: Превью", result);
        Assert.Contains("## 1 Цель", result);
    }

    [Fact]
    public void Build_WithEmptyBody_ReturnsValidMarkdown()
    {
        var builder = new MarkdownPreviewBuilder();
        var meta = new DocumentMetadata { DocId = "EMPTY" };
        var body = new DocumentBody();

        var result = builder.Build(meta, body);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("doc_id: EMPTY", result);
    }

    [Fact]
    public void Build_ReflectsBodyMutations()
    {
        var builder = new MarkdownPreviewBuilder();
        var meta = new DocumentMetadata { DocId = "MUT", Title = "T" };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body);
        body.Purpose = "Изменённая цель";

        var result = builder.Build(meta, body);

        Assert.Contains("Изменённая цель", result);
    }
}
