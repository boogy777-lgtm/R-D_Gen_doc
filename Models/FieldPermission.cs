namespace AvaloniaApp.Models;

/// <summary>
/// Режим взаимодействия поля формы с шаблоном.
/// </summary>
public enum FieldPermission
{
    /// <summary>
    /// Только значение из шаблона. Пользователь не может редактировать.
    /// Применяется к служебным полям (например, ChangeLogRow.Author из метаданных).
    /// </summary>
    Static,

    /// <summary>
    /// Значение пользователя приоритетно.
    /// Если пользователь очистил поле — fallback на значение из шаблона.
    /// </summary>
    Mutable,

    /// <summary>
    /// Шаблон + пользователь сливаются (например, boilerplate + пользовательские примечания).
    /// </summary>
    Dynamic,
}
