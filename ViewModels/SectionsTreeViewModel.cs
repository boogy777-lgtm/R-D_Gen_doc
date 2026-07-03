using System;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using AvaloniaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel для поля «Центральные секции» (TreeField).
///
/// Находится в слое Adapters: знает про <see cref="DocumentChangeEngine"/>
/// (Application), но НЕ знает про Avalonia UserControl / ItemsControl /
/// биндинги. Это позволяет тестировать логику без UI-стека.
///
/// Раньше обновление UI после мутаций Engine зависело ТОЛЬКО от цепочки
/// Engine.Changed → FormControl.OnEngineChanged → RebuildNodes.
/// Теперь VM САМА подписывается на Engine.Changed и вызывает RebuildNodes —
/// это belt-and-suspenders: даже если FormControl-цепочка сломана,
/// UI обновится (TermCollection vs RootNodes — термины работали ТОЛЬКО
/// потому что биндились к той же ObservableCollection, а RootNodes — копия).
///
/// Метод <see cref="Disconnect"/> отписывается от Engine — вызывается
/// из FormControl при пересоздании полей.
/// </summary>
public partial class SectionsTreeViewModel : ViewModelBase, IDisposable, IEngineAware
{
    private readonly DocumentBody body;
    private ITreeMutator engine;

    [ObservableProperty]
    private string label = "Содержание документа";

    public SectionsTreeViewModel(DocumentBody body, ITreeMutator engine)
    {
        this.body = body;
        this.engine = engine;

        // Belt-and-suspenders: если FormControl.OnEngineChanged не сработал,
        // VM сама перестроит узлы по структурному событию Engine.StructureChanged.
        // Контентные изменения (ввод текста) НЕ перестраивают дерево — debounce в MainWindowViewModel.
        // Структурное событие определено в IEngineCore (через IDocumentChangeEngine).
        // Здесь принимаем ITreeMutator + IEngineCore — комбинация ролей одного движка.
        if (engine is IEngineCore core)
        {
            core.StructureChanged += OnEngineStructureChanged;
        }

        RebuildNodes();
    }

    /// <summary>
    /// Корневые узлы дерева секций. Обёртки над <see cref="BodySection"/>
    /// с предвычисленным <see cref="SectionNodeViewModel.Children"/>.
    /// </summary>
    public ObservableCollection<SectionNodeViewModel> RootNodes { get; } = new();

    /// <summary>
    /// Команда «+ Подраздел» — вызывается из SectionNodeViewModel,
    /// параметр — родительская секция. Engine.AddSection(parent).
    /// </summary>
    public void AddSubsection(BodySection parent)
    {
        if (parent is null)
        {
            return;
        }

        engine.AddSection(parent: parent);

        // RebuildNodes() — через engine.Changed.
    }

    /// <summary>
    /// Команда «✕ Удалить секцию» — параметр — удаляемая секция.
    /// </summary>
    public void Remove(BodySection section)
    {
        if (section is null)
        {
            return;
        }

        engine.RemoveSection(section);

        // RebuildNodes() — через engine.Changed.
    }

    /// <summary>
    /// Пересоздаёт обёртки <see cref="SectionNodeViewModel"/> из текущего состояния
    /// <see cref="DocumentBody.Sections"/>.
    /// </summary>
    public void RebuildNodes()
    {
        RootNodes.Clear();
        foreach (var s in body.Sections.OrderBy(s => s.Position))
        {
            RootNodes.Add(BuildNode(s));
        }
    }

    /// <summary>Отписывается от Engine.StructureChanged. Вызвать перед заменой VM.</summary>
    public void Disconnect()
    {
        if (engine is IEngineCore core)
        {
            core.StructureChanged -= OnEngineStructureChanged;
        }
    }

    /// <summary>
    /// Заменяет движок мутаций, переподписываясь на структурные события
    /// и обновляя движок во всех узлах дерева без пересоздания коллекции.
    /// Принимает <see cref="IDocumentChangeEngine"/> — общий контракт, чтобы
    /// реализация <see cref="Services.DocumentChangeEngine"/> (реализующая все
    /// ролевые интерфейсы) могла быть здесь использована целиком.
    /// </summary>
    public void UpdateEngine(IDocumentChangeEngine newEngine)
    {
        ArgumentNullException.ThrowIfNull(newEngine);
        UpdateEngineCore(newEngine);
    }

    /// <inheritdoc />
    void IEngineAware.UpdateEngine(IDocumentChangeEngine? newEngine)
    {
        // SectionsTreeViewModel требует non-null движок (отписка от старого IEngineCore.StructureChanged).
        if (newEngine is null)
        {
            return;
        }

        UpdateEngineCore(newEngine);
    }

    private void UpdateEngineCore(IDocumentChangeEngine newEngine)
    {
        // Отписываемся от старого ядра, если движок также реализует IEngineCore.
        if (engine is IEngineCore oldCore)
        {
            oldCore.StructureChanged -= OnEngineStructureChanged;
        }

        // DocumentChangeEngine реализует все ролевые интерфейсы (ISP),
        // поэтому upcast через as безопасен и не возвращает null для реального движка.
        engine = newEngine as ITreeMutator
            ?? throw new ArgumentException(
                "Переданный движок не реализует ITreeMutator.",
                nameof(newEngine));

        // Подписываемся на новое ядро.
        if (engine is IEngineCore newCore)
        {
            newCore.StructureChanged += OnEngineStructureChanged;
        }

        foreach (var node in RootNodes)
        {
            node.UpdateEngine(newEngine);
        }
    }

    /// <summary>
    /// Отписывается от событий Engine. Вызывается при удалении ViewModel.
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }

    private SectionNodeViewModel BuildNode(BodySection section)
    {
        return new SectionNodeViewModel(
            section,
            body: this.body,
            engine: this.engine);
    }

    /// <summary>
    /// Команда «+ Добавить раздел верхнего уровня». Engine.AddSection(null).
    /// После мутации Engine стреляет Changed, на который подписан этот VM.
    /// </summary>
    [RelayCommand]
    private void AddRoot()
    {
        engine.AddSection(parent: null);

        // RebuildNodes() вызовется через OnEngineChanged (engine.Changed).
    }

    private void OnEngineStructureChanged(object? sender, EventArgs e)
    {
        RebuildNodes();
    }
}
