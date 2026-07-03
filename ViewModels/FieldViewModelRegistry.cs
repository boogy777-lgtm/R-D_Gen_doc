using System;
using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Реализация <see cref="IFieldViewModelRegistry"/>.
/// Маршрутизирует <see cref="FormFieldDescriptor"/> к подходящей
/// <see cref="IFieldViewModelFactory"/> через <see cref="IFieldViewModelFactory.CanHandle"/>.
/// </summary>
/// <remarks>
/// <para>
/// Каждая фабрика декларирует, какие типы дескрипторов она обслуживает,
/// через <see cref="IFieldViewModelFactory.CanHandle"/>. Это избегает
/// reflection и хрупких сигнатурных сравнений.
/// </para>
/// <para>
/// Добавление нового типа поля:
/// <list type="number">
///   <item>Создать <c>MyFieldViewModelFactory : IFieldViewModelFactory</c>
///         с реализацией <see cref="IFieldViewModelFactory.CanHandle"/>.</item>
///   <item>Зарегистрировать в <c>ServiceCollectionExtensions</c>:
///       <c>services.AddSingleton&lt;IFieldViewModelFactory, MyFieldViewModelFactory&gt;();</c>.</item>
///   <item>Реестр автоматически подхватит новую фабрику через DI.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class FieldViewModelRegistry : IFieldViewModelRegistry
{
    private readonly IReadOnlyList<IFieldViewModelFactory> factories;

    /// <summary>
    /// Создаёт реестр. Все фабрики инжектируются через DI.
    /// </summary>
    /// <param name="factories">Все зарегистрированные фабрики
    /// (DI автоматически собирает <c>IEnumerable&lt;IFieldViewModelFactory&gt;</c>).</param>
    public FieldViewModelRegistry(IEnumerable<IFieldViewModelFactory> factories)
    {
        this.factories = new List<IFieldViewModelFactory>(factories);
    }

    /// <inheritdoc />
    public ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(engine);

        var descriptorType = descriptor.GetType();

        foreach (var factory in factories)
        {
            if (factory.CanHandle(descriptorType))
            {
                return factory.Create(descriptor, body, engine);
            }
        }

        throw new NotSupportedException(
            $"Дескриптор {descriptorType.Name} (key='{descriptor.Key}') " +
            $"не поддерживается ни одной зарегистрированной фабрикой.");
    }
}