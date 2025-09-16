using System.Globalization;

namespace DataFlow.Mobile.Converters;

public class BoolToResultColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336"); // Green for success, Red for error
        }
        return Color.FromArgb("#9E9E9E"); // Gray for unknown
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToResultTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess ? "✓ Success" : "✗ Failed";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}