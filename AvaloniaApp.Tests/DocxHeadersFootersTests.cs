using System.IO;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты соответствия колонтитулов документа нормативке ГОСТ 6.1.1 / 6.1.2 / 6.1.7 / 6.2.2 / 6.2.3.
/// </summary>
public class DocxHeadersFootersTests : IDisposable
{
    private readonly string tempDir;
    private readonly DocumentExportService exportService;

    public DocxHeadersFootersTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"docx-hf-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        exportService = new DocumentExportService();
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Header_ContainsRevizionNotVersion()
    {
        // ГОСТ 6.1.1.F: ячейка «Номер ред.» должна содержать Revision (например «0»).
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts
            .SelectMany(hp => hp.Header.InnerText));

        Assert.Contains("Номер ред.:", headerText);
        Assert.Contains("0", headerText);
    }

    [Fact]
    public async Task Footer_ContainsUncontrolledCopyWarning()
    {
        // ГОСТ 6.1.2: нижний колонтитул должен содержать предупреждение
        // о неконтролируемости печатных копий.
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var footerText = string.Concat(doc.MainDocumentPart!.FooterParts
            .SelectMany(fp => fp.Footer.InnerText));

        Assert.Contains("НЕКОНТРОЛИРУЕМЫМИ", footerText);
    }

    [Fact]
    public async Task Header_ContainsAllRequiredRekvizity()
    {
        // ГОСТ 6.1.1.A-G: 7 обязательных реквизитов в шапке.
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts
            .SelectMany(hp => hp.Header.InnerText));

        Assert.Contains("Наименование документа:", headerText);     // A
        Assert.Contains("Номер документа:", headerText);            // B
        Assert.Contains("Владелец документа:", headerText);   // C
        Assert.Contains("Утвердил:", headerText);         // D
        Assert.Contains("Дата ред.:", headerText);                  // E
        Assert.Contains("Номер ред.:", headerText);                 // F
        Assert.Contains("Стр:", headerText);                        // G (с PAGE полем)
        Assert.Contains(" из ", headerText);                        // G (между PAGE и NUMPAGES)
    }

    [Fact]
    public async Task Header_ContainsNewOwnerAndApproverLabels()
    {
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts
            .SelectMany(hp => hp.Header.InnerText));

        Assert.Contains("Владелец документа:", headerText);
        Assert.Contains("Утвердил:", headerText);
    }

    [Fact]
    public async Task Header_RevisionDate_UsesLastChangeLogDate()
    {
        // Последняя (по Position) строка ЛРИ должна определять дату ред. в шапке.
        var meta = new DocumentMetadata
        {
            DocId = "TEST-001",
            DocType = "ST",
            Title = "Тест документа",
            Revision = "2",
            DepartmentName = "Подразделение",
            Owner = "Подразделение",
            Approver = "Иванов И.И.",
            EffectiveDate = "01.01.2026",
        };
        var body = new DocumentBody { Purpose = "Тест" };
        DocumentBodyFactory.EnsureRequiredFields(body);
        body.ChangeLog.Clear();
        body.ChangeLog.Add(new ChangeLogRow { Position = 0, Date = "10.01.2026", Revision = "0", Reviewer = "B", Description = "Первая" });
        body.ChangeLog.Add(new ChangeLogRow { Position = 1, Date = "15.03.2026", Revision = "1", Reviewer = "B", Description = "Вторая" });
        body.ChangeLog.Add(new ChangeLogRow { Position = 2, Date = "25.06.2026", Revision = "2", Reviewer = "B", Description = "Третья" });

        var path = Path.Combine(tempDir, "test_rev_date.docx");
        await exportService.ExportDocxAsync(meta, body, path);

        using var doc = WordprocessingDocument.Open(path, false);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts
            .SelectMany(hp => hp.Header.InnerText));

        Assert.Contains("25.06.2026", headerText);
    }

    [Fact]
    public async Task WordDocument_DoesNotContainStatus()
    {
        // Статус — только в YAML/MD, не должен попадать в DOCX.
        var path = await GenerateAsync("0", title: "Документ для проверки");

        using var doc = WordprocessingDocument.Open(path, false);
        var bodyText = string.Concat(doc.MainDocumentPart!.Document.Body!.InnerText);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts.SelectMany(hp => hp.Header.InnerText));
        var footerText = string.Concat(doc.MainDocumentPart!.FooterParts.SelectMany(fp => fp.Footer.InnerText));
        var fullText = bodyText + headerText + footerText;

        Assert.DoesNotContain("проект", fullText);
        Assert.DoesNotContain("статус", fullText);
    }

    [Fact]
    public async Task Header_RevisionCell_DoesNotContainVersion()
    {
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var headerText = string.Concat(doc.MainDocumentPart!.HeaderParts
            .SelectMany(hp => hp.Header.InnerText));

        // Ячейка «Номер ред.» содержит только Revision, Version не выводится.
        Assert.Contains("Номер ред.:", headerText);
        Assert.DoesNotContain("1.0", headerText);
    }

    [Fact]
    public async Task NewDocument_DefaultRevision_IsZero()
    {
        // ГОСТ 6.1.7: для нового документа номер первой редакции всегда «0».
        var fresh = new DocumentMetadata();
        Assert.Equal("0", fresh.Revision);
    }

    [Fact]
    public async Task NormalStyle_FontIsRoboto_Black_9pt()
    {
        // ГОСТ 6.2.2: Roboto, 9pt, чёрный.
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var styles = doc.MainDocumentPart!.StyleDefinitionsPart!.Styles!;
        var normal = styles.Elements<DocumentFormat.OpenXml.Wordprocessing.Style>()
            .First(s => s!.StyleId == "Normal");

        var rPr = normal.StyleRunProperties!;
        var fonts = rPr.RunFonts!;
        var color = rPr.Color!;
        var size = rPr.FontSize!;

        Assert.Equal("Roboto", fonts.Ascii!.Value);
        Assert.Equal("000000", color.Val!.Value);
        Assert.Equal("18", size.Val!.Value); // 9pt = 18 half-points
    }

    [Fact]
    public async Task NormalStyle_HasLineSpacing115()
    {
        // ГОСТ 6.2.3: межстрочный интервал 1.15 (= 276 twips).
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, false);
        var styles = doc.MainDocumentPart!.StyleDefinitionsPart!.Styles!;
        var normal = styles.Elements<DocumentFormat.OpenXml.Wordprocessing.Style>()
            .First(s => s!.StyleId == "Normal");

        var spacing = normal.StyleParagraphProperties!.SpacingBetweenLines!;
        Assert.Equal("276", spacing.Line!.Value);
        Assert.Equal(DocumentFormat.OpenXml.Wordprocessing.LineSpacingRuleValues.Auto, spacing.LineRule!.Value);
    }

    [Fact]
    public async Task Document_IsOpenXmlValid()
    {
        // Никаких OpenXML-ошибок после всех правок.
        var path = await GenerateAsync("0");

        using var doc = WordprocessingDocument.Open(path, true);
        var validator = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2013);
        var errors = validator.Validate(doc).ToList();

        Assert.Empty(errors);
    }

    private async Task<string> GenerateAsync(string revision, string title = "Тест документа")
    {
        var meta = new DocumentMetadata
        {
            DocId = "TEST-001",
            DocType = "ST",
            Title = title,
            Revision = revision,
            DepartmentName = "Подразделение",
            Owner = "Подразделение",
            Approver = "Иванов И.И.",
            EffectiveDate = "01.01.2026",
        };
        var body = new DocumentBody { Purpose = "Тест" };
        DocumentBodyFactory.EnsureRequiredFields(body);

        var path = Path.Combine(tempDir, $"test_{revision}.docx");
        await exportService.ExportDocxAsync(meta, body, path);
        return path;
    }
}
