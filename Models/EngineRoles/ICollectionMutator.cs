namespace AvaloniaApp.Models.EngineRoles;

/// <summary>
/// Ролевой интерфейс (ISP): операции добавления/удаления элементов коллекций
/// документа (термины, приложения, связанные документы, ЛРИ).
/// Используется <see cref="ViewModels.CollectionFieldViewModel{TItem}"/> —
/// он работает с одной коллекцией одного типа и не должен видеть API
/// одиночных полей или событий движка.
///
/// НЕ наследует <see cref="IDocumentChangeEngine"/> намеренно: контракт узкий.
/// Реализуется через <see cref="Services.DocumentChangeEngine"/>, который
/// дополнительно реализует <see cref="IDocumentChangeEngine"/> для обратной
/// совместимости с типизацией делегатов в дескрипторах.
/// </summary>
public interface ICollectionMutator
{
    /// <summary>Добавляет пустой термин в список терминов.</summary>
    void AddTerm();

    /// <summary>Удаляет термин из списка терминов.</summary>
    void RemoveTerm(TermPair term);

    /// <summary>Добавляет связанный документ.</summary>
    void AddRelatedDocument();

    /// <summary>Удаляет связанный документ.</summary>
    void RemoveRelatedDocument(StringItem doc);

    /// <summary>
    /// Добавляет строку листа регистрации изменений.
    /// Если <paramref name="autoIncrementRevision"/> — true, номер редакции
    /// автоматически увеличивается.
    /// </summary>
    void AddChangeLogRow(bool autoIncrementRevision = false);

    /// <summary>Удаляет строку листа регистрации изменений.</summary>
    void RemoveChangeLogRow(ChangeLogRow row);

    /// <summary>Добавляет приложение (приложение к документу).</summary>
    void AddAppendix();

    /// <summary>Удаляет приложение.</summary>
    void RemoveAppendix(BodyAppendix appendix);

    /// <summary>Добавляет подсекцию-таблицу в указанное приложение.</summary>
    void AddSectionToAppendix(BodyAppendix appendix);

    /// <summary>Удаляет подсекцию-таблицу из указанного приложения.</summary>
    void RemoveSectionFromAppendix(BodyAppendix appendix, BodySection section);
}
