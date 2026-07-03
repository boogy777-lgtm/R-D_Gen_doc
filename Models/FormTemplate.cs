using System.Collections.Generic;
using System.Linq;

namespace AvaloniaApp.Models;

public class FormTemplate
{
    public string Name { get; init; } = "UniversalDocumentForm";

    public List<FormFieldDescriptor> HeaderFields { get; init; } = new();

    public List<FormFieldDescriptor> CenterFields { get; init; } = new();

    public List<FormFieldDescriptor> TrailingFields { get; init; } = new();

    public List<BodySection> StarterSections { get; init; } = new();

    /// <summary>
    /// Все дескрипторы полей (Header + Center + Trailing).
    /// </summary>
    public IEnumerable<FormFieldDescriptor> AllDescriptors()
        => HeaderFields.Concat(CenterFields).Concat(TrailingFields);
}
