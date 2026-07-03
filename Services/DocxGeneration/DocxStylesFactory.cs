using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Фабрика стилей и нумерации для DOCX-документа.
/// Инкапсулирует добавление <c>StyleDefinitionsPart</c> и <c>NumberingDefinitionsPart</c>.
/// Вынесена из <c>DocxGenerator</c> (SRP).
/// </summary>
internal static class DocxStylesFactory
{
    /// <summary>
    /// Добавляет определения нумерации (маркированный и нумерованный списки) в документ.
    /// </summary>
    public static void AddNumberingDefinitions(MainDocumentPart mainPart)
    {
        var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
        var numbering = new Numbering();

        // Bullet: • (U+2022)
        var bulletAbstractNum = new AbstractNum(
            new Level(
                new StartNumberingValue { Val = 1 },
                new NumberingFormat { Val = NumberFormatValues.Bullet },
                new LevelText { Val = "\u2022" },
                new LevelJustification { Val = LevelJustificationValues.Left },
                new PreviousParagraphProperties(
                    new Indentation { Left = "1145", Hanging = "360" }))
            { LevelIndex = 0 })
        { AbstractNumberId = 0 };

        var bulletNum = new NumberingInstance(
            new AbstractNumId { Val = 0 })
        { NumberID = BlockConverterConstants.BulletNumId };

        // Decimal: 1., 2., ...
        var decimalAbstractNum = new AbstractNum(
            new Level(
                new StartNumberingValue { Val = 1 },
                new NumberingFormat { Val = NumberFormatValues.Decimal },
                new LevelText { Val = "%1." },
                new LevelJustification { Val = LevelJustificationValues.Left },
                new PreviousParagraphProperties(
                    new Indentation { Left = "1145", Hanging = "360" }))
            { LevelIndex = 0 })
        { AbstractNumberId = 1 };

        var decimalNum = new NumberingInstance(
            new AbstractNumId { Val = 1 })
        { NumberID = BlockConverterConstants.DecimalNumId };

        numbering.Append(bulletAbstractNum);
        numbering.Append(decimalAbstractNum);
        numbering.Append(bulletNum);
        numbering.Append(decimalNum);

        numberingPart.Numbering = numbering;
        numberingPart.Numbering.Save();
    }

    /// <summary>
    /// Добавляет стандартные стили (Normal, Heading 1–5, TOC 1–3) в документ.
    /// </summary>
    public static void ApplyDefaultStyles(MainDocumentPart mainPart, DocxProfile profile)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();

        // Normal: Roboto, 9pt, чёрный (GOST 6.2.2), межстрочный интервал 1.15 (GOST 6.2.3).
        styles.Append(new Style(
            new StyleName { Val = "Normal" },
            new BasedOn { Val = "Normal" },
            new StyleParagraphProperties(
                new SpacingBetweenLines { Line = "276", LineRule = LineSpacingRuleValues.Auto }),
            new StyleRunProperties(
                new RunFonts
                {
                    Ascii = profile.FontName,
                    HighAnsi = profile.FontName,
                    EastAsia = profile.FontName,
                    ComplexScript = profile.FontName,
                },
                new Color { Val = "000000" },
                new FontSize { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() },
                new FontSizeComplexScript { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() }))
        { Type = StyleValues.Paragraph, StyleId = "Normal", Default = true });

        for (int i = 1; i <= 5; i++)
        {
            styles.Append(new Style(
                new StyleName { Val = $"Heading {i}" },
                new BasedOn { Val = "Normal" },
                new StyleParagraphProperties(
                    new SpacingBetweenLines { After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                    new Indentation { Left = ((i - 1) * 425).ToString() },
                    new OutlineLevel { Val = i - 1 }),
                new StyleRunProperties(
                    new RunFonts
                    {
                        Ascii = profile.FontName,
                        HighAnsi = profile.FontName,
                        EastAsia = profile.FontName,
                        ComplexScript = profile.FontName,
                    },
                    new Color { Val = "000000" },
                    new FontSize { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() },
                    new FontSizeComplexScript { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() }))
            { Type = StyleValues.Paragraph, StyleId = $"Heading{i}" });
        }

        // TOC 1–3: не жирные, чтобы содержание в Word было обычным текстом.
        for (int i = 1; i <= 3; i++)
        {
            var tocIndent = (i - 1) * 425; // TOC1=0, TOC2=425, TOC3=850
            styles.Append(new Style(
                new StyleName { Val = $"toc {i}" },
                new BasedOn { Val = "Normal" },
                new StyleParagraphProperties(
                    new SpacingBetweenLines { After = "60", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                    new Indentation { Left = tocIndent.ToString() }),
                new StyleRunProperties(
                    new RunFonts
                    {
                        Ascii = profile.FontName,
                        HighAnsi = profile.FontName,
                        EastAsia = profile.FontName,
                        ComplexScript = profile.FontName,
                    },
                    new Color { Val = "000000" },
                    new FontSize { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() },
                    new FontSizeComplexScript { Val = DocxRunHelpers.PtToHalfPoints(profile.FontSizePt).ToString() }))
            { Type = StyleValues.Paragraph, StyleId = $"TOC{i}" });
        }

        stylesPart.Styles = styles;
        stylesPart.Styles.Save();
    }
}
