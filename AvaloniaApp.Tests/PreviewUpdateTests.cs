using System.Threading.Tasks;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионный тест на баг #BUG-MD-002:
/// «MD Preview не обновляется при печати в полях формы — только при клике на кнопку».
/// Причина: Avalonia TextBox по умолчанию обновляет binding только на LostFocus.
/// Решение: RefreshPreview вызывается явно при смене вкладки на MD Preview и при экспорте.
/// </summary>
public class PreviewUpdateTests
{
    [Fact]
    public async Task SwitchingToPreviewTab_TriggersPreviewRefresh()
    {
        // Arrange: VM с документом, у которого есть Terms/ChangeLog.
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        vm.DocumentBody!.Terms.Add(new AvaloniaApp.Models.TermPair
        {
            Position = 0,
            Term = "X",
            Definition = "Y",
        });
        vm.DocumentBody!.ChangeLog.Add(new AvaloniaApp.Models.ChangeLogRow
        {
            Date = "01.01.2026",
            Revision = "1",
            Author = "A",
            Reviewer = "B",
            Description = "Init",
        });

        // Act: переключаемся на вкладку "MD Preview" (index = 3: Документ, Метаданные, База данных, MD Preview)
        vm.ActiveTabIndex = 3;

        // Assert: MarkdownPreview содержит свежие данные
        Assert.Contains("**X**", vm.MarkdownPreview);
        Assert.Contains("Init", vm.MarkdownPreview);
    }

    [Fact]
    public async Task SwitchingToDocumentTab_DoesNotChangePreview()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        var initialPreview = vm.MarkdownPreview;
        vm.ActiveTabIndex = 0;

        Assert.Equal(initialPreview, vm.MarkdownPreview);
    }

    [Fact]
    public async Task RefreshPreview_CapturesCurrentDocumentBodyState()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        vm.DocumentBody!.Terms.Add(new AvaloniaApp.Models.TermPair
        {
            Position = 0,
            Term = "Term1",
            Definition = "Def1",
        });

        vm.RefreshPreviewCommand.Execute(null);

        Assert.Contains("**Term1**", vm.MarkdownPreview);
        Assert.Contains("Def1", vm.MarkdownPreview);
    }
}
