using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaApp.Services;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Avalonia-реализация <see cref="IDocxFilePicker"/>.
/// </summary>
public sealed class AvaloniaDocxFilePicker : IDocxFilePicker
{
    private readonly IWindowProvider windowProvider;

    public AvaloniaDocxFilePicker(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;
    }

    public async Task<string?> SaveAsAsync(string? defaultFileName)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Сохранить DOCX-файл",
            SuggestedFileName = defaultFileName ?? "document.docx",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Word Document") { Patterns = new[] { "*.docx" } },
            },
        };

        var result = await windowProvider.MainWindow.StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }
}
