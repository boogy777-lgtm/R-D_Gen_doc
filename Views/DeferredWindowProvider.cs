using System;
using Avalonia.Controls;

namespace AvaloniaApp.Views;

/// <summary>
/// Провайдер окна, который возвращает владельца отложенно — на момент
/// первого обращения. Нужен, потому что <see cref="MainWindow"/> создаёт
/// сервисы ДО того, как присвоен <c>ApplicationLifetime.MainWindow</c>.
/// </summary>
internal sealed class DeferredWindowProvider : AvaloniaApp.Services.IWindowProvider
{
    private readonly Func<Window> windowAccessor;

    public DeferredWindowProvider(Func<Window> windowAccessor)
    {
        this.windowAccessor = windowAccessor;
    }

    public Window MainWindow => windowAccessor();
}
