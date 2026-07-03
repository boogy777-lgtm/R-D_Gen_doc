namespace AvaloniaApp.Models;

/// <summary>
/// Дескриптор иерархической коллекции секций (дерево).
/// </summary>
public sealed class TreeFieldDescriptor : FormFieldDescriptor
{
    /// <summary>
    /// Gets or sets значение по умолчанию из шаблона (одно значение-строка).
    /// При пустой иерархии секций используется как fallback при
    /// <see cref="FieldPermission.Mutable"/>.
    /// Наследует общий <see cref="FormFieldDescriptor.Placeholder"/>.
    /// </summary>
    public string? TemplateDefault { get; init; }
}
