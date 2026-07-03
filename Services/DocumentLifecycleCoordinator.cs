using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Services;

/// <summary>
/// Координатор lifecycle-операций документа: создание и пересоздание
/// <see cref="DocumentChangeEngine"/>, подписки на <c>PropertyChanged</c>
/// метаданных, пересборка Form/ChangeLog-ViewModel-ов, обновление
/// статуса ЛРИ и AppendicesCount.
/// </summary>
/// <remarks>
/// <para>Вынесен из <c>MainWindowViewModel</c> для соблюдения SRP: VM
/// остаётся тонким binding-адаптером, а вся «plumbing»-логика между
/// Engine ↔ PropertyChanged ↔ FormViewModel живёт здесь.</para>
/// <para>Координатор НЕ зависит от <c>MainWindowViewModel</c> — он
/// работает через <see cref="IDocumentLifecycleSink"/>, что упрощает
/// тестирование.</para>
/// </remarks>
public sealed class DocumentLifecycleCoordinator
{
    private readonly IDocumentLifecycleSink sink;
    private readonly IDialogService dialogService;
    private readonly IMarkdownPreviewBuilder previewBuilder;
    private readonly Action<DocumentMetadata?>? onMetadataPropertyChangedForDocId;
    private readonly Func<DocumentBody, IEngineCore, CollectionFieldViewModel<ChangeLogRow>>? createChangeLogFieldViewModel;
    private readonly Func<FormTemplate?, DocumentBody, IEngineCore, List<ViewModelBase>>? buildFormFieldViewModels;

    private DocumentChangeEngine? engine;
    private ChangeLogPromptService? changeLogPromptService;

    /// <summary>
    /// Создаёт координатор.
    /// </summary>
    /// <param name="sink">UI-приёмник событий lifecycle (обычно VM).</param>
    /// <param name="dialogService">Сервис диалогов (для ChangeLogPromptService).</param>
    /// <param name="previewBuilder">Построитель MD-preview.</param>
    /// <param name="onMetadataPropertyChangedForDocId">
    /// Делегат, вызываемый при <c>Metadata.PropertyChanged</c> для авто-предложения DocId.
    /// </param>
    /// <param name="createChangeLogFieldViewModel">
    /// Фабрика <see cref="CollectionFieldViewModel{ChangeLogRow}"/>. Может быть null —
    /// тогда ЛРИ-VM не пересоздаётся.
    /// </param>
    /// <param name="buildFormFieldViewModels">
    /// Фабрика списка VM-ов полей формы. Может быть null — тогда Form-VM не пересоздаётся.
    /// </param>
    public DocumentLifecycleCoordinator(
        IDocumentLifecycleSink sink,
        IDialogService dialogService,
        IMarkdownPreviewBuilder previewBuilder,
        Action<DocumentMetadata?>? onMetadataPropertyChangedForDocId = null,
        Func<DocumentBody, IEngineCore, CollectionFieldViewModel<ChangeLogRow>>? createChangeLogFieldViewModel = null,
        Func<FormTemplate?, DocumentBody, IEngineCore, List<ViewModelBase>>? buildFormFieldViewModels = null)
    {
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.previewBuilder = previewBuilder ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.onMetadataPropertyChangedForDocId = onMetadataPropertyChangedForDocId;
        this.createChangeLogFieldViewModel = createChangeLogFieldViewModel;
        this.buildFormFieldViewModels = buildFormFieldViewModels;
    }

    /// <summary>
    /// Gets текущий движок мутаций. Null если документ ещё не загружен.
    /// </summary>
    public DocumentChangeEngine? Engine => engine;

    /// <summary>
    /// Реакция на смену <see cref="DocumentBody"/>: пересоздаёт Engine и ViewModel-ы.
    /// </summary>
    public void OnDocumentBodyChanged(DocumentBody? value)
    {
        // Старый FormViewModel владеет дочерними VM и должен быть диспоузен
        // до замены коллекции полей.
        sink.FormViewModel?.Dispose();

        if (value != null && sink.Metadata != null)
        {
            SetEngine(new DocumentChangeEngine(value, sink.Metadata));
            sink.MarkdownPreview = previewBuilder.Build(sink.Metadata, value);

            changeLogPromptService = new ChangeLogPromptService(Engine!, dialogService);
            changeLogPromptService.SeedKnownValues(sink.Metadata);
            sink.ChangeLogPromptService = changeLogPromptService;

            if (createChangeLogFieldViewModel != null)
            {
                sink.ChangeLogFieldViewModel = createChangeLogFieldViewModel(value, Engine!);
            }

            if (buildFormFieldViewModels != null)
            {
                sink.FormViewModel = new FormViewModel(buildFormFieldViewModels(sink.FormTemplate, value, Engine!));
            }

            RefreshChangeLogStatus();
        }
        else
        {
            SetEngine(null);
            sink.MarkdownPreview = string.Empty;
            sink.ChangeLogFieldViewModel = null;
            sink.FormViewModel = null;
            changeLogPromptService = null;
            sink.ChangeLogPromptService = null;
        }

        sink.AppendicesCount = value?.Appendices?.Count ?? 0;
        sink.HasUnsavedChanges = true;
    }

