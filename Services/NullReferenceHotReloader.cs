using System;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// No-op реализация <see cref="IReferenceHotReloader"/> для unit-тестов
/// и для parameterless-конструктора <c>MainWindowViewModel</c>,
/// где нежелательно поднимать <c>FileSystemWatcher</c> в temp-каталоге.
/// </summary>
public sealed class NullReferenceHotReloader : IReferenceHotReloader
{
    public Func<Task>? OnChanged { get; set; }

    public void Start()
    {
        // no-op
    }

    public void Stop()
    {
        // no-op
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
