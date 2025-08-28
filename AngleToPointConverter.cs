using System;
using System.Globalization;
using System.Windows.Data;

namespace IntegratedColorChange
{
    public class AngleToPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 8) return 0.0;

            if (values[0] is not HueCorrectionEditorViewModel.EditMode mode) return 0.0;
            if (values[1] is not double angle) return 0.0;
            if (values[2] is not double luminance) return 0.0;
            if (values[3] is not double saturation) return 0.0;
            if (values[4] is not double hue) return 0.0;
            if (values[5] is not double width || width <= 0) return 0.0;
            if (values[6] is not double height || height <= 0) return 0.0;
            if (values[7] is not string coordinateType) return 0.0;

            if (coordinateType == "X")
            {
                double x = (angle / 360.0) * width;
                return x;
            }
            else if (coordinateType == "Y")
            {
                double center = height / 2.0;
                double offsetRatio = 0;

                switch (mode)
                {
                    case HueCorrectionEditorViewModel.EditMode.Luminance:
                        offsetRatio = luminance - 1.0;
                        break;
                    case HueCorrectionEditorViewModel.EditMode.Saturation:
                        offsetRatio = saturation - 1.0;
                        break;
                    case HueCorrectionEditorViewModel.EditMode.Hue:
                        offsetRatio = hue / 180.0;
                        break;
                }
                var y = center - (offsetRatio * center);
                return Math.Clamp(y, 0, height);
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValueToCenterYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d) return d / 2.0;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;
            return ReferenceEquals(values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}