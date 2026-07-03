using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Минимальный контракт состояния главного окна, который видят координаторы
/// (<see cref="DocumentExportCoordinator"/>, <see cref="DocumentOpenCoordinator"/>,
/// <see cref="MetadataSyncCoordinator"/>). Позволяет вынести IO-оркестрацию из
/// <c>MainWindowViewModel</c> без полной передачи VM в координатор
/// (координатор остаётся тестируемым в изоляции).
/// </summary>
/// <remarks>
/// Реализуется <c>MainWindowViewModel</c>: пишет <see cref="StatusMessage"/>,
/// обновляет <see cref="RefreshPreview"/>, переключает <see cref="ActiveTabIndex"/>,
/// сбрасывает <see cref="HasUnsavedChanges"/>, подбирает <see cref="FormTemplate"/>.
/// Не раскрывает Engine и формы — координаторам они не нужны.
/// </remarks>
public interface IMainWindowState
{
    /// <summary>
    /// Gets или sets метаданные документа. Setter триггерит пересоздание
    /// <c>DocumentChangeEngine</c> в VM и подписку на <c>PropertyChanged</c>
    /// для авто-генерации DocId и синхронизации ЛРИ.
    /// </summary>
    DocumentMetadata? Metadata { get; set; }

    /// <summary>
    /// Gets или sets тело документа. Setter триггерит пересоздание
    /// <c>DocumentChangeEngine</c> и пересборку <c>FormViewModel</c>.
    /// </summary>
    DocumentBody? DocumentBody { get; set; }

    /// <summary>
    /// Gets или sets сообщение статус-бара.
    /// </summary>
    string StatusMessage { get; set; }

    /// <summary>
    /// Gets или sets флаг наличия несохранённых изменений.
    /// </summary>
    bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// Gets или sets индекс активной вкладки.
    /// </summary>
    int ActiveTabIndex { get; set; }

    /// <summary>
    /// Gets или sets шаблон формы, соответствующий типу документа. Запись инициирует
    /// пересборку <c>FormViewModel.FormFieldViewModels</c> внутри VM.
    /// </summary>
    FormTemplate? FormTemplate { get; set; }

    /// <summary>
    /// Gets или sets ID текущего документа в БД (0 = новый документ, ещё не сохранён).
    /// </summary>
    long CurrentDocumentId { get; set; }

    /// <summary>
    /// Принудительно пересобирает MD preview из текущего <see cref="Metadata"/> +
    /// <see cref="DocumentBody"/>. Sync-обёртка — должна быть быстрой и идемпотентной,
    /// т.к. вызывается из IO-обработчиков координаторов.
    /// </summary>
    void RefreshPreview();
}
