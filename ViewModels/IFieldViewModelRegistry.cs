using AvaloniaApp.Models;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// Реестр фабрик ViewModel-ов по типам <see cref="FormFieldDescriptor"/>.
/// Используется MainWindowViewModel.CreateFormFieldViewModel и FormViewModel.UpdateEngine
/// для открытого расширения (OCP): добавление нового типа поля =
/// регистрация новой фабрики в DI, без модификации ядра.
/// </summary>
/// <remarks>
/// <para>
/// API: <see cref="Create"/> делегирует подходящей фабрике.
/// Если ни одна фабрика не подходит — бросает
/// <see cref="System.NotSupportedException"/> с указанием имени дескриптора.
/// </para>
/// </remarks>
public interface IFieldViewModelRegistry
{
    /// <summary>
    /// Создаёт ViewModel для указанного дескриптора, делегируя подходящей фабрике.
    /// </summary>
    ViewModelBase Create(FormFieldDescriptor descriptor, DocumentBody body, IDocumentChangeEngine engine);
}