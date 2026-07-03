using System.Collections;
using System.Collections.ObjectModel;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Абстрактный базовый ViewModel для поля-коллекции (<see cref="Views.Fields.CollectionField"/>).
/// Нужен, чтобы XAML мог биндиться к generic VM без знания TItem.
/// </summary>
public abstract partial class CollectionFieldViewModel : ViewModelBase, IEngineAware
{
    /// <summary>
    /// Gets метка поля.
    /// </summary>
    public abstract string Label { get; }

    /// <summary>
    /// Gets вспомогательный текст.
    /// </summary>
    public abstract string? HelpText { get; }

    /// <summary>
    /// Gets признак наличия вспомогательного текста.
    /// </summary>
    public abstract bool HasHelpText { get; }

    /// <summary>
    /// Gets источник данных коллекции.
    /// </summary>
    public abstract IEnumerable ItemsSource { get; }

    /// <summary>
    /// Gets команду добавления элемента. По умолчанию — <see cref="IRelayCommand"/>,
    /// но дескриптор может подменить на любую <see cref="System.Windows.Input.ICommand"/>
    /// (например, чтобы добавить диалог подтверждения).
    /// </summary>
    public abstract System.Windows.Input.ICommand AddCommand { get; }

    /// <summary>
    /// Gets команду удаления элемента.
    /// </summary>
    public abstract IRelayCommand<object> RemoveCommand { get; }

    /// <summary>
    /// Gets признак возможности редактирования коллекции.
    /// </summary>
    public abstract bool IsEditable { get; }

    /// <summary>
    /// Gets признак что коллекция доступна только для чтения.
    /// </summary>
    public bool IsReadOnly => !IsEditable;

    /// <summary>
    /// Gets команду добавления подэлемента к элементу коллекции (опционально).
    /// Параметр — родительский элемент (TItem).
    /// </summary>
    public virtual IRelayCommand<object>? AddSubItemCommand => null;

    /// <summary>
    /// Gets команду удаления подэлемента из элемента коллекции (опционально).
    /// Параметр — удаляемый подэлемент.
    /// </summary>
    public virtual IRelayCommand<object>? RemoveSubItemCommand => null;

    /// <summary>
    /// Заменяет движок мутаций. Используется при смене метаданных без
    /// пересоздания ViewModel и UI-коллекций.
    /// Принимает узкий ролевой контракт <see cref="ICollectionMutator"/> —
    /// VM коллекции использует только Add*/Remove* коллекций.
    /// </summary>
    public abstract void UpdateEngine(ICollectionMutator? newEngine);

    /// <inheritdoc />
    void IEngineAware.UpdateEngine(IDocumentChangeEngine? newEngine)
    {
        UpdateEngine(newEngine as ICollectionMutator);
    }
}

/// <summary>
/// ViewModel для поля-коллекции с элементами типа <typeparamref name="TItem"/>.
/// </summary>
/// <typeparam name="TItem">Тип элемента коллекции.</typeparam>
public partial class CollectionFieldViewModel<TItem> : CollectionFieldViewModel, IEngineAware
{
    private readonly CollectionFieldDescriptor<TItem> descriptor;
    private readonly DocumentBody body;
    private ICollectionMutator? engine;

    public CollectionFieldViewModel(CollectionFieldDescriptor<TItem> descriptor, DocumentBody body, ICollectionMutator? engine)
    {
        this.descriptor = descriptor;
        this.body = body;
        this.engine = engine;
        ItemsSource = descriptor.ItemsSourceGetter(body);

        // Если дескриптор задаёт фабрику команды — используем её (например,
        // для ЛРИ: вместо прямого добавления строки открывается диалог
        // подтверждения). Иначе — стандартное поведение через AddAction.
        // Делегаты в CollectionFieldDescriptor объявлены через IDocumentChangeEngine?
        // (общий контракт для совместимости). Узкая роль ICollectionMutator upcast'ится
        // явно через as, поскольку ролевые интерфейсы не наследуют общий контракт (ISP).
        AddCommand = descriptor.AddCommandFactory?.Invoke(body, engine as IDocumentChangeEngine)
                     ?? new RelayCommand(Add);

        RemoveCommand = new RelayCommand<object>(Remove);

        // Опциональные команды для подэлементов (таблицы внутри приложений и т.п.).
        if (descriptor.AddSubItemAction != null)
        {
            AddSubItemCommand = new RelayCommand<object>(AddSubItem);
        }

        if (descriptor.RemoveSubItemAction != null)
        {
            RemoveSubItemCommand = new RelayCommand<object>(RemoveSubItem);
        }
    }

    /// <inheritdoc />
    public override string Label => descriptor.Label;

    /// <inheritdoc />
    public override string? HelpText => descriptor.HelpText;

    /// <inheritdoc />
    public override bool HasHelpText => !string.IsNullOrWhiteSpace(descriptor.HelpText);

    /// <inheritdoc />
    public override bool IsEditable => descriptor.IsEditable;

    /// <inheritdoc />
    public override ObservableCollection<TItem> ItemsSource { get; }

    /// <inheritdoc />
    public override System.Windows.Input.ICommand AddCommand { get; }

    /// <inheritdoc />
    public override IRelayCommand<object> RemoveCommand { get; }

    /// <inheritdoc />
    public override IRelayCommand<object>? AddSubItemCommand { get; }

    /// <inheritdoc />
    public override IRelayCommand<object>? RemoveSubItemCommand { get; }

    /// <inheritdoc />
    public override void UpdateEngine(ICollectionMutator? newEngine)
    {
        engine = newEngine;
    }

    private void Add()
    {
        descriptor.AddAction(body, engine as IDocumentChangeEngine);
    }

    private void Remove(object? parameter)
    {
        if (parameter is TItem item)
        {
            descriptor.RemoveAction(item, body, engine as IDocumentChangeEngine);
        }
    }

    private void AddSubItem(object? parameter)
    {
        if (parameter is TItem parent && engine != null)
        {
            descriptor.AddSubItemAction?.Invoke(parent, engine as IDocumentChangeEngine);
        }
    }

    private void RemoveSubItem(object? parameter)
    {
        // parameter — кортеж (родитель, подэлемент), передаётся из View через
        // MultiBinding. Раньше здесь был цикл по всем элементам коллекции (n+1).
        if (parameter is not (object parentObj, object childObj) || engine is null)
        {
            return;
        }

        if (parentObj is TItem parent)
        {
            descriptor.RemoveSubItemAction?.Invoke(parent, childObj, engine as IDocumentChangeEngine);
        }
    }
}
