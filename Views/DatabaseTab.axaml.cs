using Avalonia.Input;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Views;

/// <summary>
/// Вкладка «База данных» со списком сохранённых документов.
/// Code-behind содержит только view-level обработчики событий UI.
/// </summary>
public partial class DatabaseTab : Avalonia.Controls.UserControl
{
    public DatabaseTab()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Двойной клик по строке DataGrid — открыть выбранный документ.
    /// </summary>
    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is DatabaseTabViewModel vm && vm.OpenDocumentCommand.CanExecute(null))
        {
            vm.OpenDocumentCommand.Execute(null);
        }
    }
}
