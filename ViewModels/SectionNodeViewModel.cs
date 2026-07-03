using System;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using AvaloniaApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel узла дерева секций (один <see cref="BodySection"/> + его дети).
///
/// Находится в слое Adapters. Знает про <see cref="DocumentChangeEngine"/>,
/// но НЕ знает про Avalonia-контролы (UserControl / ItemsControl).
///
/// Команды AddSubsection / Remove делегируют в Engine.
/// Свойство <see cref="CanAddSubsection"/> проверяет лимит глубины
/// <see cref="DocumentChangeEngine.MaxSectionDepth"/>.
/// </summary>
public class SectionNodeViewModel : ViewModelBase
{
    private ITreeMutator engine;
    private IRelayCommand? removeCommand;

    public SectionNodeViewModel(BodySection section, DocumentBody body, ITreeMutator engine)
    {
        Section = section;
        this.engine = engine;
        Children = new ObservableCollection<SectionNodeViewModel>(
            section.Children.OrderBy(c => c.Position).Select(c => new SectionNodeViewModel(c, body, engine)));
    }

    public BodySection Section { get; }

    /// <summary>
    /// Дочерние узлы. ObservableCollection, обновляется при
    /// <see cref="SectionsTreeViewModel.RebuildNodes"/>.
    /// </summary>
    public ObservableCollection<SectionNodeViewModel> Children { get; }

    /// <summary>
    /// Можно ли добавить подраздел? False если достигнут лимит
    /// <see cref="DocumentChangeEngine.MaxSectionDepth"/>.
    /// </summary>
    public bool CanAddSubsection => Section.Level < DocumentChangeEngine.MaxSectionDepth;

    /// <summary>
    /// Команда «+ Подраздел». Делегирует в Engine.AddSection(parent).
    /// </summary>
    public IRelayCommand AddSubsectionCommand => new RelayCommand(
        () => engine.AddSection(parent: Section));

    /// <summary>
    /// Команда «✕ Удалить». Делегирует в Engine.RemoveSection.
    /// </summary>
    public IRelayCommand RemoveCommand => removeCommand ??= new RelayCommand(
        () => engine.RemoveSection(Section));

    /// <summary>
    /// Заменяет движок мутаций у узла и всех дочерних узлов.
    /// Принимает <see cref="IDocumentChangeEngine"/> (общий контракт),
    /// реализация <see cref="Services.DocumentChangeEngine"/> upcast'ится
    /// к <see cref="ITreeMutator"/> при присваивании.
    /// </summary>
    public void UpdateEngine(IDocumentChangeEngine newEngine)
    {
        // DocumentChangeEngine реализует все ролевые интерфейсы (ISP),
        // поэтому upcast через as безопасен и не возвращает null для реального движка.
        engine = newEngine as ITreeMutator
            ?? throw new ArgumentException(
                "Переданный движок не реализует ITreeMutator.",
                nameof(newEngine));
        foreach (var child in Children)
        {
            child.UpdateEngine(newEngine);
        }
    }
}
