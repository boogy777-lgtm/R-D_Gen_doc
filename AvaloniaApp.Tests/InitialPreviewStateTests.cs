using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты: после инициализации VM и вызова NewDocument,
/// MD Preview должен сразу содержать ВСЕ данные из DocumentBody, включая
/// стартовые коллекции (Terms, ChangeLog) и непустые Sections.
/// </summary>
public class InitialPreviewStateTests
{
    [Fact]
    public async Task AfterInitializeAsync_PreviewContainsStarterCollections()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // После InitializeAsync VM должен иметь body с starter коллекциями
        Assert.NotNull(vm.DocumentBody);
        Assert.Single(vm.DocumentBody!.Terms);
        Assert.Single(vm.DocumentBody!.ChangeLog);

        // И preview должен сразу это отражать
        Assert.NotEmpty(vm.MarkdownPreview);
    }

    [Fact]
    public async Task AfterNewDocument_PreviewContainsAllSections()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // ST шаблон имеет 5 StarterSections
        Assert.NotEmpty(vm.DocumentBody!.Sections);

        // Все они должны появиться в preview как "## 4 ...", "## 5 ..." и т.д.
        Assert.Contains("## 4", vm.MarkdownPreview);
    }

    [Fact]
    public async Task Preview_AlwaysContainsAllSectionHeaders()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Структурные заголовки рендерятся ВСЕГДА, даже для пустых секций
        Assert.Contains("## 1 Цель", vm.MarkdownPreview);
        Assert.Contains("## 2 Область применения", vm.MarkdownPreview);
        Assert.Contains("## 3 Термины и определения", vm.MarkdownPreview);
        Assert.Contains("## Обучение", vm.MarkdownPreview);
        Assert.Contains("## Ссылки", vm.MarkdownPreview);
        Assert.Contains("## Лист регистрации изменений", vm.MarkdownPreview);
    }

    [Fact]
    public async Task Preview_EmptySections_ShowGrayPlaceholder()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Пустые секции не выводят placeholder — только структурные заголовки.
        Assert.DoesNotContain("заполните", vm.MarkdownPreview);
    }

    [Fact]
    public async Task Preview_ContainsChangeLogTable_Immediately()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // ChangeLog стартовая строка имеет заполненные Date/Revision/Author,
        // поэтому preview должен содержать заголовок и таблицу
        Assert.Contains("## Лист регистрации изменений", vm.MarkdownPreview);
        Assert.Contains("| Дата ред. |", vm.MarkdownPreview);
    }
}
