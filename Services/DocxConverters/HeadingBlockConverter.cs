using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="HeadingBlock"/> в заголовок OpenXML.
/// </summary>
internal sealed class HeadingBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var heading = (HeadingBlock)block;
        var profile = ctx.Profile;

        var p = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{heading.Level}" },
                new SpacingBetweenLines { After = "120" },
                new Indentation { Left = ((heading.Level - 1) * 425).ToString() }));

        DocxInlinesProcessor.Process(p, heading.Inline, profile);
        foreach (var run in p.Elements<Run>())
        {
            DocxRunHelpers.SetRunFont(run, profile, profile.FontSizePt, true);
        }

        body.Append(p);
    }
}
