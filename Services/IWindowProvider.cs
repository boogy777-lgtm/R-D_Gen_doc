using Avalonia.Controls;

namespace AvaloniaApp.Services;

/// <summary>
/// Провайдер главного окна приложения. В Avalonia нет встроенного
/// Application.MainWindow-сервиса, поэтому тонкая обёртка.
/// </summary>
public interface IWindowProvider
{
    Window MainWindow { get; }
}
