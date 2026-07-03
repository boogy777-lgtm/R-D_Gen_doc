using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IReferenceHotReloader"/> через
/// <see cref="FileSystemWatcher"/> с debounce-окном.
///
/// Особенности:
/// <list type="bullet">
///   <item>Подписывается на <c>Changed</c>/<c>Created</c>/<c>Deleted</c>/<c>Renamed</c>
///         с <c>Filter = "*.md"</c> для каждой подпапки справочников
///         (типы_документов, подразделения, роли, дисциплины).</item>
///   <item>Debounce 500 мс через <see cref="Timer"/>: множественные события
///         от Obsidian/редактора (save → rename → save) схлопываются в один
///         коллбэк <see cref="OnChanged"/>.</item>
///   <item>Все обработчики событий FileSystemWatcher обёрнуты в try/catch —
///         падение одного callback не должно ломать наблюдение.</item>
///   <item><see cref="Dispose"/> останавливает таймер и все watcher-ы.</item>
/// </list>
///
/// Потокобезопасность: <see cref="OnChanged"/> вызывается из ThreadPool
/// (callback <see cref="Timer"/>). Подписчик (ViewModel) должен сам маршалить
/// результат на UI-поток при обращении к ObservableProperty.
/// </summary>
public sealed class FileSystemReferenceHotReloader : IReferenceHotReloader
{
    /// <summary>
    /// Debounce-окно в миллисекундах. 500 мс — компромисс между
    /// отзывчивостью UI и пропуском шумных событий Obsidian при save.
    /// </summary>
    private const int DebounceMilliseconds = 500;

    /// <summary>
    /// Подпапки внутри <c>docs/_справочники/</c>, за которыми наблюдаем.
    /// Соответствуют <see cref="ReferenceDataService"/>: GetDocTypesAsync,
    /// GetDepartmentsAsync, GetApproverRolesAsync, GetDisciplineCodesAsync.
    /// </summary>
    private static readonly string[] WatchedSubdirectories =
    {
        "типы_документов",
        "подразделения",
        "роли",
        "дисциплины",
    };

    private readonly object lockObject = new();
    private readonly string baseDirectory;
    private readonly Func<string> directoryProvider;
    private readonly Func<string, FileSystemWatcher> watcherFactory;

    private List<FileSystemWatcher>? watchers;
    private Timer? debounceTimer;
    private bool disposed;

    /// <summary>
    /// Создаёт hot-reloader для <see cref="ReferenceDataService.GetRefStoreDir"/>.
    /// Используется в production: директория вычисляется из <c>Environment.ProcessPath</c>.
    /// </summary>
    public FileSystemReferenceHotReloader()
        : this(directoryProvider: ReferenceDataService.StaticGetRefStoreDir, watcherFactory: CreateWatcher)
    {
    }

    /// <summary>
    /// Конструктор для тестов: позволяет подменить директорию и фабрику
    /// <see cref="FileSystemWatcher"/>, чтобы не зависеть от реального
    /// <c>Environment.ProcessPath</c> и реальной файловой системы.
    /// </summary>
    /// <param name="directoryProvider">Lazy-провайдер пути к папке _справочники/.</param>
    /// <param name="watcherFactory">Фабрика watcher-ов (для теста: in-memory).</param>
    public FileSystemReferenceHotReloader(
        Func<string> directoryProvider,
        Func<string, FileSystemWatcher> watcherFactory)
    {
        this.directoryProvider = directoryProvider
            ?? throw new ArgumentNullException(nameof(directoryProvider));
        this.watcherFactory = watcherFactory
            ?? throw new ArgumentNullException(nameof(watcherFactory));
        this.baseDirectory = directoryProvider();
    }

    /// <inheritdoc />
    public Func<Task>? OnChanged { get; set; }

