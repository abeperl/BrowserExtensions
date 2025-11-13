namespace ScheduledPrintService.Models;

public class SchedulerConfig
{
    public bool Enabled { get; set; } = false;
    public int IntervalSeconds { get; set; } = 60;
    // For prototype: list of URLs to fetch and print once
    public List<string> Urls { get; set; } = new();
    public string PrintedStorePath { get; set; } = "printed-urls.txt";
}