    /// <summary>
    /// Реакция на смену <see cref="DocumentMetadata"/>: пересоздаёт Engine
    /// и подписывается на PropertyChanged.
    /// </summary>
    public void OnMetadataChanged(DocumentMetadata value)
    {
        if (sink.DocumentBody != null)
        {
            SetEngine(new DocumentChangeEngine(sink.DocumentBody, value));
            sink.MarkdownPreview = previewBuilder.Build(value, sink.DocumentBody);

            sink.ChangeLogFieldViewModel?.UpdateEngine(Engine as ICollectionMutator);
            sink.FormViewModel?.UpdateEngine(Engine);
        }

        if (value != null)
        {
            value.PropertyChanged -= OnMetadataPropertyChangedForDocId;
            value.PropertyChanged += OnMetadataPropertyChangedForDocId;
            value.PropertyChanged -= OnMetadataPropertyChangedForChangeLog;
            value.PropertyChanged += OnMetadataPropertyChangedForChangeLog;

            changeLogPromptService?.MarkCurrentAsKnown(value);
            RefreshChangeLogStatus();
        }

        sink.HasUnsavedChanges = true;
    }

    /// <summary>
    /// Пересобирает <see cref="FormViewModel"/> при смене <see cref="FormTemplate"/>.
    /// </summary>
    public void OnFormTemplateChanged(FormTemplate? value)
    {
        if (value != null && sink.DocumentBody != null && Engine != null
            && buildFormFieldViewModels != null)
        {
            sink.FormViewModel?.RebuildFields(buildFormFieldViewModels(value, sink.DocumentBody, Engine));
        }
    }

    /// <summary>
    /// Освобождает ресурсы Engine (вызывается из VM.Dispose()).
    /// </summary>
    public void Dispose()
    {
        SetEngine(null);
    }

    private void SetEngine(DocumentChangeEngine? newEngine)
    {
        if (engine != null)
        {
            engine.StructureChanged -= OnEngineStructureChanged;
            engine.ContentChanged -= OnEngineContentChanged;
            engine.Dispose();
        }

        engine = newEngine;
        if (engine != null)
        {
            engine.StructureChanged += OnEngineStructureChanged;
            engine.ContentChanged += OnEngineContentChanged;
        }
    }

    private void OnEngineStructureChanged(object? sender, EventArgs e)
    {
        sink.HasUnsavedChanges = true;
        sink.AppendicesCount = sink.DocumentBody?.Appendices?.Count ?? 0;
        RefreshChangeLogStatus();
        sink.RefreshPreview();
    }

    private void OnEngineContentChanged(object? sender, EventArgs e)
    {
        sink.HasUnsavedChanges = true;
        RefreshChangeLogStatus();
        // Делегируем обновление preview sink-у (он владеет debounce-логикой).
        sink.RefreshPreview();
    }

    private void OnMetadataPropertyChangedForDocId(object? sender, PropertyChangedEventArgs e) =>
        onMetadataPropertyChangedForDocId?.Invoke(sink.Metadata);

    private void OnMetadataPropertyChangedForChangeLog(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(DocumentMetadata.Revision) or nameof(DocumentMetadata.EffectiveDate)))
        {
            return;
        }

        RefreshChangeLogStatus();
        if (sink.Metadata is not null)
        {
            changeLogPromptService?.NotifyMetadataChanged(sink.Metadata);
        }
    }

    /// <summary>
    /// Обновляет бейдж и предупреждение ЛРИ. Доступен из VM для
    /// немедленного обновления (например, после ручного добавления строки).
    /// </summary>
    public void RefreshChangeLogStatus()
    {
        if (Engine is null || sink.Metadata is null)
        {
            sink.ChangeLogBadgeText = "ЛРИ: нет записей";
            sink.IsChangeLogWarningVisible = false;
            sink.ChangeLogWarningText = string.Empty;
            return;
        }

        var lastRow = Engine.GetLastChangeLogRow();
        if (lastRow is null)
        {
            sink.ChangeLogBadgeText = "ЛРИ: нет записей";
            sink.IsChangeLogWarningVisible = true;
            sink.ChangeLogWarningText = "В Листе регистрации изменений нет записей. Добавьте первую строку.";
            return;
        }

        sink.ChangeLogBadgeText = $"Последняя редакция: Rev {lastRow.Revision} от {lastRow.Date}";

        if (Engine.IsChangeLogOutOfSync())
        {
            sink.IsChangeLogWarningVisible = true;
            sink.ChangeLogWarningText = $"Текущие метаданные (Rev {RevisionFormatter.Trim(sink.Metadata.Revision)} от {sink.Metadata.EffectiveDate}) " +
                                        "не отражены в ЛРИ. Добавьте строку истории на вкладке «Настройки».";
        }
        else
        {
            sink.IsChangeLogWarningVisible = false;
            sink.ChangeLogWarningText = string.Empty;
            changeLogPromptService?.MarkCurrentAsKnown(sink.Metadata);
        }
    }
}
