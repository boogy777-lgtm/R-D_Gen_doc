using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Регион ответственности: Лист регистрации изменений (ЛРИ).
///
/// Извлечено из <see cref="DocumentChangeEngine"/> для соблюдения SRP.
/// Владеет логикой:
/// <list type="bullet">
///   <item>Добавление/удаление строк ЛРИ (с учётом ревизии и инкремента).</item>
///   <item>Перенумерация позиций строк ЛРИ.</item>
///   <item>Read-only флаг исторических строк (редактируема только последняя).</item>
///   <item>Получение последней строки и проверка синхронности с метаданными.</item>
/// </list>
///
/// Не зависит от UI и Avalonia — чистая доменная логика.
/// </summary>
public sealed class ChangeLogManager
{
    private readonly DocumentBody body;
    private readonly DocumentMetadata metadata;

    /// <summary>
    /// Создаёт менеджер ЛРИ для указанного тела и метаданных.
    /// </summary>
    public ChangeLogManager(DocumentBody body, DocumentMetadata metadata)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Возвращает последнюю строку ЛРИ (по Position) или null.
    /// </summary>
    public ChangeLogRow? GetLastRow() =>
        body.ChangeLog.OrderBy(r => r.Position).LastOrDefault();

    /// <summary>
    /// Проверяет, отличаются ли текущие метаданные (Revision/EffectiveDate)
    /// от последней строки ЛРИ.
    /// </summary>
    public bool IsOutOfSync()
    {
        var lastRow = GetLastRow();
        if (lastRow is null)
        {
            return true;
        }

        var trimmedRevision = RevisionFormatter.Trim(metadata.Revision);
        return lastRow.Revision != trimmedRevision
            || lastRow.Date != metadata.EffectiveDate;
    }

    /// <summary>
    /// Добавляет новую строку ЛРИ. При <paramref name="autoIncrementRevision"/> = true
    /// автоматически увеличивает номер редакции (max из существующих строк и Metadata.Revision + 1).
    /// Иначе копирует текущие значения из метаданных.
    /// </summary>
    public void AddRow(bool autoIncrementRevision = false)
    {
        if (autoIncrementRevision)
        {
            // Найти максимальную ревизию: максимум из существующих строк и Metadata.Revision
            int maxRev = RevisionFormatter.ParseInteger(metadata.Revision);
            foreach (var r in body.ChangeLog)
            {
                int rev = RevisionFormatter.ParseInteger(r.Revision);
                if (rev > maxRev)
                {
                    maxRev = rev;
                }
            }

            // Инкремент и обновление метаданных
            metadata.Revision = RevisionFormatter.Increment(
                maxRev > 0 ? maxRev.ToString() : null);
        }

        var row = DocumentBodyFactory.CreatePrefilledChangeLogRow(metadata, body.ChangeLog);
        body.ChangeLog.Add(row);
        RenumberFrom(body.ChangeLog.Count - 1);
        UpdateReadOnlyFlags();
    }

    /// <summary>
    /// Удаляет строку ЛРИ (no-op, если строка не найдена).
    /// </summary>
    public void RemoveRow(ChangeLogRow row)
    {
        if (row is null)
        {
            return;
        }

        int index = body.ChangeLog.IndexOf(row);
        if (index < 0)
        {
            return;
        }

        body.ChangeLog.RemoveAt(index);
        RenumberFrom(index);
        UpdateReadOnlyFlags();
    }

    /// <summary>
    /// Полностью заменяет список строк ЛРИ на новый (используется при открытии .md файла).
    /// </summary>
    public void Replace(IEnumerable<ChangeLogRow> rows)
    {
        body.ChangeLog.Clear();
        if (rows != null)
        {
            foreach (var r in rows)
            {
                body.ChangeLog.Add(r);
            }
        }

        RenumberFrom(0);
        UpdateReadOnlyFlags();
    }

    /// <summary>
    /// Перенумеровывает позиции всех строк ЛРИ начиная с 0.
    /// Используется после полной перезаписи коллекции.
    /// </summary>
    public void RenumberAll() => RenumberFrom(0);

    /// <summary>
    /// Обновляет флаг <see cref="ChangeLogRow.IsReadOnly"/> для всех строк ЛРИ.
    /// Только последняя строка доступна для редактирования.
    /// </summary>
    public void UpdateReadOnlyFlags()
    {
        var lastRow = GetLastRow();
        foreach (var row in body.ChangeLog)
        {
            row.IsReadOnly = row != lastRow;
        }
    }

    /// <summary>
    /// Перенумеровывает позиции строк ЛРИ начиная с <paramref name="startIndex"/>.
    /// Применяет правило n+1: перенумерация только от затронутого индекса.
    /// </summary>
    private void RenumberFrom(int startIndex)
    {
        for (int i = startIndex; i < body.ChangeLog.Count; i++)
        {
            body.ChangeLog[i].Position = i;
        }
    }
}
