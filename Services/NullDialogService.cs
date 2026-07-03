using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// No-op реализация <see cref="IDialogService"/> для unit-тестов
/// и для parameterless-конструкторов <c>MainWindowViewModel</c> / <c>DatabaseTabViewModel</c>.
/// Возвращает "отмену" / <c>Task.CompletedTask</c> для всех операций диалога.
/// </summary>
public sealed class NullDialogService : IDialogService
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
