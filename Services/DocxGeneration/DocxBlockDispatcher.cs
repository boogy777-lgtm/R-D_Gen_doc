using AvaloniaApp.Services.DocxConverters;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Диспетчер <see cref="IBlockConverter"/>-ов: выбирает стратегию
/// по типу <see cref="Block"/> и делегирует конвертацию. Неизвестные
/// типы тихо игнорируются (default-ветка исходного switch).
/// </summary>
internal static class DocxBlockDispatcher
{
    public static void Dispatch(Body body, Block block, DocxContext ctx)
    {
        if (BlockConverterRegistry.All.TryGetValue(block.GetType(), out var converter))
        {
            converter.Convert(block, body, ctx);
        }
    }
}
