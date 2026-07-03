using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaApp.Composition;
using AvaloniaApp.Services;
using AvaloniaApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaApp;

public partial class App : Application
{
    /// <summary>
    /// Composition root: DI-контейнер для всего приложения.
    /// Создаётся при <c>OnFrameworkInitializationCompleted</c> и живёт
    /// до завершения процесса.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Инициализируем схему SQLite и Obsidian-base ДО создания окон.
        try
        {
            DocumentSchema.EnsureCreated();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App.OnFrameworkInitializationCompleted] Schema init failed: {ex}");
        }

        try
        {
            var exeDir = Environment.ProcessPath != null
                ? Path.GetDirectoryName(Environment.ProcessPath)!
                : AppContext.BaseDirectory;
            var docsDir = Path.Combine(exeDir, "docs");
            DocumentsBaseGenerator.Generate(docsDir);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App.OnFrameworkInitializationCompleted] Base generation failed: {ex}");
        }

        // 2. Глобальный перехватчик UI-thread-исключений.
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            Debug.WriteLine($"[UIThread.UnhandledException] {args.Exception}");
            Console.Error.WriteLine($"[UIThread.UnhandledException] {args.Exception}");
            args.Handled = true;
        };

        // 3. Собираем граф зависимостей и регистрируем MainWindow.
        var services = new ServiceCollection();
        services.AddAvaloniaAppServices();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 4. Резолвим MainWindow через DI — конструктор получает все
            //    зависимости автоматически.
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
