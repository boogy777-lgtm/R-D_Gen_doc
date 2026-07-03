using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaApp.Models;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Views;

public partial class NewDocumentDialog : Window
{
    private readonly NewDocumentDialogViewModel viewModel;

    public NewDocumentDialog()
    {
        InitializeComponent();
        viewModel = new NewDocumentDialogViewModel();
        viewModel.CloseRequested += OnCloseRequested;
        DataContext = viewModel;
    }

    public string? Result { get; private set; }

    public static async Task<string?> ShowAsync(Window owner, IEnumerable<TemplateSummary> templates)
    {
        var dialog = new NewDocumentDialog();
        dialog.viewModel.Initialize(templates);
        await dialog.ShowDialog<bool?>(owner);
        return dialog.Result;
    }

    private void OnCloseRequested(bool ok)
    {
        if (ok && viewModel.SelectedTemplate != null)
        {
            Result = viewModel.SelectedTemplate.Code;
        }

        Close(ok);
    }
}
