using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// EqualityComparer-ы для reference-типов моделей, чтобы <c>Seed</c>
/// мог отличать «тот же логический объект» (равенство по ключу) от
/// «другой экземпляр с другим ключом».
/// </summary>
/// <remarks>
/// По умолчанию <see cref="EqualityComparer{T}.Default"/> сравнивает
/// reference-типы по ссылке. Для моделей, приходящих из БД / hot-reload,
/// это даёт ложно-положительные изменения: каждый hot-reload приносит
/// новые объекты, и <c>Seed</c> думает, что ВСЕ элементы заменились.
/// Передача явного comparer-а по ключу (<c>Code</c>) решает эту проблему.
/// </remarks>
public static class ModelEqualityComparers
{
    /// <summary>
    /// EqualityComparer для <see cref="DocType"/> по <see cref="DocType.Code"/>.
    /// </summary>
    public static readonly IEqualityComparer<DocType> DocTypeByCode =
        new FieldComparer<DocType>(x => x.Code);

    /// <summary>
    /// EqualityComparer для <see cref="Department"/> по <see cref="Department.Code"/>.
    /// </summary>
    public static readonly IEqualityComparer<Department> DepartmentByCode =
        new FieldComparer<Department>(x => x.Code);

    /// <summary>
    /// Общий equality-comparer по выбранному полю (case-sensitive Ordinal).
    /// </summary>
    private sealed class FieldComparer<T> : IEqualityComparer<T>
    {
        private readonly System.Func<T, string> keySelector;

        public FieldComparer(System.Func<T, string> keySelector)
        {
            this.keySelector = keySelector;
        }

        public bool Equals(T? x, T? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var kx = keySelector(x);
            var ky = keySelector(y);
            if (kx is null || ky is null)
            {
                return false;
            }

            return string.Equals(kx, ky, System.StringComparison.Ordinal);
        }

        public int GetHashCode(T obj)
        {
            var k = keySelector(obj);
            return k is null ? 0 : k.GetHashCode(System.StringComparison.Ordinal);
        }
    }
}
