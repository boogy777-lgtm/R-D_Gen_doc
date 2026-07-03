using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над диалогом подтверждения «Да / Нет».
/// </summary>
public interface IConfirmationDialogFactory
{
    Task<bool> ShowAsync(string title, string message);
}
