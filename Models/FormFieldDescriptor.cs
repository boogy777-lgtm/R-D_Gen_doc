namespace AvaloniaApp.Models;

/// <summary>
/// Базовый дескриптор поля динамической формы.
/// Описывает данные (метаданные поля), но не знает про Avalonia-контролы.
/// Конкретный View для дескриптора выбирается в <see cref="Views.FormControl"/>
/// через pattern matching по наследнику.
/// </summary>
public abstract class FormFieldDescriptor
{
    /// <summary>
    /// Gets or sets ключ поля (используется для отладки/идентификации).
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets текст метки поля.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets значение, разрешено ли редактирование.
    /// </summary>
    public bool IsEditable { get; init; } = true;

    /// <summary>
    /// Gets or sets placeholder для текстовых полей.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// Gets or sets вспомогательный текст под полем.
    /// </summary>
    public string? HelpText { get; init; }

    /// <summary>
    /// Gets or sets признак обязательного поля.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets or sets режим взаимодействия поля с шаблоном.
    /// <see cref="FieldPermission.Static"/> — значение из шаблона, пользователь не редактирует.
    /// <see cref="FieldPermission.Mutable"/> — пользователь > шаблон (fallback при пустом поле).
    /// <see cref="FieldPermission.Dynamic"/> — слияние шаблона и пользовательского ввода.
    /// </summary>
    public FieldPermission Permission { get; init; } = FieldPermission.Mutable;
}
