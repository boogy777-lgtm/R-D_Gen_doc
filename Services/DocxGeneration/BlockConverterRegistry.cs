using System.Collections.Generic;
using AvaloniaApp.Services.DocxConverters;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Реестр <see cref="IBlockConverter"/>-ов: единая точка регистрации
/// стратегий конвертации Markdig-блоков (OCP). Доступен из
/// <c>DocxDocumentBuilder</c> (оркестратор) и из legacy-кода
/// <c>DocxGenerator.ConvertBlockDispatch</c>.
/// </summary>
internal static class BlockConverterRegistry
{
    public static readonly IReadOnlyDictionary<System.Type, IBlockConverter> All = new Dictionary<System.Type, IBlockConverter>
    {
        [typeof(HeadingBlock)] = new HeadingBlockConverter(),
        [typeof(ParagraphBlock)] = new ParagraphBlockConverter(),
        [typeof(ListBlock)] = new ListBlockConverter(),
        [typeof(Table)] = new TableBlockConverter(),
        [typeof(ThematicBreakBlock)] = new ThematicBreakBlockConverter(),
        [typeof(CodeBlock)] = new CodeBlockConverter(),
        [typeof(QuoteBlock)] = new QuoteBlockConverter(),
        [typeof(Markdig.Syntax.HtmlBlock)] = new HtmlBlockConverter(),
    };
}
