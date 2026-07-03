using System;

namespace AvaloniaApp.Models;

/// <summary>
/// Порт (интерфейс) для мутаций <see cref="DocumentBody"/> и <see cref="DocumentMetadata"/>.
/// Реализуется в слое Application/Services (<see cref="Services.DocumentChangeEngine"/>);
/// объявлен в Domain, чтобы дескрипторы полей могли хранить делегаты-мутаторы,
/// не завися от конкретного движка и не нарушая Clean Architecture.
/// </summary>
public interface IDocumentChangeEngine
{
    /// <summary>Событие изменения документа (после любой мутации).</summary>
    event EventHandler? Changed;

    /// <summary>
    /// Событие структурного изменения документа (добавление/удаление секций,
    /// терминов, приложений, строк ЛРИ и т.п.). Подписчики перестраивают дерево
    /// и тяжёлые списки немедленно.
    /// </summary>
    event EventHandler? StructureChanged;

    /// <summary>
    /// Событие изменения контента (редактирование текста в полях/элементах
    /// коллекций). Подписчики обновляют превью с debounce.
    /// </summary>
    event EventHandler? ContentChanged;

    /// <summary>
    /// Gets тело документа.
    /// </summary>
    DocumentBody Body { get; }

    /// <summary>
    /// Gets метаданные документа.
    /// </summary>
    DocumentMetadata Metadata { get; }

    /// <summary>
    /// Устанавливает назначение документа.
    /// </summary>
    void SetPurpose(string value);

    /// <summary>
    /// Устанавливает область применения.
    /// </summary>
    void SetScope(string value);

    /// <summary>
    /// Устанавливает описание первичного обучения.
    /// </summary>
    void SetPrimaryTraining(string value);

    /// <summary>
    /// Устанавливает описание повторного обучения.
    /// </summary>
    void SetRecurrentTraining(string value);

    /// <summary>
    /// Устанавливает ссылку на родительский документ.
    /// </summary>
    void SetParentDocument(string value);

    /// <summary>
    /// Добавляет пустой термин.
    /// </summary>
    void AddTerm();

    /// <summary>
    /// Удаляет термин.
    /// </summary>
    void RemoveTerm(TermPair term);

    /// <summary>
    /// Добавляет связанный документ.
    /// </summary>
    void AddRelatedDocument();

    /// <summary>
    /// Удаляет связанный документ.
    /// </summary>
    void RemoveRelatedDocument(StringItem doc);

    /// <summary>
    /// Добавляет строку листа регистрации изменений.
    /// Если <paramref name="autoIncrementRevision"/> — true, номер редакции автоматически
    /// увеличивается на 1 (max из существующих строк и Metadata.Revision + 1).
    /// Иначе копирует текущие значения из метаданных.
    /// </summary>
    void AddChangeLogRow(bool autoIncrementRevision = false);

    /// <summary>
    /// Удаляет строку листа регистрации изменений.
    /// </summary>
    void RemoveChangeLogRow(ChangeLogRow row);

    /// <summary>
    /// Добавляет приложение.
    /// </summary>
    void AddAppendix();

    /// <summary>
    /// Удаляет приложение.
    /// </summary>
    void RemoveAppendix(BodyAppendix appendix);

    /// <summary>
    /// Добавляет подсекцию-таблицу в указанное приложение.
    /// </summary>
    void AddSectionToAppendix(BodyAppendix appendix);

    /// <summary>
    /// Удаляет подсекцию-таблицу из указанного приложения.
    /// </summary>
    void RemoveSectionFromAppendix(BodyAppendix appendix, BodySection section);

    /// <summary>
    /// Добавляет секцию (верхнего уровня или подраздел).
    /// Если <paramref name="parent"/> — null, добавляется корневая секция.
    /// </summary>
    void AddSection(BodySection? parent = null);

    /// <summary>
    /// Удаляет секцию со всеми подразделами.
    /// </summary>
    void RemoveSection(BodySection section);

    /// <summary>
    /// Принудительно уведомляет подписчиков об изменении.
    /// </summary>
    void NotifyChanged();
}
