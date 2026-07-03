using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Avalonia-реализация <see cref="IAboutDialogFactory"/> через <see cref="AboutWindow"/>.
/// </summary>
public sealed class AvaloniaAboutDialogFactory : IAboutDialogFactory
{
    private readonly IUriLauncherService uriLauncher;

    public AvaloniaAboutDialogFactory(IUriLauncherService uriLauncher)
    {
        this.uriLauncher = uriLauncher;
    }

    public async Task ShowAsync(Window owner)
    {
        var viewModel = new AboutViewModel(uriLauncher);
        var dialog = new AboutWindow(viewModel)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        await dialog.ShowDialog(owner);
    }
}
