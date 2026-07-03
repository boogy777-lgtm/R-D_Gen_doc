using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Пара «термин — определение» для раздела терминов.
/// </summary>
public partial class TermPair : ObservableObject
{
    /// <summary>
    /// Gets or sets идентификатор строки в SQLite. 0 — новая строка.
    /// </summary>
    [ObservableProperty]
    private long id;

    /// <summary>
    /// Gets or sets порядковый номер в списке.
    /// </summary>
    [ObservableProperty]
    private int position;

    /// <summary>
    /// Gets or sets термин.
    /// </summary>
    [ObservableProperty]
    private string term = string.Empty;

    /// <summary>
    /// Gets or sets определение термина.
    /// </summary>
    [ObservableProperty]
    private string definition = string.Empty;
}
