using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FDS.Common
{
    public class CountryCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is not null and is a string
            if (value != null && value is string stringValue)
            {
                // Split the string by space and return the first part
                string[] parts = stringValue.Split(' ');
                return parts[0];
            }
            return value; // Return the original value if it cannot be converted
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
