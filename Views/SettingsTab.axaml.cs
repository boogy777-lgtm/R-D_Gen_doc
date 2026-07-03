namespace AvaloniaApp.Views;

/// <summary>
/// Вкладка "Настройки" с логически сгруппированными полями + подсказками
/// о соответствующем YAML-ключе для каждого поля.
///
/// Архитектурное правило: UserControl НЕ знает про Services или БД.
/// Все мутации — через MainWindowViewModel (DataContext).
/// Событие GotFocus поля номера документа обрабатывается через
/// Avalonia.Xaml.Behaviors EventTriggerBehavior, привязанный к команде VM.
/// </summary>
public partial class SettingsTab : Avalonia.Controls.UserControl
{
    public SettingsTab()
    {
        InitializeComponent();
    }
}
