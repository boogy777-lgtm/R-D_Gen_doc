using System;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Фабрика для <see cref="SingleFieldDescriptor"/> → <see cref="SingleFieldViewModel"/>.
/// </summary>
public sealed class SingleFieldViewModelFactory : IFieldViewModelFactory
{
    /// <inheritdoc />
    public bool CanHandle(Type descriptorType) => descriptorType == typeof(SingleFieldDescriptor);

    /// <inheritdoc />
    public ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine)
    {
        var fieldEngine = engine as ISingleFieldMutator;
        return new SingleFieldViewModel((SingleFieldDescriptor)descriptor, body, fieldEngine);
    }
}