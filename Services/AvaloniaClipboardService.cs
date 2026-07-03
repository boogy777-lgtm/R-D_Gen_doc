using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IClipboardService"/> через Avalonia.Input.Platform.
/// Прячет зависимость от Avalonia.Controls внутри Services-слоя.
/// </summary>
public sealed class AvaloniaClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
    {
        var clipboard = TryGetClipboard();
        if (clipboard is null || string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        return clipboard.SetTextAsync(text);
    }

    private static IClipboard? TryGetClipboard()
    {
        var app = Application.Current;
        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }
}
