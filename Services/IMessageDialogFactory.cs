using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над простым информационным диалогом.
/// </summary>
public interface IMessageDialogFactory
{
    Task ShowAsync(string title, string message);
}
