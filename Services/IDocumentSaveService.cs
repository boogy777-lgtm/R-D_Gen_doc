using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис персистенции документов в SQLite. Маршрутизирует запросы к
/// <see cref="DocumentRepository"/> и возвращает доменные типы.
/// </summary>
public interface IDocumentSaveService
{
    /// <summary>
    /// Сохраняет документ в БД (insert/update по doc_id).
    /// </summary>
    /// <returns>Идентификатор записи.</returns>
    Task<long> SaveAsync(DocumentMetadata metadata, DocumentBody body);

    /// <summary>
    /// Возвращает список всех doc_id в БД, отсортированных по дате обновления (убывание).
    /// </summary>
    Task<IReadOnlyList<string>> ListAllDocIdsAsync();

    /// <summary>
    /// Возвращает краткие сводки всех документов в БД, отсортированных по дате обновления (убывание).
    /// </summary>
    Task<IReadOnlyList<DocumentSummary>> ListAllSummariesAsync();

    /// <summary>
    /// Загружает документ по его коду.
    /// </summary>
    Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadByDocIdAsync(string docId);

    /// <summary>
    /// Удаляет документ по идентификатору.
    /// </summary>
    Task DeleteAsync(long id);
}
