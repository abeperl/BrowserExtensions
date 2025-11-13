namespace ScheduledPrintService.Models;

public class PrinterConfig
{
    // Modes: File (write PDF to directory), Windows (send to Windows spooler) - Windows not yet implemented
    public string Mode { get; set; } = "File";
    public string? PrinterName { get; set; }
    public string? FallbackPrinterName { get; set; }
    public string OutputDirectory { get; set; } = "out"; // used in File mode
    public int SpoolTimeoutSeconds { get; set; } = 60;
}
