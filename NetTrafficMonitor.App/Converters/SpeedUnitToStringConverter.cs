using System;
using System.Globalization;
using System.Windows.Data;
using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Converters;

public class SpeedUnitToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.Bps  => "B/s",
                SpeedUnit.KBps => "KB/s",
                SpeedUnit.MBps => "MB/s",
                SpeedUnit.GBps => "GB/s",
                SpeedUnit.bps  => "b/s",
                SpeedUnit.Kbps => "Kb/s",
                SpeedUnit.Mbps => "Mb/s",
                SpeedUnit.Gbps => "Gb/s",
                _ => unit.ToString()
            };
        }
        return "Mb/s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
