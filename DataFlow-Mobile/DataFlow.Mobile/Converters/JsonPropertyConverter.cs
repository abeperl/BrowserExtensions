using System.Globalization;
using System.Text.Json;

namespace DataFlow.Mobile.Converters;

public class JsonPropertyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JsonElement jsonElement && parameter is string propertyName)
        {
            try
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(propertyName, out var property))
                {
                    return property.ValueKind switch
                    {
                        JsonValueKind.String => property.GetString() ?? string.Empty,
                        JsonValueKind.Number => property.GetDecimal().ToString(),
                        JsonValueKind.True => "Yes",
                        JsonValueKind.False => "No",
                        JsonValueKind.Null => string.Empty,
                        _ => property.ToString()
                    };
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}