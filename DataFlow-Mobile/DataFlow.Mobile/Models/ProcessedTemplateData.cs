using System.Text.Json;

namespace DataFlow.Mobile.Models;

public class ProcessedTemplateData
{
    public List<ProcessedDataItem> Items { get; set; } = [];
    public List<TemplateColumn> VisibleColumns { get; set; } = [];
    public Template Template { get; set; } = null!;
    public int TotalItems { get; set; }
    public bool HasErrors { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class ProcessedDataItem
{
    public Dictionary<string, FormattedColumnValue> ColumnValues { get; set; } = [];
    public JsonElement RawData { get; set; }
    public int Index { get; set; }
}

public class FormattedColumnValue
{
    public string DisplayValue { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string? TextColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? FontWeight { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TemplateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> MissingFields { get; set; } = [];
    public List<string> AvailableFields { get; set; } = [];
}