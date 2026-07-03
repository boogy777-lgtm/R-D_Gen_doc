using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaApp.Views.Fields;

/// <summary>
/// Конвертеры значений для View полей документа.
/// </summary>
public static class FieldConverters
{
    /// <summary>
    /// Возвращает true если count == 0 (нет секций → показываем Body).
    /// </summary>
    public static readonly IValueConverter CountEqualsZeroToTrue =
        new CountEqualsZeroConverter();

    /// <summary>
    /// Преобразует 0-based индекс в 1-based номер таблицы («0» → «1», «1» → «2»).
    /// </summary>
    public static readonly IValueConverter ZeroBasedToTableNumber =
        new ZeroBasedToTableNumberConverter();

    /// <summary>
    /// Преобразует bool в <see cref="TextWrapping"/>.
    /// true → Wrap, false → NoWrap.
    /// </summary>
    public static readonly IValueConverter BooleanToTextWrapping =
        new BooleanToTextWrappingConverter();

    /// <summary>
    /// Преобразует bool в MinHeight.
    /// true → 100 (многострочное), false → 32 (однострочное).
    /// </summary>
    public static readonly IValueConverter BooleanToMinHeight =
        new BooleanToDoubleConverter(trueValue: 100.0, falseValue: 32.0);

    /// <summary>
    /// Собирает два значения в кортеж (родитель, ребёнок) для команд удаления
    /// подэлементов. Исключает линейный поиск родителя в ViewModel.
    /// </summary>
    public static readonly IMultiValueConverter ParentChild =
        new ParentChildConverter();

    /// <summary>
    /// Логическое НЕ.
    /// </summary>
    public static readonly IValueConverter BooleanNot =
        new BooleanNotConverter();

    /// <summary>
    /// Логическое ИЛИ для нескольких bool.
    /// </summary>
    public static readonly IMultiValueConverter BooleanOr =
        new BooleanOrConverter();

    /// <summary>
    /// Логическое И для нескольких bool.
    /// </summary>
    public static readonly IMultiValueConverter BooleanAnd =
        new BooleanAndConverter();

    private sealed class CountEqualsZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ZeroBasedToTableNumberConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index + 1).ToString(culture);
            }

            return "1";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class BooleanToTextWrappingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool flag)
            {
                return flag ? TextWrapping.Wrap : TextWrapping.NoWrap;
            }

            return TextWrapping.NoWrap;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class BooleanToDoubleConverter : IValueConverter
    {
        private readonly double trueValue;
        private readonly double falseValue;

        public BooleanToDoubleConverter(double trueValue, double falseValue)
        {
            this.trueValue = trueValue;
            this.falseValue = falseValue;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool flag)
            {
                return flag ? trueValue : falseValue;
            }

            return falseValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ParentChildConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is { Count: >= 2 })
            {
                return (values[0], values[1]);
            }

            return null;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class BooleanNotConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is false;
        }
    }

    private sealed class BooleanOrConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is true)
                {
                    return true;
                }
            }

            return false;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class BooleanAndConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is not true)
                {
                    return false;
                }
            }

            return values.Count > 0;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
