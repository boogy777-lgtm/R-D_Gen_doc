using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис работы с буфером обмена. Абстракция над Avalonia.Input.Platform.
/// VM получает этот интерфейс через DI и не зависит от Avalonia.Controls.
/// </summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}
