using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AvaloniaApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel для overlay-диалога успеха.
///
/// Архитектурное правило: VM НЕ зависит от Avalonia.Controls (см. AGENTS.md Hard No).
/// Всё, что связано с UI (Clipboard, TopLevel), инжектируется через
/// <see cref="IClipboardService"/> и <see cref="IFileSystemService"/>.
/// </summary>
public partial class SuccessOverlayViewModel : ViewModelBase
{
    private const string ExplorerSelectVerb = "/select,";

    private readonly IClipboardService clipboard;
    private readonly IFileSystemService fileSystem;

    private string title = string.Empty;
    private string message = string.Empty;
    private string filePath = string.Empty;
    private bool offerOpenInExplorer = true;

    public SuccessOverlayViewModel(IClipboardService clipboard, IFileSystemService fileSystem)
    {
        this.clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>
    /// Сигнализирует владельцу (MainWindow), что пользователь нажал «ОК».
    /// Завершает ожидание TaskCompletionSource в MainWindowViewModel.
    /// </summary>
    public event EventHandler? OkClicked;

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public string Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    public string FilePath
    {
        get => filePath;
        set => SetProperty(ref filePath, value);
    }

    public bool OfferOpenInExplorer
    {
        get => offerOpenInExplorer;
        set => SetProperty(ref offerOpenInExplorer, value);
    }

    [RelayCommand]
    private void Ok()
    {
        OkClicked?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                await clipboard.SetTextAsync(FilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuccessOverlayViewModel.CopyAsync] {ex}");
        }
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        try
        {
            var path = FilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (fileSystem.DirectoryExists(path))
            {
                fileSystem.StartExplorer(path);
            }
            else if (fileSystem.FileExists(path))
            {
                fileSystem.StartExplorerWithSelect(path);
            }
            else
            {
                // Путь не существует — fallback на родительскую директорию.
                var parent = fileSystem.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent) && fileSystem.DirectoryExists(parent))
                {
                    fileSystem.StartExplorer(parent);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuccessOverlayViewModel.OpenInExplorer] {ex}");
        }
    }
}
