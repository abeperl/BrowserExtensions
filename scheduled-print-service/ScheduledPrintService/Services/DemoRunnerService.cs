using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class DemoRunnerService : IHostedService
{
    private readonly ILogger<DemoRunnerService> _logger;
    private readonly PdfPrintService _pdfPrintService;
    private readonly DemoConfig _demo;
    private readonly IHostApplicationLifetime _lifetime;

    public DemoRunnerService(
        ILogger<DemoRunnerService> logger,
        PdfPrintService pdfPrintService,
        IOptions<DemoConfig> demoOptions,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _pdfPrintService = pdfPrintService;
        _demo = demoOptions.Value;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_demo.Enabled)
        {
            _logger.LogInformation("Demo disabled. Service will remain idle.");
            return;
        }

        try
        {
            var html = $"<html><head><meta charset=\"utf-8\"><title>Demo</title></head><body><h1>ScheduledPrintService Demo</h1><p>Time: {DateTime.Now:u}</p><p>Source: {_demo.Url}</p></body></html>";
            _logger.LogInformation("Demo start: printing inline HTML (source url: {Url})", _demo.Url);
            await _pdfPrintService.PrintHtmlAsync(html, _demo.OutputFilePrefix, cancellationToken);
            _logger.LogInformation("Demo complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo failed.");
        }
        finally
        {
            // For demo mode, shut down the host after one run
            _lifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
