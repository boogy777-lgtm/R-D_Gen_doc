using System;

namespace AvaloniaApp.Models;

/// <summary>
/// Дескриптор поля метаданных документа с разрешением на мутацию
/// и значением по умолчанию из шаблона.
/// Аналог <see cref="FormFieldDescriptor"/> для полей <see cref="DocumentMetadata"/>.
/// </summary>
public sealed class MetadataFieldDescriptor
{
    /// <summary>
    /// Gets or sets ключ поля (для отладки/идентификации).
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets режим взаимодействия с шаблоном.
    /// </summary>
    public FieldPermission Permission { get; init; } = FieldPermission.Mutable;

    /// <summary>
    /// Gets or sets значение по умолчанию из шаблона.
    /// При <see cref="FieldPermission.Mutable"/> используется как fallback,
    /// когда пользователь очистил поле и для поля не задан <see cref="Placeholder"/>.
    /// При <see cref="FieldPermission.Static"/> — единственное допустимое значение.
    /// </summary>
    public string? TemplateDefault { get; init; }

    /// <summary>
    /// Gets or sets placeholder (серый подсказка в UI).
    /// Используется как fallback при пустом значении при генерации MD/DOCX,
    /// но НЕ мутирует исходные метаданные.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// Gets or sets getter строкового значения из метаданных.
    /// </summary>
    public Func<DocumentMetadata, string> ValueGetter { get; init; } = _ => string.Empty;

    /// <summary>
    /// Gets or sets setter строкового значения в метаданные.
    /// </summary>
    public Action<DocumentMetadata, string> ValueSetter { get; init; } = (_, _) => { };

    /// <summary>
    /// Gets or sets признак boolean-поля.
    /// </summary>
    public bool IsBool { get; init; }
}
