using System;

namespace AvaloniaApp.Services;

/// <summary>
/// Аргументы события изменения элемента коллекции,
/// генерируемого <see cref="CollectionObserver"/>.
/// </summary>
public sealed class CollectionItemChangedEventArgs : EventArgs
{
    public CollectionItemChangedEventArgs(object? item, string? propertyName)
    {
        Item = item;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Элемент коллекции, у которого изменилось свойство.
    /// </summary>
    public object? Item { get; }

    /// <summary>
    /// Имя изменившегося свойства (или null).
    /// </summary>
    public string? PropertyName { get; }
}
