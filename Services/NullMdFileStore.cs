using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// No-op реализация <see cref="IMdFileStore"/> для unit-тестов
/// и для parameterless-конструкторов <c>MainWindowViewModel</c> /
/// <c>DatabaseTabViewModel</c>, где нежелательно трогать реальную папку docs/.
/// Возвращает пустые списки и «отмену» для операций записи/удаления.
/// </summary>
public sealed class NullMdFileStore : IMdFileStore
{
    /// <summary>
    /// Не используется реальной файловой системой; для тестов достаточно
    /// стабильного in-memory пути, чтобы <see cref="DocumentsBaseGenerator"/>
    /// не падал, если его случайно вызовут.
    /// </summary>
    public string DocsDirectory => System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "AvaloniaApp_NullMdFileStore_docs");

    public Task SaveAsync(DocumentMetadata metadata, DocumentBody body) => Task.CompletedTask;

    public Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadAsync(string docId) =>
        Task.FromResult<(DocumentMetadata Metadata, DocumentBody Body)?>(null);

    public Task<IReadOnlyList<string>> ListDocIdsAsync() =>
        Task.FromResult<IReadOnlyList<string>>(System.Array.Empty<string>());

    public Task<IReadOnlyList<DocumentSummary>> ListSummariesAsync() =>
        Task.FromResult<IReadOnlyList<DocumentSummary>>(System.Array.Empty<DocumentSummary>());

    public Task DeleteAsync(string docId) => Task.CompletedTask;
}
