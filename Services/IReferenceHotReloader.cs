using System;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Hot-reloader папки справочников <c>docs/_справочники/</c>.
///
/// Подписывается на изменения любых <c>*.md</c>-файлов внутри папки
/// (типы документов, подразделения, роли, дисциплины) и уведомляет
/// подписчика через <see cref="OnChanged"/> после debounce-окна.
///
/// Интерфейс нужен, чтобы ViewModel мог зависеть только от абстракции,
/// а реализация через <c>System.IO.FileSystemWatcher</c> жила в Services
/// (Clean Architecture: инфраструктурная деталь инкапсулирована за интерфейсом).
///
/// Потокобезопасность: <see cref="OnChanged"/> вызывается синхронно с тем
/// потоком, в котором сработал debounce-timer (ThreadPool по умолчанию).
/// Подписчик должен сам маршалить вызовы на UI-поток (например, через
/// <c>Avalonia.Threading.Dispatcher.UIThread.Post</c>) при работе с VM.
/// </summary>
public interface IReferenceHotReloader : IDisposable
{
    /// <summary>
    /// Делегат, вызываемый после debounce-окна при изменении любого
    /// <c>*.md</c>-файла в отслеживаемой папке. Должен вернуть
    /// <see cref="Task"/>, чтобы подписчик мог выполнить async-перезагрузку.
    /// </summary>
    Func<Task>? OnChanged { get; set; }

    /// <summary>
    /// Запускает наблюдение за папкой. Повторный вызов без
    /// промежуточного <see cref="Stop"/> — no-op.
    /// Безопасно вызывать, если папка ещё не создана — наблюдение
    /// стартует при её появлении (см. реализацию).
    /// </summary>
    void Start();

    /// <summary>
    /// Останавливает наблюдение и сбрасывает pending debounce.
    /// После <see cref="Stop"/> можно снова вызвать <see cref="Start"/>.
    /// </summary>
    void Stop();
}
