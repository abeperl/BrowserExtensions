using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class FilePdfPrinter : IPdfPrinter
{
    private readonly ILogger<FilePdfPrinter> _logger;
    private readonly PrinterConfig _config;

    public FilePdfPrinter(ILogger<FilePdfPrinter> logger, IOptions<PrinterConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public Task PrintAsync(byte[] pdfBytes, string jobName, CancellationToken ct)
        => PrintAsync(pdfBytes, jobName, null, ct);

    public Task PrintAsync(byte[] pdfBytes, string jobName, string? printerName, CancellationToken ct)
    {
        var dir = DataPaths.EnsureDir(_config.OutputDirectory);
        var safeName = string.Join("_", jobName.Split(Path.GetInvalidFileNameChars()));
        var file = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{safeName}.pdf");
        File.WriteAllBytes(file, pdfBytes);
        _logger.LogInformation("PDF written to {File} (printerName parameter ignored for file printer)", file);
        return Task.CompletedTask;
    }
}
