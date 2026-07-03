using System.IO;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты соответствия таблиц эталону convert_from_scratch.py:
/// «Утвержден» (титул) и «Лист регистрации изменений».
/// </summary>
public class DocxReferenceLayoutTests : IDisposable
{
    private readonly string tempDir;
    private readonly DocumentExportService exportService;

    public DocxReferenceLayoutTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"docx-ref-{System.Guid.NewGuid():N}");
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
    public async Task ApprovalTable_HasCorrectWidths()
    {
        // Эталон convert_from_scratch.py: ширины колонок «УТВЕРЖДЕНО»-таблицы: 10.58 cm и 7.53 cm.
        // 1 cm = 567 dxa.
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);

        // Первая таблица в body — это ApprovalTable (титульный лист).
        var body = doc.MainDocumentPart!.Document.Body!;
        var firstTable = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().First();

        var columns = firstTable.Descendants<DocumentFormat.OpenXml.Wordprocessing.GridColumn>().ToList();
        Assert.Equal(2, columns.Count);

        var expectedLeft = (int)(10.58 * 567);
        var expectedRight = (int)(7.53 * 567);
        Assert.Equal(expectedLeft, int.Parse(columns[0].Width!.Value!));
        Assert.Equal(expectedRight, int.Parse(columns[1].Width!.Value!));
    }

    [Fact]
    public async Task ApprovalTable_IsTableWithTwoCells()
    {
        // Блок утверждения должен быть таблицей 2×1 (две ячейки, одна строка).
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var firstTable = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().First();

        var rows = firstTable.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();
        Assert.Single(rows);

        var cells = rows[0].Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>().ToList();
        Assert.Equal(2, cells.Count);
    }

    [Fact]
    public async Task ApprovalTable_ContainsUтверждено_AndApprover()
    {
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var firstTable = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().First();
        var tableText = string.Concat(firstTable.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));

        Assert.Contains("УТВЕРЖДЕНО", tableText);
        Assert.Contains("Иванов И.И.", tableText);
        Assert.Contains("Директор по качеству", tableText);
        Assert.Contains("PR-2026-001", tableText);
        Assert.Contains("01.01.2026", tableText);
    }

    [Fact]
    public async Task ChangeLogTable_Has5ColumnsWithReferenceWidths()
    {
        // Таблица ЛРИ: Дата ред. | Номер ред. | Разработал | Проверил | Краткое описание
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        // Находим таблицу, у которой в первой ячейке «Дата редакции» (это LRI).
        var lri = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>()
            .FirstOrDefault(t =>
            {
                var firstCellText = t.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                    .FirstOrDefault()?.Text ?? string.Empty;
                return firstCellText.Contains("Дата ред.");
            });

        Assert.NotNull(lri);

        var columns = lri!.Descendants<DocumentFormat.OpenXml.Wordprocessing.GridColumn>().ToList();
        Assert.Equal(5, columns.Count);
    }

    [Fact]
    public async Task ChangeLogTable_HasCorrectRowHeights()
    {
        // Эталон: header row = 0.45 cm, data row = 0.65 cm.
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var lri = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>()
            .First(t => (t.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().FirstOrDefault()?.Text ?? string.Empty)
                .Contains("Дата ред."));

        var rows = lri.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();
        Assert.True(rows.Count >= 2);

        var headerHeight = rows[0].TableRowProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRowHeight>().FirstOrDefault()?.Val?.Value;
        var dataHeight = rows[1].TableRowProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRowHeight>().FirstOrDefault()?.Val?.Value;

        Assert.Equal((uint)((int)(0.45 * 567)), headerHeight);
        Assert.Equal((uint)((int)(0.65 * 567)), dataHeight);
    }

    [Fact]
    public async Task PageBreak_BeforeChangeLogSection()
    {
        // Эталон convert_from_scratch.py: page_break_before для «7 Лист регистрации».
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var lriHeading = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .FirstOrDefault(p => p.InnerText.Contains("Лист регистрации"));

        Assert.NotNull(lriHeading);
        var pPr = lriHeading!.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties>();
        Assert.NotNull(pPr?.PageBreakBefore);
    }

    [Fact]
    public async Task Toc_UsesWordField()
    {
        // Эталон: Содержание формируется через поле TOC \o "1-3" \h \z \u.
        var path = await GenerateAsync();

        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        // Содержание: параграф «Содержание» + параграф с FieldCode «TOC»
        var tocField = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldCode>()
            .FirstOrDefault(f => f.Text != null && f.Text.Contains("TOC"));

        Assert.NotNull(tocField);
        Assert.Contains("1-3", tocField!.Text!);
    }

    private async Task<string> GenerateAsync()
    {
        var meta = new DocumentMetadata
        {
            DocId = "REF-001",
            DocType = "ST",
            Title = "Тестовый стандарт",
            Revision = "0",
            Owner = "Подразделение",
            Approver = "Иванов И.И.",
            ApproverRole = "Директор по качеству",
            EffectiveDate = "01.01.2026",
            Protocol = "PR-2026-001",
        };
        var body = new DocumentBody { Purpose = "Тест", Scope = "Все подразделения" };
        DocumentBodyFactory.EnsureRequiredFields(body);
        body.Terms.Add(new TermPair { Position = 0, Term = "Термин", Definition = "Определение" });
        body.ChangeLog.Add(new ChangeLogRow
        {
            Position = 0,
            Date = "01.01.2026",
            Revision = "0",
            Author = "Иванов",
            Reviewer = "Петров",
            Description = "Первая редакция",
        });

        var path = Path.Combine(tempDir, "reference.docx");
        await exportService.ExportDocxAsync(meta, body, path);
        return path;
    }
}
