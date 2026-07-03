using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над overlay-диалогом успеха.
/// </summary>
public interface ISuccessOverlayPresenter
{
    Task ShowAsync(string title, string message, string filePath, bool offerOpenInExplorer);
}
