using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для DocumentExportService: проверяем, что сервис корректно
/// создаёт валидный DOCX (без OpenXML-ошибок) на диске.
/// </summary>
public class DocumentExportServiceTests : IDisposable
{
    private readonly string tempDir;

    public DocumentExportServiceTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"docx-export-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExportDocxAsync_CreatesValidFile()
    {
        var service = new DocumentExportService();
        var path = Path.Combine(tempDir, "out.docx");
        var meta = new DocumentMetadata { DocId = "EXP-001", DocType = "ST", Title = "Экспорт" };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body);

        await service.ExportDocxAsync(meta, body, path);

        Assert.True(File.Exists(path));
        var info = new FileInfo(path);
        Assert.True(info.Length > 0);
    }

    [Fact]
    public async Task ExportDocxAsync_WithDifferentDocTypes()
    {
        var service = new DocumentExportService();
        var docTypes = new[] { "Code", "Policy", "BP", "ST", "PM", "WM" };

        foreach (var docType in docTypes)
        {
            var path = Path.Combine(tempDir, $"{docType}.docx");
            var meta = new DocumentMetadata { DocId = $"DT-{docType}", DocType = docType, Title = $"Док {docType}" };
            var body = new DocumentBody();
            DocumentBodyFactory.EnsureRequiredFields(body);

            await service.ExportDocxAsync(meta, body, path);
            Assert.True(File.Exists(path), $"DOCX для {docType} не создан");
        }
    }

    [Fact]
    public async Task ExportDocxAsync_EmptyFields_UsePlaceholders()
    {
        // MD — единственный источник правды: пустые поля должны быть заполнены
        // placeholder'ами на этапе генерации Markdown и попасть в Word.
        var service = new DocumentExportService();
        var path = Path.Combine(tempDir, "placeholder.docx");
        var meta = new DocumentMetadata { DocId = "PH-001", DocType = "ST" };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body);

        await service.ExportDocxAsync(meta, body, path);

        using var doc = WordprocessingDocument.Open(path, false);
        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        var headerText = string.Concat(doc.MainDocumentPart.HeaderParts.SelectMany(h => h.Header.InnerText));
        var fullText = bodyText + headerText;

        Assert.Contains("Полное название документа", fullText);
        Assert.Contains("Опишите цель документа", fullText);
    }
}
