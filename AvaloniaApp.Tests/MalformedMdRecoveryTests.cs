using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты: «битый» или «чужой» MD-файл должен нормально открываться,
/// а недостающие поля формы автоматически дополняться через EnsureRequiredFields.
/// </summary>
public class MalformedMdRecoveryTests
{
    [Fact]
    public void OpenLegacySampleMd_ParserProducesEmptyDocument()
    {
        // Эмулируем парсинг sample.md (у него нет Terms, нет ChangeLog,
        // нет секций 1.Цель / 2.Область применения — он legacy-формата).
        var samplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "sample.md");
        var sampleText = File.ReadAllText(samplePath);
        var (metadata, bodyMarkdown) = MarkdownService.Parse(sampleText);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        // Парсер не нашёл наших полей — body почти пустой
        Assert.True(string.IsNullOrEmpty(body.Purpose));
        Assert.True(string.IsNullOrEmpty(body.Scope));
        Assert.Empty(body.Terms);
        Assert.Empty(body.ChangeLog);
    }

    [Fact]
    public void EnsureRequiredFields_AfterParsingLegacyMd_AddsStarterRows()
    {
        // Полный путь: sample.md → парсинг → EnsureRequiredFields
        var samplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "sample.md");
        var sampleText = File.ReadAllText(samplePath);
        var (metadata, bodyMarkdown) = MarkdownService.Parse(sampleText);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        DocumentBodyFactory.EnsureRequiredFields(body);

        // Теперь у документа есть стартовые строки для пользователя
        Assert.Single(body.Terms);
        Assert.Single(body.ChangeLog);
        Assert.Empty(body.Terms[0].Term);              // пустой шаблон
        Assert.Empty(body.ChangeLog[0].Description);   // серый hint в UI
    }

    [Fact]
    public void EmptyMd_ParserAndEnsure_ProducesValidDocument()
    {
        var emptyText = string.Empty;
        var (metadata, bodyMarkdown) = MarkdownService.Parse(emptyText);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        DocumentBodyFactory.EnsureRequiredFields(body);

        Assert.NotNull(body);
        Assert.Single(body.Terms);
        Assert.Single(body.ChangeLog);
    }

    [Fact]
    public void OnlyYamlFrontmatter_NoBody_StillRecovers()
    {
        var onlyYaml = "---\ndoc_id: TEST-001\ndoc_type: PM\ntitle: Test\n---\n";
        var (metadata, bodyMarkdown) = MarkdownService.Parse(onlyYaml);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        DocumentBodyFactory.EnsureRequiredFields(body);

        Assert.Equal("TEST-001", metadata.DocId);
        Assert.Equal("PM", metadata.DocType);
        Assert.Single(body.Terms);
        Assert.Single(body.ChangeLog);
    }

    [Fact]
    public void FullValidMd_PreservesAllData_DoesNotOverwrite()
    {
        var fullMd = "---\ndoc_id: VALID-001\ndoc_type: PM\n---\n\n" +
                     "## 1 Цель\n\nTest purpose\n\n" +
                     "## 2 Область применения\n\nTest scope\n\n" +
                     "## 3 Термины и определения\n\n- **SOP** — Standard\n\n" +
                     "## Лист регистрации изменений\n\n" +
                     "| Дата редакции | Номер ред. | Разработал | Проверил | Краткое описание изменений |\n" +
                     "| :--- | :--- | :--- | :--- | :--- |\n" +
                     "| 01.06.2026 | 1 | Иванов | Петров | Init |\n";

        var (metadata, bodyMarkdown) = MarkdownService.Parse(fullMd);
        var body = DocumentBodyParser.Parse(bodyMarkdown);

        // Данные распарсились
        Assert.Contains("Test purpose", body.Purpose);
        Assert.Single(body.Terms);
        Assert.Equal("SOP", body.Terms[0].Term);
        Assert.Single(body.ChangeLog);

        // EnsureRequiredFields НЕ должен перезаписывать
        DocumentBodyFactory.EnsureRequiredFields(body);

        Assert.Single(body.Terms);  // остался один
        Assert.Equal("SOP", body.Terms[0].Term);
        Assert.Single(body.ChangeLog);
        Assert.Equal("Init", body.ChangeLog[0].Description);
    }

    [Fact]
    public async Task OpenFileAsync_WithMalformedFile_DoesNotCrash()
    {
        // Имитируем OpenFileAsync: создаём временный «битый» MD,
        // парсим через DocumentBodyParser, применяем EnsureRequiredFields.
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, "# Random content\n\nNot our format at all.\n");

            var text = await File.ReadAllTextAsync(tempPath);
            var (metadata, bodyMarkdown) = MarkdownService.Parse(text);
            var body = DocumentBodyParser.Parse(bodyMarkdown);

            DocumentBodyFactory.EnsureRequiredFields(body);

            // Body валиден, не null, имеет стартовые поля
            Assert.NotNull(body);
            Assert.Single(body.Terms);
            Assert.Single(body.ChangeLog);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
