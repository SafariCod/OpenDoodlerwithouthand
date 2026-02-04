using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenBoardAnim.Utils
{
    public class BrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                if (TryParseColor(colorString, out Color color))
                    return new SolidColorBrush(color);
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color c = brush.Color;
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return "#000000";
        }

        public static bool TryParseColor(string input, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim();
            if (!s.StartsWith("#")) s = "#" + s;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
