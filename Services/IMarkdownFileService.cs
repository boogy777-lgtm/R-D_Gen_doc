using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис чтения/записи Markdown-документов. Инкапсулирует файловые операции,
/// парсинг frontmatter и структурированного тела, а также инициализацию starter-rows.
/// </summary>
public interface IMarkdownFileService
{
    /// <summary>
    /// Читает .md-файл, разбирает frontmatter и тело, гарантирует обязательные поля.
    /// </summary>
    /// <param name="path">Полный путь к файлу.</param>
    /// <returns>Кортеж (метаданные, тело) с уже инициализированными starter-rows.</returns>
    Task<(DocumentMetadata Metadata, DocumentBody Body)> OpenAsync(string path);

    /// <summary>
    /// Компонует Markdown (frontmatter + тело) и записывает в файл как UTF-8.
    /// </summary>
    /// <param name="path">Полный путь к файлу (включая расширение .md).</param>
    /// <param name="metadata">Метаданные для frontmatter.</param>
    /// <param name="body">Тело документа.</param>
    Task SaveAsync(string path, DocumentMetadata metadata, DocumentBody body);
}

/// <summary>
/// Результат операции открытия Markdown-файла.
/// </summary>
/// <param name="Path">Полный путь к прочитанному файлу.</param>
/// <param name="Metadata">Распарсенные YAML-метаданные из frontmatter.</param>
/// <param name="Body">Распарсенное структурированное тело документа.</param>
public sealed record OpenedMarkdownFile(string Path, DocumentMetadata Metadata, DocumentBody Body);
