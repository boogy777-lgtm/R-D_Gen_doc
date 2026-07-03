using AvaloniaApp.Models;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Ролевой контракт: ViewModel, которая может принять обновлённый движок мутаций.
/// Используется <see cref="FormViewModel"/> для распространения
/// нового <see cref="IDocumentChangeEngine"/> на все дочерние VM без
/// жёсткого switch по типам (OCP).
/// </summary>
/// <remarks>
/// <para>
/// Добавление нового типа <see cref="ViewModelBase"/> с поддержкой движка =
/// реализация этого интерфейса. FormViewModel остаётся неизменной.
/// </para>
/// </remarks>
public interface IEngineAware
{
    /// <summary>
    /// Заменяет движок мутаций.
    /// Если <paramref name="newEngine"/> — null, VM продолжает работать
    /// без движка (используется при сбросе документа).
    /// </summary>
    void UpdateEngine(IDocumentChangeEngine? newEngine);
}