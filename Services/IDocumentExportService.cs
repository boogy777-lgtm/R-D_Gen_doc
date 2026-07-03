using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис экспорта документа в DOCX. Скрывает координацию профиля шаблона,
/// сериализацию тела в Markdown и сборку OpenXML.
/// </summary>
public interface IDocumentExportService
{
    /// <summary>
    /// Экспортирует документ в DOCX, используя профиль фабричного шаблона.
    /// Сервис конвертирует <see cref="DocumentBody"/> в Markdown и передаёт
    /// его генератору Word, который не зависит от UI или структурированной модели.
    /// </summary>
    /// <param name="metadata">Метаданные (для профиля DOCX).</param>
    /// <param name="body">Структурированное тело документа.</param>
    /// <param name="path">Полный путь к выходному .docx-файлу.</param>
    Task ExportDocxAsync(DocumentMetadata metadata, DocumentBody body, string path);
}
