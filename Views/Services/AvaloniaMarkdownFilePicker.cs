using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaApp.Models;
using AvaloniaApp.Services;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Avalonia-реализация <see cref="IMarkdownFilePicker"/> через
/// <see cref="IStorageProvider.OpenFilePickerAsync"/>. Делегирует
/// Avalonia-специфичные API и легко подменяется в unit-тестах.
/// </summary>
public sealed class AvaloniaMarkdownFilePicker : IMarkdownFilePicker
{
    private readonly IWindowProvider windowProvider;

    public AvaloniaMarkdownFilePicker(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;
    }

    public async Task<string?> OpenAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Открыть Markdown-файл",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Markdown") { Patterns = new[] { "*.md", "*.markdown" } },
                new("Все файлы") { Patterns = new[] { "*" } },
            },
        };

        var result = await windowProvider.MainWindow.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveAsAsync(string? defaultFileName)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Сохранить Markdown-файл",
            SuggestedFileName = defaultFileName ?? "document.md",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Markdown") { Patterns = new[] { "*.md" } },
            },
        };

        var result = await windowProvider.MainWindow.StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }
}
