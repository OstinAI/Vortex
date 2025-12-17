using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Vortex
{
    public partial class VortexStyles : ResourceDictionary
    {
        public VortexStyles()
        {
            InitializeComponent();

            this["NeonSideSimpleConverter"] = new NeonSideSimpleConverter();
            this["FirstValue"] = new FirstValueConverter();
            this["SkewAngleConverter"] = new SkewAngleConverter();
        }
    }

    public class NeonSideSimpleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0.0;

            return value.ToString() == parameter.ToString() ? 1.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class FirstValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var v in values)
            {
                if (v != null && v != DependencyProperty.UnsetValue)
                    return v;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // SkewDirection + SkewAngle -> AngleX
    // parameter: "Invert" (для обратного наклона текста/GIF)
    public class SkewAngleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0.0;

            SkewDirection dir = SkewDirection.None;
            if (values[0] is SkewDirection sd)
                dir = sd;

            double angle = 0.0;
            if (values[1] is double d)
                angle = d;
            else if (values[1] != null)
                double.TryParse(values[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out angle);

            angle = Math.Abs(angle);

            double result = 0.0;

            if (dir == SkewDirection.Left)
                result = -angle;
            else if (dir == SkewDirection.Right)
                result = angle;
            else
                result = 0.0;

            string p = parameter?.ToString();
            if (!string.IsNullOrWhiteSpace(p) && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                result = -result;

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
