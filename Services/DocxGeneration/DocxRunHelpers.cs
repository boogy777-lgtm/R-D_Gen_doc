using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Низкоуровневые helpers для создания OpenXML <see cref="Run"/> / <see cref="RunProperties"/>
/// с правильным порядком элементов (Color перед sz/szCs по схеме OpenXML).
/// Вынесены из <c>DocxGenerator</c> для соблюдения SRP.
/// </summary>
internal static class DocxRunHelpers
{
    /// <summary>
    /// Создаёт <see cref="Run"/> с заданным текстом и шрифтом.
    /// </summary>
    public static Run CreateRun(string text, DocxProfile profile, double sizePt, bool bold, bool monospace = false)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        SetRunFont(run, profile, sizePt, bold, monospace);
        return run;
    }

    /// <summary>
    /// Применяет шрифт/размер/жирность к <see cref="Run"/>, сохраняя корректный
    /// порядок элементов в <see cref="RunProperties"/> (Color перед sz/szCs).
    /// </summary>
    public static void SetRunFont(Run run, DocxProfile profile, double sizePt, bool bold, bool monospace = false)
    {
        var rPr = run.GetFirstChild<RunProperties>() ?? new RunProperties();
        EnsureFontInRunProperties(rPr, profile, sizePt, monospace);

        // GOST 6.2.2: цвет шрифта — чёрный. Color идёт ПЕРЕД sz/szCs.
        if (rPr.Color is null)
        {
            var sz = rPr.FontSize;
            var szCs = rPr.FontSizeComplexScript;
            sz?.Remove();
            szCs?.Remove();

            rPr.AppendChild(new Color { Val = "000000" });

            if (sz != null)
            {
                rPr.AppendChild(sz);
            }

            if (szCs != null)
            {
                rPr.AppendChild(szCs);
            }
        }

        rPr.Bold = bold ? new Bold() : null;

        if (run.GetFirstChild<RunProperties>() is null)
        {
            run.PrependChild(rPr);
        }
    }

    /// <summary>
    /// Создаёт <see cref="RunProperties"/> с шрифтом/размером/цветом по умолчанию.
    /// </summary>
    public static RunProperties CreateDefaultRunProperties(DocxProfile profile, double sizePt, bool monospace)
    {
        var rPr = new RunProperties();
        EnsureFontInRunProperties(rPr, profile, sizePt, monospace);

        // Color перед sz/szCs.
        var sz = rPr.FontSize;
        var szCs = rPr.FontSizeComplexScript;
        sz?.Remove();
        szCs?.Remove();

        rPr.AppendChild(new Color { Val = "000000" });

        if (sz != null)
        {
            rPr.AppendChild(sz);
        }

        if (szCs != null)
        {
            rPr.AppendChild(szCs);
        }

        return rPr;
    }

    /// <summary>
    /// Гарантирует наличие RunFonts/FontSize/FontSizeComplexScript в <paramref name="rPr"/>.
    /// </summary>
    public static void EnsureFontInRunProperties(RunProperties rPr, DocxProfile profile, double sizePt, bool monospace)
    {
        string font = monospace ? DocxPageLayout.MonospaceFont : profile.FontName;
        string fallback = monospace ? profile.FontName : "Roboto";

        if (rPr.RunFonts is null)
        {
            rPr.RunFonts = new RunFonts
            {
                Ascii = font,
                HighAnsi = font,
                EastAsia = font,
                ComplexScript = fallback,
            };
        }

        if (rPr.FontSize is null)
        {
            rPr.FontSize = new FontSize { Val = PtToHalfPoints(sizePt).ToString() };
        }

        if (rPr.FontSizeComplexScript is null)
        {
            rPr.FontSizeComplexScript = new FontSizeComplexScript { Val = PtToHalfPoints(sizePt).ToString() };
        }
    }

    /// <summary>
    /// Создаёт <see cref="Paragraph"/> с multiline-текстом: строки разделены <see cref="Break"/>.
    /// В OpenXML <c>\n</c> не создаёт визуальный разрыв строки.
    /// </summary>
    public static Paragraph CreateMultilineParagraph(string text, bool bold, DocxProfile profile, ParagraphProperties? props = null)
    {
        var p = props != null ? new Paragraph(props) : new Paragraph();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                p.Append(new Run(new Break()));
            }

            if (!string.IsNullOrEmpty(lines[i]))
            {
                p.Append(CreateRun(lines[i], profile, profile.FontSizePt, bold));
            }
        }

        return p;
    }

    /// <summary>Переводит пункты в полу-пункты (OpenXML: FontSize Val в half-points).</summary>
    public static int PtToHalfPoints(double pt) => (int)(pt * 2);

    /// <summary>
    /// Создаёт <see cref="Paragraph"/> из <see cref="CodeBlock"/> с моноширинным
    /// (или обычным) шрифтом и серой заливкой фона.
    /// </summary>
    public static Paragraph ConvertCodeBlock(CodeBlock code, DocxProfile profile, bool monospace)
    {
        var p = new Paragraph(
            new ParagraphProperties(
                new Shading { Val = ShadingPatternValues.Clear, Fill = "F0F0F0" },
                new SpacingBetweenLines { After = "120" }));
        var text = string.Join("\n", code.Lines.Lines.Select(l => l.ToString()));
        var run = p.AppendChild(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        SetRunFont(run, profile, profile.FontSizePt, false, monospace);
        return p;
    }
}
