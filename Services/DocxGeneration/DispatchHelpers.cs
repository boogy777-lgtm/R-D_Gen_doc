using AvaloniaApp.Services.DocxConverters;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Адаптер для конвертеров: позволяет вызывать диспетчер из подконтекста
/// (например, из вложенного <c>QuoteBlock</c>), не зная о существовании
/// <see cref="DocxBlockDispatcher"/>.
/// </summary>
internal static class DispatchHelpers
{
    public static void ConvertBlockDispatch(Body body, MarkdownObject block, DocxContext ctx)
    {
        if (block is Block b)
        {
            DocxBlockDispatcher.Dispatch(body, b, ctx);
        }
    }
}
