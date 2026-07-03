using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты поведения кнопки «+» в редакторе Листа регистрации изменений.
///
/// Требование: кнопка «+» НЕ должна добавлять строку в ChangeLog напрямую.
/// Вместо этого она должна вызывать popup-диалог подтверждения (тот же,
/// что появляется при изменении ревизии/даты в метаданных). Только после
/// согласия пользователя строка действительно добавляется.
///
/// Регрессия: ранее кнопка «+» обходила popup и сразу вызывала
/// <see cref="IDocumentChangeEngine.AddChangeLogRow"/>, что нарушало
/// бизнес-процесс (ЛРИ фиксирует только осознанные изменения).
/// </summary>
public class ChangeLogAddButtonTests
{
    [Fact]
    public async Task ChangeLogField_AddButton_PopsUpConfirmationDialog()
    {
        // Arrange: VM со StubDialogService, который вернёт true.
        var stub = new StubDialogService { ConfirmationResult = true };
        var vm = new MainWindowViewModel(stub);
        await vm.InitializeAsync();

        var changeLogField = vm.ChangeLogFieldViewModel;
        Assert.NotNull(changeLogField);

        var initialCount = vm.DocumentBody!.ChangeLog.Count;

        // Act: нажимаем «+» в редакторе ЛРИ.
        changeLogField!.AddCommand.Execute(null);

        // AsyncRelayCommand.Execute — fire-and-forget; даём тесту
        // шанс «прокрутить» задачу до завершения (xUnit без SynchronizationContext).
        await Task.Yield();
        await Task.Delay(50);

        // Assert: popup был показан ровно один раз.
        Assert.Equal(1, stub.ConfirmationCallCount);

        // Assert: строка добавлена (пользователь подтвердил).
        Assert.Equal(initialCount + 1, vm.DocumentBody!.ChangeLog.Count);
    }

    [Fact]
    public async Task ChangeLogField_AddButton_DoesNotAddRow_WhenUserCancels()
    {
        // Arrange: VM со StubDialogService, который вернёт false.
        var stub = new StubDialogService { ConfirmationResult = false };
        var vm = new MainWindowViewModel(stub);
        await vm.InitializeAsync();

        var changeLogField = vm.ChangeLogFieldViewModel;
        Assert.NotNull(changeLogField);

        var initialCount = vm.DocumentBody!.ChangeLog.Count;

        // Act: нажимаем «+».
        changeLogField!.AddCommand.Execute(null);
        await Task.Yield();
        await Task.Delay(50);

        // Assert: popup был показан.
        Assert.Equal(1, stub.ConfirmationCallCount);

        // Assert: ключевое требование регрессии — строка НЕ добавлена
        // при отказе пользователя.
        Assert.Equal(initialCount, vm.DocumentBody!.ChangeLog.Count);
    }

    [Fact]
    public async Task ChangeLogField_AddButton_PromptsEvenWhenRevisionUnchanged()
    {
        // Arrange: не меняем ни ревизию, ни дату — только жмём «+».
        // Popup всё равно должен появиться, потому что кнопка «+»
        // не зависит от авто-триггера по метаданным.
        var stub = new StubDialogService { ConfirmationResult = true };
        var vm = new MainWindowViewModel(stub);
        await vm.InitializeAsync();

        var initialCount = vm.DocumentBody!.ChangeLog.Count;

        // Act: нажимаем «+» два раза подряд.
        vm.ChangeLogFieldViewModel!.AddCommand.Execute(null);
        await Task.Yield();
        await Task.Delay(50);
        vm.ChangeLogFieldViewModel!.AddCommand.Execute(null);
        await Task.Yield();
        await Task.Delay(50);

        // Assert: было два popup-а (по одному на каждое нажатие).
        Assert.Equal(2, stub.ConfirmationCallCount);

        // Assert: добавились две строки.
        Assert.Equal(initialCount + 2, vm.DocumentBody!.ChangeLog.Count);
    }

    [Fact]
    public async Task ChangeLogField_UsesPopupService_RatherThanDirectAddAction()
    {
        // Arrange: создаём VM с обычным конструктором (NullDialogService).
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Проверяем, что дескриптор ЛРИ в BuildFormFieldViewModels использует
        // фабрику команды, а не прямую AddAction.
        var formViewModel = vm.FormViewModel;
        Assert.NotNull(formViewModel);

        // Найти ChangeLog-поле среди полей формы — это поле с Label
        // «Лист регистрации изменений». Однако в BuildFormFieldViewModels
        // ЛРИ НЕ включается (ЛРИ выводится отдельным полем на вкладке
        // «Метаданные», а не в форме). Значит, нужно проверить свойство
        // ChangeLogFieldViewModel напрямую.
        var changeLogField = vm.ChangeLogFieldViewModel;
        Assert.NotNull(changeLogField);

        // Assert: команда «Добавить» — AsyncRelayCommand (наш popup-обёртка),
        // а не синхронный RelayCommand (AddAction напрямую).
        // AsyncRelayCommand не наследует RelayCommand синхронно, поэтому
        // тип команды должен быть именно асинхронным.
        Assert.IsType<CommunityToolkit.Mvvm.Input.AsyncRelayCommand>(changeLogField!.AddCommand);
    }

    /// <summary>
    /// Тестовый диалог-сервис с программно заданным ответом на
    /// <see cref="ShowConfirmationAsync"/> и счётчиком вызовов.
    /// </summary>
    private sealed class StubDialogService : IDialogService
    {
        public int ConfirmationCallCount { get; private set; }

        /// <summary>
        /// Ответ, который вернёт <see cref="ShowConfirmationAsync"/>.
        /// </summary>
        public bool ConfirmationResult { get; set; }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            ConfirmationCallCount++;
            return Task.FromResult(ConfirmationResult);
        }

        // Прочие методы IDialogService тестам не нужны — оставляем заглушки.
        public Task<string?> OpenMarkdownFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) => Task.CompletedTask;

        public Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates) => Task.FromResult<string?>(null);

        public Task ShowAboutAsync() => Task.CompletedTask;
    }
}
