using System.Threading.Tasks;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Провайдер overlay-диалога успеха. Делегирует показ
/// <see cref="MainWindowViewModel.ShowSuccessOverlayAsync"/>.
/// </summary>
public sealed class MainWindowSuccessOverlayPresenter : ISuccessOverlayPresenter
{
    private readonly IWindowProvider windowProvider;

    public MainWindowSuccessOverlayPresenter(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;
    }

    public Task ShowAsync(string title, string message, string filePath, bool offerOpenInExplorer)
    {
        if (windowProvider.MainWindow.DataContext is MainWindowViewModel vm)
        {
            return vm.ShowSuccessOverlayAsync(title, message, filePath, offerOpenInExplorer);
        }

        return Task.CompletedTask;
    }
}
