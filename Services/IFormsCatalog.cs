using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Каталог шаблонов форм. Возвращает <see cref="FormTemplate"/> с дескрипторами полей,
/// не зависящими от конкретных Avalonia-контролов.
/// </summary>
/// <remarks>
/// <para>
/// Интерфейс выделен из static-класса <c>Defaults.Forms</c> для соблюдения DIP:
/// MainWindowViewModel, DocumentOpenCoordinator, DocumentChangeEngine и другие
/// зависят от абстракции, а не от конкретного класса, прибитого к namespace.
/// </para>
/// <para>
/// Default-каталог создаёт экземпляр-одиночку, поэтому <c>Forms.GetTemplate(...)</c>
/// остаётся работоспособным как backward-compat фасад.
/// </para>
/// </remarks>
public interface IFormsCatalog
{
    /// <summary>
    /// Возвращает шаблон формы для указанного DocType.
    /// Если <paramref name="docTypeCode"/> — null или неизвестен, возвращается
    /// универсальный шаблон со стартовым набором по умолчанию.
    /// </summary>
    FormTemplate GetTemplate(string? docTypeCode);
}