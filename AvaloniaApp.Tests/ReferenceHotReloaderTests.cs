using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaApp.Services;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты для <see cref="FileSystemReferenceHotReloader"/>.
///
/// Используем in-memory фабрику watcher-ов (<see cref="FakeFileSystemWatcher"/>),
/// чтобы не зависеть от реальной файловой системы и не делать тесты flaky.
/// </summary>
public class ReferenceHotReloaderTests
{
    /// <summary>
    /// Количество подпапок справочников, за которыми наблюдает
    /// <see cref="FileSystemReferenceHotReloader"/>. Должно соответствовать
    /// реальному количеству в production-коде, иначе тест ловит рассинхрон.
    /// </summary>
    private const int WatchedSubdirectoryCount = 4;

    [Fact]
    public async Task Start_InvokesOnChanged_WhenMdFileEventArrives()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ref-hot-reload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var fake = new FakeFileSystemWatcher(directory);
            using var reloader = new FileSystemReferenceHotReloader(
                directoryProvider: () => directory,
                watcherFactory: _ => fake);
            var invoked = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            reloader.OnChanged = () =>
            {
                invoked.TrySetResult(1);
                return Task.CompletedTask;
            };

            reloader.Start();

            fake.RaiseChanged("doc.md");

            // Ждём debounce + небольшой запас на ThreadPool.
            var completed = await Task.WhenAny(invoked.Task, Task.Delay(2000));
            Assert.Same(invoked.Task, completed);
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                /* no-op */
            }
        }
    }

    [Fact]
    public async Task Start_DebouncesMultipleEventsIntoSingleCallback()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ref-hot-reload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var fake = new FakeFileSystemWatcher(directory);
            using var reloader = new FileSystemReferenceHotReloader(
                directoryProvider: () => directory,
                watcherFactory: _ => fake);

            var callCount = 0;
            var counterLock = new object();
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            reloader.OnChanged = () =>
            {
                lock (counterLock)
                {
                    callCount++;
                    if (callCount >= 1)
                    {
                        tcs.TrySetResult(callCount);
                    }
                }

                return Task.CompletedTask;
            };

            reloader.Start();

            // 5 быстрых событий подряд — debounce должен схлопнуть их в 1 callback.
            for (var i = 0; i < 5; i++)
            {
                fake.RaiseChanged("doc.md");
            }

            await Task.WhenAny(tcs.Task, Task.Delay(2000));
            // Даём ещё 100 мс, чтобы убедиться, что после первого
            // callback больше ничего не пришло.
            await Task.Delay(100);

            lock (counterLock)
            {
                Assert.Equal(1, callCount);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                /* no-op */
            }
        }
    }

    [Fact]
    public void Start_IsIdempotent_DoesNotCreateDuplicateWatchers()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ref-hot-reload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            // Считаем, сколько раз фабрика watcher-ов была вызвана.
            // Идемпотентность проверяем так: после 3 вызовов Start() фабрика
            // должна быть вызвана ровно 4 раза (по одному watcher-у на
            // каждую из 4 подпапок: типы_документов, подразделения, роли,
            // дисциплины) — и НЕ больше.
            var factoryCalls = 0;
            using var reloader = new FileSystemReferenceHotReloader(
                directoryProvider: () => directory,
                watcherFactory: path =>
                {
                    factoryCalls++;
                    return new FakeFileSystemWatcher(path);
                });

            reloader.Start();
            reloader.Start();
            reloader.Start();

            Assert.Equal(WatchedSubdirectoryCount, factoryCalls);
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                /* no-op */
            }
        }
    }

    [Fact]
    public void Stop_DisablesWatcher_AndDiscardsPendingCallback()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ref-hot-reload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var fake = new FakeFileSystemWatcher(directory);
            using var reloader = new FileSystemReferenceHotReloader(
                directoryProvider: () => directory,
                watcherFactory: _ => fake);

            var called = false;
            reloader.OnChanged = () =>
            {
                called = true;
                return Task.CompletedTask;
            };

            reloader.Start();
            fake.RaiseChanged("doc.md");
            reloader.Stop();

            // После Stop ожидание callback бессмысленно — даже если debounce
            // ещё не выстрелил, мы должны игнорировать его.
            // Даём таймеру шанс сработать, если бы он мог.
            Thread.Sleep(50);
            Assert.False(called, "Stop должен отменить pending debounce.");
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                /* no-op */
            }
        }
    }

    [Fact]
    public void Start_IgnoresNonMarkdownFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ref-hot-reload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var fake = new FakeFileSystemWatcher(directory);
            using var reloader = new FileSystemReferenceHotReloader(
                directoryProvider: () => directory,
                watcherFactory: _ => fake);

            var called = false;
            reloader.OnChanged = () =>
            {
                called = true;
                return Task.CompletedTask;
            };

            reloader.Start();
            fake.RaiseChanged("not-markdown.txt");

            Thread.Sleep(50);
            Assert.False(called);
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                /* no-op */
            }
        }
    }

    /// <summary>
    /// In-memory замена <see cref="FileSystemWatcher"/> для unit-тестов.
    /// Позволяет руками вызывать <see cref="RaiseChanged"/> / <see cref="RaiseRenamed"/>
    /// без реальной файловой системы.
    /// </summary>
    private sealed class FakeFileSystemWatcher : FileSystemWatcher
    {
        public FakeFileSystemWatcher(string path)
            : base(path, "*.md")
        {
        }

        public void RaiseChanged(string fileName)
        {
            OnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path, fileName));
        }

        public void RaiseRenamed(string oldName, string newName)
        {
            OnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, Path, newName, oldName));
        }
    }
}
