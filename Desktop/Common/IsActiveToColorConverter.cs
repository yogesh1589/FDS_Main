using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FDS.Common
{
    public class IsActiveToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert IsActive boolean value to SolidColorBrush
            if (value is bool isActive && isActive)
            {
                return Brushes.Green; // Active color
            }
            else
            {
                return Brushes.Red; // Inactive color
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
