using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Раздел центральной части документа. Поддерживает вложенность:
/// каждый раздел может содержать подразделы (Children).
/// Пример иерархии: «4 Описание процесса» → «4.1 Назначение» → «4.1.1 Этапы».
///
/// Все свойства — ObservableProperty, чтобы UI в реальном времени обновлял
/// позицию/уровень после перестановок через DocumentChangeEngine.
/// </summary>
public partial class BodySection : ObservableObject
{
    /// <summary>
    /// Gets or sets идентификатор раздела в SQLite. 0 — новый раздел.
    /// </summary>
    [ObservableProperty]
    private long id;

    /// <summary>
    /// Gets or sets порядковый номер среди соседей (0-based, обновляется Engine.RenumberSections).
    /// Используется для UI-сортировки и для построения <see cref="HierarchyPath"/>.
    /// </summary>
    [ObservableProperty]
    private int position;

    /// <summary>
    /// Иерархический путь секции в документе: «4», «4.1», «4.1.2».
    /// Рассчитывается в <see cref="Services.DocumentChangeEngine.RenumberSections"/>
    /// на основе Position среди соседей и Level.
    /// Используется для UI-индикатора и для рендеринга в DOCX.
    /// Пример: для верхнего уровня «4», «5», «6»; для подразделов — «4.1», «4.1.1».
    /// </summary>
    [ObservableProperty]
    private string hierarchyPath = string.Empty;

    /// <summary>
    /// Gets or sets заголовок раздела. Например: «Описание процесса».
    /// В MD/DOCX префикс номера («4.1») вычисляется автоматически по вложенности.
    /// </summary>
    [ObservableProperty]
    private string title = string.Empty;

    /// <summary>
    /// Gets or sets содержимое раздела в Markdown.
    /// </summary>
    [ObservableProperty]
    private string body = string.Empty;

    /// <summary>
    /// Gets or sets уровень вложенности (1 = верхний уровень, 2 = подраздел, и т.д.).
    /// Автоматически пересчитывается из глубины дерева при сохранении в DOCX.
    /// </summary>
    [ObservableProperty]
    private int level = 1;

    /// <summary>
    /// Gets or sets идентификатор родительского раздела (0 = верхний уровень).
    /// </summary>
    [ObservableProperty]
    private long parentId;

    /// <summary>
    /// Gets or sets сквозной номер таблицы в документе (1-based),
    /// считается по всем приложениям по порядку.
    /// Рассчитывается в <see cref="Services.DocumentChangeEngine.RecalculateGlobalTableIndex"/>
    /// при изменении коллекции приложений или подсекций.
    /// Используется UI и рендером для отображения «Таблица N» без хардкода.
    /// Подсекции верхнего уровня (вне приложений) имеют <c>0</c> — не таблица.
    /// </summary>
    [ObservableProperty]
    private int globalTableIndex;

    /// <summary>
    /// Gets or sets вложенные подразделы.
    /// </summary>
    public ObservableCollection<BodySection> Children { get; set; } = new();
}
