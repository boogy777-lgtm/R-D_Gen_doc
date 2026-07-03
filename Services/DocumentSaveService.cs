using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <inheritdoc />
public sealed class DocumentSaveService : IDocumentSaveService
{
    /// <inheritdoc />
    public Task<long> SaveAsync(DocumentMetadata metadata, DocumentBody body)
        => DocumentRepository.SaveAsync(metadata, body);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListAllDocIdsAsync()
        => await DocumentRepository.ListAllDocIdsAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentSummary>> ListAllSummariesAsync()
        => await DocumentRepository.ListAllSummariesAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadByDocIdAsync(string docId)
        => await DocumentRepository.LoadByDocIdAsync(docId).ConfigureAwait(false);

    /// <inheritdoc />
    public Task DeleteAsync(long id)
        => DocumentRepository.DeleteAsync(id);
}
