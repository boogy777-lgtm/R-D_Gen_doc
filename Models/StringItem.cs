using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Изменяемая обёртка над строкой для использования в <see cref="ObservableCollection{T}"/>.
/// Необходима, потому что Avalonia не может делать двусторонний биндинг
/// непосредственно к элементу <c>ObservableCollection&lt;string&gt;</c>:
/// <c>{Binding}</c> для значимого типа обновляет DataContext контейнера,
/// а не элемент коллекции.
/// </summary>
public partial class StringItem : ObservableObject
{
    /// <summary>
    /// Gets or sets строковое значение.
    /// </summary>
    [ObservableProperty]
    private string value = string.Empty;
}
