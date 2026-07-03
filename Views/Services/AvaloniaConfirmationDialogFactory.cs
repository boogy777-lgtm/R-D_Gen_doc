using System.Threading.Tasks;
using AvaloniaApp.Services;
using FAContentDialog = FluentAvalonia.UI.Controls.FAContentDialog;
using FAContentDialogButton = FluentAvalonia.UI.Controls.FAContentDialogButton;
using FAContentDialogResult = FluentAvalonia.UI.Controls.FAContentDialogResult;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Avalonia-реализация <see cref="IConfirmationDialogFactory"/> через
/// <see cref="FAContentDialog"/> из FluentAvalonia. Диалог появляется
/// как in-app overlay с двумя кнопками «Да / Нет», привязанный к
/// MainWindow через <see cref="IWindowProvider"/>.
/// </summary>
/// <remarks>
/// Использует WinUI-семантику: PrimaryButton = «Да» (положительное
/// подтверждение), SecondaryButton = «Нет» (отказ). Закрытие окна
/// (ESC / клик мимо) = <see cref="FAContentDialogResult.None"/> = <c>false</c>.
/// </remarks>
public sealed class AvaloniaConfirmationDialogFactory : IConfirmationDialogFactory
{
    private readonly IWindowProvider windowProvider;

    public AvaloniaConfirmationDialogFactory(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;
    }

    public async Task<bool> ShowAsync(string title, string message)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Да",
            SecondaryButtonText = "Нет",
            DefaultButton = FAContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync(windowProvider.MainWindow);
        return result == FAContentDialogResult.Primary;
    }
}
