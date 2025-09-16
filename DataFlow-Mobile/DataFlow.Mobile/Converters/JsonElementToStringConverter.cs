using System.Globalization;
using System.Text.Json;

namespace DataFlow.Mobile.Converters;

public class JsonElementToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JsonElement jsonElement)
        {
            try
            {
                // For Phase 4, just pretty-print the JSON
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return jsonElement.ToString();
            }
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}