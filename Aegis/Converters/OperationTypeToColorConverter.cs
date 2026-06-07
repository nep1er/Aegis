using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Aegis.Converters;

public class OperationTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string operationType)
        {
            return operationType.ToLower() switch
            {
                "приёмка" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                "выдача" => new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
            };
        }
        return new SolidColorBrush(Color.FromRgb(149, 165, 166));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}