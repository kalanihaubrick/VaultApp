using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VaultApp.Converters;

/// <summary>String não-vazia → Visible | string vazia → Collapsed</summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverte um bool (usado para IsEnabled quando IsLoading=true)</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
