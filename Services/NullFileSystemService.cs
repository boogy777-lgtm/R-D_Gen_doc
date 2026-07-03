namespace AvaloniaApp.Services;

/// <summary>
/// No-op реализация <see cref="IFileSystemService"/> для unit-тестов
/// и для дизайнера. Все операции возвращают «не найдено» / no-op.
/// </summary>
public sealed class NullFileSystemService : IFileSystemService
{
    public bool FileExists(string path) => false;

    public bool DirectoryExists(string path) => false;

    public string? GetDirectoryName(string path) => null;

    public void StartExplorer(string path)
    {
    }

    public void StartExplorerWithSelect(string path)
    {
    }
}
