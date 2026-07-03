using Avalonia.Controls;

namespace AvaloniaApp.Views.Fields;

/// <summary>
/// Одиночное текстовое поле формы (одно- или многострочное).
/// DataContext = <see cref="ViewModels.SingleFieldViewModel"/>.
/// </summary>
public partial class SingleField : UserControl
{
    public SingleField()
    {
        InitializeComponent();
    }
}
