using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Globalization;

namespace DataFlow.Mobile.Converters;

public class TemplateColumnConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessedDataItem dataItem && parameter is TemplateColumn column)
        {
            if (dataItem.ColumnValues.TryGetValue(column.PropertyName, out var columnValue))
            {
                return columnValue.DisplayValue;
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TemplateColumnColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessedDataItem dataItem && parameter is TemplateColumn column)
        {
            if (dataItem.ColumnValues.TryGetValue(column.PropertyName, out var columnValue))
            {
                var colorString = columnValue.TextColor ?? column.TextColor ?? "#000000";
                return Color.FromArgb(colorString);
            }
        }

        return Colors.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TemplateColumnBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessedDataItem dataItem && parameter is TemplateColumn column)
        {
            if (dataItem.ColumnValues.TryGetValue(column.PropertyName, out var columnValue))
            {
                var colorString = columnValue.BackgroundColor ?? column.BackgroundColor;
                if (!string.IsNullOrEmpty(colorString))
                {
                    return Color.FromArgb(colorString);
                }
            }
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TemplateColumnFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessedDataItem dataItem && parameter is TemplateColumn column)
        {
            if (dataItem.ColumnValues.TryGetValue(column.PropertyName, out var columnValue))
            {
                var fontWeight = columnValue.FontWeight ?? column.FontWeight ?? "Normal";
                return fontWeight.ToLower() switch
                {
                    "bold" => FontAttributes.Bold,
                    "italic" => FontAttributes.Italic,
                    _ => FontAttributes.None
                };
            }
        }

        return FontAttributes.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}