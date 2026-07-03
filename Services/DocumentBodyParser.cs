using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Markdown;

namespace AvaloniaApp.Services;

/// <summary>
    /// Простой парсер markdown → DocumentBody.
    /// Все маркеры разметки (заголовки, паттерны) берутся из
    /// <see cref="MarkdownSyntaxDictionary"/>.
    /// </summary>
    public static class DocumentBodyParser
{
    /// <summary>
    /// Извлекает DocumentBody из markdown. Не идеален — работает только с выводом DocumentBodyToMarkdown.
    /// </summary>
    public static DocumentBody Parse(string markdownBody)
    {
        var body = new DocumentBody();

        if (string.IsNullOrWhiteSpace(markdownBody))
        {
            return body;
        }

        var lines = markdownBody.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        int i = 0;

        while (i < lines.Count)
        {
            var line = lines[i];

            // Section header (supports hierarchy: «4», «4.1», «4.1.1»)
            var sectionMatch = MarkdownSyntaxDictionary.SectionHeader.Match(line);
            if (sectionMatch.Success)
            {
                string numberPath = sectionMatch.Groups[1].Value;
                string title = sectionMatch.Groups[2].Value;

                // Collect content until next header
                var content = new StringBuilder();
                i++;
                while (i < lines.Count && !IsNewHeader(lines[i]))
                {
                    content.AppendLine(lines[i]);
                    i++;
                }

                var contentText = content.ToString().Trim();

                // Top-level section numbers: 1, 2, 3 — фиксированные секции.
                // 4+ — пользовательские секции центральной части.
                int topLevelNumber = ParseTopLevelNumber(numberPath);

                if (topLevelNumber == 1 && title.Contains(MarkdownSyntaxDictionary.PurposeTitleMarker))
                {
                    body.Purpose = contentText;
                }
                else if (topLevelNumber == 2 && title.Contains(MarkdownSyntaxDictionary.ScopeTitleMarker))
                {
                    body.Scope = contentText;
                }
                else if (topLevelNumber == 3 && title.Contains(MarkdownSyntaxDictionary.TermsTitleMarker))
                {
                    int pos = 0;
                    foreach (Match m in MarkdownSyntaxDictionary.BulletTerm.Matches(contentText))
                    {
                        body.Terms.Add(new TermPair
                        {
                            Position = pos++,
                            Term = m.Groups[1].Value,
                            Definition = m.Groups[2].Value,
                        });
                    }
                }
                else if (topLevelNumber >= MarkdownSyntaxDictionary.FirstUserSectionNumber && topLevelNumber <= 20)
                {
                    // Иерархия: «4» — верхний, «4.1» — подраздел верхнего, и т.д.
                    AddSectionHierarchical(body, numberPath, title, contentText);
                }

                continue;
            }

            // Appendix header: «## Приложение N»
            var appendixMatch = MarkdownSyntaxDictionary.AppendixHeader.Match(line);
            if (appendixMatch.Success)
            {
                int appendixNumber = int.Parse(appendixMatch.Groups[1].Value);

                var content = new StringBuilder();
                i++;
                while (i < lines.Count && !IsNewHeader(lines[i]))
                {
                    content.AppendLine(lines[i]);
                    i++;
                }

                var appendix = new BodyAppendix
                {
                    Position = appendixNumber,
                    Title = string.Empty,
                    Body = string.Empty,
                };

                var fullText = content.ToString();
                var sections = SplitAppendixSections(fullText);

                if (sections.Count > 0)
                {
                    // Первая секция — это контент ДО первого ### (заголовок приложения + body).
                    var first = sections[0];
                    string firstTitle = first.Title ?? string.Empty;

                    // Если первая секция не выглядит как таблица — это заголовок приложения.
                    if (!string.IsNullOrWhiteSpace(firstTitle)
                        && !firstTitle.StartsWith(MarkdownSyntaxDictionary.TableTitleMarker)
                        && !firstTitle.StartsWith(MarkdownSyntaxDictionary.UnderscoreMarker))
                    {
                        appendix.Title = firstTitle.Trim();
                        appendix.Body = first.Body?.Trim() ?? string.Empty;
                    }
                    else if (!string.IsNullOrWhiteSpace(first.Body))
                    {
                        appendix.Body = first.Body.Trim();
                    }

                    // Остальные секции — таблицы (### Таблица № N).
                    for (int si = 1; si < sections.Count; si++)
                    {
                        var sec = sections[si];
                        appendix.Sections.Add(new BodySection
                        {
                            Position = si - 1,
                            Title = sec.Title?.Trim() ?? string.Empty,
                            Body = sec.Body?.Trim() ?? string.Empty,
                        });
                    }
                }
                else
                {
                    appendix.Body = fullText.Trim();
                }

                // Всегда добавляем хотя бы одну подсекцию-таблицу, если их нет.
                if (appendix.Sections.Count == 0)
                {
                    appendix.Sections.Add(new BodySection
                    {
                        Position = 0,
                        Title = string.Empty,
                        Body = string.Empty,
                    });
                }

                body.Appendices.Add(appendix);
                continue;
            }

            // ## Обучение
            if (line.StartsWith(MarkdownSyntaxDictionary.Level2Prefix)
                && line.Contains(MarkdownSyntaxDictionary.TrainingHeader.Substring(MarkdownSyntaxDictionary.Level2PrefixLength)))
            {
                i++;
                string? currentSub = null;
                var subContent = new StringBuilder();
                while (i < lines.Count && !IsTrainingSectionEnd(lines[i]))
                {
                    if (lines[i].StartsWith(MarkdownSyntaxDictionary.PrimaryTrainingSubHeader))
                    {
                        if (currentSub != null && subContent.Length > 0)
                        {
                            ApplyTrainingSubBody(body, currentSub, subContent);
                        }

                        currentSub = MarkdownSyntaxDictionary.PrimaryTrainingMarker;
                        subContent.Clear();
                        i++;
                        continue;
                    }

                    if (lines[i].StartsWith(MarkdownSyntaxDictionary.RecurrentTrainingSubHeader))
                    {
                        if (currentSub != null && subContent.Length > 0)
                        {
                            ApplyTrainingSubBody(body, currentSub, subContent);
                        }

                        currentSub = MarkdownSyntaxDictionary.RecurrentTrainingMarker;
                        subContent.Clear();
                        i++;
                        continue;
                    }

                    subContent.AppendLine(lines[i]);
                    i++;
                }

                if (currentSub != null && subContent.Length > 0)
                {
                    ApplyTrainingSubBody(body, currentSub, subContent);
                }

                continue;
            }

            // ## Ссылки
            if (line.StartsWith(MarkdownSyntaxDictionary.Level2Prefix)
                && line.Contains(MarkdownSyntaxDictionary.ReferencesHeader.Substring(MarkdownSyntaxDictionary.Level2PrefixLength)))
            {
                i++;
                var content = new StringBuilder();
                while (i < lines.Count && !IsTrainingSectionEnd(lines[i]))
                {
                    content.AppendLine(lines[i]);
                    i++;
                }

                var text = content.ToString();
                var subMatches = MarkdownSyntaxDictionary.SubHeader.Matches(text);
                foreach (Match sub in subMatches)
                {
                    if (sub.Groups[1].Value.Contains(MarkdownSyntaxDictionary.ParentDocumentTitleMarker))
                    {
                        var start = sub.Index + sub.Length;
                        var end = subMatches.Cast<Match>().Where(m => m.Index > sub.Index).Select(m => m.Index).DefaultIfEmpty(text.Length).Min();
                        body.ParentDocument = MdFormatHelper.Unformat(text.Substring(start, end - start).Trim(), MdSyntax.Wikilink);
                    }
                    else if (sub.Groups[1].Value.Contains(MarkdownSyntaxDictionary.RelatedDocumentsTitleMarker))
                    {
                        var start = sub.Index + sub.Length;
                        var end = subMatches.Cast<Match>().Where(m => m.Index > sub.Index).Select(m => m.Index).DefaultIfEmpty(text.Length).Min();
                        var subText = text.Substring(start, end - start);
                        foreach (Match b in MarkdownSyntaxDictionary.BulletPlain.Matches(subText))
                        {
                            if (!string.IsNullOrWhiteSpace(b.Groups[1].Value))
                            {
                                body.RelatedDocuments.Add(new StringItem
                                {
                                    Value = MdFormatHelper.Unformat(b.Groups[1].Value.Trim(), MdSyntax.Wikilink),
                                });
                            }
                        }
                    }
                }

                continue;
            }

            // ## Лист регистрации изменений
            if (line.StartsWith(MarkdownSyntaxDictionary.Level2Prefix)
                && line.Contains(MarkdownSyntaxDictionary.ChangeLogHeader.Substring(MarkdownSyntaxDictionary.Level2PrefixLength)))
            {
                i++;
                int pos = 0;
                while (i < lines.Count && !lines[i].StartsWith(MarkdownSyntaxDictionary.Level1Prefix))
                {
                    var rowLine = lines[i];
                    if (string.IsNullOrWhiteSpace(rowLine)
                        || rowLine.Contains(MarkdownSyntaxDictionary.LegacyChangeLogDateHeader)
                        || rowLine.Contains(MarkdownSyntaxDictionary.ChangeLogDateHeaderToken)
                        || rowLine.Contains(MarkdownSyntaxDictionary.MarkdownTableSeparatorMarker))
                    {
                        i++;
                        continue;
                    }

                    if (rowLine.StartsWith("|"))
                    {
                        var cells = rowLine.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                        if (cells.Length >= 5)
                        {
                            body.ChangeLog.Add(new ChangeLogRow
                            {
                                Position = pos++,
                                Date = cells[0],
                                Revision = cells[1],
                                Author = cells[2],
                                Reviewer = cells[3],
                                Description = cells[4],
                            });
                        }
                    }

                    i++;
                }

                continue;
            }

            i++;
        }

        return body;
    }

