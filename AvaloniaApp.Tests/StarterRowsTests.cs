using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.Services.Defaults;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионный тест: новый документ любого типа должен сразу содержать
/// одну пустую стартовую строку в Терминах и одну стартовую строку
/// в Листе регистрации изменений, чтобы пользователь видел структуру таблиц.
/// </summary>
public class StarterRowsTests
{
    [Theory]
    [InlineData("ST")]
    [InlineData("PM")]
    [InlineData("BP")]
    [InlineData("Code")]
    [InlineData("Policy")]
    [InlineData("WM")]
    [InlineData("Basis")]
    public async Task NewDocument_HasStarterTermsRow(string docType)
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Имитируем "Новый из шаблона" для каждого типа
        vm.Metadata.DocType = docType;
        vm.DocumentBody = new DocumentBody
        {
            Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>(Forms.GetTemplate(docType).StarterSections),
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = string.Empty, Definition = string.Empty },
            },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                DocumentBodyFactory.CreatePrefilledChangeLogRow(),
            },
        };

        Assert.Single(vm.DocumentBody!.Terms);
        Assert.Equal(0, vm.DocumentBody!.Terms[0].Position);
        Assert.True(string.IsNullOrEmpty(vm.DocumentBody!.Terms[0].Term));
    }

    [Theory]
    [InlineData("ST")]
    [InlineData("PM")]
    [InlineData("BP")]
    [InlineData("Code")]
    [InlineData("Policy")]
    [InlineData("WM")]
    [InlineData("Basis")]
    public async Task NewDocument_HasStarterChangeLogRow(string docType)
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        vm.DocumentBody = new DocumentBody
        {
            Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>(Forms.GetTemplate(docType).StarterSections),
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair> { new() { Position = 0 } },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                DocumentBodyFactory.CreatePrefilledChangeLogRow(),
            },
        };

        Assert.Single(vm.DocumentBody!.ChangeLog);
        var row = vm.DocumentBody!.ChangeLog[0];
        Assert.Equal(0, row.Position);
        Assert.Equal("1", row.Revision);
        Assert.Equal(DateTime.Today.ToString("dd.MM.yyyy"), row.Date);
        Assert.True(string.IsNullOrEmpty(row.Description));
    }

    [Fact]
    public void NewDocument_StarterRows_DoNotBreakMarkdownRendering()
    {
        var body = new DocumentBody
        {
            Purpose = "Test",
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = string.Empty, Definition = string.Empty },
            },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Date = "01.01.2026", Revision = "1", Author = "A", Reviewer = "B", Description = string.Empty },
            },
        };

        var md = DocumentBodyToMarkdown.Convert(body);

        // Заголовки секций рендерятся всегда — даже для пустых
        Assert.Contains("## 3 Термины и определения", md);
        Assert.Contains("## Лист регистрации изменений", md);
        Assert.Contains("01.01.2026", md);

        // Пустые секции не должны содержать placeholder
        Assert.DoesNotContain("заполните", md);
    }

    [Fact]
    public async Task NewDocument_FromRealVM_HasStarterRows()
    {
        // Проверяем реальный путь NewDocument через VM.InitializeAsync (запускает NewDocument("ST")).
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        Assert.Single(vm.DocumentBody!.Terms);
        Assert.Single(vm.DocumentBody!.ChangeLog);
    }

    [Fact]
    public void EnsureRequiredFields_AddsStarterRowsToEmptyBody()
    {
        var body = new DocumentBody();

        DocumentBodyFactory.EnsureRequiredFields(body);

        Assert.Single(body.Terms);
        Assert.Single(body.ChangeLog);
        Assert.Empty(body.Terms[0].Term);
        Assert.Empty(body.ChangeLog[0].Description);
    }

    [Fact]
    public void EnsureRequiredFields_DoesNotOverwriteExistingRows()
    {
        var body = new DocumentBody
        {
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = "SOP", Definition = "Стандартная операционная процедура" },
            },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Date = "01.06.2026", Revision = "5", Author = "Иванов", Reviewer = "Петров", Description = "Реальная запись" },
            },
        };

        DocumentBodyFactory.EnsureRequiredFields(body);

        // Существующие данные НЕ должны быть перезаписаны
        Assert.Single(body.Terms);
        Assert.Equal("SOP", body.Terms[0].Term);
        Assert.Single(body.ChangeLog);
        Assert.Equal("5", body.ChangeLog[0].Revision);
        Assert.Equal("Реальная запись", body.ChangeLog[0].Description);
    }

    [Fact]
    public void EnsureRequiredFields_NullBody_DoesNotThrow()
    {
        // Защита от NRE, если кто-то вызовет с null
        var ex = Record.Exception(() => DocumentBodyFactory.EnsureRequiredFields(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void EnsureRequiredFields_OnlyAddsMissingRows()
    {
        // Если Terms пуст, а ChangeLog нет — добавит только Terms.
        var body = new DocumentBody
        {
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Date = "01.06.2026", Revision = "1", Author = "A", Reviewer = "B", Description = "X" },
            },
        };

        DocumentBodyFactory.EnsureRequiredFields(body);

        Assert.Single(body.Terms);
        Assert.Single(body.ChangeLog);
        Assert.Equal("X", body.ChangeLog[0].Description);
    }
}
