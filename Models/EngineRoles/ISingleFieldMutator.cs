namespace AvaloniaApp.Models.EngineRoles;

/// <summary>
/// Ролевой интерфейс (ISP): операции над одиночными строковыми полями документа.
/// Используется <see cref="ViewModels.SingleFieldViewModel"/> — он редактирует
/// ровно одно текстовое поле формы и не должен видеть API коллекций/дерева.
///
/// НЕ наследует <see cref="IDocumentChangeEngine"/> намеренно: контракт узкий.
/// Реализуется через <see cref="Services.DocumentChangeEngine"/>, который
/// дополнительно реализует <see cref="IDocumentChangeEngine"/> для обратной
/// совместимости с типизацией делегатов в дескрипторах.
/// </summary>
public interface ISingleFieldMutator
{
    /// <summary>Устанавливает назначение документа.</summary>
    void SetPurpose(string value);

    /// <summary>Устанавливает область применения.</summary>
    void SetScope(string value);

    /// <summary>Устанавливает описание первичного обучения.</summary>
    void SetPrimaryTraining(string value);

    /// <summary>Устанавливает описание повторного обучения.</summary>
    void SetRecurrentTraining(string value);

    /// <summary>Устанавливает ссылку на родительский документ.</summary>
    void SetParentDocument(string value);
}
