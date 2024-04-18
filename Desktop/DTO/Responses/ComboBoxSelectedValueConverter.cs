using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FDS.DTO.Responses
{
    public class ComboBoxSelectedValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
                return DependencyProperty.UnsetValue;

            string phoneCode = values[0] as string;
            string countryCode = values[1] as string;
            string countryName = values[2] as string;

            return $"{phoneCode} - {countryCode} - {countryName}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
