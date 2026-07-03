using Avalonia.Controls;

namespace AvaloniaApp.Views;

/// <summary>
/// Контейнер динамической формы. Отображает ViewModel-ы полей через DataTemplates.
/// Не содержит бизнес-логики и не создаёт ViewModel-ы — они создаются в MainWindowViewModel.
/// </summary>
public partial class FormControl : UserControl
{
    public FormControl()
    {
        InitializeComponent();
    }
}
