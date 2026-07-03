using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Координатор открытия / создания / загрузки документов. Вынесен из
/// <c>MainWindowViewModel</c> (бывшие <c>OpenFileAsync</c>,
/// <c>LoadFromDatabaseAsync</c>, <c>OpenDocumentFromDatabaseAsync</c>, <c>NewDocument</c>).
/// </summary>
/// <remarks>
/// <para>Зависимости (всё через конструктор, без DI-контейнера):</para>
/// <list type="bullet">
///   <item><see cref="IMainWindowState"/> — проброс метаданных, тела, статус-бара;</item>
///   <item><see cref="IMarkdownFileService"/> — чтение .md-файлов;</item>
///   <item><see cref="IDialogService"/> — диалоги открытия и сообщений об ошибке;</item>
///   <item><see cref="IMdFileStore"/> — файловое хранилище документов (LoadAsync / ListDocIdsAsync);</item>
///   <item><see cref="TemplateCatalog"/> — каталог фабричных шаблонов для <c>NewDocument</c>;</item>
///   <item><see cref="IFormsCatalog"/> — каталог FormTemplate (DIP).</item>
/// </list>
/// </remarks>
public sealed class DocumentOpenCoordinator
{
    private readonly IMainWindowState state;
    private readonly IMarkdownFileService markdownFileService;
    private readonly IDialogService dialogService;
    private readonly IMdFileStore mdStore;
    private readonly TemplateCatalog templateCatalog;
    private readonly IFormsCatalog formsCatalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentOpenCoordinator"/> class.
    /// </summary>
    public DocumentOpenCoordinator(
        IMainWindowState state,
        IMarkdownFileService markdownFileService,
        IDialogService dialogService,
        IMdFileStore mdStore,
        TemplateCatalog? templateCatalog = null,
        IFormsCatalog? formsCatalog = null)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.markdownFileService = markdownFileService
            ?? throw new ArgumentNullException(nameof(markdownFileService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.mdStore = mdStore ?? throw new ArgumentNullException(nameof(mdStore));
        this.templateCatalog = templateCatalog ?? new TemplateCatalog();
        this.formsCatalog = formsCatalog ?? new FormsCatalog();
    }

    /// <summary>
    /// Открывает .md-файл через диалог <see cref="IDialogService.OpenMarkdownFileAsync"/>;
    /// записывает результат в <see cref="IMainWindowState.Metadata"/>,
    /// <see cref="IMainWindowState.DocumentBody"/>, <see cref="IMainWindowState.ActiveTabIndex"/>.
    /// </summary>
    public async Task OpenFileAsync()
    {
        var path = await dialogService.OpenMarkdownFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await SafeExecutor.RunAsync(
            async () =>
            {
                var (metadata, body) = await markdownFileService.OpenAsync(path);

                ApplyLoadedDocument(metadata, body);
                state.CurrentDocumentId = 0;
                state.StatusMessage = $"Открыт: {Path.GetFileName(path)}";
                await dialogService.ShowSuccessAsync(
                    "Документ открыт",
                    $"Markdown-файл успешно загружен:\n{Path.GetFileName(path)}",
                    path);
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка открытия: {ex.Message}",
                DialogMessage: ex => $"Не удалось открыть файл:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => state.StatusMessage = msg,
            extraLog: ex => Console.Error.WriteLine($"[{nameof(OpenFileAsync)}] {ex}"));
    }

    /// <summary>
    /// Показывает список документов из <see cref="IMdFileStore"/> через
    /// диалог выбора и открывает выбранный документ через <see cref="OpenDocumentFromDatabaseAsync"/>.
    /// </summary>
    public async Task LoadFromDatabaseAsync()
    {
        IReadOnlyList<string> docIds;

        // Сохраняем историческое поведение: ошибка показывается через диалог и
        // StatusMessage, но без Debug-трассировки.
        try
        {
            docIds = await mdStore.ListDocIdsAsync();
        }
        catch (Exception ex)
        {
            state.StatusMessage = $"Ошибка загрузки списка документов: {ex.Message}";
            await dialogService.ShowMessageAsync("Ошибка", $"Не удалось получить список документов:\n{ex.Message}");
            return;
        }

        if (docIds.Count == 0)
        {
            state.StatusMessage = "В папке docs/ нет документов";
            return;
        }

        var summaries = docIds
            .Select(id => new TemplateSummary { Code = id, DisplayName = id, Level = 0 })
            .ToList();

        var selectedId = await dialogService.ShowNewDocumentDialogAsync(summaries);
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            return;
        }

        await OpenDocumentFromDatabaseAsync(selectedId);
    }

    /// <summary>
    /// Загружает документ из <see cref="IMdFileStore"/> по <paramref name="docId"/>;
    /// пишет ошибки в <see cref="IMainWindowState.StatusMessage"/>.
    /// </summary>
    /// <param name="docId">Шифр документа (имя .md-файла без расширения).</param>
    public async Task OpenDocumentFromDatabaseAsync(string docId)
    {
        if (string.IsNullOrWhiteSpace(docId))
        {
            state.StatusMessage = "Код документа не указан";
            return;
        }

        await SafeExecutor.RunAsync(
            async () =>
            {
                var result = await mdStore.LoadAsync(docId);
                if (result == null)
                {
                    state.StatusMessage = $"Документ {docId} не найден";
                    return;
                }

                ApplyLoadedDocument(result.Value.Metadata, result.Value.Body);
                state.ActiveTabIndex = 0;
                state.StatusMessage = $"Загружен документ: {docId}";
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка загрузки документа: {ex.Message}",
                DialogMessage: ex => $"Не удалось загрузить документ:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => state.StatusMessage = msg);
    }

    /// <summary>
    /// Создаёт новый документ указанного типа через <see cref="TemplateCatalog"/>
    /// и стартовые коллекции <see cref="DocumentBodyFactory"/>. Пишет результат
    /// в <see cref="IMainWindowState"/>.
    /// </summary>
    /// <param name="docTypeCode">Код типа документа (например, <c>ST</c>).</param>
    /// <param name="createDefaultMetadata">
    /// Фабрика дефолтных метаданных — VM создаёт их через
    /// <c>CreateDefaultMetadata(docTypeCode, definition)</c> с собственным
    /// дополнительным заполнением (Title, EffectiveDate, Revision, шаблонные дефолты).
    /// </param>
    /// <param name="onLoaded">
    /// Колбэк после установки <see cref="IMainWindowState.Metadata"/> +
    /// <see cref="IMainWindowState.DocumentBody"/> — VM использует его, чтобы
/// сгенерировать DocId и обновить сервисные поля (CurrentDocumentId,
    /// CurrentBasisCardId, HasUnsavedChanges, StatusMessage).
    /// </param>
    public void NewDocument(
        string docTypeCode,
        Func<string, TemplateDefinition?, DocumentMetadata> createDefaultMetadata,
        Action onLoaded)
    {
        ArgumentNullException.ThrowIfNull(createDefaultMetadata);
        ArgumentNullException.ThrowIfNull(onLoaded);

        var definition = templateCatalog.GetByDocType(docTypeCode);
        var meta = createDefaultMetadata(docTypeCode, definition);
        var template = formsCatalog.GetTemplate(docTypeCode);

        var (starterTerms, starterChangeLog) = DocumentBodyFactory.CreateStarterCollections();

        var body = new DocumentBody
        {
            Sections = new ObservableCollection<BodySection>(template.StarterSections),
            Terms = starterTerms,
            ChangeLog = starterChangeLog,
        };

        DocumentBodyFactory.EnsureRequiredFields(body, meta);

        // Порядок важен: Metadata ДО DocumentBody (см. исторический комментарий в VM).
        // ActiveTabIndex НЕ трогаем — VM решает, нужно ли переключать вкладку.
        state.Metadata = meta;
        state.DocumentBody = body;

        onLoaded();
    }

    /// <summary>
    /// Применяет загруженный документ к состоянию VM (общий хвост для
    /// <see cref="OpenFileAsync"/> и <see cref="OpenDocumentFromDatabaseAsync"/>):
    /// пишет <paramref name="metadata"/> / <paramref name="body"/> в state,
    /// обеспечивает стартовые поля, подбирает <see cref="IMainWindowState.FormTemplate"/>
    /// и сбрасывает <see cref="IMainWindowState.HasUnsavedChanges"/>.
    /// ActiveTabIndex НЕ трогаем — оригинальная семантика:
    /// <c>OpenFileAsync</c> оставлял пользователя на текущей вкладке,
    /// <c>OpenDocumentFromDatabaseAsync</c> переключал на вкладку «Документ».
    /// </summary>
    private void ApplyLoadedDocument(DocumentMetadata metadata, DocumentBody body)
    {
        DocumentBodyFactory.EnsureRequiredFields(body, metadata);

        state.Metadata = metadata;
        state.DocumentBody = body;
        state.FormTemplate = formsCatalog.GetTemplate(metadata.DocType);
        state.HasUnsavedChanges = false;
    }
}
