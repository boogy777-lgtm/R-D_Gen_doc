using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AvaloniaApp.Models;

/// <summary>
/// Дескриптор плоской коллекции элементов одного типа.
/// Поддерживает опциональные действия для подэлементов (например, таблицы внутри приложения).
/// </summary>
/// <typeparam name="TItem">Тип элемента коллекции.</typeparam>
public sealed class CollectionFieldDescriptor<TItem> : FormFieldDescriptor
{
    /// <summary>
    /// Gets or sets getter коллекции из <see cref="DocumentBody"/>.
    /// Возвращает ту же ссылку, чтобы <see cref="ObservableCollection{T}.CollectionChanged"/>
    /// обновлял UI напрямую.
    /// </summary>
    public Func<DocumentBody, ObservableCollection<TItem>> ItemsSourceGetter { get; init; } = _ => new();

    /// <summary>
    /// Gets or sets действие добавления нового элемента.
    /// Используется только если <see cref="AddCommandFactory"/> не задан.
    /// </summary>
    public Action<DocumentBody, IDocumentChangeEngine?> AddAction { get; init; } = (_, _) => { };

    /// <summary>
    /// Gets or sets фабрика команды добавления. Если задана —
    /// переопределяет стандартное поведение кнопки «+» (вместо прямого вызова
    /// <see cref="AddAction"/> используется построенная команда).
    ///
    /// Применяется для коллекций, требующих подтверждения/дополнительного
    /// ввода перед добавлением (например, ЛРИ — пользователь должен явно
    /// подтвердить добавление записи об изменении через диалог).
    /// </summary>
    /// <remarks>
    /// Сигнатура: <c>(DocumentBody, IDocumentChangeEngine?) → ICommand</c>.
    /// Реализация полностью отвечает за то, какие побочные эффекты
    /// (popup, сериализация состояния, прогресс) сопровождают добавление.
    /// </remarks>
    public Func<DocumentBody, IDocumentChangeEngine?, ICommand>? AddCommandFactory { get; init; }

    /// <summary>
    /// Gets or sets действие удаления элемента.
    /// </summary>
    public Action<TItem, DocumentBody, IDocumentChangeEngine?> RemoveAction { get; init; } = (_, _, _) => { };

    /// <summary>
    /// Gets or sets действие добавления подэлемента к элементу коллекции
    /// (например, таблицы к приложению). Опционально.
    /// </summary>
    public Action<TItem, IDocumentChangeEngine?>? AddSubItemAction { get; init; }

    /// <summary>
    /// Gets or sets действие удаления подэлемента из элемента коллекции.
    /// Первый параметр — родительский элемент, второй — удаляемый подэлемент.
    /// </summary>
    public Action<TItem, object, IDocumentChangeEngine?>? RemoveSubItemAction { get; init; }

    /// <summary>
    /// Gets or sets значение по умолчанию из шаблона (одно значение-строка).
    /// При пустой коллекции используется как fallback при
    /// <see cref="FieldPermission.Mutable"/>.
    /// </summary>
    public string? TemplateDefault { get; init; }
}
