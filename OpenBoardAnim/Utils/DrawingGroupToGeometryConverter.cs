using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenBoardAnim.Utils
{
    public class DrawingGroupToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DrawingGroup drawingGroup)
                return GeometryHelper.ConvertToGeometry(drawingGroup);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
