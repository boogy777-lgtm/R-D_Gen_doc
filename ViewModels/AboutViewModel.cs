using System;
using System.Reflection;
using System.Threading.Tasks;
using AvaloniaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel окна «О программе».
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    private const string CompanyUrl = "https://www.burservis.ru/niokr";
    private const string ObsidianUrl = "https://obsidian.md";

    private readonly IUriLauncherService uriLauncher;

    [ObservableProperty]
    private string appName = "Генератор документации";

    [ObservableProperty]
    private string versionText = string.Empty;

    [ObservableProperty]
    private string author = "Семенишин Руслан";

    [ObservableProperty]
    private string companyLinkText = CompanyUrl;

    [ObservableProperty]
    private string obsidianLinkText = ObsidianUrl;

    /// <summary>
    /// Инициализирует ViewModel с системной заглушкой лаунчера (для дизайнера и тестов).
    /// </summary>
    public AboutViewModel()
        : this(new Services.NullUriLauncherService())
    {
    }

    /// <summary>
    /// Инициализирует ViewModel с указанным сервисом открытия URI.
    /// </summary>
    public AboutViewModel(IUriLauncherService uriLauncher)
    {
        this.uriLauncher = uriLauncher
            ?? throw new ArgumentNullException(nameof(uriLauncher));

        VersionText = FormatVersion();
    }

    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler? CloseRequested;

    private static string FormatVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "—";
        }

        // AssemblyVersion обычно содержит 4 компонента; показываем Major.Minor.Build.
        return $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void DebugTrace(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        System.Console.Error.WriteLine(message);
    }

    [RelayCommand]
    private async Task OpenCompanyLinkAsync()
    {
        try
        {
            await uriLauncher.LaunchAsync(new Uri(CompanyUrl, UriKind.Absolute));
        }
        catch (Exception ex)
        {
            // Подавляем ошибку открытия ссылки: не прерываем работу приложения.
            DebugTrace($"[OpenCompanyLinkAsync] {ex}");
        }
    }

    [RelayCommand]
    private async Task OpenObsidianLinkAsync()
    {
        try
        {
            await uriLauncher.LaunchAsync(new Uri(ObsidianUrl, UriKind.Absolute));
        }
        catch (Exception ex)
        {
            DebugTrace($"[OpenObsidianLinkAsync] {ex}");
        }
    }

    [RelayCommand]
    private void Ok()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