    /// <inheritdoc />
    public void Start()
    {
        lock (lockObject)
        {
            if (disposed || watchers != null)
            {
                return;
            }

            watchers = new List<FileSystemWatcher>(WatchedSubdirectories.Length);

            foreach (var sub in WatchedSubdirectories)
            {
                var subPath = Path.Combine(baseDirectory, sub);

                // Папка может не существовать при первом запуске (Obsidian
                // сгенерирует её при первом save). Создаём каталог, чтобы
                // FileSystemWatcher мог за ним наблюдать.
                try
                {
                    Directory.CreateDirectory(subPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileSystemReferenceHotReloader] Cannot create '{subPath}': {ex.Message}");
                    continue;
                }

                try
                {
                    var watcher = watcherFactory(subPath);
                    watcher.NotifyFilter = NotifyFilters.FileName
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.Size
                                         | NotifyFilters.CreationTime;

                    watcher.Changed += OnFsEvent;
                    watcher.Created += OnFsEvent;
                    watcher.Deleted += OnFsEvent;
                    watcher.Renamed += OnFsRenamed;

                    // InternalBufferSize по умолчанию 8KB — на Windows может
                    // не хватить при сохранении крупных .md. 64KB — безопасно.
                    watcher.InternalBufferSize = 65536;

                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileSystemReferenceHotReloader] Watcher for '{subPath}' failed: {ex.Message}");
                }
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (lockObject)
        {
            DisposeWatchers();

            if (debounceTimer != null)
            {
                debounceTimer.Dispose();
                debounceTimer = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (lockObject)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            DisposeWatchers();

            if (debounceTimer != null)
            {
                debounceTimer.Dispose();
                debounceTimer = null;
            }

            GC.SuppressFinalize(this);
        }
    }

    private static bool IsMarkdown(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var ext = Path.GetExtension(path);
        return string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Фабрика <see cref="FileSystemWatcher"/> по умолчанию. Вынесена в
    /// отдельный метод, чтобы её можно было подменить в тестах.
    /// </summary>
    private static FileSystemWatcher CreateWatcher(string path)
    {
        return new FileSystemWatcher(path, "*.md")
        {
            IncludeSubdirectories = false,
        };
    }

    private void DisposeWatchers()
    {
        if (watchers == null)
        {
            return;
        }

        foreach (var w in watchers)
        {
            try
            {
                w.EnableRaisingEvents = false;
                w.Changed -= OnFsEvent;
                w.Created -= OnFsEvent;
                w.Deleted -= OnFsEvent;
                w.Renamed -= OnFsRenamed;
                w.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileSystemReferenceHotReloader] Watcher dispose failed: {ex.Message}");
            }
        }

        watchers = null;
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsMarkdown(e.FullPath))
        {
            return;
        }

        ScheduleCallback();
    }

    private void OnFsRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsMarkdown(e.FullPath) && !IsMarkdown(e.OldFullPath))
        {
            return;
        }

        ScheduleCallback();
    }

    /// <summary>
    /// Планирует вызов <see cref="OnChanged"/> через <see cref="DebounceMilliseconds"/>.
    /// Повторные вызовы сбрасывают таймер (classic debounce).
    /// </summary>
    private void ScheduleCallback()
    {
        lock (lockObject)
        {
            if (disposed)
            {
                return;
            }

            if (debounceTimer == null)
            {
                debounceTimer = new Timer(OnDebounceElapsed, null, DebounceMilliseconds, Timeout.Infinite);
            }
            else
            {
                debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        var callback = OnChanged;
        if (callback == null)
        {
            return;
        }

        try
        {
            // Fire-and-log: исключение внутри async-цепочки подписчика
            // не должно ломать watcher. Подписчик сам логирует через Debug.
            var task = callback();
            if (task is not null)
            {
                _ = task.ContinueWith(
                    t => Debug.WriteLine($"[FileSystemReferenceHotReloader] OnChanged failed: {t.Exception?.GetBaseException().Message}"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileSystemReferenceHotReloader] OnDebounceElapsed: {ex.Message}");
        }
    }
}
