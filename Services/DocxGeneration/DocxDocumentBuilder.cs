using System.Collections.Generic;
using AvaloniaApp.Models;
using AvaloniaApp.Services.DocxConverters;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Оркестратор DOCX-генерации. Координирует <see cref="DocxStylesFactory"/>,
/// <see cref="DocxHeaderFooterBuilder"/>, <see cref="DocxBodyBuilder"/> и
/// реестр <see cref="BlockConverterRegistry"/>. Заменяет «God class» <c>DocxGenerator</c>
/// (972 строки) — оставлен только thin public API для обратной совместимости.
/// </summary>
internal static class DocxDocumentBuilder
{
    /// <summary>
    /// Главная точка входа: генерирует DOCX из Markdown + метаданных.
    /// </summary>
    public static void Build(string markdown, DocxProfile profile, string outputPath)
    {
        var (metadata, bodyMarkdown) = MarkdownService.Parse(markdown);
        bodyMarkdown = MarkdownService.StripFrontmatter(bodyMarkdown);
        bodyMarkdown = DocxBodyBuilder.StripWikilinks(bodyMarkdown);

        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = new Body();

        DocxStylesFactory.ApplyDefaultStyles(mainPart, profile);
        DocxStylesFactory.AddNumberingDefinitions(mainPart);

        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = DocxHeaderFooterBuilder.CreateHeader(headerPart, metadata, profile, bodyMarkdown);

        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = DocxHeaderFooterBuilder.CreateFooter(metadata, profile);

        var sectionProps = new SectionProperties();
        sectionProps.Append(new HeaderReference { Id = mainPart.GetIdOfPart(headerPart), Type = HeaderFooterValues.Default });
        sectionProps.Append(new FooterReference { Id = mainPart.GetIdOfPart(footerPart), Type = HeaderFooterValues.Default });
        sectionProps.Append(new PageSize { Width = DocxPageLayout.PageWidthTwips, Height = DocxPageLayout.PageHeightTwips });
        sectionProps.Append(new PageMargin
        {
            Top = DocxPageLayout.TopMarginTwips,
            Bottom = DocxPageLayout.BottomMarginTwips,
            Left = DocxPageLayout.LeftMarginTwips,
            Right = DocxPageLayout.RightMarginTwips,
        });

        // --- ТИТУЛЬНЫЙ ЛИСТ ---
        if (!string.IsNullOrWhiteSpace(profile.ApprovalRoleRu))
        {
            body.Append(DocxHeaderFooterBuilder.CreateApprovalTable(metadata, profile));
        }
        else
        {
            body.Append(new Paragraph());
        }

        for (int i = 0; i < 8; i++)
        {
            body.Append(new Paragraph());
        }

        body.Append(DocxBodyBuilder.CreateTitleParagraph(metadata.DocumentName, profile));
        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

        // --- СОДЕРЖАНИЕ ---
        body.Append(DocxBodyBuilder.CreateTocTitle(profile));
        body.Append(DocxBodyBuilder.CreateTocField(profile));
        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

        // --- ТЕЛО ДОКУМЕНТА ---
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseGridTables()
            .UsePipeTables()
            .Build();

        var document = Markdig.Markdown.Parse(bodyMarkdown, pipeline);
        string? lastHeadingText = null;

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                lastHeadingText = DocxBodyBuilder.GetHeadingText(heading);
                ConvertBlock(body, block, profile);
                continue;
            }

            if (block is Markdig.Extensions.Tables.Table mdTable
                && DocxBodyBuilder.IsMonospaceTable(mdTable, lastHeadingText, profile))
            {
                var table = DocxConverters.TableBlockConverter.Build(mdTable, profile);
                DocxBodyBuilder.ApplyMonospaceToTable(table, profile);
                body.Append(table);
                continue;
            }

            ConvertBlock(body, block, profile);
        }

        body.Append(sectionProps);
        mainPart.Document.Append(body);

        DocxBodyBuilder.ApplyPageBreaksBeforeStandardSections(mainPart.Document);
        DocxBodyBuilder.ApplySpacingAfterPageBreaks(mainPart.Document);

        mainPart.Document.Save();
    }

    /// <summary>
    /// Диспетчер блоков: выбирает <see cref="IBlockConverter"/> по типу
    /// и делегирует конвертацию. Неизвестные типы тихо игнорируются.
    /// </summary>
    private static void ConvertBlock(Body body, MarkdownObject block, DocxProfile profile)
    {
        if (block is Block b)
        {
            ConvertBlockDispatch(body, b, new DocxContext(profile));
        }
    }

    private static void ConvertBlockDispatch(Body body, Block block, DocxContext ctx)
    {
        if (BlockConverterRegistry.All.TryGetValue(block.GetType(), out var converter))
        {
            converter.Convert(block, body, ctx);
        }
    }
}
