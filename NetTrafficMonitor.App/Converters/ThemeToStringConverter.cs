using System;
using System.Globalization;
using System.Windows.Data;

namespace NetTrafficMonitor.Converters;

/// <summary>
/// Converts Theme enum values to readable strings.
/// </summary>
public class ThemeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Theme theme)
        {
            return theme switch
            {
                Theme.Dark => "Dark",
                Theme.Light => "Light",
                Theme.System => "System",
                _ => theme.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s.ToLowerInvariant() switch
            {
                "dark" => Theme.Dark,
                "light" => Theme.Light,
                "system" => Theme.System,
                _ => Theme.Dark
            };
        }
        return Theme.Dark;
    }
}
