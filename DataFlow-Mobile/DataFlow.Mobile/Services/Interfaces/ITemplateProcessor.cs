using DataFlow.Mobile.Models;
using System.Text.Json;

namespace DataFlow.Mobile.Services.Interfaces;

public interface ITemplateProcessor
{
    /// <summary>
    /// Processes API response data using the specified template
    /// </summary>
    Task<ProcessedTemplateData> ProcessDataAsync(Template template, JsonElement apiData);

    /// <summary>
    /// Validates if a template is compatible with the given API response structure
    /// </summary>
    Task<TemplateValidationResult> ValidateTemplateAsync(Template template, JsonElement sampleData);

    /// <summary>
    /// Extracts available field names from API response data
    /// </summary>
    Task<List<string>> ExtractFieldNamesAsync(JsonElement apiData);

    /// <summary>
    /// Auto-generates template columns based on API response structure
    /// </summary>
    Task<List<TemplateColumn>> AutoGenerateColumnsAsync(JsonElement sampleData, int templateId);

    /// <summary>
    /// Applies conditional formatting rules to template columns
    /// </summary>
    Task<FormattedColumnValue> ApplyFormattingAsync(TemplateColumn column, JsonElement value, JsonElement rowData);
}

