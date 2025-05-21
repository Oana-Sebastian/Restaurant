using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Restaurant.Models;

namespace Restaurant.Helpers
{
    public class ImageListConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<DishImage> list)
            {
                return string.Join(", ",
                    list
                     .Select(di => di.Url)
                     .Where(n => !string.IsNullOrEmpty(n))
                );
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
