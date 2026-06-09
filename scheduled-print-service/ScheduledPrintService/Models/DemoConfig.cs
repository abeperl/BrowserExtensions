namespace ScheduledPrintService.Models;

public class DemoConfig
{
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = "https://example.com";
    public string OutputFilePrefix { get; set; } = "demo-print";
}
