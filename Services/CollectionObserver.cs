using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Регион ответственности: подписки на изменения коллекций и элементов документа.
///
/// Извлечено из <see cref="DocumentChangeEngine"/> для соблюдения SRP.
/// Когда пользователь редактирует <see cref="StringItem.Value"/>, <see cref="TermPair.Term"/>
/// и т.п. — UI меняет свойство элемента напрямую через TwoWay-биндинг, минуя Engine.
/// Без подписки на <c>PropertyChanged</c> preview не обновляется до следующей явной мутации.
///
/// Архитектура:
/// <list type="bullet">
///   <item><see cref="RegisterCollection{T}"/> — универсальный метод: подписывается
///         на <c>CollectionChanged</c> коллекции, зацепляет <c>PropertyChanged</c>
///         у каждого элемента и рекурсивно обходит вложенные коллекции через селектор.</item>
///   <item><see cref="hookedCollections"/> (HashSet) — предотвращает двойную подписку.</item>
///   <item><see cref="ServiceProperties"/> (HashSet) — фильтрует служебные поля
///         (Position, Id, Level, ParentId, HierarchyPath, GlobalTableIndex).</item>
///   <item><see cref="propertySubscriptionCounts"/> — счётчик подписок на элемент,
///         предотвращает утечки и обеспечивает идемпотентность.</item>
/// </list>
/// </summary>
public sealed class CollectionObserver
{
    /// <summary>
    /// Свойства, которые меняет Engine при перенумерации — не триггерят preview.
    /// </summary>
    private static readonly HashSet<string> ServiceProperties = new(StringComparer.Ordinal)
    {
        "Position", "Id", "Level", "ParentId", "HierarchyPath", "Children",
        "GlobalTableIndex",
    };

    private readonly DocumentBody body;
    private readonly DocumentMetadata metadata;
    private readonly HashSet<object> hookedCollections = new();
    private readonly Dictionary<object, HashSet<INotifyPropertyChanged>> collectionItems = new();
    private readonly Dictionary<INotifyPropertyChanged, HashSet<object>> itemNestedCollections = new();
    private readonly Dictionary<INotifyPropertyChanged, int> propertySubscriptionCounts = new();

    /// <summary>
    /// Создаёт наблюдатель для указанного тела документа и метаданных.
    /// </summary>
    public CollectionObserver(DocumentBody body, DocumentMetadata metadata)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Доступ к метаданным (для синхронизации Author последней строки ЛРИ).
    /// </summary>
    public DocumentMetadata Metadata => metadata;

    /// <summary>
    /// Доступ к телу документа.
    /// </summary>
    public DocumentBody Body => body;

    /// <summary>
    /// Событие изменения элемента коллекции (используется Facade для ContentChanged + Author sync).
    /// </summary>
    public event EventHandler<CollectionItemChangedEventArgs>? ItemChanged;

    /// <summary>
    /// Подписывается на изменения всех коллекций документа.
    /// Вызывается один раз при инициализации Engine.
    /// </summary>
    public void HookAll()
    {
        RegisterCollection(body.Terms);
        RegisterCollection(body.RelatedDocuments);
        RegisterCollection(body.ChangeLog);
        RegisterCollection(body.Appendices, app => app.Sections);
        RegisterCollection(body.Sections, s => s.Children);
    }

    /// <summary>
    /// Отписывается от всех коллекций. Вызывается из Dispose.
    /// </summary>
    public void UnhookAll()
    {
        foreach (var collection in hookedCollections.ToList())
        {
            UnregisterCollection(collection);
        }

        hookedCollections.Clear();
        collectionItems.Clear();
        itemNestedCollections.Clear();
        propertySubscriptionCounts.Clear();
    }

    /// <summary>
    /// Регистрирует <paramref name="collection"/> для отслеживания изменений элементов.
    /// Опциональный <paramref name="nestedSelector"/> указывает на вложенную коллекцию
    /// внутри элемента (например, <c>app => app.Sections</c> для приложений).
    /// </summary>
    public void RegisterCollection<T>(
        ObservableCollection<T> collection,
        Func<T, INotifyCollectionChanged?>? nestedSelector = null)
        where T : INotifyPropertyChanged
    {
        if (!hookedCollections.Add(collection))
        {
            return; // уже зацеплена
        }

        collectionItems[collection] = new HashSet<INotifyPropertyChanged>();

        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Отписываемся от всех элементов этой коллекции (включая вложенные).
                var tracked = collectionItems[collection].ToList();
                foreach (var item in tracked.Cast<T>())
                {
                    DetachItem(item, nestedSelector);
                }

                collectionItems[collection].Clear();

                // Подписываемся на текущие элементы после Reset.
                foreach (var item in collection)
                {
                    AttachItem(item, nestedSelector);
                    collectionItems[collection].Add(item);
                }

                return;
            }

