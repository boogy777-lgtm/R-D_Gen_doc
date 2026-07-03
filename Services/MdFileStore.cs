using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Файловое хранилище: каждый документ — отдельный .md файл в папке docs/.
/// Obsidian открывает папку как vault и видит все документы.
/// Реализует <see cref="IMdFileStore"/> для инжекции через DI/конструктор.
/// </summary>
public class MdFileStore : IMdFileStore
{
    private readonly string docsDir;

    public MdFileStore(string? docsDir = null)
    {
        this.docsDir = docsDir ?? Path.Combine(ExeDirectory, "docs");
    }

    public string DocsDirectory => docsDir;

    /// <summary>
    /// Gets директория .exe (работает в single-file режиме).
    /// </summary>
    private static string ExeDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath!)
        ?? AppContext.BaseDirectory;

    /// <summary>
    /// Сохраняет документ как .md файл (YAML frontmatter + body).
    /// Markdown — единый источник правды: DocumentBodyToMarkdown.Convert(metadata, body)
    /// сам подбирает шаблон и применяет Placeholder/TemplateDefault к пустым полям.
    /// Никаких условных веток "для сохранения" vs "для preview" нет.
    /// </summary>
    public Task SaveAsync(DocumentMetadata metadata, DocumentBody body)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(docsDir);

            var markdown = MarkdownService.Compose(
                metadata,
                DocumentBodyToMarkdown.Convert(metadata, body));

            var fileName = SanitizeFileName(metadata.DocId) + ".md";
            var path = Path.Combine(docsDir, fileName);
            File.WriteAllText(path, markdown, System.Text.Encoding.UTF8);
        });
    }

    /// <summary>
    /// Загружает документ из .md файла по doc_id.
    /// </summary>
    public Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadAsync(string docId)
    {
        return Task.Run(() =>
        {
            var fileName = SanitizeFileName(docId) + ".md";
            var path = Path.Combine(docsDir, fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            var text = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var (metadata, mdBody) = MarkdownService.Parse(text);

            var body = DocumentBodyParser.Parse(mdBody);
            return ((DocumentMetadata, DocumentBody)?)(metadata, body);
        });
    }

    /// <summary>
    /// Возвращает список всех doc_id (из имён .md файлов).
    /// </summary>
    public Task<IReadOnlyList<string>> ListDocIdsAsync()
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(docsDir))
            {
                return Array.Empty<string>() as IReadOnlyList<string>;
            }

            var files = Directory.GetFiles(docsDir, "*.md");
            return files
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderByDescending(f => new FileInfo(Path.Combine(docsDir, f + ".md")).LastWriteTimeUtc)
                .ToList() as IReadOnlyList<string>;
        });
    }

    /// <summary>
    /// Возвращает краткие сводки всех документов.
    /// </summary>
    public Task<IReadOnlyList<DocumentSummary>> ListSummariesAsync()
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(docsDir))
            {
                return Array.Empty<DocumentSummary>() as IReadOnlyList<DocumentSummary>;
            }

            var result = new List<DocumentSummary>();
            foreach (var file in Directory.GetFiles(docsDir, "*.md"))
            {
                try
                {
                    var text = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    var (meta, _) = MarkdownService.Parse(text);
                    var fi = new FileInfo(file);
                    result.Add(new DocumentSummary(
                        Id: 0,
                        DocId: meta.DocId,
                        Title: meta.Title,
                        DocType: meta.DocType,
                        Revision: meta.Revision,
                        Status: meta.Status,
                        EffectiveDate: meta.EffectiveDate,
                        UpdatedAt: fi.LastWriteTimeUtc));
                }
                catch
                {
                    // Пропускаем битые .md файлы.
                }
            }

            return result.OrderByDescending(s => s.UpdatedAt).ToList() as IReadOnlyList<DocumentSummary>;
        });
    }

    /// <summary>
    /// Удаляет .md файл по doc_id.
    /// </summary>
    public Task DeleteAsync(string docId)
    {
        return Task.Run(() =>
        {
            var fileName = SanitizeFileName(docId) + ".md";
            var path = Path.Combine(docsDir, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>
    /// Заменяет недопустимые символы в имени файла.
    /// </summary>
    private static string SanitizeFileName(string docId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Join("_", docId.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(safe) ? "untitled" : safe;
    }
}
