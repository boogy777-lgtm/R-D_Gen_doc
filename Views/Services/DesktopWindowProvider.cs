using System;
using Avalonia.Controls;
using AvaloniaApp.Services;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Простая реализация <see cref="IWindowProvider"/> через классический
/// desktop-lifetime.
/// </summary>
public sealed class DesktopWindowProvider : IWindowProvider
{
    public Window MainWindow =>
        (Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow
        ?? throw new InvalidOperationException("MainWindow не инициализировано.");
}
