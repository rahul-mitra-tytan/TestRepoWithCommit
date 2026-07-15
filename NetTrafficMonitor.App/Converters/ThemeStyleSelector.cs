using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace NetTrafficMonitor.Converters;

/// <summary>
/// Selects appropriate styles based on the current application theme.
/// </summary>
public class ThemeStyleSelector : StyleSelector
{
    private static bool _isDark = true;

    public override Style SelectStyle(object item, DependencyObject container)
    {
        if (item is null) return null;

        var theme = item as Theme;
        if (theme == null) theme = (Theme)int.Parse(((int)item).ToString(), System.Globalization.CultureInfo.InvariantCulture);

        var style = base.SelectStyle(item, container);
        if (style == null) return null;

        switch (theme)
        {
            case Theme.Light:
                _isDark = false;
                style = ApplyLightStyle(style);
                break;
            case Theme.System:
            default:
                _isDark = true;
                style = ApplyDarkStyle(style);
                break;
        }

        return style;
    }

    private static Style ApplyDarkStyle(Style baseStyle)
    {
        var clone = (Style)baseStyle.Clone();
        if (clone.Setters != null)
        {
            foreach (var s in clone.Setters)
            {
                if (s.Property == FrameworkElement.ForegroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                else if (s.Property == FrameworkElement.BackgroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                else if (s.Property == System.Windows.Controls.Primitives.ButtonBase.ContentForegroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            }
        }
        return clone;
    }

    private static Style ApplyLightStyle(Style baseStyle)
    {
        var clone = (Style)baseStyle.Clone();
        if (clone.Setters != null)
        {
            foreach (var s in clone.Setters)
            {
                if (s.Property == FrameworkElement.ForegroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                else if (s.Property == FrameworkElement.BackgroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                else if (s.Property == System.Windows.Controls.Primitives.ButtonBase.ContentForegroundProperty)
                    s.Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            }
        }
        return clone;
    }
}
