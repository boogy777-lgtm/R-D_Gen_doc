using System;
using System.Diagnostics;
using System.IO;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IFileSystemService"/> через System.IO + Process.Start.
/// </summary>
public sealed class SystemFileSystemService : IFileSystemService
{
    private const string ExplorerSelectVerb = "/select,";

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public void StartExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemFileSystemService.StartExplorer] {ex}");
        }
    }

    public void StartExplorerWithSelect(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"{ExplorerSelectVerb}\"{path}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemFileSystemService.StartExplorerWithSelect] {ex}");
        }
    }
}
