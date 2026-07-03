using System;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция для запуска внешних URI (браузер, почта и т.п.).
/// </summary>
public interface IUriLauncherService
{
    /// <summary>
    /// Открывает указанный URI через системный обработчик.
    /// </summary>
    /// <param name="uri">URI для открытия.</param>
    Task LaunchAsync(Uri uri);
}
