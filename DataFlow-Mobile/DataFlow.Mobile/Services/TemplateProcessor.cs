using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataFlow.Mobile.Services;

public class TemplateProcessor : ITemplateProcessor
{
    private readonly ILogger<TemplateProcessor> _logger;

    public TemplateProcessor(ILogger<TemplateProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessedTemplateData> ProcessDataAsync(Template template, JsonElement apiData)
    {
        var result = new ProcessedTemplateData
        {
            Template = template,
            VisibleColumns = template.VisibleColumns.ToList()
        };

        try
        {
            if (apiData.ValueKind == JsonValueKind.Array)
            {
                await ProcessArrayDataAsync(result, apiData);
            }
            else if (apiData.ValueKind == JsonValueKind.Object)
            {
                await ProcessSingleItemAsync(result, apiData);
            }
            else
            {
                result.HasErrors = true;
                result.Errors.Add("Invalid API response format. Expected object or array.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template data");
            result.HasErrors = true;
            result.Errors.Add($"Processing error: {ex.Message}");
        }

        return result;
    }

    private async Task ProcessArrayDataAsync(ProcessedTemplateData result, JsonElement arrayData)
    {
        var items = arrayData.EnumerateArray().ToList();
        result.TotalItems = items.Count;

        for (int i = 0; i < items.Count; i++)
        {
            var processedItem = await ProcessDataItemAsync(items[i], result.VisibleColumns, i);
            result.Items.Add(processedItem);
        }
    }

    private async Task ProcessSingleItemAsync(ProcessedTemplateData result, JsonElement itemData)
    {
        result.TotalItems = 1;
        var processedItem = await ProcessDataItemAsync(itemData, result.VisibleColumns, 0);
        result.Items.Add(processedItem);
    }

    private async Task<ProcessedDataItem> ProcessDataItemAsync(JsonElement itemData, List<TemplateColumn> columns, int index)
    {
        var processedItem = new ProcessedDataItem
        {
            RawData = itemData,
            Index = index
        };

        foreach (var column in columns)
        {
            try
            {
                var rawValue = ExtractValueFromJson(itemData, column.PropertyName);
                var formattedValue = await ApplyFormattingAsync(column, rawValue, itemData);
                processedItem.ColumnValues[column.PropertyName] = formattedValue;
            }
            catch (Exception ex)
            {
                processedItem.ColumnValues[column.PropertyName] = new FormattedColumnValue
                {
                    DisplayValue = column.DefaultValue ?? "Error",
                    RawValue = string.Empty,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        return processedItem;
    }

    public async Task<FormattedColumnValue> ApplyFormattingAsync(TemplateColumn column, JsonElement value, JsonElement rowData)
    {
        var result = new FormattedColumnValue
        {
            DataType = column.DataType,
            TextColor = column.TextColor,
            BackgroundColor = column.BackgroundColor,
            FontWeight = column.FontWeight
        };

        try
        {
            // Extract raw value
            result.RawValue = ExtractStringValue(value);

            // Apply data type formatting
            result.DisplayValue = await FormatByDataTypeAsync(result.RawValue, column);

            // Apply conditional formatting if configured
            if (!string.IsNullOrEmpty(column.ConditionalFormatting))
            {
                await ApplyConditionalFormattingAsync(result, column, rowData);
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.ErrorMessage = ex.Message;
            result.DisplayValue = column.DefaultValue ?? "Error";
        }

        return result;
    }

    private async Task<string> FormatByDataTypeAsync(string rawValue, TemplateColumn column)
    {
        if (string.IsNullOrEmpty(rawValue))
            return column.DefaultValue ?? string.Empty;

        try
        {
            return column.DataType.ToLower() switch
            {
                "string" => ApplyStringFormatting(rawValue, column),
                "number" => await FormatNumberAsync(rawValue, column),
                "date" => await FormatDateAsync(rawValue, column),
                "boolean" => FormatBoolean(rawValue, column),
                "currency" => await FormatCurrencyAsync(rawValue, column),
                "percentage" => await FormatPercentageAsync(rawValue, column),
                _ => rawValue
            };
        }
        catch
        {
            return column.DefaultValue ?? rawValue;
        }
    }

    private string ApplyStringFormatting(string value, TemplateColumn column)
    {
        if (!string.IsNullOrEmpty(column.FormatString))
        {
            return string.Format(column.FormatString, value);
        }

        return value;
    }

    private async Task<string> FormatNumberAsync(string value, TemplateColumn column)
    {
        if (decimal.TryParse(value, out var number))
        {
            var format = column.FormatString ?? "N2";
            return number.ToString(format, CultureInfo.CurrentCulture);
        }
        return value;
    }

    private async Task<string> FormatDateAsync(string value, TemplateColumn column)
    {
        if (DateTime.TryParse(value, out var date))
        {
            var format = column.FormatString ?? "MMM dd, yyyy";
            return date.ToString(format, CultureInfo.CurrentCulture);
        }
        return value;
    }

    private string FormatBoolean(string value, TemplateColumn column)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue ? "Yes" : "No";
        }
        return value;
    }

    private async Task<string> FormatCurrencyAsync(string value, TemplateColumn column)
    {
        if (decimal.TryParse(value, out var amount))
        {
            var format = column.FormatString ?? "C";
            return amount.ToString(format, CultureInfo.CurrentCulture);
        }
        return value;
    }

    private async Task<string> FormatPercentageAsync(string value, TemplateColumn column)
    {
        if (decimal.TryParse(value, out var percentage))
        {
            var format = column.FormatString ?? "P2";
            return percentage.ToString(format, CultureInfo.CurrentCulture);
        }
        return value;
    }

    private async Task ApplyConditionalFormattingAsync(FormattedColumnValue result, TemplateColumn column, JsonElement rowData)
    {
        try
        {
            // Simple conditional formatting rules (can be extended)
            var rules = ParseConditionalRules(column.ConditionalFormatting);

            foreach (var rule in rules)
            {
                if (await EvaluateConditionAsync(rule, result.RawValue, rowData))
                {
                    if (!string.IsNullOrEmpty(rule.TextColor))
                        result.TextColor = rule.TextColor;
                    if (!string.IsNullOrEmpty(rule.BackgroundColor))
                        result.BackgroundColor = rule.BackgroundColor;
                    if (!string.IsNullOrEmpty(rule.FontWeight))
                        result.FontWeight = rule.FontWeight;
                    break; // Apply first matching rule
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply conditional formatting for column {ColumnName}", column.PropertyName);
        }
    }

    public async Task<TemplateValidationResult> ValidateTemplateAsync(Template template, JsonElement sampleData)
    {
        var result = new TemplateValidationResult { IsValid = true };

        try
        {
            var availableFields = await ExtractFieldNamesAsync(sampleData);
            result.AvailableFields = availableFields;

            foreach (var column in template.Columns)
            {
                if (!availableFields.Contains(column.PropertyName))
                {
                    result.MissingFields.Add(column.PropertyName);
                    result.Warnings.Add($"Field '{column.PropertyName}' not found in API response");
                }
            }

            if (result.MissingFields.Count > template.Columns.Count / 2)
            {
                result.IsValid = false;
                result.Errors.Add("More than half of the template fields are missing from the API response");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task<List<string>> ExtractFieldNamesAsync(JsonElement apiData)
    {
        var fieldNames = new HashSet<string>();

        try
        {
            if (apiData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in apiData.EnumerateArray())
                {
                    ExtractFieldsFromObject(item, fieldNames);
                }
            }
            else if (apiData.ValueKind == JsonValueKind.Object)
            {
                ExtractFieldsFromObject(apiData, fieldNames);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting field names from API data");
        }

        return fieldNames.ToList();
    }

    private void ExtractFieldsFromObject(JsonElement obj, HashSet<string> fieldNames, string prefix = "")
    {
        if (obj.ValueKind != JsonValueKind.Object) return;

        foreach (var property in obj.EnumerateObject())
        {
            var fieldName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            fieldNames.Add(fieldName);

            // Handle nested objects (one level deep for now)
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                ExtractFieldsFromObject(property.Value, fieldNames, fieldName);
            }
        }
    }

    public async Task<List<TemplateColumn>> AutoGenerateColumnsAsync(JsonElement sampleData, int templateId)
    {
        var columns = new List<TemplateColumn>();
        var fieldNames = await ExtractFieldNamesAsync(sampleData);

        for (int i = 0; i < fieldNames.Count; i++)
        {
            var fieldName = fieldNames[i];
            var sampleValue = ExtractValueFromJson(sampleData.ValueKind == JsonValueKind.Array
                ? sampleData.EnumerateArray().FirstOrDefault()
                : sampleData, fieldName);

            var column = new TemplateColumn
            {
                TemplateId = templateId,
                PropertyName = fieldName,
                DisplayName = FormatDisplayName(fieldName),
                DataType = InferDataType(sampleValue),
                IsVisible = i < 5, // Show first 5 fields by default
                SortOrder = i,
                Width = "Auto",
                TextAlignment = "Left",
                FontSize = "14",
                TextColor = "#000000",
                AllowSorting = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            columns.Add(column);
        }

        return columns;
    }

    private JsonElement ExtractValueFromJson(JsonElement data, string propertyPath)
    {
        if (data.ValueKind != JsonValueKind.Object) return new JsonElement();

        var parts = propertyPath.Split('.');
        var current = data;

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
            {
                current = property;
            }
            else
            {
                return new JsonElement();
            }
        }

        return current;
    }

    private string ExtractStringValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetDecimal().ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => value.ToString()
        };
    }

    private string FormatDisplayName(string fieldName)
    {
        // Convert camelCase or snake_case to Title Case
        var result = Regex.Replace(fieldName, @"([a-z])([A-Z])", "$1 $2");
        result = result.Replace("_", " ");
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());
    }

    private string InferDataType(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => InferStringDataType(value.GetString()),
            JsonValueKind.Number => "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            _ => "String"
        };
    }

    private string InferStringDataType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "String";

        if (DateTime.TryParse(value, out _)) return "Date";
        if (decimal.TryParse(value, out _)) return "Number";
        if (value.StartsWith("$") || value.EndsWith("%")) return "Currency";

        return "String";
    }

    private List<ConditionalRule> ParseConditionalRules(string rulesJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ConditionalRule>>(rulesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<bool> EvaluateConditionAsync(ConditionalRule rule, string value, JsonElement rowData)
    {
        try
        {
            return rule.Operator.ToLower() switch
            {
                "equals" => value.Equals(rule.Value, StringComparison.OrdinalIgnoreCase),
                "contains" => value.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
                "startswith" => value.StartsWith(rule.Value, StringComparison.OrdinalIgnoreCase),
                "endswith" => value.EndsWith(rule.Value, StringComparison.OrdinalIgnoreCase),
                "greater" => decimal.TryParse(value, out var num1) && decimal.TryParse(rule.Value, out var num2) && num1 > num2,
                "less" => decimal.TryParse(value, out var num3) && decimal.TryParse(rule.Value, out var num4) && num3 < num4,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private class ConditionalRule
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? FontWeight { get; set; }
    }
}