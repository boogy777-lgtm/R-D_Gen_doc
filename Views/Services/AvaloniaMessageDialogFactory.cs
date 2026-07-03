using System.Threading.Tasks;
using AvaloniaApp.Services;
using FAContentDialog = FluentAvalonia.UI.Controls.FAContentDialog;
using FAContentDialogButton = FluentAvalonia.UI.Controls.FAContentDialogButton;
using FAContentDialogResult = FluentAvalonia.UI.Controls.FAContentDialogResult;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Avalonia-реализация <see cref="IMessageDialogFactory"/> через
/// <see cref="FAContentDialog"/> из FluentAvalonia. Диалог появляется
/// как красивый in-app overlay (не отдельное ОС-окно), привязанный
/// к MainWindow через <see cref="IWindowProvider"/>.
/// </summary>
public sealed class AvaloniaMessageDialogFactory : IMessageDialogFactory
{
    private readonly IWindowProvider windowProvider;

    public AvaloniaMessageDialogFactory(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;
    }

    public async Task ShowAsync(string title, string message)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            DefaultButton = FAContentDialogButton.Primary,
        };

        await dialog.ShowAsync(windowProvider.MainWindow);
    }
}
