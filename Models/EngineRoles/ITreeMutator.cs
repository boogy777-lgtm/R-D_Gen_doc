namespace AvaloniaApp.Models.EngineRoles;

/// <summary>
/// Ролевой интерфейс (ISP): операции над иерархическим деревом секций документа.
/// Используется <see cref="ViewModels.SectionsTreeViewModel"/> и
/// <see cref="ViewModels.SectionNodeViewModel"/> — они работают только
/// со структурой секций и не должны видеть API терминов/ЛРИ/приложений.
///
/// НЕ наследует <see cref="IDocumentChangeEngine"/> намеренно: контракт узкий.
/// Реализуется через <see cref="Services.DocumentChangeEngine"/>, который
/// дополнительно реализует <see cref="IDocumentChangeEngine"/> для обратной
/// совместимости с типизацией делегатов и существующими ссылками.
/// </summary>
public interface ITreeMutator
{
    /// <summary>
    /// Добавляет секцию (верхнего уровня или подраздел).
    /// Если <paramref name="parent"/> — null, добавляется корневая секция.
    /// </summary>
    void AddSection(BodySection? parent = null);

    /// <summary>Удаляет секцию со всеми её подразделами.</summary>
    void RemoveSection(BodySection section);
}
