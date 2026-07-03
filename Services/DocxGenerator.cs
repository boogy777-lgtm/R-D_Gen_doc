using AvaloniaApp.Services.DocxConverters;
using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services;

/// <summary>
/// Тонкий public-API фасад для DOCX-генерации. Реальная работа делегирована
/// <see cref="DocxDocumentBuilder"/>. Класс оставлен для обратной совместимости
/// с тестами и <c>DocumentExportService</c>, которые вызывают
/// <see cref="Generate(string, string)"/> / <see cref="Generate(string, DocxProfile, string)"/>.
/// </summary>
/// <remarks>
/// <para>Внутренние helpers (<c>ProcessInlines</c>, <c>IsChangeLogTable</c>,
/// <c>ConvertCodeBlockImpl</c>, <c>IsShieldQuote</c>, <c>SetRunFont</c>,
/// <c>CreateDefaultRunProperties</c>) перенесены в <see cref="DocxGeneration"/>:
/// <c>DocxInlinesProcessor</c>, <c>DocxBlockRecognizers</c>, <c>DocxRunHelpers</c>.</para>
/// </remarks>
public static class DocxGenerator
{
    // Идентификаторы нумерации — re-export из BlockConverterConstants.
    internal const int BulletNumId = BlockConverterConstants.BulletNumId;
    internal const int DecimalNumId = BlockConverterConstants.DecimalNumId;

    /// <summary>
    /// Генерирует DOCX из готовой Markdown-строки, содержащей YAML frontmatter.
    /// </summary>
    public static void Generate(string markdown, string outputPath)
    {
        var (metadata, _) = MarkdownService.Parse(markdown);
        var profile = DocxProfile.Create(metadata, new TemplateCatalog().GetByDocType(metadata.DocType));
        Generate(markdown, profile, outputPath);
    }

    /// <summary>
    /// Генерирует DOCX из готовой Markdown-строки с явно заданным профилем.
    /// </summary>
    public static void Generate(string markdown, DocxProfile profile, string outputPath) =>
        DocxDocumentBuilder.Build(markdown, profile, outputPath);

    /// <summary>
    /// Диспетчер блоков: выбирает <see cref="IBlockConverter"/> по типу
    /// и делегирует конвертацию. Неизвестные типы тихо игнорируются.
    /// Делегирует <see cref="DocxBlockDispatcher.Dispatch"/> для единой точки регистрации.
    /// </summary>
    internal static void ConvertBlockDispatch(Body body, MarkdownObject block, DocxContext ctx)
    {
        if (block is Block b)
        {
            DocxBlockDispatcher.Dispatch(body, b, ctx);
        }
    }
}
