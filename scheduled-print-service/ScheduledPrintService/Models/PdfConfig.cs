namespace ScheduledPrintService.Models;

public class PdfConfig
{
    public string ChromiumDownloadMode { get; set; } = "Auto"; // Auto | External | Disabled
    public string? ChromiumExecutablePath { get; set; }
    public string CacheDirectory { get; set; } = "chromium-cache";
    public int NavigationTimeoutSeconds { get; set; } = 30;
    public double? PageWidthInches { get; set; } = 8.27; // Default A4 width
    public double? PageHeightInches { get; set; } = 11.69; // Default A4 height
    public double MarginMillimeters { get; set; } = 10; // uniform margin
    public bool Landscape { get; set; } = false;
    public bool PrintBackground { get; set; } = true;
}
