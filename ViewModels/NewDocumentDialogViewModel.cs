using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

public partial class NewDocumentDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<TemplateSummary> availableTemplates = new();

    [ObservableProperty]
    private TemplateSummary? selectedTemplate;

    public event Action<bool>? CloseRequested;

    public void Initialize(IEnumerable<TemplateSummary> templates)
    {
        var list = templates.ToList();
        AvailableTemplates = new ObservableCollection<TemplateSummary>(list);
        SelectedTemplate = list.FirstOrDefault();
    }

    [RelayCommand]
    private void Ok() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
