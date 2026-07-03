using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Файловое хранилище документов в формате Markdown (YAML frontmatter + body).
/// Каждый документ — отдельный .md файл в папке docs/, которую Obsidian
/// открывает как vault и индексирует автоматически.
/// </summary>
/// <remarks>
/// DIP-граница между потребителями (ViewModel) и конкретной файловой реализацией.
/// Позволяет подменять хранилище (например, на in-memory) в тестах и preview,
/// не затрагивая код ViewModel.
/// </remarks>
public interface IMdFileStore
{
    /// <summary>
    /// Абсолютный путь к папке, в которой хранятся .md файлы документов.
    /// Используется <see cref="DocumentsBaseGenerator"/> для генерации
    /// служебных .base-файлов Obsidian.
    /// </summary>
    string DocsDirectory { get; }

    /// <summary>
    /// Сохраняет документ как .md файл (YAML frontmatter + body).
    /// </summary>
    Task SaveAsync(DocumentMetadata metadata, DocumentBody body);

    /// <summary>
    /// Загружает документ из .md файла по doc_id, либо <c>null</c>, если файл не найден.
    /// </summary>
    Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadAsync(string docId);

    /// <summary>
    /// Возвращает список всех doc_id (из имён .md файлов) в порядке убывания
    /// времени последней модификации.
    /// </summary>
    Task<IReadOnlyList<string>> ListDocIdsAsync();

    /// <summary>
    /// Возвращает краткие сводки всех документов в порядке убывания
    /// времени последней модификации.
    /// </summary>
    Task<IReadOnlyList<DocumentSummary>> ListSummariesAsync();

    /// <summary>
    /// Удаляет .md файл по doc_id; no-op, если файл не существует.
    /// </summary>
    Task DeleteAsync(string docId);
}
