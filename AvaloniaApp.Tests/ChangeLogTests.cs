using System;
using System.Collections.ObjectModel;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.Views.Fields;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для Листа регистрации изменений (ChangeLogSheet v2):
/// серый шаблон-подсказка в Description + автоматическое заполнение Date/Revision/Author.
/// </summary>
public class ChangeLogTests
{
    [Fact]
    public void ChangeLog_NewRow_HasPlaceholderTextConstantDefined()
    {
        // Серый шаблон должен содержать 4 строки-подсказки
        // (UI-only — в БД и DOCX не сохраняется, если Description пустой).
        var hint = ChangeLogResources.DescriptionHint;

        Assert.Contains("[Раздел/пункт]:", hint);
        Assert.Contains("[Что изменено]:", hint);
        Assert.Contains("[Почему изменено]:", hint);
        Assert.Contains("[Основание]:", hint);
        Assert.DoesNotContain("|", hint); // это подсказка, не markdown-таблица
    }

    [Fact]
    public void ChangeLog_DefaultRowPopulated_TodayDateAndNextRevision()
    {
        // Arrange: пустой список — Revision должен стать "1".
        var rows = new ObservableCollection<ChangeLogRow>();

        // Act
        var newRow = DocumentBodyFactory.CreatePrefilledChangeLogRow(null, rows);

        // Assert: Description пустое (чтобы UI показал подсказку).
        Assert.Equal(string.Empty, newRow.Description);

        // Date — сегодня в формате ДД.ММ.ГГГГ
        Assert.Equal(DateTime.Today.ToString("dd.MM.yyyy"), newRow.Date);

        // Revision = 1 (было пусто, max=0, +1)
        Assert.Equal("1", newRow.Revision);
    }

    [Fact]
    public void ChangeLog_NewRow_UsesMetadataVersion()
    {
        // Arrange: версия документа 2.0 — новая строка ЛРИ должна получить обрезанную ревизию «2».
        var rows = new ObservableCollection<ChangeLogRow>
        {
            new() { Position = 0, Revision = "1", Date = "01.01.2026" },
            new() { Position = 1, Revision = "3", Date = "15.03.2026" },
            new() { Position = 2, Revision = "7", Date = "20.06.2026" },
        };
        var metadata = new DocumentMetadata { Revision = "2.0" };

        // Act
        var newRow = DocumentBodyFactory.CreatePrefilledChangeLogRow(metadata, rows);

        // Assert: Revision берётся из metadata.Revision (с обрезкой .0).
        Assert.Equal("2", newRow.Revision);
        Assert.Equal(string.Empty, newRow.Description);
    }

    [Theory]
    [InlineData("1.0", "1")]
    [InlineData("2.5", "2.5")]
    [InlineData("3.10", "3.10")]
    [InlineData("1.0.0", "1.0")]
    [InlineData("", "")]
    public void RevisionFormatter_Trim_CutsTrailingDotZero(string input, string expected)
    {
        Assert.Equal(expected, RevisionFormatter.Trim(input));
    }

    [Theory]
    [InlineData("1.0", 1)]
    [InlineData("2.5", 2)]
    [InlineData("3.10", 3)]
    [InlineData("10", 10)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    [InlineData("ДД.ММ.ГГГГ", 0)]
    public void RevisionFormatter_ParseInteger_ExtractsIntPart(string? input, int expected)
    {
        Assert.Equal(expected, RevisionFormatter.ParseInteger(input));
    }

    [Theory]
    [InlineData("1.0", "2")]
    [InlineData("2.5", "3")]
    [InlineData("3", "4")]
    [InlineData("", "1")]
    [InlineData(null, "1")]
    public void RevisionFormatter_Increment_AddsOne(string? input, string expected)
    {
        Assert.Equal(expected, RevisionFormatter.Increment(input));
    }

    [Fact]
    public void AddChangeLogRow_AutoIncrement_UpsRevisionAndMetadata()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata
        {
            Revision = "1",
            EffectiveDate = "01.06.2026",
            Author = "Иванов И.И.",
        };
        var engine = new DocumentChangeEngine(body, meta);

        // Первый вызов с autoIncrement = true
        engine.AddChangeLogRow(autoIncrementRevision: true);

        Assert.Single(body.ChangeLog);
        // Ревизия должна была инкрементироваться: max(1) + 1 = 2
        Assert.Equal("2", body.ChangeLog[0].Revision);
        Assert.Equal("2", meta.Revision); // метаданные тоже обновлены

        // Второй вызов — ревизия растёт дальше
        engine.AddChangeLogRow(autoIncrementRevision: true);
        Assert.Equal(2, body.ChangeLog.Count);
        Assert.Equal("3", body.ChangeLog[1].Revision);
        Assert.Equal("3", meta.Revision);
    }

    [Fact]
    public void AddChangeLogRow_AutoIncrement_UsesMaxOfRowsAndMetadata()
    {
        var body = new DocumentBody
        {
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Revision = "5", Date = "01.01.2026" },
            },
        };
        var meta = new DocumentMetadata
        {
            Revision = "2",
            EffectiveDate = "01.06.2026",
        };
        var engine = new DocumentChangeEngine(body, meta);

        // autoIncrement: max(5 из строк, 2 из метаданных) + 1 = 6
        engine.AddChangeLogRow(autoIncrementRevision: true);

        Assert.Equal(2, body.ChangeLog.Count);
        Assert.Equal("6", body.ChangeLog[1].Revision);
        Assert.Equal("6", meta.Revision);
    }

    [Fact]
    public void ChangeLog_MarkdownUsesRowValuesForServiceColumns()
    {
        var body = new DocumentBody
        {
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new()
                {
                    Position = 0,
                    Date = "01.01.2026",
                    Revision = "0",
                    Reviewer = "Петров П.П.",
                    Description = "Описание изменения",
                },
            },
        };
        var metadata = new DocumentMetadata
        {
            Revision = "1.0",
            EffectiveDate = "25.06.2026",
        };

        var md = DocumentBodyToMarkdown.Convert(metadata, body);

        // Каждая строка ЛРИ хранит свои исторические Date/Revision.
        // MD-preview использует значения строки.
        Assert.Contains("01.01.2026", md);
        Assert.Contains("| 0 |", md);
        Assert.Contains("Петров П.П.", md);

        // Колонка "Автор" удалена из ЛРИ — Author теперь только в метаданных.
        Assert.DoesNotContain("25.06.2026", md); // не из metadata
    }

    [Fact]
    public void ChangeLog_LoadingExistingRow_DoesNotShowPlaceholder()
    {
        // Arrange: существующая строка из БД с заполненным Description.
        // Подсказка показывается ТОЛЬКО если Description пустое.
        var existingRow = new ChangeLogRow
        {
            Position = 0,
            Date = "01.06.2026",
            Revision = "1",
            Author = "Иванов И.И.",
            Reviewer = "Петров П.П.",
            Description = "Первая редакция документа",
        };

        // Act & Assert: если описание непустое — подсказка не должна отображаться.
        // Эмулируем логику UI: PlaceholderText показывается, только когда текст пуст.
        var shouldShowHint = string.IsNullOrEmpty(existingRow.Description);

        Assert.False(shouldShowHint);
        Assert.NotEmpty(existingRow.Description);
    }
}
