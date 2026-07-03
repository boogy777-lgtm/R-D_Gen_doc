using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Реализация <see cref="IDialogService"/> поверх Avalonia-окон.
/// Вынесена из <c>MainWindow.axaml.cs</c> (SRP): ранее Window совмещал
/// роли View + Dialog-фабрика + бойлерплейт UI-конструирования (200+ строк
/// code-behind). Теперь Window — только View, а диалоги инкапсулированы
/// в отдельных View + ViewModel парах.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly IWindowProvider windowProvider;
    private readonly IAboutDialogFactory aboutDialogFactory;
    private readonly IMarkdownFilePicker filePicker;
    private readonly IDocxFilePicker docxFilePicker;
    private readonly IMessageDialogFactory messageDialogFactory;
    private readonly IConfirmationDialogFactory confirmationDialogFactory;
    private readonly ISuccessOverlayPresenter successOverlay;

    public AvaloniaDialogService(
        IWindowProvider windowProvider,
        IAboutDialogFactory aboutDialogFactory,
        IMarkdownFilePicker filePicker,
        IDocxFilePicker docxFilePicker,
        IMessageDialogFactory messageDialogFactory,
        IConfirmationDialogFactory confirmationDialogFactory,
        ISuccessOverlayPresenter successOverlay)
    {
        this.windowProvider = windowProvider;
        this.aboutDialogFactory = aboutDialogFactory;
        this.filePicker = filePicker;
        this.docxFilePicker = docxFilePicker;
        this.messageDialogFactory = messageDialogFactory;
        this.confirmationDialogFactory = confirmationDialogFactory;
        this.successOverlay = successOverlay;
    }

    public Task<string?> OpenMarkdownFileAsync() => filePicker.OpenAsync();

    public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) =>
        filePicker.SaveAsAsync(defaultFileName);

    public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) =>
        docxFilePicker.SaveAsAsync(defaultFileName);

    public Task ShowMessageAsync(string title, string message) =>
        messageDialogFactory.ShowAsync(title, message);

    public Task<bool> ShowConfirmationAsync(string title, string message) =>
        confirmationDialogFactory.ShowAsync(title, message);

    public Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates) =>
        NewDocumentDialogShim.ShowAsync(templates);

    public Task ShowAboutAsync() =>
        aboutDialogFactory.ShowAsync(windowProvider.MainWindow);

    public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) =>
        successOverlay.ShowAsync(title, message, filePath, offerOpenInExplorer);
}
