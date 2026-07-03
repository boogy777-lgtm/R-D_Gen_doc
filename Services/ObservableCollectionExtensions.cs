using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Утилиты для идемпотентного заполнения <see cref="ObservableCollection{T}"/>
/// и нормализации кодов подразделений. Вынесены из <c>MainWindowViewModel</c>
/// и <c>MetadataSyncCoordinator</c> для устранения дубликатов (DRY).
/// </summary>
public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Заполняет <paramref name="target"/> значениями из <paramref name="items"/>
    /// через дифф-обновление (без полного Clear+Add). Существующие элементы
    /// в правильной позиции сохраняются → подписчики <c>CollectionChanged</c>
    /// (SelectedItem, ScrollViewer.Offset) и UI-визуализация (ComboBox, ListBox)
    /// не «мигают» при hot-reload справочников.
    /// </summary>
    /// <remarks>
    /// <para>Алгоритм (O(n+m), n=target.Count, m=items.Count):</para>
    /// <list type="number">
    ///   <item>Удалить из <paramref name="target"/> все элементы, которых нет
    ///       в <paramref name="items"/> (с конца, чтобы индексы не сбивались).</item>
    ///   <item>Для каждого элемента из <paramref name="items"/> в порядке
    ///       источника: если его нет в текущей позиции target — вставить;
    ///       если есть, но в другой позиции — переместить (или оставить как есть,
    ///       см. <paramref name="moveExisting"/>).</item>
    /// </list>
    /// <para>По умолчанию используется <see cref="EqualityComparer{T}.Default"/> —
    /// ссылочное равенство для reference-типов, value-равенство для value-типов.
    /// Для reference-типов с собственной семантикой равенства (например,
    /// <see cref="DocType"/>) передавайте явный <see cref="IEqualityComparer{T}"/>.</para>
    /// </remarks>
    /// <param name="target">Целевая коллекция (будет мутирована in-place).</param>
    /// <param name="items">Источник (может быть null — тогда target очищается).</param>
    /// <param name="comparer">
    /// Компаратор равенства элементов. По умолчанию <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="moveExisting">
    /// Если true (по умолчанию), существующие элементы в неправильной позиции
    /// перемещаются (через Move). Если false — оставляются как есть (порядок
    /// может разойтись с источником, но элементы не «прыгают» в UI).
    /// </param>
    public static void Seed<T>(
        this ObservableCollection<T> target,
        IEnumerable<T>? items,
        IEqualityComparer<T>? comparer = null,
        bool moveExisting = true)
    {
        ArgumentNullException.ThrowIfNull(target);

        IEqualityComparer<T> cmp = comparer ?? EqualityComparer<T>.Default;

        // 1. Материализуем источник и проверяем что нам нужно оставить.
        var sourceList = items is null
            ? new List<T>()
            : items.ToList();

        // 2. Удаляем из target элементы, которых нет в sourceList
        //    (с конца, чтобы индексы не сдвигались).
        for (int i = target.Count - 1; i >= 0; i--)
        {
            var existing = target[i];
            if (!sourceList.Contains(existing, cmp))
            {
                target.RemoveAt(i);
            }
        }

        // 3. Приводим target к списку sourceList поэлементно.
        //    Для каждого i из sourceList: если target[i] != sourceList[i] (с учётом
        //    равенства по Code/etc.), то вставляем sourceList[i] в позицию i
        //    (или перемещаем существующий элемент в позицию i).
        for (int i = 0; i < sourceList.Count; i++)
        {
            var desired = sourceList[i];

            if (i < target.Count)
            {
                var current = target[i];
                if (cmp.Equals(current, desired))
                {
                    // Уже на своём месте — ничего не делаем.
                    continue;
                }

                if (moveExisting)
                {
                    // Ищем `desired` в оставшейся части target.
                    int existingIdx = -1;
                    for (int j = i + 1; j < target.Count; j++)
                    {
                        if (cmp.Equals(target[j], desired))
                        {
                            existingIdx = j;
                            break;
                        }
                    }

                    if (existingIdx >= 0)
                    {
                        // Перемещаем существующий элемент в позицию i.
                        target.Move(existingIdx, i);
                        continue;
                    }
                }

                // Не нашли — вставляем (либо moveExisting=false, либо новый элемент).
                target.Insert(i, desired);
            }
            else
            {
                // target короче sourceList — добавляем в конец.
                target.Add(desired);
            }
        }
    }

    /// <summary>
    /// Возвращает ТОЛЬКО child_id (без префикса родителя) для использования
    /// в ComboBox «Отдел / функциональное направление». Например, для
    /// <c>Department(Code="SD-NNB", ParentCode="SD")</c> → <c>"NNB"</c>.
    /// Если parent не задан — возвращает полный <c>Code</c> как есть.
    /// </summary>
    public static string GetChildId(Department? d)
    {
        if (d == null || string.IsNullOrWhiteSpace(d.Code))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(d.ParentCode))
        {
            return d.Code;
        }

        var prefix = d.ParentCode + "-";
        return d.Code.StartsWith(prefix, StringComparison.Ordinal)
            ? d.Code.Substring(prefix.Length)
            : d.Code;
    }

    /// <summary>
    /// Возвращает упорядоченный список уникальных <c>child_id</c> для
    /// дочерних подразделений <paramref name="parentCode"/>.
    /// </summary>
    public static IEnumerable<string> ChildIdsOf(
        IEnumerable<Department> allDepartments,
        string parentCode) =>
        allDepartments
            .Where(d => string.Equals(d.ParentCode, parentCode, StringComparison.OrdinalIgnoreCase))
            .Select(GetChildId)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal);
}
