using Avalonia.Controls;

namespace AvaloniaApp.Views.Fields;

/// <summary>
/// View для одного узла дерева секций.
/// DataContext = <see cref="AvaloniaApp.ViewModels.SectionNodeViewModel"/>,
/// который предоставляет Section, Children, AddSubsectionCommand, RemoveCommand.
/// </summary>
public partial class SectionNodeView : UserControl
{
    public SectionNodeView()
    {
        InitializeComponent();
    }
}
