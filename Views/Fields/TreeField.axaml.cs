using Avalonia.Controls;

namespace AvaloniaApp.Views.Fields;

/// <summary>
/// View для поля «Центральные секции» (TreeField).
///
/// DataContext задаётся из FormControl — это <see cref="AvaloniaApp.ViewModels.SectionsTreeViewModel"/>.
/// Сам View НЕ хранит никаких коллекций — они в VM.
/// </summary>
public partial class TreeField : UserControl
{
    public TreeField()
    {
        InitializeComponent();
    }
}
