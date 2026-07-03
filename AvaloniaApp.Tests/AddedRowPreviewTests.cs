using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионный тест: добавление строки в Repeating*Field должно СРАЗУ
/// отражаться в MD preview — без необходимости переключать вкладки.
/// </summary>
public class AddedRowPreviewTests
{
    [Fact]
    public async Task AddingSection_AppearsInPreviewImmediately()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        var initialSectionsCount = vm.DocumentBody!.Sections.Count;
        var initialPreview = vm.MarkdownPreview;

        // Имитируем добавление новой секции через TreeField
        vm.DocumentBody!.Sections.Add(new BodySection
        {
            Position = vm.DocumentBody!.Sections.Count,
            Title = "Новая секция",
            Body = string.Empty,
        });

        // RefreshPreview вызывается через FormChanged → vm.NotifyBodyChanged()
        vm.RefreshPreviewCommand.Execute(null);

        // В preview должно появиться "## 5 Новая секция" (или выше, в зависимости от DocType)
        Assert.Contains("Новая секция", vm.MarkdownPreview);
    }

    [Fact]
    public async Task AddingTermPair_AppearsInPreviewImmediately()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Имитируем добавление термина через CollectionField
        vm.DocumentBody!.Terms.Add(new TermPair
        {
            Position = vm.DocumentBody!.Terms.Count,
            Term = "ABC",
            Definition = "Азбука",
        });

        vm.RefreshPreviewCommand.Execute(null);

        Assert.Contains("**ABC**", vm.MarkdownPreview);
        Assert.Contains("Азбука", vm.MarkdownPreview);
    }

    [Fact]
    public async Task AddingRelatedDocument_AppearsInPreviewImmediately()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        vm.DocumentBody!.RelatedDocuments.Add(new StringItem { Value = "[[Новый-документ]]" });
        vm.RefreshPreviewCommand.Execute(null);

        Assert.Contains("[[Новый-документ]]", vm.MarkdownPreview);
    }

    [Fact]
    public async Task AddingChangeLogRow_AppearsInPreviewImmediately()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Служебные колонки ЛРИ теперь хранятся в самой строке (Date/Revision).
        vm.Metadata.EffectiveDate = "01.07.2026";
        vm.Metadata.Revision = "2.0";

        vm.DocumentBody!.ChangeLog.Add(new ChangeLogRow
        {
            Position = vm.DocumentBody!.ChangeLog.Count,
            Date = "01.07.2026",
            Revision = "2",
            Description = "Новое изменение",
            Reviewer = "Сидоров",
        });

        vm.RefreshPreviewCommand.Execute(null);

        Assert.Contains("01.07.2026", vm.MarkdownPreview);
        Assert.Contains("| 2 |", vm.MarkdownPreview);
        Assert.Contains("Новое изменение", vm.MarkdownPreview);
    }

    [Fact]
    public async Task RemovedTerm_DisappearsFromPreview()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Добавим термин
        var term = new TermPair { Position = 0, Term = "ВРЕМЕННЫЙ", Definition = "Удалить" };
        vm.DocumentBody!.Terms.Add(term);
        vm.RefreshPreviewCommand.Execute(null);
        Assert.Contains("**ВРЕМЕННЫЙ**", vm.MarkdownPreview);

        // Удалим
        vm.DocumentBody!.Terms.Remove(term);
        vm.RefreshPreviewCommand.Execute(null);
        Assert.DoesNotContain("**ВРЕМЕННЫЙ**", vm.MarkdownPreview);
    }

    [Fact]
    public async Task RemovingSection_DisappearsFromPreview()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        var section = new BodySection { Position = 99, Title = "Удаляемая", Body = string.Empty };
        vm.DocumentBody!.Sections.Add(section);
        vm.RefreshPreviewCommand.Execute(null);
        Assert.Contains("Удаляемая", vm.MarkdownPreview);

        vm.DocumentBody!.Sections.Remove(section);
        vm.RefreshPreviewCommand.Execute(null);
        Assert.DoesNotContain("Удаляемая", vm.MarkdownPreview);
    }
}
