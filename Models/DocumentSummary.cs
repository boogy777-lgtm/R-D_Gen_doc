using System;

namespace AvaloniaApp.Models;

/// <summary>
/// Краткая сводка сохранённого документа для списка в вкладке «База данных».
/// </summary>
public sealed record DocumentSummary(
    long Id,
    string DocId,
    string Title,
    string DocType,
    string Revision,
    string Status,
    string EffectiveDate,
    DateTime UpdatedAt);
