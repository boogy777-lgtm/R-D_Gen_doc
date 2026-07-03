using System;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Универсальная фабрика для generic-дескрипторов
/// <see cref="CollectionFieldDescriptor{TItem}"/> → <see cref="CollectionFieldViewModel{TItem}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Эта фабрика обрабатывает ВСЕ generic-варианты коллекций:
/// <see cref="TermPair"/>, <see cref="ChangeLogRow"/>, <see cref="BodyAppendix"/>,
/// <see cref="StringItem"/>. Узкая роль <see cref="ICollectionMutator"/>
/// берётся через upcast от общего <see cref="IDocumentChangeEngine"/>.
/// </para>
/// </remarks>
public sealed class CollectionFieldViewModelFactory : IFieldViewModelFactory
{
    /// <inheritdoc />
    public bool CanHandle(Type descriptorType)
    {
        var t = descriptorType;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(CollectionFieldDescriptor<>))
            {
                return true;
            }

            t = t.BaseType;
        }

        return false;
    }

    /// <inheritdoc />
    public ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine)
    {
        var collectionEngine = engine as ICollectionMutator;
        return descriptor switch
        {
            CollectionFieldDescriptor<TermPair> terms => new CollectionFieldViewModel<TermPair>(terms, body, collectionEngine),
            CollectionFieldDescriptor<ChangeLogRow> changeLog => new CollectionFieldViewModel<ChangeLogRow>(changeLog, body, collectionEngine),
            CollectionFieldDescriptor<BodyAppendix> appendices => new CollectionFieldViewModel<BodyAppendix>(appendices, body, collectionEngine),
            CollectionFieldDescriptor<StringItem> relatedDocs => new CollectionFieldViewModel<StringItem>(relatedDocs, body, collectionEngine),
            _ => throw new System.NotSupportedException(
                $"Коллекционный дескриптор {descriptor.GetType().Name} не поддерживается фабрикой коллекций."),
        };
    }
}