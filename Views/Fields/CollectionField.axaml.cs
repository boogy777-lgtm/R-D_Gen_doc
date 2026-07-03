using Avalonia.Controls;

namespace AvaloniaApp.Views.Fields;

/// <summary>
/// Универсальный хост для полей-коллекций.
/// DataContext = <see cref="ViewModels.CollectionFieldViewModel"/>.
/// Шаблон элемента выбирается Avalonia автоматически по типу данных
/// из <see cref="App.axaml"/> DataTemplates.
/// </summary>
public partial class CollectionField : UserControl
{
    public CollectionField()
    {
        InitializeComponent();
    }
}
