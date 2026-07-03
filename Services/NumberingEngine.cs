using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;
using AvaloniaApp.Services.Markdown;

namespace AvaloniaApp.Services;

/// <summary>
/// Регион ответственности: нумерация и иерархия секций/приложений/терминов/таблиц.
///
/// Извлечено из <see cref="DocumentChangeEngine"/> для соблюдения SRP.
/// Владеет логикой:
/// <list type="bullet">
///   <item>Поддержка <see cref="BodySection.HierarchyPath"/>, <see cref="BodySection.Level"/>,
///         <see cref="BodySection.Position"/>, <see cref="BodySection.ParentId"/>.</item>
///   <item>Поддержка <see cref="BodySection.GlobalTableIndex"/> (сквозная нумерация таблиц).</item>
///   <item>Перенумерация позиций <see cref="DocumentBody.Terms"/> и <see cref="DocumentBody.Appendices"/>.</item>
///   <item>Поиск родительской секции и удаление поддерева для <see cref="DocumentChangeEngine.RemoveSection"/>.</item>
///   <item>Применение шаблонных секций из <see cref="Forms"/> при добавлении корневых секций.</item>
/// </list>
///
/// Правило n+1: все методы Renumber* работают локально от затронутого индекса —
/// не перенумеровывают весь документ при локальных мутациях.
/// </summary>
public sealed class NumberingEngine
{
    private readonly DocumentBody body;
    private readonly DocumentMetadata metadata;

    /// <summary>
    /// Создаёт движок нумерации для указанного тела и метаданных.
    /// </summary>
    public NumberingEngine(DocumentBody body, DocumentMetadata metadata)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    // === Статические утилиты (без состояния) ===

    /// <summary>
    /// Ищет родительскую секцию, в чьей коллекции <c>Children</c> находится <paramref name="child"/>.
    /// Поиск по ссылке, а не по ID — все новые секции имеют <c>Id == 0</c>.
    /// </summary>
    public static BodySection? FindParentSection(IEnumerable<BodySection> sections, BodySection child)
    {
        foreach (var s in sections)
        {
            if (s.Children.Contains(child))
            {
                return s;
            }

            var found = FindParentSection(s.Children, child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Рекурсивно удаляет всех потомков секции (очищает Children).
    /// Вызывается перед удалением самой секции.
    /// </summary>
    public static void RemoveAllDescendants(BodySection section)
    {
        foreach (var child in section.Children)
        {
            RemoveAllDescendants(child);
        }

        section.Children.Clear();
    }

    // === Terms ===

    /// <summary>
    /// Перенумеровывает позиции всех терминов начиная с 0.
    /// </summary>
    public void RenumberTerms() => RenumberTermsFrom(0);

    /// <summary>
    /// Перенумеровывает позиции терминов начиная с <paramref name="startIndex"/>.
    /// </summary>
    public void RenumberTermsFrom(int startIndex)
    {
        for (int i = startIndex; i < body.Terms.Count; i++)
        {
            body.Terms[i].Position = i;
        }
    }

    // === Appendices ===

    /// <summary>
    /// Перенумеровывает позиции всех приложений (1-based).
    /// </summary>
    public void RenumberAppendices() => RenumberAppendicesFrom(0);

    /// <summary>
    /// Перенумеровывает позиции приложений начиная с <paramref name="startIndex"/>.
    /// </summary>
    public void RenumberAppendicesFrom(int startIndex)
    {
        for (int i = startIndex; i < body.Appendices.Count; i++)
        {
            body.Appendices[i].Position = i + 1;
        }
    }

    /// <summary>
    /// Перенумеровывает подсекции (таблицы) внутри указанного приложения.
    /// Position = 0-based индекс среди Sections одного приложения.
    /// </summary>
    public void RenumberAppendixSections(BodyAppendix appendix)
    {
        if (appendix?.Sections is not { Count: > 0 })
        {
            return;
        }

        for (int i = 0; i < appendix.Sections.Count; i++)
        {
            appendix.Sections[i].Position = i;
        }
    }

    /// <summary>
    /// Пересчитывает <see cref="BodySection.GlobalTableIndex"/> для всех таблиц
    /// во всех приложениях документа. Нумерация сквозная (1, 2, 3, …),
    /// назначается в порядке обхода приложений + подсекций.
    /// </summary>
    public void RecalculateGlobalTableIndex()
    {
        var running = 0;
        foreach (var appendix in body.Appendices)
        {
            if (appendix?.Sections == null)
            {
                continue;
            }

            foreach (var section in appendix.Sections.OrderBy(s => s.Position))
            {
                running++;
                section.GlobalTableIndex = running;
            }
        }
    }

    // === Sections (иерархия) ===

    /// <summary>
    /// Перенумеровывает корневые секции с начала (Level=1, parentId=0).
    /// </summary>
    public void RenumberSections() => RenumberSectionsFrom(body.Sections, 0, 1, 0, string.Empty);

    /// <summary>
    /// Перенумеровывает секции, начиная с <paramref name="startIndex"/>,
    /// в указанном родительском контейнере. Рекурсивно обновляет потомков.
    /// </summary>
    public void RenumberSectionsFrom(
        ObservableCollection<BodySection> siblings,
        int startIndex,
        int level,
        long parentId,
        string parentPath)
    {
        if (startIndex < 0 || startIndex >= siblings.Count)
        {
            return;
        }

        for (int i = startIndex; i < siblings.Count; i++)
        {
            var section = siblings[i];
            section.Level = level;
            section.ParentId = parentId;
            section.Position = i;

            string path;
            if (level == 1)
            {
                path = (MarkdownSyntaxDictionary.FirstUserSectionNumber + i).ToString();
            }
            else
            {
                path = $"{parentPath}.{i + 1}";
            }

            section.HierarchyPath = path;

            if (section.Children.Count > 0)
            {
                RenumberSectionsFrom(section.Children, 0, level + 1, section.Id, path);
            }
        }
    }

    /// <summary>
    /// Создаёт новую секцию. Для корневых секций подставляет шаблон из
    /// <see cref="Forms"/> по индексу <see cref="DocumentBody.Sections"/>.Count.
    /// Подразделы получаются пустыми — шаблоны применяются только к верхнему уровню.
    /// </summary>
    public BodySection CreateSectionWithTemplate(BodySection? parent)
    {
        var section = new BodySection();

        if (parent != null)
        {
            return section;
        }

        var template = TryGetSectionTemplate(body.Sections.Count);
        if (template != null)
        {
            section.Title = template.Title;
            section.Body = template.Body;
        }

        return section;
    }

    /// <summary>
    /// Возвращает клон шаблонной секции для указанного DocType и индекса.
    /// Если шаблон не найден — null; вызывающий код добавляет пустую секцию.
    /// </summary>
    private BodySection? TryGetSectionTemplate(int index)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metadata.DocType))
            {
                return null;
            }

            var template = Forms.GetTemplate(metadata.DocType);
            var starterSections = template.StarterSections;
            if (starterSections == null || index < 0 || index >= starterSections.Count)
            {
                return null;
            }

            var source = starterSections[index];
            return new BodySection
            {
                Title = source.Title,
                Body = source.Body,
            };
        }
        catch
        {
            // Любая ошибка загрузки шаблона — не ломаем добавление секции.
            return null;
        }
    }
}
