using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="ParagraphBlock"/> в параграф OpenXML.
/// </summary>
internal sealed class ParagraphBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var paragraph = (ParagraphBlock)block;
        var profile = ctx.Profile;

        var p = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "120" }));
        DocxInlinesProcessor.Process(p, paragraph.Inline, profile);
        body.Append(p);
    }
}
