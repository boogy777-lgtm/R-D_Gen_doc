using System;
using AvaloniaApp.Models;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Ролевой контракт (ISP): фабрика ViewModel-ов для конкретного типа
/// <see cref="FormFieldDescriptor"/>. Каждая реализация ответственна
/// за ОДИН тип дескриптора.
/// </summary>
/// <remarks>
/// <para>
/// Реестр фабрик — это реализация паттерна Registry + Strategy.
/// Добавление нового типа поля сводится к регистрации новой фабрики
/// в DI-контейнере — MainWindowViewModel и FormViewModel не меняются (OCP).
/// </para>
/// <para>
/// Контракт двух-уровневый:
/// <list type="number">
///   <item><see cref="CanHandle"/> — фабрика декларирует, какие типы дескрипторов
///         она обслуживает (дешевая проверка без аллокаций).</item>
///   <item><see cref="Create"/> — создаёт VM для конкретного дескриптора.</item>
/// </list>
/// </para>
/// </remarks>
public interface IFieldViewModelFactory
{
    /// <summary>
    /// Проверяет, может ли фабрика обработать дескриптор указанного типа.
    /// Используется реестром для маршрутизации.
    /// </summary>
    /// <param name="descriptorType">Тип дескриптора (например, <c>typeof(SingleFieldDescriptor)</c>).</param>
    /// <returns><c>true</c>, если фабрика знает, как создать VM для этого дескриптора.</returns>
    bool CanHandle(Type descriptorType);

    /// <summary>
    /// Создаёт ViewModel для указанного дескриптора.
    /// </summary>
    /// <param name="descriptor">Дескриптор поля формы.</param>
    /// <param name="body">Тело документа.</param>
    /// <param name="engine">Движок мутаций (принимает общий контракт
    /// <see cref="IDocumentChangeEngine"/>, узкие роли получаются через upcast).</param>
    /// <returns>Готовая ViewModel для биндинга в UI.</returns>
    ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine);
}