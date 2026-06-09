using System.Globalization;
using System.Collections;

namespace DataFlow.Mobile.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue > 0;

        if (value is ICollection collection)
            return collection.Count > 0;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}