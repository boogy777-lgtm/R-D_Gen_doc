using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvaloniaApp.Models;

/// <summary>
/// Строка журнала изменений документа.
/// </summary>
public class ChangeLogRow : INotifyPropertyChanged
{
    private long id;
    private int position;
    private string date = string.Empty;
    private string revision = string.Empty;
    private string reviewer = string.Empty;
    private string author = string.Empty;
    private string description = string.Empty;
    private bool isReadOnly;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets идентификатор строки в SQLite.
    /// </summary>
    public long Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    /// <summary>
    /// Gets or sets порядковый номер строки.
    /// </summary>
    public int Position
    {
        get => position;
        set => SetProperty(ref position, value);
    }

    /// <summary>
    /// Gets or sets дата изменения в формате ДД.ММ.ГГГГ.
    /// </summary>
    public string Date
    {
        get => date;
        set => SetProperty(ref date, value);
    }

    /// <summary>
    /// Gets or sets номер редакции.
    /// </summary>
    public string Revision
    {
        get => revision;
        set => SetProperty(ref revision, value);
    }

    /// <summary>
    /// Gets or sets проверивший изменение.
    /// </summary>
    public string Reviewer
    {
        get => reviewer;
        set => SetProperty(ref reviewer, value);
    }

    /// <summary>
    /// Gets or sets автор изменения.
    /// </summary>
    public string Author
    {
        get => author;
        set => SetProperty(ref author, value);
    }

    /// <summary>
    /// Gets or sets описание изменения.
    /// </summary>
    public string Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    /// <summary>
    /// Gets or sets признак блокировки исторической строки.
    /// Только последняя строка ЛРИ доступна для редактирования.
    /// Управляется <see cref="Services.DocumentChangeEngine"/>.
    /// </summary>
    public bool IsReadOnly
    {
        get => isReadOnly;
        set => SetProperty(ref isReadOnly, value);
    }

    /// <summary>
    /// Заполняет служебные колонки строки ЛРИ значениями из метаданных.
    /// </summary>
    public void SyncFromMetadata(DocumentMetadata metadata, string formattedRevision)
    {
        if (metadata is null)
        {
            return;
        }

        Date = metadata.EffectiveDate;
        Revision = formattedRevision ?? string.Empty;
        Author = metadata.Author;
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
