using System;
using System.Globalization;
using System.Linq;
using AvaloniaApp.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Построитель верхнего/нижнего колонтитулов и таблицы утверждения для DOCX.
/// Вынесен из <c>DocxGenerator</c> для соблюдения SRP.
/// </summary>
internal static class DocxHeaderFooterBuilder
{
    /// <summary>
    /// Создаёт верхний колонтитул документа (Header) с таблицей
    /// «Наименование документа / Номер документа / Владелец / Утвердил / Стр.».
    /// </summary>
    public static Header CreateHeader(HeaderPart headerPart, DocumentMetadata metadata, DocxProfile profile, string markdownBody)
    {
        var header = new Header();

        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

        var tblProp = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }));
        table.Append(tblProp);

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = ((int)(DocxPageLayout.HeaderCol1Cm * DocxPageLayout.DxaPerCm)).ToString() });
        grid.Append(new GridColumn { Width = ((int)(DocxPageLayout.HeaderCol2Cm * DocxPageLayout.DxaPerCm)).ToString() });
        grid.Append(new GridColumn { Width = ((int)(DocxPageLayout.HeaderCol3Cm * DocxPageLayout.DxaPerCm)).ToString() });
        grid.Append(new GridColumn { Width = ((int)(DocxPageLayout.HeaderCol4Cm * DocxPageLayout.DxaPerCm)).ToString() });
        grid.Append(new GridColumn { Width = ((int)(DocxPageLayout.HeaderCol5Cm * DocxPageLayout.DxaPerCm)).ToString() });
        table.Append(grid);

        var row1 = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        row1.Append(CreateMergedHeaderCell("Наименование документа:", metadata.DocumentName, 2, profile));
        row1.Append(CreateMergedHeaderCell("Номер документа:", metadata.DocumentCode, 3, profile));
        table.Append(row1);

        var row2 = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        row2.Append(CreateHeaderCell("Владелец документа:", metadata.DepartmentName, profile));
        row2.Append(CreateHeaderCell("Утвердил:", metadata.ApprovedBy, profile));
        row2.Append(CreateHeaderCell("Дата ред.:", GetRevisionDate(metadata, markdownBody), profile));
        row2.Append(CreateHeaderCell("Номер ред.:", metadata.Revision, profile));
        row2.Append(CreatePageHeaderCell(profile));
        table.Append(row2);

        header.Append(table);
        return header;
    }

    /// <summary>
    /// Создаёт нижний колонтитул (Footer) с текстом о неконтролируемости печатных копий.
    /// </summary>
    public static Footer CreateFooter(DocumentMetadata metadata, DocxProfile profile)
    {
        // ВНИМАНИЕ: pBdr должен идти ПЕРЕД jc в порядке OpenXML ParagraphProperties children.
        var footer = new Footer();
        var pPr = new ParagraphProperties(
            new ParagraphBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }),
            new Justification { Val = JustificationValues.Center });
        var p = new Paragraph(pPr);
        var footerText = string.IsNullOrWhiteSpace(metadata.FooterText)
            ? "Печатные версии этого документа считаются НЕКОНТРОЛИРУЕМЫМИ."
            : metadata.FooterText;
        var run = p.AppendChild(new Run(new Text(footerText)));
        DocxRunHelpers.SetRunFont(run, profile, profile.FontSizePt, false);
        run.RunProperties!.Italic = new Italic();
        run.RunProperties.Color = new Color { Val = "595959" };
        footer.Append(p);
        return footer;
    }

    /// <summary>
    /// Создаёт таблицу утверждения («УТВЕРЖДЕНО» + реквизиты).
    /// </summary>
    public static DocumentFormat.OpenXml.Wordprocessing.Table CreateApprovalTable(DocumentMetadata metadata, DocxProfile profile)
    {
        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

        const int leftColDxa = (int)(10.58 * DocxPageLayout.DxaPerCm);
        const int rightColDxa = (int)(DocxPageLayout.ApprovalRightColCm * DocxPageLayout.DxaPerCm);

        table.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableIndentation { Width = 57, Type = TableWidthUnitValues.Dxa },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" })));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = leftColDxa.ToString() });
        grid.Append(new GridColumn { Width = rightColDxa.ToString() });
        table.Append(grid);

        var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        row.Append(new DocumentFormat.OpenXml.Wordprocessing.TableCell(
            new TableCellProperties(new TableCellWidth { Width = leftColDxa.ToString(), Type = TableWidthUnitValues.Dxa }),
            new Paragraph()));

        var protocol = string.IsNullOrWhiteSpace(metadata.Protocol) ? "ХХ" : metadata.Protocol;
        var approvalText = $"{profile.ApprovalRoleRu}\n{metadata.Approver}\nООО «БурСервис»\n\n{metadata.EffectiveDate}, Протокол № {protocol}";
        var cell = new DocumentFormat.OpenXml.Wordprocessing.TableCell(
            new TableCellProperties(new TableCellWidth { Width = rightColDxa.ToString(), Type = TableWidthUnitValues.Dxa }),
            new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "120" }, new Justification { Val = JustificationValues.Left }),
                DocxRunHelpers.CreateRun("УТВЕРЖДЕНО", profile, profile.FontSizePt, true)),
            DocxRunHelpers.CreateMultilineParagraph(
                approvalText,
                false,
                profile,
                new ParagraphProperties(new SpacingBetweenLines { Line = "276", LineRule = LineSpacingRuleValues.Auto }, new Justification { Val = JustificationValues.Left })));
        row.Append(cell);
        table.Append(row);
        return table;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.TableCell CreateMergedHeaderCell(string label, string value, int gridSpan, DocxProfile profile)
    {
        var cell = CreateBaseHeaderCell();
        var p = cell.Elements<Paragraph>().First();
        p.Append(DocxRunHelpers.CreateRun(label, profile, profile.FontSizePt, true));
        p.Append(new Run(new Break()));
        p.Append(DocxRunHelpers.CreateRun(value, profile, profile.FontSizePt, false));
        cell.TableCellProperties!.PrependChild(new GridSpan { Val = gridSpan });
        return cell;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.TableCell CreateHeaderCell(string label, string value, DocxProfile profile)
    {
        var cell = CreateBaseHeaderCell();
        var p = cell.Elements<Paragraph>().First();
        p.Append(DocxRunHelpers.CreateRun(label, profile, profile.FontSizePt, true));
        p.Append(new Run(new Break()));
        p.Append(DocxRunHelpers.CreateRun(value, profile, profile.FontSizePt, false));
        return cell;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.TableCell CreatePageHeaderCell(DocxProfile profile)
    {
        var cell = CreateBaseHeaderCell();
        var p = cell.Elements<Paragraph>().First();
        p.Append(DocxRunHelpers.CreateRun("Стр:", profile, profile.FontSizePt, true));
        p.Append(new Run(new Break()));

        var runPage = p.AppendChild(new Run());
        runPage.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
        runPage.Append(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve });
        runPage.Append(new FieldChar { FieldCharType = FieldCharValues.End });
        DocxRunHelpers.SetRunFont(runPage, profile, profile.FontSizePt, false);

        p.Append(DocxRunHelpers.CreateRun(" из ", profile, profile.FontSizePt, false));

        var runNumPages = p.AppendChild(new Run());
        runNumPages.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
        runNumPages.Append(new FieldCode(" NUMPAGES ") { Space = SpaceProcessingModeValues.Preserve });
        runNumPages.Append(new FieldChar { FieldCharType = FieldCharValues.End });
        DocxRunHelpers.SetRunFont(runNumPages, profile, profile.FontSizePt, false);

        return cell;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.TableCell CreateBaseHeaderCell()
    {
        return new DocumentFormat.OpenXml.Wordprocessing.TableCell(
            new TableCellProperties(
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
            new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = "0" },
                    new Justification { Val = JustificationValues.Center })));
    }

    /// <summary>
    /// Возвращает дату редакции для шапки: последняя строка таблицы ЛРИ в Markdown,
    /// либо effective_date если ЛРИ не найден.
    /// </summary>
    private static string GetRevisionDate(DocumentMetadata metadata, string markdownBody)
    {
        var lastDate = ExtractLastChangeLogDate(markdownBody);
        var raw = lastDate ?? metadata.DateApproved;
        if (DateTime.TryParseExact(
                raw,
                new[] { "dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        return raw;
    }

    /// <summary>
    /// Извлекает дату из последней строки таблицы «Лист регистрации изменений» в Markdown.
    /// </summary>
    private static string? ExtractLastChangeLogDate(string markdownBody)
    {
        if (string.IsNullOrWhiteSpace(markdownBody))
        {
            return null;
        }

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseGridTables()
            .UsePipeTables()
            .Build();

        var document = Markdig.Markdown.Parse(markdownBody, pipeline);
        foreach (var block in document)
        {
            if (block is not Markdig.Extensions.Tables.Table table || !DocxBlockRecognizers.IsChangeLogTable(table))
            {
                continue;
            }

            Markdig.Extensions.Tables.TableRow? lastDataRow = null;
            foreach (var row in table)
            {
                if (row is Markdig.Extensions.Tables.TableRow tableRow && tableRow.Count > 0)
                {
                    lastDataRow = tableRow;
                }
            }

            if (lastDataRow == null)
            {
                continue;
            }

            // Ищем первый LiteralInline в ячейке (Markdig-таблица).
            // Markdig: Markdig.Extensions.Tables.TableCell — ContainerBlock, у которого
            // Descendants<T>() возвращает все вложенные элементы типизированно.
            LiteralInline? literal = null;
            if (lastDataRow.FirstOrDefault() is Markdig.Extensions.Tables.TableCell mdFirstCell)
            {
                literal = mdFirstCell.Descendants<LiteralInline>().FirstOrDefault();
            }

            if (literal != null)
            {
                return literal.Content.ToString();
            }
        }

        return null;
    }
}