    /// <summary>
    /// Определяет, что строка — новый заголовок (любого уровня) или code-fence.
    /// Используется как граница сбора контента секции.
    /// </summary>
    private static bool IsNewHeader(string line) =>
        line.StartsWith(MarkdownSyntaxDictionary.Level2Prefix)
        || line.StartsWith(MarkdownSyntaxDictionary.Level1Prefix)
        || line.StartsWith(MarkdownSyntaxDictionary.CodeFenceMarker);

    /// <summary>
    /// Определяет границу для секций Обучение/Ссылки: следующий «## » или «# ».
    /// </summary>
    private static bool IsTrainingSectionEnd(string line) =>
        line.StartsWith(MarkdownSyntaxDictionary.Level2Prefix)
        || line.StartsWith(MarkdownSyntaxDictionary.Level1Prefix);

    /// <summary>
    /// Применяет накопленное содержимое подсекции обучения к <paramref name="body"/>.
    /// </summary>
    private static void ApplyTrainingSubBody(DocumentBody body, string subKind, StringBuilder subContent)
    {
        var value = subContent.ToString().Trim();
        if (subKind == MarkdownSyntaxDictionary.PrimaryTrainingMarker)
        {
            body.PrimaryTraining = value;
        }
        else
        {
            body.RecurrentTraining = value;
        }
    }

