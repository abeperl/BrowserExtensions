using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class ApiPollSchedulerService : BackgroundService
{
    private readonly ILogger<ApiPollSchedulerService> _logger;
    private readonly ApiConfig _cfg;
    private readonly IOrderApiService _apiService;
    private readonly ISubActionExecutor _actionExecutor;
    private readonly IEmailNotificationService _email;
    private readonly SchedulerConfig _schedulerCfg;
    private readonly OrderTracker _tracker;
    private readonly IHostApplicationLifetime _lifetime;

    public ApiPollSchedulerService(
        ILogger<ApiPollSchedulerService> logger,
        IOptions<ApiConfig> apiConfig,
        IOptions<SchedulerConfig> schedulerConfig,
        IOrderApiService apiService,
        ISubActionExecutor actionExecutor,
        IEmailNotificationService email,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _cfg = apiConfig.Value;
        _schedulerCfg = schedulerConfig.Value;
        _apiService = apiService;
        _actionExecutor = actionExecutor;
        _email = email;
        _lifetime = lifetime;
        _tracker = new OrderTracker(_cfg.ProcessedIdsPath, logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_cfg.Enabled)
        {
            _logger.LogInformation("API polling disabled. Exiting ApiPollSchedulerService loop.");
            return;
        }

        _logger.LogInformation("API polling starting. Interval: {Seconds}s", _schedulerCfg.IntervalSeconds);
        _logger.LogInformation("API Base URL: {BaseUrl}", _cfg.BaseUrl);
        _logger.LogInformation("Sub-actions configured: {Count}", _cfg.SubActions.Count);
        _logger.LogInformation("Manual mode: {ManualMode}", _cfg.ManualMode);

        // Initial delay to allow service to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling orders API...");

                // Fetch orders from API
                var orders = await _apiService.GetOrdersListAsync(stoppingToken);

                if (orders.Count == 0)
                {
                    _logger.LogInformation("No orders returned from API");
                }
                else
                {
                    _logger.LogInformation("Processing {Count} orders", orders.Count);

                    // Execute any configured batch sub-actions (e.g., CreatePicklistBatch) once per poll cycle
                    var batchActions = _cfg.SubActions
                        .Where(a => a.Enabled)
                        .Where(a => a.Type.Equals("CreatePicklistBatch", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var hasBatch = batchActions.Count > 0;

                    if (batchActions.Count > 0)
                    {
                        var idList = orders.Select(o => o.Id).ToList();
                        foreach (var action in batchActions)
                        {
                            try
                            {
                                _logger.LogInformation("Executing batch action: {Name}", action.Name);
                                await _actionExecutor.ExecuteBatchCreatePicklistAsync(action, idList, stoppingToken);
                                _logger.LogInformation("Batch action '{Name}' completed successfully", action.Name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Batch action '{Name}' failed: {Message}", action.Name, ex.Message);
                                if (!action.ContinueOnError)
                                {
                                    _logger.LogWarning("Halting poll cycle due to batch action failure (ContinueOnError=false)");
                                    throw;
                                }
                            }
                        }
                    }

                    var processedCount = 0;
                    var skippedCount = 0;
                    var failedCount = 0;

                    foreach (var order in orders)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        // Check if already processed
                        if (_tracker.HasProcessed(order.Id))
                        {
                            _logger.LogDebug("Skipping already processed order: {OrderId}", order.Id);
                            skippedCount++;
                            continue;
                        }

                        try
                        {
                            _logger.Log(hasBatch ? LogLevel.Debug : LogLevel.Information, "Processing order: {OrderId}", order.Id);

                            // Execute sub-actions for this order
                            await _actionExecutor.ExecuteActionsForOrderAsync(order.Id, order.RawData, stoppingToken);

                            // Mark as processed
                            _tracker.MarkProcessed(order.Id);
                            processedCount++;

                            _logger.Log(hasBatch ? LogLevel.Debug : LogLevel.Information, "Successfully processed order: {OrderId}", order.Id);
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex, "Failed to process order {OrderId}: {Message}", order.Id, ex.Message);
                        }
                    }

                    _logger.LogInformation("Batch complete: {Processed} processed, {Skipped} skipped, {Failed} failed",
                        processedCount, skippedCount, failedCount);

                    // Send email notification if there were failures
                    if (failedCount > 0)
                    {
                        await _email.TrySendAsync(
                            subject: $"ScheduledPrintService: {failedCount} order processing failures",
                            body: $"Failed to process {failedCount} order(s) at {DateTime.Now:u}.\nCheck logs for details.",
                            ct: stoppingToken);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error polling API: {Message}", ex.Message);
                await _email.TrySendAsync(
                    subject: "ScheduledPrintService: API Polling Failed",
                    body: $"Failed to poll orders API at {DateTime.Now:u}.\nError: {ex.Message}",
                    ct: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in API polling: {Message}", ex.Message);
                    await _email.TrySendAsync(
                        subject: "ScheduledPrintService: API Polling Error",
                        body: $"Unexpected error polling API at {DateTime.Now:u}.\nError: {ex.Message}\n\nStack: {ex.StackTrace}",
                        ct: stoppingToken);
            }

            // Exit immediately if in manual mode
            if (_cfg.ManualMode)
            {
                _logger.LogInformation("Manual mode enabled - exiting after single run");
                _lifetime.StopApplication();
                return; // Exit cleanly without waiting for cancellation
            }

            // Wait for next interval
            try
            {
                _logger.LogDebug("Waiting {Seconds}s until next poll...", _schedulerCfg.IntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(_schedulerCfg.IntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("API polling service shutting down");
            }
        }
    }
}

// Simple file-based tracker for processed order IDs
public class OrderTracker
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly HashSet<string> _processedIds;
    private readonly object _lock = new();

    public OrderTracker(string filePath, ILogger logger)
    {
        _filePath = DataPaths.EnsureFile(filePath);
        _logger = logger;
        _processedIds = new HashSet<string>();

        LoadProcessedIds();
    }

    private void LoadProcessedIds()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _processedIds.Add(line.Trim());
                    }
                }
                _logger.LogInformation("Loaded {Count} processed order IDs", _processedIds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load processed IDs from {Path}", _filePath);
        }
    }

    public bool HasProcessed(string orderId)
    {
        lock (_lock)
        {
            return _processedIds.Contains(orderId);
        }
    }

    public void MarkProcessed(string orderId)
    {
        lock (_lock)
        {
            if (_processedIds.Add(orderId))
            {
                try
                {
                    File.AppendAllText(_filePath, orderId + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist processed ID: {OrderId}", orderId);
                }
            }
        }
    }
}
