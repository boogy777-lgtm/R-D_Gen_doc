using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel динамической формы документа (вкладка «Документ»).
///
/// Создаётся один раз при смене <see cref="DocumentBody"/> и НЕ пересоздаётся
/// при смене метаданных. Вместо этого <see cref="UpdateEngine(IDocumentChangeEngine)"/>
/// заменяет движок мутаций во всех дочерних VM, сохраняя UI-коллекции
/// (фокус, скролл, выделение).
/// </summary>
public partial class FormViewModel : ViewModelBase, IDisposable
{
    /// <summary>
    /// ViewModel-ы полей формы. Создаются ViewModel-first;
    /// View (FormControl) только отображает их через DataTemplates.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ViewModelBase> formFieldViewModels = new();

    public FormViewModel()
    {
    }

    public FormViewModel(IEnumerable<ViewModelBase> fieldViewModels)
    {
        foreach (var vm in fieldViewModels)
        {
            FormFieldViewModels.Add(vm);
        }
    }

    /// <summary>
    /// Передаёт новый движок мутаций всем дочерним полям без пересоздания
    /// коллекций и UI-контролов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Использует паттерн Visitor через <see cref="IEngineAware"/>: каждая
    /// дочерняя VM сама знает, как принять новый движок. FormViewModel
    /// не знает о конкретных типах VM (OCP) — добавление нового типа поля
    /// не требует изменений здесь.
    /// </para>
    /// <para>
    /// <c>SectionsTreeViewModel.UpdateEngine</c> требует ненулевой движок
    /// (для отписки от старого IEngineCore.StructureChanged); для остальных
    /// VM движок может быть null (используется при сбросе документа).
    /// </para>
    /// </remarks>
    public void UpdateEngine(IDocumentChangeEngine? newEngine)
    {
        foreach (var vm in FormFieldViewModels)
        {
            if (vm is IEngineAware aware)
            {
                // SectionsTreeViewModel не принимает null — пропускаем в этом случае.
                if (newEngine != null || vm is not SectionsTreeViewModel)
                {
                    aware.UpdateEngine(newEngine);
                }
            }
        }
    }

    /// <summary>
    /// Полностью пересобирает поля формы (используется при смене шаблона).
    /// Старые дочерние VM диспоузятся, коллекция очищается и заполняется заново.
    /// </summary>
    public void RebuildFields(IEnumerable<ViewModelBase> newFieldViewModels)
    {
        DisposeFields();
        FormFieldViewModels.Clear();

        foreach (var vm in newFieldViewModels)
        {
            FormFieldViewModels.Add(vm);
        }
    }

    /// <summary>
    /// Диспоузит дочерние VM, но не трогает <see cref="IDocumentChangeEngine"/>
    /// — владельцем Engine является <see cref="MainWindowViewModel"/>.
    /// </summary>
    public void Dispose()
    {
        DisposeFields();
        GC.SuppressFinalize(this);
    }

    private void DisposeFields()
    {
        foreach (var vm in FormFieldViewModels)
        {
            (vm as IDisposable)?.Dispose();
        }
    }
}
