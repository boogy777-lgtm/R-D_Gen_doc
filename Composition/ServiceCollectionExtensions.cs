using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using AvaloniaApp.Views;
using AvaloniaApp.Views.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaApp.Composition;

/// <summary>
/// Централизованная регистрация всех сервисов приложения в DI-контейнере.
/// Вызывается из <c>App.OnFrameworkInitializationCompleted</c> (composition root).
/// </summary>
/// <remarks>
/// <para>Живёт в <c>AvaloniaApp.Composition</c> (а не в <c>Services</c>), потому что
/// знает обо всех слоях (Services ↔ ViewModels ↔ Views) — это часть composition root,
/// а не домен.</para>
/// <para>Правила регистрации:</para>
/// <list type="bullet">
///   <item><b>Singletons</b>: stateless сервисы и шины событий.</item>
///   <item><b>Transients</b>: ViewModel и View — создаются при каждом resolve.</item>
/// </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы AvaloniaApp в <paramref name="services"/>.
    /// </summary>
    public static IServiceCollection AddAvaloniaAppServices(this IServiceCollection services)
    {
        // --- Domain-сервисы (singletons) ---
        services.AddSingleton<TemplateCatalog>();

        // --- Каталоги defaults (DIP: интерфейс + singleton-реализация) ---
        // Реализации по умолчанию — Singleton, т.к. они stateless или read-only
        // (IFormsCatalog, IMetadataCatalog). IReferenceDataService — Singleton
        // потому что хранит состояние SeedStatusMessage.
        services.AddSingleton<IReferenceDataService, ReferenceDataService>();
        services.AddSingleton<IFormsCatalog, FormsCatalog>();
        services.AddSingleton<IMetadataCatalog, MetadataCatalog>();

        // --- IO-сервисы (transients / singletons) ---
        services.AddTransient<IMarkdownFileService, MarkdownFileService>();
        services.AddTransient<IDocumentExportService, DocumentExportService>();
        services.AddTransient<IDocumentSaveService, DocumentSaveService>();
        services.AddSingleton<IMarkdownPreviewBuilder, MarkdownPreviewBuilder>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<IFileSystemService, SystemFileSystemService>();
        services.AddSingleton<IUriLauncherService, SystemUriLauncherService>();
        services.AddSingleton<IMdFileStore, MdFileStore>();
        services.AddSingleton<IReferenceHotReloader, FileSystemReferenceHotReloader>();

        // --- ViewModel factory registry (OCP) ---
        // Каждая фабрика ответственна за один тип дескриптора.
        // Реестр собирает их все и маршрутизирует descriptor → VM по типу.
        // Добавление нового типа поля = регистрация новой фабрики, без изменений в MainWindowViewModel.
        services.AddSingleton<IFieldViewModelFactory, SingleFieldViewModelFactory>();
        services.AddSingleton<IFieldViewModelFactory, CollectionFieldViewModelFactory>();
        services.AddSingleton<IFieldViewModelFactory, TreeFieldViewModelFactory>();
        services.AddSingleton<FieldViewModelRegistry>();
        services.AddSingleton<IFieldViewModelRegistry>(sp => sp.GetRequiredService<FieldViewModelRegistry>());

        // --- DI-абстракции View-уровня ---
        services.AddSingleton<IWindowProvider, DesktopWindowProvider>();
        services.AddSingleton<IAboutDialogFactory, AvaloniaAboutDialogFactory>();
        services.AddSingleton<IMarkdownFilePicker, AvaloniaMarkdownFilePicker>();
        services.AddSingleton<IDocxFilePicker, AvaloniaDocxFilePicker>();
        services.AddSingleton<IMessageDialogFactory, AvaloniaMessageDialogFactory>();
        services.AddSingleton<IConfirmationDialogFactory, AvaloniaConfirmationDialogFactory>();
        services.AddSingleton<ISuccessOverlayPresenter, MainWindowSuccessOverlayPresenter>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();

        // --- ViewModel-ы ---
        services.AddSingleton<DatabaseTabViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // --- Views ---
        services.AddTransient<MainWindow>();

        return services;
    }
}
