using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="QuoteBlock"/> в OpenXML.
/// Поддерживает две формы:
/// <list type="bullet">
/// <item><c>[!shield]</c> callout — при <c>profile.RenderShieldAsCallout</c> (см. <c>IsShieldQuote</c>).</item>
/// <item>Обычная цитата с левой границей.</item>
/// </list>
/// </summary>
internal sealed class QuoteBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var quote = (QuoteBlock)block;
        var profile = ctx.Profile;

        if (profile.RenderShieldAsCallout && DocxBlockRecognizers.IsShieldQuote(quote))
        {
            ConvertShieldCallout(body, quote, profile);
        }
        else
        {
            ConvertQuoteBlock(body, quote, profile);
        }
    }

    private static void ConvertQuoteBlock(Body body, QuoteBlock quote, DocxProfile profile)
    {
        foreach (var child in quote)
        {
            if (child is ParagraphBlock pb)
            {
                var p = new Paragraph(
                    new ParagraphProperties(
                        new ParagraphBorders(
                            new LeftBorder { Val = BorderValues.Single, Size = 24, Color = "808080", Space = 10 }),
                        new SpacingBetweenLines { After = "120" },
                        new Indentation { Left = "720" }));
                DocxInlinesProcessor.Process(p, pb.Inline, profile);
                body.Append(p);
            }
            else
            {
                DispatchHelpers.ConvertBlockDispatch(body, child, new DocxContext(profile));
            }
        }
    }

    private static void ConvertShieldCallout(Body body, QuoteBlock quote, DocxProfile profile)
    {
        var inherited = DocxRunHelpers.CreateDefaultRunProperties(profile, profile.FontSizePt, false);
        inherited.Italic = new Italic();

        foreach (var child in quote)
        {
            if (child is ParagraphBlock pb)
            {
                var p = new Paragraph(
                    new ParagraphProperties(
                        new ParagraphBorders(
                            new LeftBorder { Val = BorderValues.Single, Size = 24, Color = "2E75B5", Space = 10 }),
                        new Shading { Val = ShadingPatternValues.Clear, Fill = "F5F5F5" },
                        new SpacingBetweenLines { After = "120" },
                        new Indentation { Left = "720" }));
                DocxInlinesProcessor.Process(p, pb.Inline, profile, inherited.CloneNode(true) as RunProperties);
                body.Append(p);
            }
            else
            {
                DispatchHelpers.ConvertBlockDispatch(body, child, new DocxContext(profile));
            }
        }
    }
}
