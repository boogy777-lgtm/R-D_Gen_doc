using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using AvaloniaApp.Services;
using AvaloniaApp.Services.Defaults;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable, IMainWindowState, IDocumentLifecycleSink
{
    /// <summary>
    /// Индекс вкладки «MD Preview» (см. порядок TabItem в MainWindow.axaml).
    /// </summary>
    private const int PreviewTabIndex = 3;

    /// <summary>
    /// Сообщение статус-бара, которое выставляется после успешной
    /// hot-reload перезагрузки справочников из Obsidian.
    /// </summary>
    private const string ReferencesReloadedStatusMessage = "Справочники обновлены";

    private readonly IDialogService dialogService;
    private readonly IMarkdownFileService markdownFileService;
    private readonly IDocumentExportService exportService;
    private readonly IDocumentSaveService saveService;
    private readonly IMarkdownPreviewBuilder previewBuilder;
    private readonly IClipboardService clipboard;
    private readonly IFileSystemService fileSystem;
    private readonly IMdFileStore mdStore;
    private readonly TemplateCatalog templateCatalog;
    private readonly IReferenceHotReloader referenceHotReloader;
    private readonly IReferenceDataService referenceDataService;
    private readonly IFormsCatalog formsCatalog;
    private readonly IMetadataCatalog metadataCatalog;
    private readonly IFieldViewModelRegistry fieldViewModelRegistry;

    /// <summary>
    /// Координатор lifecycle: Engine ↔ PropertyChanged ↔ Form/ChangeLog-ViewModel.
    /// Инкапсулирует всю «plumbing»-логику, оставляя VM как тонкий binding-адаптер.
    /// </summary>
    private readonly DocumentLifecycleCoordinator lifecycleCoordinator;

    /// <summary>
    /// Координатор экспорта: <c>ExportMarkdownAsync</c> / <c>ExportDocxAsync</c> /
    /// <c>SaveToDatabaseAsync</c>.
    /// </summary>
    private readonly DocumentExportCoordinator exportCoordinator;

    /// <summary>
    /// Координатор открытия: <c>OpenFileAsync</c> / <c>LoadFromDatabaseAsync</c> /
    /// <c>OpenDocumentFromDatabaseAsync</c> / <c>NewDocument</c>.
    /// </summary>
    private readonly DocumentOpenCoordinator openCoordinator;

    /// <summary>
    /// Координатор синхронизации метаданных: <c>SelectedDocType</c> ↔ <c>DocType</c>,
    /// <c>DepartmentCode</c> → <c>DepartmentName</c>, child departments, DocId.
    /// </summary>
    private readonly MetadataSyncCoordinator metadataSyncCoordinator;

    /// <summary>
    /// Сервис popup-диалога добавления строки ЛРИ. Делегируется координатору
    /// (sink-контракт).
    /// </summary>
    private IChangeLogPromptService? changeLogPromptService;

    private CancellationTokenSource? previewDebounceCts;
    private bool docIdManuallyEdited;

    /// <summary>
    /// Подавляет взаимную синхронизацию <see cref="SelectedDocType"/> ↔ <see cref="Metadata"/>.
    /// </summary>
    private bool suppressDocTypeSync;

    // --- Существующие свойства ---
    [ObservableProperty]
    private DocumentMetadata metadata = new();

    [ObservableProperty]
    private ObservableCollection<DocType> docTypes = new();

    [ObservableProperty]
    private DocType? selectedDocType;

    [ObservableProperty]
    private ObservableCollection<Department> departments = new();

    /// <summary>
    /// Полный список подразделений (включая дочерние) — для внутренних lookup-ов.
    /// </summary>
    private IReadOnlyList<Department> allDepartments = Array.Empty<Department>();

    [ObservableProperty]
    private string departmentCode = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> approverRoles = new();

    [ObservableProperty]
    private ObservableCollection<string> statuses = new();

    [ObservableProperty]
    private ObservableCollection<string> childDepartments = new();

    [ObservableProperty]
    private string seedStatus = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Готов. Откройте Markdown-файл или создайте документ из шаблона.";

    [ObservableProperty]
    private long currentBasisCardId;

    [ObservableProperty]
    private string currentBasisCardLabel = string.Empty;

    [ObservableProperty]
    private DocumentBody? documentBody;

    [ObservableProperty]
    private FormTemplate? formTemplate;

    [ObservableProperty]
    private int activeTabIndex;

    [ObservableProperty]
    private string markdownPreview = string.Empty;

    [ObservableProperty]
    private long currentDocumentId;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    [ObservableProperty]
    private DatabaseTabViewModel databaseTabViewModel = null!;

    [ObservableProperty]
    private SuccessOverlayViewModel? successOverlay;

    [ObservableProperty]
    private bool isSuccessOverlayVisible;

    [ObservableProperty]
    private int appendicesCount;

    [ObservableProperty]
    private CollectionFieldViewModel<ChangeLogRow>? changeLogFieldViewModel;

    [ObservableProperty]
    private FormViewModel? formViewModel;

    // --- Справочники для опциональных сегментов кода (SettingsTab) ---
    [ObservableProperty]
    private ObservableCollection<CodeOption> disciplineCodes = new(); // инициализируется в LoadReferenceDataAsync

    [ObservableProperty]
    private ObservableCollection<CodeOption> languageCodes = new()
    {
        new(string.Empty, "Русский (без кода)"),
        new("Eng", "Английский (Eng)"),
        new("Rus", "Русский (явный Rus)"),
        new("Eng-Rus", "Английский + Русский (Eng-Rus)"),
    };

    // --- Состояние ЛРИ (badge + предупреждение) ---
    [ObservableProperty]
    private string changeLogBadgeText = "ЛРИ: нет записей";

    [ObservableProperty]
    private bool isChangeLogWarningVisible;

    [ObservableProperty]
    private string changeLogWarningText = string.Empty;

    public MainWindowViewModel(
        IDialogService dialogService,
        IMarkdownFileService markdownFileService,
        IDocumentExportService exportService,
        IDocumentSaveService saveService,
        IMarkdownPreviewBuilder previewBuilder,
        IClipboardService clipboard,
        IFileSystemService fileSystem,
        IMdFileStore mdStore,
        IReferenceDataService? referenceDataService = null,
        IFormsCatalog? formsCatalog = null,
        IMetadataCatalog? metadataCatalog = null,
        IFieldViewModelRegistry? fieldViewModelRegistry = null,
        TemplateCatalog? templateCatalog = null,
        IReferenceHotReloader? referenceHotReloader = null)
    {
        this.dialogService = dialogService
            ?? throw new ArgumentNullException(nameof(dialogService));
        this.markdownFileService = markdownFileService
            ?? throw new ArgumentNullException(nameof(markdownFileService));
        this.exportService = exportService
            ?? throw new ArgumentNullException(nameof(exportService));
        this.saveService = saveService
            ?? throw new ArgumentNullException(nameof(saveService));
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.clipboard = clipboard
            ?? throw new ArgumentNullException(nameof(clipboard));
        this.fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
        this.mdStore = mdStore
            ?? throw new ArgumentNullException(nameof(mdStore));
        this.referenceDataService = referenceDataService ?? new ReferenceDataService();
        this.formsCatalog = formsCatalog ?? new FormsCatalog();
        this.metadataCatalog = metadataCatalog ?? new MetadataCatalog();
        this.fieldViewModelRegistry = fieldViewModelRegistry ?? new FieldViewModelRegistry(
            new IFieldViewModelFactory[]
            {
                new SingleFieldViewModelFactory(),
                new CollectionFieldViewModelFactory(),
                new TreeFieldViewModelFactory(),
            });
        this.templateCatalog = templateCatalog ?? new TemplateCatalog();
        this.referenceHotReloader = referenceHotReloader ?? new NullReferenceHotReloader();

        this.lifecycleCoordinator = new DocumentLifecycleCoordinator(
            sink: this,
            dialogService: dialogService,
            previewBuilder: previewBuilder,
            onMetadataPropertyChangedForDocId: _ => TryAutoProposeDocId(),
            createChangeLogFieldViewModel: CreateChangeLogFieldViewModel,
            buildFormFieldViewModels: BuildFormFieldViewModels);

        this.exportCoordinator = new DocumentExportCoordinator(
            this,
            exportService,
            markdownFileService,
            dialogService,
            mdStore);
        this.openCoordinator = new DocumentOpenCoordinator(
            this,
            markdownFileService,
            dialogService,
            mdStore,
            templateCatalog,
            formsCatalog);
        this.metadataSyncCoordinator = new MetadataSyncCoordinator(
            getMetadata: () => Metadata,
            getAllDepartments: () => allDepartments,
            getSelectedDocType: () => SelectedDocType,
            setSelectedDocType: value => SelectedDocType = value,
            setSuppressDocTypeSync: value => suppressDocTypeSync = value,
            setChildDepartments: value => ChildDepartments.Seed(value),
            getDepartmentCode: () => DepartmentCode,
            markDocIdManuallyEdited: () => docIdManuallyEdited = true,
            tryProposeDocId: _ => TryAutoProposeDocId());

        DatabaseTabViewModel = new DatabaseTabViewModel(
            dialogService,
            exportService,
            mdStore,
            OpenDocumentFromDatabaseAsync);
    }

    public MainWindowViewModel()
        : this(
            new NullDialogService(),
            new MarkdownFileService(),
            new DocumentExportService(),
            new DocumentSaveService(),
            new MarkdownPreviewBuilder(),
            new NullClipboardService(),
            new NullFileSystemService(),
            new NullMdFileStore(),
            null,
            null,
            null,
            null,
            null,
            null)
    {
    }

    /// <summary>
    /// Конструктор для тестов: позволяет подменить только <see cref="IDialogService"/>,
    /// остальные сервисы — дефолтные Null*/System*-реализации.
    /// </summary>
    public MainWindowViewModel(IDialogService dialogServiceOverride)
        : this(
            dialogServiceOverride,
            new MarkdownFileService(),
            new DocumentExportService(),
            new DocumentSaveService(),
            new MarkdownPreviewBuilder(),
            new NullClipboardService(),
            new NullFileSystemService(),
            new NullMdFileStore(),
            null,
            null,
            null,
            null,
            null,
            null)
    {
    }

    // --- IDocumentLifecycleSink (explicit interface implementations) ---
    IChangeLogPromptService? IDocumentLifecycleSink.ChangeLogPromptService
    {
        get => changeLogPromptService;
        set => changeLogPromptService = value;
    }

    /// <summary>
    /// Gets текущий движок мутаций. Делегирует <see cref="DocumentLifecycleCoordinator.Engine"/>.
    /// </summary>
    public DocumentChangeEngine? Engine => lifecycleCoordinator.Engine;

    /// <summary>
    /// Показывает overlay-диалог успеха и асинхронно ждёт нажатия «ОК».
    /// </summary>
    public Task ShowSuccessOverlayAsync(string title, string message, string filePath, bool offerOpenInExplorer = true)
    {
        var overlay = new SuccessOverlayViewModel(clipboard, fileSystem)
        {
            Title = title,
            Message = message,
            FilePath = filePath,
            OfferOpenInExplorer = offerOpenInExplorer,
        };

        var tcs = new TaskCompletionSource();
        EventHandler okHandler = null!;
        okHandler = (_, _) =>
        {
            overlay.OkClicked -= okHandler;
            SuccessOverlay = null;
            IsSuccessOverlayVisible = false;
            tcs.TrySetResult();
        };
        overlay.OkClicked += okHandler;

        SuccessOverlay = overlay;
        IsSuccessOverlayVisible = true;
        return tcs.Task;
    }

    /// <summary>
    /// Инициализация при старте окна: справочники + новый документ ST по умолчанию.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadReferenceDataAsync();
        StartReferenceHotReload();
        NewDocument("ST");
    }

    /// <summary>
    /// Освобождает ресурсы VM: останавливает hot-reload watcher и lifecycle-координатор.
    /// </summary>
    public void Dispose()
    {
        SafeExecutor.RunAsync(
                () =>
                {
                    referenceHotReloader.OnChanged = null;
                    referenceHotReloader.Dispose();
                    lifecycleCoordinator.Dispose();
                    return Task.CompletedTask;
                },
                policy: ErrorPolicy.Silent(),
                extraLog: null)
            .GetAwaiter()
            .GetResult();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Открывает документ из БД по шифру.
    /// </summary>
    public Task OpenDocumentFromDatabaseAsync(string docId) =>
        openCoordinator.OpenDocumentFromDatabaseAsync(docId);

    /// <summary>
    /// Помечает DocId как отредактированный вручную.
    /// </summary>
    [RelayCommand]
    public void MarkDocIdAsManuallyEdited() => docIdManuallyEdited = true;

    /// <summary>
    /// Формирует список дисциплин для ComboBox «Дисциплина».
    /// </summary>
    private ObservableCollection<CodeOption> BuildDisciplineOptions()
    {
        var collection = new ObservableCollection<CodeOption>();
        foreach (var code in referenceDataService.DefaultDisciplinesFull())
        {
            var label = ReferenceData.DisciplineLabels.TryGetValue(code, out var l) ? l : code;
            collection.Add(new CodeOption(code, label));
        }

        return collection;
    }

    /// <summary>
    /// Создаёт ViewModel-ы полей формы по текущему шаблону и телу документа.
    /// Делегирует каждый дескриптор в <see cref="IFieldViewModelRegistry"/> —
    /// открыто для расширения через DI (OCP).
    /// </summary>
    private List<ViewModelBase> BuildFormFieldViewModels(FormTemplate? template, DocumentBody body, IEngineCore engine)
    {
        var vms = new List<ViewModelBase>();
        if (template is null)
        {
            return vms;
        }

        foreach (var descriptor in template.HeaderFields)
        {
            vms.Add(CreateFormFieldViewModel(descriptor, body, engine));
        }

        foreach (var descriptor in template.CenterFields)
        {
            vms.Add(CreateFormFieldViewModel(descriptor, body, engine));
        }

        foreach (var descriptor in template.TrailingFields)
        {
            vms.Add(CreateFormFieldViewModel(descriptor, body, engine));
        }

        return vms;
    }

    private ViewModelBase CreateFormFieldViewModel(FormFieldDescriptor descriptor, DocumentBody body, IEngineCore engine)
    {
        // Upcast от узкой роли (IEngineCore) к общему контракту (IDocumentChangeEngine).
        // DocumentChangeEngine реализует все ролевые интерфейсы (ISP), поэтому upcast безопасен.
        var fullEngine = engine as IDocumentChangeEngine
            ?? throw new InvalidOperationException(
                "Движок должен реализовывать IDocumentChangeEngine для построения FormViewModel.");

        return fieldViewModelRegistry.Create(descriptor, body, fullEngine);
    }

    /// <summary>
    /// Подписывает <see cref="referenceHotReloader"/> на callback перезагрузки
    /// и запускает наблюдение. Безопасно вызывать повторно (no-op).
    /// </summary>
    private void StartReferenceHotReload()
    {
        referenceHotReloader.OnChanged = OnReferenceFilesChangedAsync;
        referenceHotReloader.Start();
    }

    /// <summary>
    /// Callback, вызываемый из ThreadPool после debounce-окна при изменении
    /// любого <c>*.md</c> в <c>docs/_справочники/</c>. Маршалит перезагрузку
    /// на UI-поток через <see cref="Avalonia.Threading.Dispatcher"/>.
    /// </summary>
    private async Task OnReferenceFilesChangedAsync()
    {
        var dispatcher = Avalonia.Threading.Dispatcher.UIThread;
        if (!dispatcher.CheckAccess())
        {
            await dispatcher.InvokeAsync(ReloadReferencesFromUiThreadAsync);
            return;
        }

        await ReloadReferencesFromUiThreadAsync();
    }

    private async Task ReloadReferencesFromUiThreadAsync()
    {
        await SafeExecutor.RunAsync(
            async () =>
            {
                await LoadReferenceDataAsync();
                StatusMessage = ReferencesReloadedStatusMessage;
            },
            policy: ErrorPolicy.Silent(statusMessage: ex => $"Ошибка обновления справочников: {ex.Message}"),
            setStatusMessage: msg => StatusMessage = msg);
    }

    [RelayCommand]
    private async Task LoadReferenceDataAsync()
    {
        // LoadReferenceDataAsync пишет ошибку в SeedStatus (а не StatusMessage),
        // поэтому передаём кастомный error-handler через extraLog, который
        // подменяет и Source-статус, и Debug-лог.
        await SafeExecutor.RunAsync(
            async () =>
            {
                await referenceDataService.SeedIfEmptyAsync();
                var docTypes = await referenceDataService.GetDocTypesAsync();
                DocTypes.Seed(docTypes, ModelEqualityComparers.DocTypeByCode);
                SyncSelectedDocTypeFromMetadata();

                var departments = await referenceDataService.GetDepartmentsAsync();
                Departments.Seed(departments.Where(d => d.IsRoot), ModelEqualityComparers.DepartmentByCode);
                allDepartments = departments;

                ChildDepartments.Seed(departments.Where(d => !d.IsRoot)
                    .Select(ObservableCollectionExtensions.GetChildId)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(c => c, StringComparer.Ordinal));

                ApproverRoles.Seed(ReferenceData.ApproverRoles);
                Statuses.Seed(ReferenceData.Statuses);

                // Инициализируем справочник дисциплин (раньше был через BuildDisciplineOptions).
                DisciplineCodes.Clear();
                foreach (var code in referenceDataService.DefaultDisciplinesFull())
                {
                    var label = ReferenceData.DisciplineLabels.TryGetValue(code, out var l) ? l : code;
                    if (!DisciplineCodes.Any(d => d.Code == code))
                    {
                        DisciplineCodes.Add(new CodeOption(code, label));
                    }
                }

                SeedStatus = referenceDataService.SeedStatusMessage;
            },
            policy: ErrorPolicy.Silent(),
            extraLog: ex => SeedStatus = $"Ошибка загрузки справочников: {ex.Message}");
    }

    [RelayCommand]
    private async Task NewFromTemplateAsync()
    {
        await SafeExecutor.RunAsync(
            async () =>
            {
                var templates = TemplateLoader.LoadFromDirectory(AppSettings.FactoryRoot);
                var summaries = templates
                    .Select(t => new TemplateSummary
                    {
                        Code = t.DocTypeCode,
                        DisplayName = $"{t.DocTypeCode} (уровень {t.HmsLevel})",
                        Level = t.HmsLevel,
                    })
                    .ToList();

                var selectedCode = await dialogService.ShowNewDocumentDialogAsync(summaries);
                if (string.IsNullOrWhiteSpace(selectedCode))
                {
                    return;
                }

                NewDocument(selectedCode);
            },
            policy: new ErrorPolicy(
                StatusMessage: ex => $"Ошибка создания документа: {ex.Message}",
                DialogMessage: ex => $"Не удалось создать документ из шаблона:\n{ex.Message}"),
            dialog: dialogService,
            setStatusMessage: msg => StatusMessage = msg);
    }

    [RelayCommand]
    private Task OpenFileAsync() => openCoordinator.OpenFileAsync();

    [RelayCommand]
    private Task SaveToDatabaseAsync() => exportCoordinator.SaveToDatabaseAsync();

    [RelayCommand]
    private Task ExportMarkdownAsync() => exportCoordinator.ExportMarkdownAsync();

    [RelayCommand]
    private Task LoadFromDatabaseAsync() => openCoordinator.LoadFromDatabaseAsync();

    [RelayCommand]
    private Task ExportDocxAsync() => exportCoordinator.ExportDocxAsync();

    [RelayCommand]
    private void SwitchToDocumentTab() => ActiveTabIndex = 0;

    [RelayCommand]
    private void SwitchToMetadataTab() => ActiveTabIndex = 1;

    [RelayCommand]
    private void SwitchToDatabaseTab() => ActiveTabIndex = 2;

    [RelayCommand]
    private void SwitchToPreviewTab() => ActiveTabIndex = 3;

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        try
        {
            await dialogService.ShowAboutAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShowAboutAsync] {ex}");
            StatusMessage = $"Ошибка открытия окна «О программе»: {ex.Message}";
        }
    }

    /// <inheritdoc />
    void IDocumentLifecycleSink.RefreshPreview() => RefreshPreview();

    void IMainWindowState.RefreshPreview() => RefreshPreview();

    [RelayCommand]
    private void RefreshPreview()
    {
        // sync-обёртка: RefreshPreview() вызывается из setter'ов ObservableProperty
        // и OnActiveTabIndexChanged, которые требуют sync-сигнатуры.
        SafeExecutor.RunAsync(
                () =>
                {
                    if (DocumentBody != null && Metadata != null)
                    {
                        MarkdownPreview = previewBuilder.Build(Metadata, DocumentBody);
                    }

                    return Task.CompletedTask;
                },
                policy: ErrorPolicy.Silent(statusMessage: ex => $"Ошибка обновления предпросмотра: {ex.Message}"),
                extraLog: ex => Console.Error.WriteLine($"[{nameof(RefreshPreview)}] {ex}"))
            .GetAwaiter()
            .GetResult();
    }

    [RelayCommand]
    private void RegenerateDocId()
    {
        docIdManuallyEdited = false;
        TryAutoProposeDocId();
        StatusMessage = $"Сгенерирован номер: {Metadata?.DocId}";
    }

    private void DebounceRefreshPreview()
    {
        previewDebounceCts?.Cancel();
        previewDebounceCts = new CancellationTokenSource();
        _ = RefreshPreviewAsync(previewDebounceCts.Token);
    }

    private async Task RefreshPreviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            RefreshPreview();
        }
        catch (OperationCanceledException)
        {
            // Пользователь продолжает печатать — отменяем устаревший запрос.
        }
    }

    private DocumentMetadata CreateDefaultMetadata(string docTypeCode, TemplateDefinition? definition)
    {
        var meta = new DocumentMetadata();

        meta.DocType = docTypeCode;
        meta.Title = docTypeCode;

        if (!string.IsNullOrWhiteSpace(definition?.DefaultApproverRole))
        {
            meta.ApproverRole = definition.DefaultApproverRole;
        }

        meta.EffectiveDate = DateTime.Now.ToString("yyyy-MM-dd");
        meta.Revision = "1";

        global::AvaloniaApp.Services.Defaults.Metadata.ApplyTemplateDefaults(meta);

        return meta;
    }

    /// <summary>
    /// Создаёт новый документ указанного типа с дефолтными метаданными и стартовыми секциями.
    /// </summary>
    private void NewDocument(string docTypeCode) =>
        openCoordinator.NewDocument(
            docTypeCode,
            createDefaultMetadata: CreateDefaultMetadata,
            onLoaded: () =>
            {
                FormTemplate = formsCatalog.GetTemplate(docTypeCode);
                CurrentDocumentId = 0;

                TryAutoProposeDocId();
                CurrentBasisCardId = 0;
                HasUnsavedChanges = true;
                StatusMessage = $"Создан новый документ: {docTypeCode}";
            });

    // --- Partial методы (reactions на смену свойств) ---
    partial void OnFormTemplateChanged(FormTemplate? value) =>
        lifecycleCoordinator.OnFormTemplateChanged(value);

    partial void OnActiveTabIndexChanged(int value)
    {
        if (value == PreviewTabIndex)
        {
            RefreshPreview();
        }
    }

    partial void OnCurrentBasisCardIdChanged(long value)
    {
        CurrentBasisCardLabel = value > 0 ? $"Карточка основания: #{value}" : string.Empty;
    }

    /// <summary>
    /// Синхронизирует выбор кода подразделения из ComboBox с полным названием в Metadata.
    /// </summary>
    partial void OnDepartmentCodeChanged(string value) =>
        metadataSyncCoordinator.OnDepartmentCodeChanged(value);

    /// <summary>
    /// Преобразует название подразделения в КОД КОРНЕВОГО подразделения (parent_id).
    /// </summary>
    private string ResolveDepartmentCode(string departmentName) =>
        metadataSyncCoordinator.ResolveDepartmentCode(departmentName);

    /// <summary>
    /// Обновляет список дочерних подразделений для выбранного корневого подразделения.
    /// </summary>
    private void RefreshChildDepartments(string parentCode) =>
        ChildDepartments.Seed(ObservableCollectionExtensions.ChildIdsOf(allDepartments, parentCode));

    /// <summary>
    /// Реакция на смену <see cref="DocumentBody"/>: делегирует координатору.
    /// </summary>
    partial void OnDocumentBodyChanged(DocumentBody? value) =>
        lifecycleCoordinator.OnDocumentBodyChanged(value);

    /// <summary>
    /// Реакция на смену <see cref="Metadata"/>: делегирует координатору +
    /// синхронизирует <see cref="SelectedDocType"/> с <see cref="DocumentMetadata.DocType"/>
    /// (если пользователь редактировал DocType через TextBox, а не ComboBox).
    /// </summary>
    partial void OnMetadataChanged(DocumentMetadata value)
    {
        lifecycleCoordinator.OnMetadataChanged(value);

        // UX-аудит #6.1: если DocType был изменён напрямую (TextBox), SelectedDocType
        // рассинхронизируется — TextBox показывает код, ComboBox — пустое/старое значение.
        // Пересинхронизируем только если есть реальное расхождение (избегаем лишних нотификаций).
        if (SelectedDocType?.Code != value.DocType)
        {
            SyncSelectedDocTypeFromMetadata();
        }
    }

    /// <summary>
    /// Реакция на смену выбранного объекта <see cref="SelectedDocType"/>.
    /// </summary>
    partial void OnSelectedDocTypeChanged(DocType? value)
    {
        if (suppressDocTypeSync)
        {
            return;
        }

        metadataSyncCoordinator.OnSelectedDocTypeChanged(value);
    }

    /// <summary>
    /// Пересинхронизирует <see cref="SelectedDocType"/> с текущим <see cref="DocumentMetadata.DocType"/>.
    /// </summary>
    private void SyncSelectedDocTypeFromMetadata() =>
        metadataSyncCoordinator.SyncSelectedDocTypeFromMetadata(DocTypes);

    /// <summary>
    /// Если пользователь НЕ правил DocId руками, пересчитываем предложение.
    /// </summary>
    private void TryAutoProposeDocId()
    {
        if (docIdManuallyEdited || Metadata is null)
        {
            return;
        }

        Metadata.DocId = metadataSyncCoordinator.BuildDocId();
    }

    /// <summary>
    /// Создаёт ViewModel редактора ЛРИ для вкладки «Настройки».
    /// </summary>
    private CollectionFieldViewModel<ChangeLogRow> CreateChangeLogFieldViewModel(DocumentBody body, IEngineCore engine)
    {
        var descriptor = new CollectionFieldDescriptor<ChangeLogRow>
        {
            Key = "ChangeLog",
            Label = "Лист регистрации изменений",
            IsEditable = true,
            HelpText = "Колонки «Дата ред.» и «Номер ред.» заполняются автоматически из полей верхнего колонтитула. " +
                       "Автор, указанный в последней строке, считается автором текущей редакции документа. " +
                       "Последняя строка редактируема; исторические строки защищены от изменений.",
            ItemsSourceGetter = b => b.ChangeLog,
            AddCommandFactory = (b, e) =>
            {
                // Захватываем ссылку на актуальный сервис через замыкание на this
                // (интерфейс IDocumentLifecycleSink предоставляет доступ к сервису).
                var promptService = changeLogPromptService;
                return new AsyncRelayCommand(async () =>
                {
                    if (promptService != null)
                    {
                        await promptService.PromptAndAddAsync("данные вручную");
                    }
                    else if (e != null)
                    {
                        e.AddChangeLogRow(autoIncrementRevision: true);
                    }
                    else
                    {
                        b.ChangeLog.Add(DocumentBodyFactory.CreatePrefilledChangeLogRow(null, b.ChangeLog));
                    }
                });
            },
            AddAction = (b, e) =>
            {
                if (e != null)
                {
                    e.AddChangeLogRow(autoIncrementRevision: true);
                }
                else
                {
                    b.ChangeLog.Add(DocumentBodyFactory.CreatePrefilledChangeLogRow(null, b.ChangeLog));
                }
            },
            RemoveAction = (item, b, e) =>
            {
                if (e != null)
                {
                    e.RemoveChangeLogRow(item);
                }
                else
                {
                    b.ChangeLog.Remove(item);
                }
            },
        };

        return new CollectionFieldViewModel<ChangeLogRow>(descriptor, body, engine as ICollectionMutator);
    }
}
