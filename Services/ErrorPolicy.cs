using System;

namespace AvaloniaApp.Services;

/// <summary>
/// Контекст результата операции: что делать с ошибкой.
/// Позволяет вызывающему коду описать политику один раз, без повторения boilerplate.
/// </summary>
public readonly record struct ErrorPolicy(
    Func<Exception, string>? StatusMessage = null,
    Func<Exception, string>? DialogMessage = null,
    string DialogTitle = "Ошибка",
    bool ShowDialog = true)
{
    /// <summary>
    /// Политика по умолчанию: пишет в StatusMessage и показывает модальный диалог.
    /// </summary>
    public static ErrorPolicy Default => new(
        StatusMessage: ex => $"Ошибка: {ex.Message}",
        DialogMessage: null,
        DialogTitle: "Ошибка",
        ShowDialog: true);

    /// <summary>
    /// Тихая политика: только Debug/Console.Error лог + опциональный StatusMessage.
    /// Используется для некритичных операций (Dispose, hot-reload, периодический refresh).
    /// </summary>
    public static ErrorPolicy Silent(Func<Exception, string>? statusMessage = null) => new(
        StatusMessage: statusMessage,
        DialogMessage: null,
        DialogTitle: string.Empty,
        ShowDialog: false);
}
