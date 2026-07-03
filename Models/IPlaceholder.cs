namespace AvaloniaApp.Models;

/// <summary>
/// Маркер «placeholder-элемента» — фиктивной записи, которую
/// <see cref="Services.DocumentChangeEngine"/> добавляет в пустую коллекцию,
/// чтобы UI всегда мог показать хотя бы одну строку.
/// Снимается Engine-ом при первом же изменении пользовательского поля
/// (см. <see cref="Services.DocumentChangeEngine.ReplacePlaceholderIfNeeded{T}"/>).
/// Реализуют: <see cref="TermPair"/>, <see cref="BodySection"/>,
/// <see cref="BodyAppendix"/>, <see cref="ChangeLogRow"/>, <see cref="StringItem"/>.
/// </summary>
public interface IPlaceholder
{
    /// <summary>
    /// Gets or sets признак placeholder-элемента.
    /// </summary>
    bool IsPlaceholder { get; set; }
}
