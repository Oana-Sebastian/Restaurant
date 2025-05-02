using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Models;
using System.Windows.Data;

namespace Restaurant.Helpers
{
    public class AllergenListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<DishAllergen> list)
            {
                return string.Join(", ",
                    list
                     .Select(da => da.Allergen?.Name)
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
