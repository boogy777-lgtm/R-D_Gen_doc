using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция для диалогов приложения.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Показывает диалог открытия Markdown-файла. Возвращает путь или null.
    /// </summary>
    Task<string?> OpenMarkdownFileAsync();

    /// <summary>
    /// Показывает диалог сохранения Markdown-файла. Возвращает путь или null.
    /// </summary>
    Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null);

    /// <summary>
    /// Показывает диалог сохранения DOCX-файла. Возвращает путь или null.
    /// </summary>
    Task<string?> SaveDocxFileAsync(string? defaultFileName = null);

    /// <summary>
    /// Показывает простое сообщение.
    /// </summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Показывает диалог успеха с полным путём к сохранённому/открытому/экспортированному файлу.
    /// Поддерживает кнопки «Скопировать путь», «Открыть в проводнике», «ОК».
    /// </summary>
    /// <param name="title">Заголовок диалога.</param>
    /// <param name="message">Краткое описание операции (например, «Экспортирован DOCX»).</param>
    /// <param name="filePath">Полный путь к файлу (или БД-идентификатор для SaveToDatabase).</param>
    /// <param name="offerOpenInExplorer">Показать кнопку «Открыть в проводнике». False для не-файловых операций (SQLite).</param>
    Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true);

    /// <summary>
    /// Показывает диалог подтверждения. Возвращает true, если пользователь нажал «Да».
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Показывает диалог выбора фабричного шаблона. Возвращает код выбранного шаблона или null.
    /// </summary>
    Task<string?> ShowNewDocumentDialogAsync(IReadOnlyList<TemplateSummary> templates);

    /// <summary>
    /// Показывает окно «О программе».
    /// </summary>
    Task ShowAboutAsync();
}
