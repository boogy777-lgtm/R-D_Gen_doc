using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="ThematicBreakBlock"/> (Markdown <c>---</c>) в пустой параграф.
/// Идентично исходному <c>case ThematicBreakBlock</c>.
/// </summary>
internal sealed class ThematicBreakBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        body.Append(new Paragraph());
    }
}
