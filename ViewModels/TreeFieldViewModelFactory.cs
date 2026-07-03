using System;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Фабрика для <see cref="TreeFieldDescriptor"/> → <see cref="SectionsTreeViewModel"/>.
/// </summary>
public sealed class TreeFieldViewModelFactory : IFieldViewModelFactory
{
    /// <inheritdoc />
    public bool CanHandle(Type descriptorType) => descriptorType == typeof(TreeFieldDescriptor);

    /// <inheritdoc />
    public ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine)
    {
        var tree = (TreeFieldDescriptor)descriptor;
        var treeEngine = engine as ITreeMutator
            ?? throw new InvalidOperationException(
                "Движок должен реализовывать ITreeMutator для TreeFieldDescriptor.");

        return new SectionsTreeViewModel(body, treeEngine)
        {
            Label = tree.Label,
        };
    }
}