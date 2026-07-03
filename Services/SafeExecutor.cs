using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Единая утилита безопасного выполнения асинхронных операций.
/// Заменяет три копии <c>SafeExecuteAsync</c> в <c>MainWindowViewModel</c>,
/// <c>DocumentOpenCoordinator</c> и <c>DocumentExportCoordinator</c> (DRY).
/// </summary>
/// <remarks>
/// <para>Поведение:</para>
/// <list type="bullet">
///   <item>Любое исключение внутри <paramref name="action"/> перехватывается;</item>
///   <item>Логируется в <see cref="Debug"/> с именем вызывающего метода
///       (подставляется компилятором через <see cref="CallerMemberNameAttribute"/>);</item>
///   <item>Опционально дублируется в <see cref="Console.Error"/> через <paramref name="extraLog"/>;</item>
///   <item>Опционально пишет в <paramref name="setStatusMessage"/>;</item>
///   <item>Опционально показывает модальный диалог через <paramref name="dialog"/>.</item>
/// </list>
/// </remarks>
public static class SafeExecutor
{
    /// <summary>
    /// Выполняет <paramref name="action"/> в защищённом контексте с явной
    /// политикой обработки ошибок. Диалог/StatusMessage управляются через
    /// <paramref name="policy"/>.
    /// </summary>
    public static async Task RunAsync(
        Func<Task> action,
        ErrorPolicy policy = default,
        IDialogService? dialog = null,
        Action<string>? setStatusMessage = null,
        Action<Exception>? extraLog = null,
        [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{operation}] {ex}");
            extraLog?.Invoke(ex);

            var status = policy.StatusMessage?.Invoke(ex) ?? $"Ошибка: {ex.Message}";
            if (setStatusMessage != null && !string.IsNullOrEmpty(status))
            {
                setStatusMessage(status);
            }

            if (policy.ShowDialog && dialog != null)
            {
                var message = policy.DialogMessage?.Invoke(ex) ?? ex.Message;
                await dialog.ShowMessageAsync(policy.DialogTitle, message);
            }
        }
    }
}
