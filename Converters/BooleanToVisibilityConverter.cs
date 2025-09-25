using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UVCCameraControl.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter?.ToString() == "Invert";
                bool result = invert ? !boolValue : boolValue;

                if (targetType == typeof(bool))
                    return result;

                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                bool invert = parameter?.ToString() == "Invert";
                return invert ? !result : result;
            }

            if (value is bool boolValue)
            {
                bool invert = parameter?.ToString() == "Invert";
                return invert ? !boolValue : boolValue;
            }

            return false;
        }
    }
}