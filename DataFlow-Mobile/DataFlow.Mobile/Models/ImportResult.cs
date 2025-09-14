namespace DataFlow.Mobile.Models;

public class ImportResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = [];
    public ImportSummary? Summary { get; set; }
}

public class ImportSummary
{
    public int PagesCount { get; set; }
    public int TemplatesCount { get; set; }
    public int ActionsCount { get; set; }
    public int SettingsCount { get; set; }
    public List<string> ConflictingItems { get; set; } = [];
}