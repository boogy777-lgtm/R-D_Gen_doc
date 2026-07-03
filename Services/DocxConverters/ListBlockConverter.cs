using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="ListBlock"/> в маркированный/нумерованный список OpenXML.
/// </summary>
internal sealed class ListBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var list = (ListBlock)block;
        var profile = ctx.Profile;

        int numId = list.IsOrdered ? BlockConverterConstants.DecimalNumId : BlockConverterConstants.BulletNumId;
        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                bool isFirstParagraph = true;
                foreach (var child in listItem)
                {
                    if (child is ParagraphBlock pb)
                    {
                        var pProps = new ParagraphProperties();

                        if (isFirstParagraph)
                        {
                            pProps.Append(new NumberingProperties(
                                new NumberingLevelReference { Val = 0 },
                                new NumberingId { Val = numId }));
                            pProps.Append(new SpacingBetweenLines { After = "120" });
                            isFirstParagraph = false;
                        }
                        else
                        {
                            pProps.Append(new SpacingBetweenLines { After = "120" });
                            pProps.Append(new Indentation { Left = "1145" });
                        }

                        var p = new Paragraph(pProps);
                        DocxInlinesProcessor.Process(p, pb.Inline!, profile);
                        body.Append(p);
                    }
                    else
                    {
                        DispatchHelpers.ConvertBlockDispatch(body, child, ctx);
                    }
                }
            }
        }
    }
}
