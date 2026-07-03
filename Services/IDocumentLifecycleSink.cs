using AvaloniaApp.Models;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Services;

/// <summary>
/// Контракт UI-слоя для <see cref="DocumentLifecycleCoordinator"/>.
/// Координатор не знает о <c>MainWindowViewModel</c> — он читает/пишет
/// только через этот интерфейс. Это позволяет тестировать координатор
/// в изоляции и не нарушает Dependency Rule (Services → ViewModels).
/// </summary>
/// <remarks>
/// Интерфейс объявлен в <c>Services</c> (где живёт координатор), но
/// методы <c>set_*</c> возвращают <c>void</c> и принимают готовые
/// <see cref="ViewModelBase"/>-производные, чтобы не было циклической
/// зависимости Services ↔ ViewModels на уровне типов.
/// </remarks>
public interface IDocumentLifecycleSink
{
    /// <summary>Текущие метаданные документа.</summary>
    DocumentMetadata? Metadata { get; }

    /// <summary>Текущее тело документа.</summary>
    DocumentBody? DocumentBody { get; }

    /// <summary>Текущий шаблон формы (для пересборки FormViewModel).</summary>
    FormTemplate? FormTemplate { get; }

    /// <summary>Текущая MD preview (для синхронизации после мутаций).</summary>
    string MarkdownPreview { get; set; }

    /// <summary>VM формы документа (вкладка «Документ»).</summary>
    FormViewModel? FormViewModel { get; set; }

    /// <summary>VM редактора ЛРИ (вкладка «Настройки»).</summary>
    CollectionFieldViewModel<ChangeLogRow>? ChangeLogFieldViewModel { get; set; }

    /// <summary>Текст бейджа «Последняя редакция».</summary>
    string ChangeLogBadgeText { get; set; }

    /// <summary>Видимость предупреждения о рассинхроне ЛРИ.</summary>
    bool IsChangeLogWarningVisible { get; set; }

    /// <summary>Текст предупреждения о рассинхроне ЛРИ.</summary>
    string ChangeLogWarningText { get; set; }

    /// <summary>Количество приложений в текущем документе.</summary>
    int AppendicesCount { get; set; }

    /// <summary>Флаг наличия несохранённых изменений.</summary>
    bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// Сервис popup-диалога добавления строки ЛРИ. Null = не инициализирован.
    /// Используется <c>CollectionFieldDescriptor.AddCommandFactory</c>
    /// в <c>MainWindowViewModel.CreateChangeLogFieldViewModel</c>.
    /// </summary>
    IChangeLogPromptService? ChangeLogPromptService { get; set; }

    /// <summary>Триггер пересборки MD preview (вызывает previewBuilder).</summary>
    void RefreshPreview();
}
