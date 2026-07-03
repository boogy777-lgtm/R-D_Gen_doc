using System.IO;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для MarkdownFileService: round-trip Open/Save в реальной файловой системе.
/// </summary>
public class MarkdownFileServiceTests : IDisposable
{
    private readonly string tempDir;

    public MarkdownFileServiceTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"md-file-service-{System.Guid.NewGuid():N}");
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
    public async Task SaveAsync_WritesUtf8Markdown()
    {
        // Arrange
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "test.md");
        var meta = new DocumentMetadata { DocId = "TEST-001", Title = "Тестовый документ" };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body);

        // Act
        await service.SaveAsync(path, meta, body);

        // Assert
        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("doc_id: TEST-001", content);
        Assert.Contains("title: Тестовый документ", content);
    }

    [Fact]
    public async Task OpenAsync_ReturnsParsedMetadata()
    {
        // Arrange: записываем файл вручную
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "parse.md");
        var md = "---\ndoc_id: PARSED-002\ntitle: Парсинг\n---\n\n## 1 Назначение\n\nТекст.\n\n## 3 Термины и определения\n\n- **API** — Интерфейс\n- **REST** — Архитектурный стиль\n";
        await File.WriteAllTextAsync(path, md);

        // Act
        var (metadata, body) = await service.OpenAsync(path);

        // Assert
        Assert.Equal("PARSED-002", metadata.DocId);
        Assert.Equal("Парсинг", metadata.Title);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Terms);
        Assert.Equal("API", body.Terms[0].Term);
        Assert.Equal("Интерфейс", body.Terms[0].Definition);
    }

    [Fact]
    public async Task OpenAsync_GuaranteesStarterFields_ForLegacyFile()
    {
        // Arrange: legacy .md файл без Terms/ChangeLog
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "legacy.md");
        await File.WriteAllTextAsync(path, "---\ndoc_id: LEGACY\ntitle: Старый\n---\n\n## 1 Назначение\n\nТекст.\n");

        // Act
        var (_, body) = await service.OpenAsync(path);

        // Assert: EnsureRequiredFields добавил starter rows
        Assert.NotEmpty(body.Terms);
        Assert.NotEmpty(body.ChangeLog);
    }

    [Fact]
    public async Task SaveAsync_WritesDepartmentKey()
    {
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "department.md");
        var meta = new DocumentMetadata
        {
            DocId = "DEPT-001",
            Title = "Тест",
            DepartmentName = "Управление качеством",
        };
        var body = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(body, meta);

        await service.SaveAsync(path, meta, body);

        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("department: Управление качеством", content);
        Assert.DoesNotContain("domain:", content);
    }

    [Fact]
    public async Task OpenAsync_MigratesLegacyDomain_ToDepartment()
    {
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "legacy-domain.md");
        var md = "---\ndoc_id: LEGACY-DOMAIN\ntitle: Старый\ndomain: QHSE\n---\n\n## 1 Назначение\n\nТекст.\n";
        await File.WriteAllTextAsync(path, md);

        var (metadata, _) = await service.OpenAsync(path);

        Assert.Equal("Бывший домен: QHSE", metadata.DepartmentName);
    }

    [Fact]
    public async Task OpenAsync_IgnoresLegacyBasisCard()
    {
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "legacy-basis.md");
        var md = "---\ndoc_id: LEGACY-BASIS\ntitle: Старый\nbasis_card: BC-2026-001\n---\n\n## 1 Назначение\n\nТекст.\n";
        await File.WriteAllTextAsync(path, md);

        var (metadata, _) = await service.OpenAsync(path);

        Assert.Empty(metadata.Protocol); // basis_card не мигрирует в protocol
    }

    [Fact]
    public async Task SaveThenOpen_RoundTrip_PreservesDocId()
    {
        var service = new MarkdownFileService();
        var path = Path.Combine(tempDir, "roundtrip.md");
        var meta = new DocumentMetadata { DocId = "RT-007", Title = "Round Trip" };
        var body = new DocumentBody { Purpose = "Цель" };
        DocumentBodyFactory.EnsureRequiredFields(body);
        body.Terms[0].Term = "X";
        body.Terms[0].Definition = "Y";

        await service.SaveAsync(path, meta, body);
        var (loadedMeta, loadedBody) = await service.OpenAsync(path);

        Assert.Equal("RT-007", loadedMeta.DocId);
        Assert.Equal("Цель", loadedBody.Purpose);
        Assert.Single(loadedBody.Terms);
        Assert.Equal("X", loadedBody.Terms[0].Term);
        Assert.Equal("Y", loadedBody.Terms[0].Definition);
    }
}
