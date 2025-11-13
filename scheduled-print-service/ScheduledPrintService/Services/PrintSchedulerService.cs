using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class PrintSchedulerService : BackgroundService
{
    private readonly ILogger<PrintSchedulerService> _logger;
    private readonly SchedulerConfig _cfg;
    private readonly IHtmlFetcher _fetcher;
    private readonly PdfPrintService _printer;
    private readonly IPrintTracker _tracker;
    private readonly IEmailNotificationService _email;

    public PrintSchedulerService(
        ILogger<PrintSchedulerService> logger,
        IOptions<SchedulerConfig> schedulerOptions,
        IHtmlFetcher fetcher,
        PdfPrintService printer,
        IPrintTracker tracker,
        IEmailNotificationService email)
    {
        _logger = logger;
        _cfg = schedulerOptions.Value;
        _fetcher = fetcher;
        _printer = printer;
        _tracker = tracker;
        _email = email;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_cfg.Enabled)
        {
            _logger.LogInformation("Scheduler disabled. Exiting PrintSchedulerService loop.");
            return;
        }

        _logger.LogInformation("Scheduler starting. Interval: {Seconds}s", _cfg.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var failures = 0;
            foreach (var url in _cfg.Urls)
            {
                if (stoppingToken.IsCancellationRequested) break;
                var key = url; // simple key; in future, use ID extraction
                if (_tracker.HasPrinted(key))
                {
                    _logger.LogDebug("Already printed: {Url}", url);
                    continue;
                }

                try
                {
                    var html = await _fetcher.FetchAsync(url, stoppingToken);
                    await _printer.PrintHtmlAsync(html, jobName: key, stoppingToken);
                    _tracker.MarkPrinted(key);
                }
                catch (Exception ex)
                {
                    failures++;
                    _logger.LogError(ex, "Failed to print from {Url}", url);
                }
            }

            if (failures > 0)
            {
                await _email.TrySendAsync(
                    subject: $"ScheduledPrintService: {failures} failures",
                    body: $"Scheduler encountered {failures} failed job(s) at {DateTime.Now:u}.",
                    ct: stoppingToken);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_cfg.IntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
        }
    }
}