            if (e.NewItems != null)
            {
                foreach (T item in e.NewItems)
                {
                    AttachItem(item, nestedSelector);
                    collectionItems[collection].Add(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (T item in e.OldItems)
                {
                    if (collectionItems[collection].Remove(item))
                    {
                        DetachItem(item, nestedSelector);
                    }
                }
            }
        };

        foreach (var item in collection)
        {
            AttachItem(item, nestedSelector);
            collectionItems[collection].Add(item);
        }
    }

    /// <summary>
    /// Регистрирует вложенную коллекцию без знания типа элемента на этапе компиляции.
    /// Используется для рекурсивного обхода (например, <c>BodyAppendix.Sections</c>).
    /// </summary>
    private void RegisterNestedCollection(INotifyCollectionChanged collection)
    {
        if (!hookedCollections.Add(collection))
        {
            return;
        }

        collectionItems[collection] = new HashSet<INotifyPropertyChanged>();

        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var tracked = collectionItems[collection].ToList();
                foreach (var item in tracked)
                {
                    UnsubscribePropertyChanged(item);
                }

                collectionItems[collection].Clear();

                if (collection is System.Collections.IEnumerable current)
                {
                    foreach (INotifyPropertyChanged item in current)
                    {
                        SubscribePropertyChanged(item);
                        collectionItems[collection].Add(item);
                    }
                }

                return;
            }

            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    SubscribePropertyChanged(item);
                    collectionItems[collection].Add(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    if (collectionItems[collection].Remove(item))
                    {
                        UnsubscribePropertyChanged(item);
                    }
                }
            }
        };

        if (collection is System.Collections.IEnumerable existing)
        {
            foreach (INotifyPropertyChanged item in existing)
            {
                SubscribePropertyChanged(item);
                collectionItems[collection].Add(item);
            }
        }
    }

    private void UnregisterCollection(object collection)
    {
        hookedCollections.Remove(collection);
        if (collectionItems.Remove(collection, out var items))
        {
            foreach (var item in items.ToList())
            {
                UnsubscribePropertyChanged(item);
            }
        }
    }

    private void AttachItem<T>(T item, Func<T, INotifyCollectionChanged?>? nestedSelector)
        where T : INotifyPropertyChanged
    {
        SubscribePropertyChanged(item);

        if (nestedSelector != null)
        {
            var nested = nestedSelector(item);
            if (nested is not null)
            {
                if (!itemNestedCollections.TryGetValue(item, out var nestedSet))
                {
                    nestedSet = new HashSet<object>();
                    itemNestedCollections[item] = nestedSet;
                }

                if (nestedSet.Add(nested))
                {
                    RegisterNestedCollection(nested);
                }
            }
        }
    }

    private void DetachItem<T>(T item, Func<T, INotifyCollectionChanged?>? nestedSelector)
        where T : INotifyPropertyChanged
    {
        UnsubscribePropertyChanged(item);

        if (nestedSelector != null && itemNestedCollections.TryGetValue(item, out var nestedSet))
        {
            foreach (var nested in nestedSet.ToList())
            {
                UnregisterCollection(nested);
            }

            itemNestedCollections.Remove(item);
        }
    }

    private void SubscribePropertyChanged(INotifyPropertyChanged item)
    {
        if (propertySubscriptionCounts.TryAdd(item, 1))
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
        else
        {
            propertySubscriptionCounts[item]++;
        }
    }

    private void UnsubscribePropertyChanged(INotifyPropertyChanged item)
    {
        if (!propertySubscriptionCounts.TryGetValue(item, out var count))
        {
            return;
        }

        if (count <= 1)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            propertySubscriptionCounts.Remove(item);
        }
        else
        {
            propertySubscriptionCounts[item] = count - 1;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Игнорируем служебные свойства (Position, Id, …) — их меняет Engine.
        if (e.PropertyName is not null && ServiceProperties.Contains(e.PropertyName))
        {
            return;
        }

        ItemChanged?.Invoke(this, new CollectionItemChangedEventArgs(sender, e.PropertyName));
    }

    /// <summary>
    /// Возвращает последнюю строку ЛРИ или null (для Author sync).
    /// </summary>
    public ChangeLogRow? GetLastChangeLogRow() =>
        body.ChangeLog.OrderBy(r => r.Position).LastOrDefault();
}
