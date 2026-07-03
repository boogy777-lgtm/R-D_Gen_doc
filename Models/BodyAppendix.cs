using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Приложение к документу. Каждое приложение — на отдельной странице.
/// Нумерация: «Приложение 1», «Приложение 2», и т.д. — формируется автоматически по Position.
/// При создании автоматически получает одну подсекцию-таблицу.
/// Номер таблицы («Таблица № 1», «Таблица № 2») отображается автоматически через конвертер Position → 1-based.
/// </summary>
public partial class BodyAppendix : ObservableObject
{
    /// <summary>
    /// Gets or sets идентификатор приложения в SQLite. 0 — новое.
    /// </summary>
    [ObservableProperty]
    private long id;

    /// <summary>
    /// Gets or sets порядковый номер (1-based).
    /// </summary>
    [ObservableProperty]
    private int position;

    /// <summary>
    /// Gets or sets заголовок приложения (опционально).
    /// Например: «Карта процесса» или «Матрица RACI».
    /// </summary>
    [ObservableProperty]
    private string title = string.Empty;

    /// <summary>
    /// Gets or sets содержимое приложения в Markdown.
    /// </summary>
    [ObservableProperty]
    private string body = string.Empty;

    /// <summary>
    /// Подсекции внутри приложения (таблицы). Номер каждой таблицы отображается
    /// через <see cref="Position"/> (0-based → 1-based через конвертер).
    /// При создании нового приложения автоматически добавляется одна подсекция.
    /// </summary>
    public ObservableCollection<BodySection> Sections { get; set; } = new();
}
