using System;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Заглушка <see cref="IUriLauncherService"/> для дизайнера и тестов.
/// </summary>
public sealed class NullUriLauncherService : IUriLauncherService
{
    public Task LaunchAsync(Uri uri) => Task.CompletedTask;
}
