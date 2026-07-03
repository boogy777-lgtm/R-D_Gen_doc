using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;

namespace AvaloniaApp.Tools;

/// <summary>
/// CLI-тест: воспроизводит сценарий 3 кнопок без UI.
/// Если тесты проходят — NRE в UI-слое. Если падают — в логике.
/// </summary>
internal static class HeadlessButtonRepro
{
    public static async Task<int> RunAsync()
    {
        Console.WriteLine("=== Headless Button Repro ===");

        // Создаём настоящий VM с stub-диалогом (без UI) и пытаемся выполнить команды.
        // Это имитирует нажатие кнопок, минуя Avalonia UI.
        var stub1 = new StubDialogService();
        var vm = new MainWindowViewModel(
            dialogService: stub1,
            markdownFileService: new MarkdownFileService(),
            exportService: new DocumentExportService(),
            saveService: new DocumentSaveService(),
            previewBuilder: new MarkdownPreviewBuilder(),
            clipboard: new StubClipboardService(),
            fileSystem: new StubFileSystemService(),
            mdStore: new MdFileStore());

        // Симулируем InitializeAsync: даём VM валидный DocumentBody.
        vm.Metadata = new DocumentMetadata { DocId = "REPRO-1", Title = "Тест" };
        vm.DocumentBody = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(vm.DocumentBody, vm.Metadata);

        Console.WriteLine($"VM state: Metadata={vm.Metadata.DocId}, DocumentBody set");

        // Вызываем private метод SaveToDatabaseAsync через рефлексию (имитация клика).
        Console.WriteLine($"\n[1/3] Click 'SaveToDatabaseCommand'...");
        try
        {
            var method = typeof(MainWindowViewModel).GetMethod("SaveToDatabaseAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method!.Invoke(vm, null)!;
            Console.WriteLine($"  OK: StatusMessage='{vm.StatusMessage}'");
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            Console.WriteLine($"  FAIL: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}");
            Console.WriteLine($"  Stack: {tie.InnerException.StackTrace}");
            return 1;
        }

        Console.WriteLine($"\n[2/3] Click 'ExportMarkdownCommand'...");
        try
        {
            // Подменяем IDialogService на stub, который возвращает путь сохранения.
            var stub = new SaveDialogStub(Path.Combine(Path.GetTempPath(), $"repro_{Guid.NewGuid():N}.md"));
            var dialogField = typeof(MainWindowViewModel).GetField("_dialogService", BindingFlags.NonPublic | BindingFlags.Instance);
            dialogField!.SetValue(vm, stub);

            var method = typeof(MainWindowViewModel).GetMethod("ExportMarkdownAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method!.Invoke(vm, null)!;
            Console.WriteLine($"  OK: StatusMessage='{vm.StatusMessage}'");
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            Console.WriteLine($"  FAIL: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}");
            Console.WriteLine($"  Stack: {tie.InnerException.StackTrace}");
            return 2;
        }

        Console.WriteLine($"\n[3/3] Click 'ExportDocxCommand'...");
        try
        {
            var stub = new SaveDialogStub(Path.Combine(Path.GetTempPath(), $"repro_{Guid.NewGuid():N}.docx"));
            var dialogField = typeof(MainWindowViewModel).GetField("_dialogService", BindingFlags.NonPublic | BindingFlags.Instance);
            dialogField!.SetValue(vm, stub);

            var method = typeof(MainWindowViewModel).GetMethod("ExportDocxAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method!.Invoke(vm, null)!;
            Console.WriteLine($"  OK: StatusMessage='{vm.StatusMessage}'");
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            Console.WriteLine($"  FAIL: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}");
            Console.WriteLine($"  Stack: {tie.InnerException.StackTrace}");
            return 3;
        }

        Console.WriteLine("\n=== ALL 3 BUTTONS WORK ===");
        return 0;
    }

    /// <summary>Stub диалога: возвращает заранее заданный путь.</summary>
    private sealed class SaveDialogStub : IDialogService
    {
        private readonly string path;

        public SaveDialogStub(string path) => this.path = path;

        public Task<string?> OpenMarkdownFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(path);

        public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(path);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

        public Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates) => Task.FromResult<string?>(null);

        public Task ShowAboutAsync() => Task.CompletedTask;
    }

    /// <summary>Stub без пути — для SaveToDatabaseAsync (не требует файлового диалога).</summary>
    private sealed class StubDialogService : IDialogService
    {
        public Task<string?> OpenMarkdownFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

        public Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates) => Task.FromResult<string?>(null);

        public Task ShowAboutAsync() => Task.CompletedTask;
    }

    private sealed class StubClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text) => Task.CompletedTask;
    }

    private sealed class StubFileSystemService : IFileSystemService
    {
        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => false;

        public string? GetDirectoryName(string path) => null;

        public void StartExplorer(string path)
        {
        }

        public void StartExplorerWithSelect(string path)
        {
        }
    }
}
