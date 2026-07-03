using System;
using System.Linq;
using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableRow = Markdig.Extensions.Tables.TableRow;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="MdTable"/> в таблицу OpenXML.
/// </summary>
internal sealed class TableBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var mdTable = (MdTable)block;
        body.Append(Build(mdTable, ctx.Profile));
    }

    /// <summary>
    /// Возвращает OpenXML-таблицу без добавления в <see cref="Body"/>.
    /// Используется из <c>DocxDocumentBuilder</c> для monospace-таблиц (RACI, JSA),
    /// когда нужно сначала получить таблицу и применить моноширинный шрифт.
    /// </summary>
    internal static DocumentFormat.OpenXml.Wordprocessing.Table Build(MdTable mdTable, DocxProfile profile)
    {
        return BuildInternal(mdTable, profile);
    }

    private static DocumentFormat.OpenXml.Wordprocessing.Table BuildInternal(MdTable mdTable, DocxProfile profile)
    {
        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
        bool isLri = DocxBlockRecognizers.IsChangeLogTable(mdTable);

        table.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" })));

        int maxCols = 0;
        foreach (var row in mdTable)
        {
            if (row is MdTableRow mdRow)
            {
                maxCols = Math.Max(maxCols, mdRow.Count);
            }
        }

        var grid = new TableGrid();
        int[] lriWidthsDxa = { (int)(2.37 * 567), (int)(1.59 * 567), (int)(3.64 * 567), (int)(3.72 * 567), (int)(6.79 * 567) };
        for (int i = 0; i < maxCols; i++)
        {
            int w = isLri && i < lriWidthsDxa.Length ? lriWidthsDxa[i] : 1500;
            grid.Append(new GridColumn { Width = w.ToString() });
        }

        table.Append(grid);

        int rowIndex = 0;
        foreach (var row in mdTable)
        {
            if (row is MdTableRow mdRow)
            {
                var tr = new DocumentFormat.OpenXml.Wordprocessing.TableRow();

                if (isLri)
                {
                    var rowHeightDxa = rowIndex == 0 ? (int)(0.45 * 567) : (int)(0.65 * 567);
                    var trp = new TableRowProperties();
                    trp.Append(new TableRowHeight { Val = (uint)rowHeightDxa });
                    tr.Append(trp);
                }

                int colIndex = 0;
                foreach (var cellObj in mdRow)
                {
                    if (cellObj is MdTableCell mdCell)
                    {
                        TableCellProperties? tcpExtra = null;
                        if (isLri && colIndex < lriWidthsDxa.Length)
                        {
                            tcpExtra = new TableCellProperties(
                                new TableCellWidth { Width = lriWidthsDxa[colIndex].ToString(), Type = TableWidthUnitValues.Dxa });
                        }

                        var tc = new DocumentFormat.OpenXml.Wordprocessing.TableCell(
                            new TableCellProperties(
                                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                            new Paragraph(
                                new ParagraphProperties(
                                    new SpacingBetweenLines { After = "0" },
                                    new Justification { Val = JustificationValues.Center })));

                        if (tcpExtra != null)
                        {
                            var tProps = tc.TableCellProperties!;
                            tProps.TableCellWidth = new TableCellWidth
                            {
                                Width = lriWidthsDxa[colIndex].ToString(),
                                Type = TableWidthUnitValues.Dxa,
                            };
                        }

                        var cellParagraph = tc.Elements<Paragraph>().First();

                        var firstParagraph = mdCell.OfType<ParagraphBlock>().FirstOrDefault();
                        if (firstParagraph is not null)
                        {
                            DocxInlinesProcessor.Process(cellParagraph, firstParagraph.Inline, profile);
                        }
                        else
                        {
                            var literal = mdCell.Descendants<LiteralInline>().FirstOrDefault();
                            if (literal is not null)
                            {
                                var run = cellParagraph.AppendChild(new Run(new Text(literal.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve }));
                                DocxRunHelpers.SetRunFont(run, profile, profile.FontSizePt, false);
                            }
                        }

                        tr.Append(tc);
                        colIndex++;
                    }
                }

                table.Append(tr);
                rowIndex++;
            }
        }

        return table;
    }
}
