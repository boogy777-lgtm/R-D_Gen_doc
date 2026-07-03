using System.Threading.Tasks;

namespace AvaloniaApp.Services;

/// <summary>
/// Абстракция над DOCX-файловым пикером (save only).
/// </summary>
public interface IDocxFilePicker
{
    Task<string?> SaveAsAsync(string? defaultFileName);
}
