using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для DatabaseTabViewModel.
/// </summary>
public class DatabaseTabViewModelTests
{
    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        var vm = new DatabaseTabViewModel(new FakeDialogService(), new DocumentExportService(), new NullMdFileStore(), null);

        Assert.Empty(vm.Documents);
        Assert.Null(vm.SelectedDocument);
        Assert.Empty(vm.SearchText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task SearchText_FiltersDocuments()
    {
        var vm = new DatabaseTabViewModel(new FakeDialogService(), new DocumentExportService(), new NullMdFileStore(), null);

        vm.SearchText = "ST";

        Assert.Empty(vm.Documents);
    }

    private sealed class FakeDialogService : IDialogService
    {
        public Task<string?> OpenMarkdownFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

        public Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates) => Task.FromResult<string?>(null);

        public Task ShowAboutAsync() => Task.CompletedTask;
    }
}
