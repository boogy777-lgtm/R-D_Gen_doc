using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// No-op реализация <see cref="IClipboardService"/> для unit-тестов
/// и для дизайнера. Не трогает системный clipboard.
/// </summary>
public sealed class NullClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
}