    /// <summary>
    /// Извлекает верхний уровень номера из «4.1.2» → 4.
    /// </summary>
    private static int ParseTopLevelNumber(string numberPath)
    {
        var dotIndex = numberPath.IndexOf('.');
        var head = dotIndex >= 0 ? numberPath[..dotIndex] : numberPath;
        return int.TryParse(head, out var n) ? n : -1;
    }

    /// <summary>
    /// Добавляет секцию в дерево с учётом иерархии. «4.1» становится подразделом «4»,
    /// «4.1.2» — подразделом «4.1» (подразделом «4») и т.д.
    /// </summary>
    private static void AddSectionHierarchical(DocumentBody body, string numberPath, string title, string contentText)
    {
        var parts = numberPath.Split('.');
        int topLevel = int.Parse(parts[0]);
        int topLevelIndex = topLevel - MarkdownSyntaxDictionary.FirstUserSectionNumber;

        // Наращиваем список верхнего уровня до нужного размера.
        while (body.Sections.Count <= topLevelIndex)
        {
            body.Sections.Add(new BodySection
            {
                Position = body.Sections.Count,
                Title = string.Empty,
                Body = string.Empty,
            });
        }

        // Спускаемся по иерархии: находим или создаём вложенные секции.
        var current = body.Sections[topLevelIndex];
        if (string.IsNullOrEmpty(current.Title))
        {
            // Верхняя секция ещё без названия — это первое вхождение, ставим title/body.
            current.Title = title;
            current.Body = contentText;
            return;
        }

        // Углубляемся: для каждого следующего уровня добавляем подраздел.
        for (int level = 1; level < parts.Length; level++)
        {
            int childIndex = int.Parse(parts[level]) - 1;
            while (current.Children.Count <= childIndex)
            {
                current.Children.Add(new BodySection
                {
                    Position = current.Children.Count,
                    Title = string.Empty,
                    Body = string.Empty,
                });
            }

            var next = current.Children[childIndex];
            if (string.IsNullOrEmpty(next.Title) && level == parts.Length - 1)
            {
                next.Title = title;
                next.Body = contentText;
                return;
            }

            current = next;
        }

        // Если дошли сюда — секция дублируется. Добавляем как есть (с авто-инкрементом Position).
        current.Children.Add(new BodySection
        {
            Position = current.Children.Count,
            Title = title,
            Body = contentText,
        });
    }

    /// <summary>
    /// Разделяет содержимое приложения на секции по заголовкам ###.
    /// Возвращает список (Title, Body); Title — текст после ### (может быть null).
    /// Первый элемент — контент до первого ### (заголовок приложения или body).
    /// </summary>
    private static List<(string? Title, string? Body)> SplitAppendixSections(string text)
    {
        var result = new List<(string? Title, string? Body)>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var lines = text.Split('\n');
        int start = 0;
        string? currentTitle = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.StartsWith(MarkdownSyntaxDictionary.Level3Prefix))
            {
                // Сохраняем предыдущую секцию (от start до i-1).
                var body = string.Join("\n", lines[start..i]).Trim();
                result.Add((currentTitle, string.IsNullOrWhiteSpace(body) ? null : body));

                // Начинаем новую секцию.
                currentTitle = line[MarkdownSyntaxDictionary.Level3PrefixLength..].Trim();
                start = i + 1;
            }
        }

        // Последняя секция.
        var lastBody = string.Join("\n", lines[start..]).Trim();
        result.Add((currentTitle, string.IsNullOrWhiteSpace(lastBody) ? null : lastBody));

        return result;
    }
}
