using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Структурированное тело документа, редактируемое через форму.
///
/// Все коллекции — <see cref="ObservableCollection{T}"/>, чтобы UI
/// (ItemsControl с x:CompileBindings=False) мог биндиться напрямую к оригиналам
/// и получать уведомления через <c>CollectionChanged</c>. Это устраняет
/// необходимость в копиях и ручной синхронизации между Body и UI.
/// </summary>
public class DocumentBody
{
    /// <summary>
    /// Gets or sets идентификатор документа в SQLite. 0 — ещё не сохранён.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets секция 1 — назначение документа.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets секция 2 — область применения.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets секция 3 — термины и определения.
    /// </summary>
    public ObservableCollection<TermPair> Terms { get; set; } = new();

    /// <summary>
    /// Gets or sets центральная часть документа — произвольные разделы.
    /// Поддерживается иерархия: верхний уровень здесь, вложенные — в
    /// <see cref="BodySection.Children"/> каждой секции.
    /// </summary>
    public ObservableCollection<BodySection> Sections { get; set; } = new();

    /// <summary>
    /// Gets or sets первичное обучение персонала.
    /// </summary>
    public string PrimaryTraining { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets повторное обучение персонала.
    /// </summary>
    public string RecurrentTraining { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets ссылка на родительский документ.
    /// </summary>
    public string ParentDocument { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets связанные документы.
    /// </summary>
    public ObservableCollection<StringItem> RelatedDocuments { get; set; } = new();

    /// <summary>
    /// Gets or sets история изменений.
    /// </summary>
    public ObservableCollection<ChangeLogRow> ChangeLog { get; set; } = new();

    /// <summary>
    /// Gets or sets приложения к документу. Каждое приложение печатается
    /// с новой страницы с заголовком «Приложение N».
    /// </summary>
    public ObservableCollection<BodyAppendix> Appendices { get; set; } = new();

    /// <summary>
    /// Перезаписывает служебные поля всех строк ЛРИ значениями из метаданных.
    /// Domain-модель не форматирует Revision — вызывающая сторона (Engine) передаёт
    /// уже обрезанное значение через <see cref="DocumentChangeEngine"/>.
    /// </summary>
    public void SyncChangeLogFromMetadata(DocumentMetadata metadata, string formattedRevision)
    {
        if (metadata is null)
        {
            return;
        }

        foreach (var row in ChangeLog)
        {
            row.SyncFromMetadata(metadata, formattedRevision ?? string.Empty);
        }
    }
}
