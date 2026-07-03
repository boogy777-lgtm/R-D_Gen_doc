using System;

namespace AvaloniaApp.Models;

/// <summary>
/// Дескриптор одиночного текстового поля (одно- или многострочного).
/// </summary>
public sealed class SingleFieldDescriptor : FormFieldDescriptor
{
    /// <summary>
    /// Gets or sets признак многострочного поля.
    /// </summary>
    public bool IsMultiline { get; init; }

    /// <summary>
    /// Gets or sets getter значения из <see cref="DocumentBody"/>.
    /// </summary>
    public Func<DocumentBody, string?> ValueGetter { get; init; } = _ => string.Empty;

    /// <summary>
    /// Gets or sets setter значения, делегирующий в <see cref="IDocumentChangeEngine"/>
    /// или напрямую изменяющий <see cref="DocumentBody"/>.
    /// </summary>
    public Action<DocumentBody, string?, IDocumentChangeEngine?> ValueSetter { get; init; } = (_, _, _) => { };

    /// <summary>
    /// Gets or sets значение по умолчанию из шаблона.
    /// Используется как fallback при <see cref="FieldPermission.Mutable"/>,
    /// когда пользователь очистил поле.
    /// При <see cref="FieldPermission.Static"/> — единственное допустимое значение.
    /// </summary>
    public string? TemplateDefault { get; init; }
}
