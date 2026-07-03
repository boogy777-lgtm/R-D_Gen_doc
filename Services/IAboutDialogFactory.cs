using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над диалогом «О программе» (AboutWindow).
/// </summary>
public interface IAboutDialogFactory
{
    Task ShowAsync(Window owner);
}
