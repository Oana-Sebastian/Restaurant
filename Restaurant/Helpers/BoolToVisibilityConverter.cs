using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Restaurant.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool b) flag = b;

            bool invert = (parameter as string)?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

            if (invert) flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = (parameter as string)?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

            if (value is Visibility vis)
            {
                bool result = vis == Visibility.Visible;
                return invert ? !result : result;
            }

            return false;
        }
    }
}
