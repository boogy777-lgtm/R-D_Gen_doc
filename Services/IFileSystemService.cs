namespace AvaloniaApp.Services;

/// <summary>
/// Сервис файловой системы + запуск внешних процессов (explorer.exe).
/// VM получает этот интерфейс через DI и не зависит от System.IO / Process.
/// </summary>
public interface IFileSystemService
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    string? GetDirectoryName(string path);

    void StartExplorer(string path);

    void StartExplorerWithSelect(string path);
}
