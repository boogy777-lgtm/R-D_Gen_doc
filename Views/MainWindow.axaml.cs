using Avalonia.Controls;
using AvaloniaApp.ViewModels;
using FluentAvalonia.UI.Windowing;

namespace AvaloniaApp.Views;

public partial class MainWindow : FAAppWindow
{
    /// <summary>
    /// Конструктор для DI-контейнера. Получает <see cref="MainWindowViewModel"/>
    /// через конструкторскую инъекцию.
    /// </summary>
    /// <remarks>
    /// <para>DI-контейнер автоматически резолвит все зависимости <paramref name="viewModel"/>.</para>
    /// <para>Композиция View ↔ VM остаётся здесь, в code-behind — это единственное место,
    /// где <c>Window</c> и <c>ViewModel</c> встречаются (composition root View-слоя).</para>
    /// <para>Использует <see cref="FAAppWindow"/> из FluentAvalonia — расширенный
    /// <c>Window</c> с WinUI-стилизованной шапкой (titlebar), Mica-эффектом
    /// (Windows 11) и правильным слиянием с in-app overlay-диалогами
    /// <c>FAContentDialog</c>.</para>
    /// </remarks>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // FluentAvalonia UI: Extend content into title bar для красивого
        // «бесшовного» окна с in-app overlay-диалогами FAContentDialog.
        // На Windows 11 это даёт Mica-эффект; на других платформах
        // — стандартный OS-заголовок (без расширения, если не поддерживается).
        if (this.WindowState != WindowState.FullScreen)
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            // Цвет titlebar совпадает с брендовым серым (#F0F0F0).
            TitleBar.BackgroundColor = Avalonia.Media.Color.Parse("#F0F0F0");
            TitleBar.InactiveBackgroundColor = Avalonia.Media.Color.Parse("#F0F0F0");
            TitleBar.ForegroundColor = Avalonia.Media.Color.Parse("#202020");
            TitleBar.InactiveForegroundColor = Avalonia.Media.Color.Parse("#7C1800");
        }

        Loaded += async (_, _) => await viewModel.InitializeAsync();
        Closed += (_, _) => viewModel.Dispose();
    }

    /// <summary>
    /// Конструктор для дизайнера XAML (без DI-контейнера). Использует
    /// дефолтные сервисы для preview в IDE.
    /// </summary>
    public MainWindow()
        : this(new MainWindowViewModel())
    {
    }
}
