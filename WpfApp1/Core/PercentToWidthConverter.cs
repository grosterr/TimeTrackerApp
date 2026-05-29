using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Core
{
    public class PercentToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2
                && values[0] is double percentage
                && values[1] is double containerWidth
                && containerWidth > 0)
            {
                return Math.Max(2, containerWidth * percentage / 100.0);
            }
            return 2.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
