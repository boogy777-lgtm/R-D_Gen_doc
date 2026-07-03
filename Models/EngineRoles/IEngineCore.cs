using System;

namespace AvaloniaApp.Models.EngineRoles;

/// <summary>
/// Ролевой интерфейс (ISP): ядро движка — доступ к <see cref="DocumentBody"/>,
/// <see cref="DocumentMetadata"/>, событиям жизненного цикла и освобождению ресурсов.
/// Используется <see cref="ViewModels.MainWindowViewModel"/> — он владеет
/// движком, подписывается на структурные/контентные события и пересоздаёт
/// Engine при смене документа или метаданных.
///
/// НЕ наследует <see cref="IDocumentChangeEngine"/> намеренно: это «горизонтальный»
/// контракт (жизненный цикл и наблюдаемость), который дополняет узкие
/// ролевые интерфейсы (<see cref="ISingleFieldMutator"/>,
/// <see cref="ICollectionMutator"/>, <see cref="ITreeMutator"/>).
/// <see cref="Services.DocumentChangeEngine"/> реализует все пять контрактов.
/// </summary>
public interface IEngineCore : IDisposable
{
    /// <summary>Событие любого изменения документа (после любой мутации).</summary>
    event EventHandler? Changed;

    /// <summary>
    /// Событие структурного изменения документа (добавление/удаление секций,
    /// терминов, приложений, строк ЛРИ и т.п.).
    /// </summary>
    event EventHandler? StructureChanged;

    /// <summary>
    /// Событие изменения контента (редактирование текста в полях/элементах коллекций).
    /// </summary>
    event EventHandler? ContentChanged;

    /// <summary>Gets тело документа.</summary>
    DocumentBody Body { get; }

    /// <summary>Gets метаданные документа.</summary>
    DocumentMetadata Metadata { get; }

    /// <summary>Принудительно уведомляет подписчиков об изменении.</summary>
    void NotifyChanged();

    /// <summary>
    /// Возвращает последнюю строку ЛРИ или null, если строк нет.
    /// Используется для отображения бейджа редакции в <c>MainWindowViewModel</c>.
    /// </summary>
    ChangeLogRow? GetLastChangeLogRow();

    /// <summary>
    /// Проверяет, отличаются ли текущие метаданные (Revision/EffectiveDate)
    /// от последней строки ЛРИ. Если отличаются — нужно добавить новую строку.
    /// </summary>
    bool IsChangeLogOutOfSync();
}
