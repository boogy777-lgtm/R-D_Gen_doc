using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над Markdown-файловым пикером (open / save).
/// </summary>
public interface IMarkdownFilePicker
{
    Task<string?> OpenAsync();

    Task<string?> SaveAsAsync(string? defaultFileName);
}
