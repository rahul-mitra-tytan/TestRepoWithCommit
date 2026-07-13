using System;
using System.Globalization;
using System.Windows.Data;
using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Converters;

public class DataPeriodToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DataPeriod period)
        {
            return period switch
            {
                DataPeriod.Today     => "Today",
                DataPeriod.ThisWeek  => "This Week",
                DataPeriod.ThisMonth => "This Month",
                _                    => period.ToString()
            };
        }
        return "Today";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
