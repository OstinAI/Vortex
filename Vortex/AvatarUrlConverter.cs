using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Vortex
{
    public class AvatarUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            try
            {
                string path = value.ToString();
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                string url = ApiConfig.BaseUrl + path;

                return new BitmapImage(new Uri(url, UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
