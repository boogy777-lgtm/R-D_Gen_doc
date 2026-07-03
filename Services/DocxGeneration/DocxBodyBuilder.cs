using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Построитель тела DOCX-документа: TOC, page breaks, монопространственные таблицы.
/// Вынесен из <c>DocxGenerator</c> для соблюдения SRP.
/// </summary>
internal static class DocxBodyBuilder
{
    /// <summary>
    /// Создаёт параграф заголовка документа.
    /// </summary>
    public static Paragraph CreateTitleParagraph(string title, DocxProfile profile)
    {
        return new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            DocxRunHelpers.CreateRun(title, profile, profile.TitleFontSizePt, true));
    }

    /// <summary>
    /// Создаёт параграф «Содержание».
    /// </summary>
    public static Paragraph CreateTocTitle(DocxProfile profile)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { After = "120" },
                new Justification { Val = JustificationValues.Center }),
            DocxRunHelpers.CreateRun("Содержание", profile, profile.FontSizePt, false));
    }

    /// <summary>
    /// Создаёт параграф с TOC-полем (Word автоматически сгенерирует содержание).
    /// </summary>
    public static Paragraph CreateTocField(DocxProfile profile)
    {
        var p = new Paragraph();
        var run = p.AppendChild(new Run());
        run.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
        run.Append(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve });
        run.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
        run.Append(new FieldChar { FieldCharType = FieldCharValues.End });
        DocxRunHelpers.SetRunFont(run, profile, profile.FontSizePt, false);
        return p;
    }

    /// <summary>
    /// Распознаёт таблицу, требующую monospace-шрифта (RACI / JSA),
    /// по содержимому последнего заголовка.
    /// </summary>
    public static bool IsMonospaceTable(Markdig.Extensions.Tables.Table table, string? lastHeadingText, DocxProfile profile)
    {
        if (string.IsNullOrWhiteSpace(lastHeadingText))
        {
            return false;
        }

        var text = lastHeadingText;
        if (profile.RenderRaciAsMonospace && text.Contains("RACI", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (profile.RenderJsaAsMonospace &&
            (text.Contains("JSA", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("Матрица рисков", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Применяет моноширинный шрифт ко всем Run-ам в таблице.
    /// </summary>
    public static void ApplyMonospaceToTable(DocumentFormat.OpenXml.Wordprocessing.Table table, DocxProfile profile)
    {
        foreach (var run in table.Descendants<Run>())
        {
            DocxRunHelpers.SetRunFont(run, profile, 8, false, monospace: true);
        }
    }

    /// <summary>
    /// Извлекает plain-text из заголовка Markdig.
    /// </summary>
    public static string GetHeadingText(HeadingBlock heading)
    {
        if (heading.Inline is null)
        {
            return string.Empty;
        }

        return string.Concat(heading.Inline.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
    }

    /// <summary>
    /// Вставляет разрыв страницы перед заголовками «Лист регистрации» и каждым «Приложение N».
    /// Эталон: convert_from_scratch.py — page_break_before.
    /// </summary>
    public static void ApplyPageBreaksBeforeStandardSections(Document doc)
    {
        string[] triggers = { "Лист регистрации", "Приложение" };
        var paragraphs = doc.Descendants<Paragraph>().ToList();
        foreach (var p in paragraphs)
        {
            var text = p.InnerText.Trim();
            if (!triggers.Any(t => text.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var pPr = p.GetFirstChild<ParagraphProperties>();
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                p.PrependChild(pPr);
            }

            if (pPr.PageBreakBefore is null)
            {
                pPr.PageBreakBefore = new PageBreakBefore();
            }
        }
    }

    /// <summary>
    /// После каждого разрыва страницы (PageBreakBefore или Break Type="Page")
    /// добавляет отступ сверху к следующему параграфу, чтобы текст
    /// не сливался с верхним колонтитулом.
    /// </summary>
    public static void ApplySpacingAfterPageBreaks(Document doc)
    {
        var paragraphs = doc.Descendants<Paragraph>().ToList();
        bool afterPageBreak = false;

        foreach (var p in paragraphs)
        {
            var pPr = p.GetFirstChild<ParagraphProperties>();

            var hasPageBreak = p.Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page)
                || (pPr?.PageBreakBefore is not null);

            if (afterPageBreak && pPr is not null)
            {
                var spacing = pPr.GetFirstChild<SpacingBetweenLines>();
                if (spacing is null)
                {
                    spacing = new SpacingBetweenLines();
                    pPr.AppendChild(spacing);
                }

                if (string.IsNullOrWhiteSpace(spacing.Before))
                {
                    spacing.Before = "240"; // 12 pt ≈ 240 twips
                }

                afterPageBreak = false;
            }

            if (hasPageBreak)
            {
                afterPageBreak = true;
            }
        }
    }

    /// <summary>
    /// Убирает <c>[[wikilinks]]</c> из markdown: оставляет только внутренний текст.
    /// </summary>
    public static string StripWikilinks(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        return System.Text.RegularExpressions.Regex.Replace(
            markdown,
            @"\[\[([^\]]+?)\]\]",
            "$1");
    }
}
