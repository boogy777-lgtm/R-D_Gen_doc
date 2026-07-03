using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер по умолчанию для любых типов <see cref="Block"/>,
/// не имеющих специализированного конвертера.
/// Заменяет <c>default</c>-ветку отсутствующего switch — ничего не делает.
/// Используется через <see cref="DocxGeneration.DocxBlockDispatcher"/>.
/// </summary>
internal sealed class FallbackBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        // Ничего не делаем: default-ветка исходного switch.
    }
}
