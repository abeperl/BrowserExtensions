namespace DataFlow.Mobile.Models;

public class ImportResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Conflicts { get; set; } = [];
    public ImportSummary? Summary { get; set; }
}

public class ImportSummary
{
    public int PagesCount { get; set; }
    public int TemplatesCount { get; set; }
    public int ActionsCount { get; set; }
    public int SettingsCount { get; set; }
    public int ColorSchemesCount { get; set; }
    public int LayoutTemplatesCount { get; set; }
    public int AudioConfigsCount { get; set; }
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public List<string> ConflictingItems { get; set; } = [];
}