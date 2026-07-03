using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaApp.Services;

namespace AvaloniaApp.Models;

/// <summary>
/// Фабрика стартовых строк для коллекций DocumentBody.
/// Вынесена из View, чтобы ViewModel могла использовать её без нарушения Clean Architecture
/// (VM не должен зависеть от AvaloniaApp.Views).
/// </summary>
public static class DocumentBodyFactory
{
    /// <summary>
    /// Создаёт одну пустую строку термина (для предзаполнения формы).
    /// </summary>
    public static TermPair CreateEmptyTermPair() => new()
    {
        Position = 0,
        Term = string.Empty,
        Definition = string.Empty,
    };

    /// <summary>
    /// Создаёт пустое приложение с одной подсекцией-таблицей.
    /// Номер таблицы формируется через Position (0 → «Таблица № 1»).
    /// </summary>
    public static BodyAppendix CreateEmptyAppendix()
    {
        var appendix = new BodyAppendix
        {
            Position = 0,
            Title = string.Empty,
            Body = string.Empty,
        };

        appendix.Sections.Add(new BodySection
        {
            Title = string.Empty,
            Body = string.Empty,
            Level = 1,
            Position = 0,
        });

        return appendix;
    }

    /// <summary>
    /// Создаёт стартовую строку Листа регистрации изменений.
    /// Если переданы метаданные — Date/Revision/Author берутся из них (Revision обрезается).
    /// Иначе используются сегодняшняя дата, ревизия «1» и пустой автор.
    /// </summary>
    public static ChangeLogRow CreatePrefilledChangeLogRow(
        DocumentMetadata? metadata = null,
        IEnumerable<ChangeLogRow>? existing = null)
    {
        var row = new ChangeLogRow
        {
            Description = string.Empty,
        };

        if (metadata != null)
        {
            row.SyncFromMetadata(metadata, RevisionFormatter.Trim(metadata.Revision));
        }

        // Fallback-значения, если метаданные не заполнены.
        if (string.IsNullOrWhiteSpace(row.Date) || row.Date == "ДД.ММ.ГГГГ")
        {
            row.Date = DateTime.Today.ToString("dd.MM.yyyy");
        }

        if (string.IsNullOrWhiteSpace(row.Revision))
        {
            row.Revision = "1";
        }

        return row;
    }

    /// <summary>
    /// Создаёт стартовый набор коллекций для нового документа:
    /// 1 пустая строка Terms + 1 строка ChangeLog.
    /// </summary>
    public static (ObservableCollection<TermPair> Terms, ObservableCollection<ChangeLogRow> ChangeLog) CreateStarterCollections()
    {
        return (
            new ObservableCollection<TermPair> { CreateEmptyTermPair() },
            new ObservableCollection<ChangeLogRow> { CreatePrefilledChangeLogRow() });
    }

    /// <summary>
    /// Гарантирует, что все обязательные поля формы имеют хотя бы одну стартовую строку.
    /// Применяется единообразно после любого пути входа: NewDocument / OpenFile / LoadFromDatabase.
    /// Не перезаписывает существующие непустые данные — только дополняет отсутствующие.
    /// </summary>
    public static void EnsureRequiredFields(DocumentBody body, DocumentMetadata? metadata = null)
    {
        if (body is null)
        {
            return;
        }

        // Terms: если список пуст — добавляем одну пустую строку.
        if (body.Terms.Count == 0)
        {
            body.Terms.Add(CreateEmptyTermPair());
        }

        // ChangeLog: если список пуст — добавляем одну prefilled строку.
        if (body.ChangeLog.Count == 0)
        {
            body.ChangeLog.Add(CreatePrefilledChangeLogRow(metadata));
        }

        // RelatedDocuments: если список пуст — добавляем одну пустую строку,
        // как и для Terms. Иначе UI показывает пустую коллекцию без placeholder-строки.
        if (body.RelatedDocuments.Count == 0)
        {
            body.RelatedDocuments.Add(new StringItem { Value = string.Empty });
        }

        // Sections: если список пуст (но только если шаблон требует секций — здесь не проверяем).
        // StarterSections применяются в NewDocument; для OpenFile/LoadFromDatabase оставляем как есть.

        // Поля Purpose, Scope, PrimaryTraining, RecurrentTraining, ParentDocument — string,
        // уже инициализированы пустой строкой в POCO. UI покажет их как пустые TextBox'ы
        // с PlaceholderText, что даёт пользователю подсказку "Опишите цель документа".
    }
}
