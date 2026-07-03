using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IUriLauncherService"/> через системный Process.Start.
/// </summary>
public sealed class SystemUriLauncherService : IUriLauncherService
{
    public Task LaunchAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var startInfo = new ProcessStartInfo(uri.AbsoluteUri)
        {
            UseShellExecute = true,
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
