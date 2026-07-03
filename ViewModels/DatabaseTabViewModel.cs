using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel вкладки «База данных»: список сохранённых документов, поиск,
/// открытие, экспорт в DOCX и удаление.
/// </summary>
public partial class DatabaseTabViewModel : ViewModelBase
{
    private readonly IDialogService dialogService;
    private readonly IDocumentExportService exportService;
    private readonly IMdFileStore mdStore;
    private readonly Func<string, Task>? openDocumentCallback;

    private IReadOnlyList<DocumentSummary> allDocuments = new List<DocumentSummary>();

    [ObservableProperty]
    private ObservableCollection<DocumentSummary> documents = new();

    [ObservableProperty]
    private DocumentSummary? selectedDocument;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusText = "Нет данных. Нажмите «Обновить список» для загрузки.";

    [ObservableProperty]
    private string lastUpdatedText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTabViewModel"/> class for Avalonia designer.
    /// </summary>
    public DatabaseTabViewModel()
        : this(
            new NullDialogService(),
            new DocumentExportService(),
            new NullMdFileStore(),
            null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTabViewModel"/> class.
    /// </summary>
    public DatabaseTabViewModel(
        IDialogService dialogService,
        IDocumentExportService exportService,
        IMdFileStore mdStore,
        Func<string, Task>? openDocumentCallback)
    {
        this.dialogService = dialogService
            ?? throw new ArgumentNullException(nameof(dialogService));
        this.exportService = exportService
            ?? throw new ArgumentNullException(nameof(exportService));
        this.mdStore = mdStore
            ?? throw new ArgumentNullException(nameof(mdStore));
        this.openDocumentCallback = openDocumentCallback;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedDocumentChanged(DocumentSummary? value)
    {
        NotifySelectionCommandsCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifySelectionCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Перезагружает список документов из БД и применяет текущий фильтр поиска.
    /// </summary>
    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        IsBusy = true;
        try
        {
            allDocuments = await mdStore.ListSummariesAsync();
            ApplyFilter();
            LastUpdatedText = DateTime.Now.ToString("g");
            StatusText = $"Найдено {Documents.Count} документов";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoadDocumentsAsync] {ex}");
            Console.Error.WriteLine($"[LoadDocumentsAsync] {ex}");
            StatusText = $"Ошибка загрузки: {ex.Message}";
            await dialogService.ShowMessageAsync(
                "Ошибка",
                $"Не удалось загрузить список документов:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Открывает выбранный документ в основной форме через callback.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateOnSelectedDocument))]
    private async Task OpenDocumentAsync()
    {
        var docId = SelectedDocument?.DocId;
        if (string.IsNullOrWhiteSpace(docId) || openDocumentCallback is null)
        {
            return;
        }

        try
        {
            await openDocumentCallback(docId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OpenDocumentAsync] {ex}");
            Console.Error.WriteLine($"[OpenDocumentAsync] {ex}");
            StatusText = $"Ошибка открытия: {ex.Message}";
            await dialogService.ShowMessageAsync(
                "Ошибка",
                $"Не удалось открыть документ:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Экспортирует выбранный документ в DOCX.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateOnSelectedDocument))]
    private async Task ExportDocxAsync()
    {
        var selected = SelectedDocument;
        if (selected is null)
        {
            return;
        }

        var defaultName = string.IsNullOrWhiteSpace(selected.DocId)
            ? "document.docx"
            : $"{selected.DocId}.docx";

        var path = await dialogService.SaveDocxFileAsync(defaultName);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var loaded = await mdStore.LoadAsync(selected.DocId);
            if (loaded is null)
            {
                StatusText = $"Документ {selected.DocId} не найден";
                await dialogService.ShowMessageAsync(
                    "Ошибка",
                    $"Документ «{selected.DocId}» не найден в папке docs/.");
                return;
            }

            await exportService.ExportDocxAsync(loaded.Value.Metadata, loaded.Value.Body, path);
            StatusText = $"Экспортирован DOCX: {System.IO.Path.GetFileName(path)}";
            await dialogService.ShowSuccessAsync(
                "Документ экспортирован",
                $"DOCX-файл успешно создан:\n{System.IO.Path.GetFileName(path)}",
                path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExportDocxAsync] {ex}");
            Console.Error.WriteLine($"[ExportDocxAsync] {ex}");
            StatusText = $"Ошибка экспорта: {ex.Message}";
            await dialogService.ShowMessageAsync(
                "Ошибка",
                $"Не удалось экспортировать документ в DOCX:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Удаляет выбранный документ из БД с предупреждением через диалог.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateOnSelectedDocument))]
    private async Task DeleteDocumentAsync()
    {
        var selected = SelectedDocument;
        if (selected is null)
        {
            return;
        }

        var deleteWarning =
            $"ВНИМАНИЕ: будет удалён документ «{selected.DocId}» (ревизия {selected.Revision}).\n" +
            "Эта операция необратима. Нажмите OK для подтверждения удаления.";

        await dialogService.ShowMessageAsync("Подтверждение удаления", deleteWarning);

        try
        {
            await mdStore.DeleteAsync(selected.DocId);
            StatusText = $"Документ «{selected.DocId}» удалён";
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeleteDocumentAsync] {ex}");
            Console.Error.WriteLine($"[DeleteDocumentAsync] {ex}");
            StatusText = $"Ошибка удаления: {ex.Message}";
            await dialogService.ShowMessageAsync(
                "Ошибка",
                $"Не удалось удалить документ:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Условие доступности команд, зависящих от выбранного документа.
    /// </summary>
    private bool CanOperateOnSelectedDocument()
        => SelectedDocument is not null && !IsBusy;

    private void NotifySelectionCommandsCanExecuteChanged()
    {
        OpenDocumentCommand.NotifyCanExecuteChanged();
        ExportDocxCommand.NotifyCanExecuteChanged();
        DeleteDocumentCommand.NotifyCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var filter = SearchText?.Trim() ?? string.Empty;
        IEnumerable<DocumentSummary> filtered = allDocuments;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filtered = allDocuments.Where(d =>
                d.DocId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                d.Title.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        Documents = new ObservableCollection<DocumentSummary>(filtered);
        StatusText = $"Найдено {Documents.Count} документов";
    }

    private sealed class NullDialogService : IDialogService
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
}
