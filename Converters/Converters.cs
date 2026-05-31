using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PixivTools.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is false;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => v is false;
}

public class BoolToVisConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => v is Visibility.Visible;
}

public class EqConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => string.Equals(v?.ToString(), p?.ToString(), StringComparison.OrdinalIgnoreCase);
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => p?.ToString() ?? "";
}

public class NullToVisConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        var invert = string.Equals(p?.ToString(), "invert", StringComparison.OrdinalIgnoreCase);
        var isNull = v == null || (v is string s && string.IsNullOrEmpty(s));
        return invert ? (isNull ? Visibility.Visible : Visibility.Collapsed) : (isNull ? Visibility.Collapsed : Visibility.Visible);
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}
