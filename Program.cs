using System;
using Avalonia;
using AvaloniaApp.Tools;

namespace AvaloniaApp;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--smoke")
        {
            SmokeTest.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--repro-buttons")
        {
            var rc = HeadlessButtonRepro.RunAsync().GetAwaiter().GetResult();
            Environment.Exit(rc);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
