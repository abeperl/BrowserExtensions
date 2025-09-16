namespace DataFlow.Mobile.Models;

public class TemplateColumnSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool IsSortable { get; set; } = false;
    public bool IsFilterable { get; set; } = false;
    public int Order { get; set; }
    public string Width { get; set; } = "Auto";
    public string Alignment { get; set; } = "Left";
    public string Format { get; set; } = string.Empty;
    public int TotalColumns { get; set; }
    public int VisibleColumns { get; set; }
    public int HiddenColumns { get; set; }
    public List<string> AvailableDataTypes { get; set; } = [];
    public List<string> MostUsedDataTypes { get; set; } = [];
    public bool HasCustomFormatting { get; set; }
    public bool HasConditionalFormatting { get; set; }
}