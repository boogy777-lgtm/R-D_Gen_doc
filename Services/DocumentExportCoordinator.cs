using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Координатор экспорта и сохранения документов. Вынесен из <c>MainWindowViewModel</c>
/// (бывшие <c>ExportMarkdownAsync</c> / <c>ExportDocxAsync</c> / <c>SaveToDatabaseAsync</c>).
/// </summary>
/// <remarks>
/// <para>Зависимости (всё через конструктор, без DI-контейнера):</para>
/// <list type="bullet">
///   <item><see cref="IMainWindowState"/> — проброс метаданных, тела, статус-бара и preview;</item>
///   <item><see cref="IDocumentExportService"/> — DOCX-генерация;</item>
///   <item><see cref="IMarkdownFileService"/> — экспорт в .md;</item>
///   <item><see cref="IDialogService"/> — диалоги сохранения и сообщений об ошибке;</item>
///   <item><see cref="IMdFileStore"/> — файловое хранилище .md (SaveToDatabase).</item>
/// </list>
/// <para>Все методы идемпотентны и не бросают наружу — ошибки логируются в
/// <c>Debug</c>/<c>Console.Error</c> и показываются пользователю через
/// <see cref="IDialogService.ShowMessageAsync"/>.</para>
/// </remarks>
public sealed class DocumentExportCoordinator
{
    private readonly IMainWindowState state;
    private readonly IDocumentExportService exportService;
    private readonly IMarkdownFileService markdownFileService;
    private readonly IDialogService dialogService;
    private readonly IMdFileStore mdStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExportCoordinator"/> class.
    /// </summary>
    /// <param name="state">Состояние главного окна (VM-фасад).</param>
    /// <param name="exportService">Сервис экспорта в DOCX.</param>
    /// <param name="markdownFileService">Сервис чтения/записи .md-файлов.</param>
    /// <param name="dialogService">Абстракция диалогов.</param>
    /// <param name="mdStore">Файловое хранилище документов.</param>
    public DocumentExportCoordinator(
        IMainWindowState state,
        IDocumentExportService exportService,
        IMarkdownFileService markdownFileService,
        IDialogService dialogService,
        IMdFileStore mdStore)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        this.markdownFileService = markdownFileService
            ?? throw new ArgumentNullException(nameof(markdownFileService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.mdStore = mdStore ?? throw new ArgumentNullException(nameof(mdStore));
    }

    /// <summary>
    /// Имя файла по умолчанию для Markdown-экспорта:
    /// <c>{DocId}.md</c>, либо <c>document.md</c>, если DocId пустой.
    /// </summary>
    public static string DefaultMarkdownFileName(DocumentMetadata metadata) =>
        string.IsNullOrWhiteSpace(metadata.DocId) ? "document.md" : $"{metadata.DocId}.md";

    /// <summary>
    /// Имя файла по умолчанию для DOCX-экспорта:
    /// <c>{DocId}.docx</c>, либо <c>document.docx</c>, если DocId пустой.
    /// </summary>
    public static string DefaultDocxFileName(DocumentMetadata metadata) =>
        string.IsNullOrWhiteSpace(metadata.DocId) ? "document.docx" : $"{metadata.DocId}.docx";

    /// <summary>
    /// Сохраняет документ как .md в локальную папку docs/ через <see cref="IMdFileStore"/>
    /// и обновляет documents.base для Obsidian.
    /// </summary>
    public async Task SaveToDatabaseAsync()
    {
        if (state.DocumentBody == null)
        {
            state.StatusMessage = "Нет данных для сохранения";
            return;
        }

        if (state.Metadata == null)
        {
            state.StatusMessage = "Метаданные не инициализированы";
            return;
        }

        await SafeExecutor.RunAsync(
            async () =>
            {
                state.RefreshPreview();
                await mdStore.SaveAsync(state.Metadata, state.DocumentBody);
                DocumentsBaseGenerator.Generate(mdStore.DocsDirectory);

                state.HasUnsavedChanges = false;
                state.StatusMessage = $"Документ сохранён: {state.Metadata.DocId}.md";
                await dialogService.ShowSuccessAsync(
                    "Документ сохранён",
                    $"Файл «{state.Metadata.DocId}.md» записан в папку docs/.",
                    mdStore.DocsDirectory,
                    offerOpenInExplorer: false);
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка сохранения: {ex.Message}",
                DialogMessage: ex => $"Не удалось сохранить документ:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => state.StatusMessage = msg,
            extraLog: ex => Console.Error.WriteLine($"[{nameof(SaveToDatabaseAsync)}] {ex}"));
    }

    /// <summary>
    /// Экспортирует документ в Markdown через диалог сохранения.
    /// </summary>
    public async Task ExportMarkdownAsync()
    {
        if (state.DocumentBody == null)
        {
            state.StatusMessage = "Нет данных для экспорта";
            return;
        }

        if (state.Metadata == null)
        {
            state.StatusMessage = "Метаданные не инициализированы";
            return;
        }

        state.RefreshPreview();

        var defaultName = DefaultMarkdownFileName(state.Metadata);
        var path = await dialogService.SaveMarkdownFileAsync(defaultName);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await SafeExecutor.RunAsync(
            async () =>
            {
                await markdownFileService.SaveAsync(path, state.Metadata, state.DocumentBody);
                state.StatusMessage = $"Markdown сохранён: {Path.GetFileName(path)}";
                await dialogService.ShowSuccessAsync(
                    "Markdown сохранён",
                    $"Файл .md успешно записан:\n{Path.GetFileName(path)}",
                    path);
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка экспорта Markdown: {ex.Message}",
                DialogMessage: ex => $"Не удалось записать .md:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => state.StatusMessage = msg,
            extraLog: ex => Console.Error.WriteLine($"[{nameof(ExportMarkdownAsync)}] {ex}"));
    }

    /// <summary>
    /// Экспортирует документ в DOCX через диалог сохранения.
    /// </summary>
    public async Task ExportDocxAsync()
    {
        if (state.DocumentBody == null)
        {
            state.StatusMessage = "Нет данных для экспорта";
            return;
        }

        if (state.Metadata == null)
        {
            state.StatusMessage = "Метаданные не инициализированы";
            return;
        }

        state.RefreshPreview();

        var defaultName = DefaultDocxFileName(state.Metadata);
        var path = await dialogService.SaveDocxFileAsync(defaultName);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await SafeExecutor.RunAsync(
            async () =>
            {
                await exportService.ExportDocxAsync(state.Metadata, state.DocumentBody, path);
                state.StatusMessage = $"Экспортирован DOCX: {Path.GetFileName(path)}";
                await dialogService.ShowSuccessAsync(
                    "Документ экспортирован",
                    $"DOCX-файл успешно создан:\n{Path.GetFileName(path)}",
                    path);
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка экспорта: {ex.Message}",
                DialogMessage: ex => $"Не удалось создать DOCX:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => state.StatusMessage = msg,
            extraLog: ex => Console.Error.WriteLine($"[{nameof(ExportDocxAsync)}] {ex}"));
    }
}
