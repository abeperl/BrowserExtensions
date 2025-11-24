using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;
using Microsoft.Data.Sqlite;

namespace ScheduledPrintService.Services;

public class ApiPollSchedulerService : BackgroundService
{
    private readonly ILogger<ApiPollSchedulerService> _logger;
    private readonly ApiConfig _cfg;
    private readonly IOrderApiService _apiService;
    private readonly ISubActionExecutor _actionExecutor;
    private readonly IEmailNotificationService _email;
    private readonly SchedulerConfig _schedulerCfg;
    private readonly DatabaseOrderTracker _tracker;
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
        _tracker = new DatabaseOrderTracker(_cfg.ApiNumber, logger);
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

        if (_cfg.ManualMode)
        {
            _logger.LogInformation("Manual mode: clearing processed item history for API #{ApiNumber} to force reprocessing.", _cfg.ApiNumber);
            _tracker.ClearAllForApi();
        }

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

                        // Retry logic: attempt up to 3 times
                        var maxAttempts = 3;
                        var attemptDelay = TimeSpan.FromSeconds(5);
                        Exception? lastException = null;
                        var success = false;

                        for (var attempt = 1; attempt <= maxAttempts; attempt++)
                        {
                            try
                            {
                                if (attempt > 1)
                                {
                                    _logger.LogInformation("Retry attempt {Attempt}/{MaxAttempts} for order: {OrderId}", attempt, maxAttempts, order.Id);
                                    await Task.Delay(attemptDelay * (attempt - 1), stoppingToken);
                                }
                                else
                                {
                                    _logger.Log(hasBatch ? LogLevel.Debug : LogLevel.Information, "Processing order: {OrderId}", order.Id);
                                }

                                // Execute sub-actions for this order
                                await _actionExecutor.ExecuteActionsForOrderAsync(order.Id, order.RawData, stoppingToken);

                                // If we get here, all operations succeeded (navigation, PDF generation, save, print)
                                // Only NOW mark as processed
                                _tracker.MarkProcessed(order.Id);
                                processedCount++;
                                success = true;

                                _logger.Log(hasBatch ? LogLevel.Debug : LogLevel.Information, "Successfully processed order: {OrderId} (attempt {Attempt})", order.Id, attempt);
                                break; // Success - exit retry loop
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                if (attempt < maxAttempts)
                                {
                                    _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for order {OrderId}: {Message} - will retry", attempt, maxAttempts, order.Id, ex.Message);
                                }
                                else
                                {
                                    _logger.LogError(ex, "All {MaxAttempts} attempts failed for order {OrderId}: {Message}", maxAttempts, order.Id, ex.Message);
                                }
                            }
                        }

                        if (!success)
                        {
                            failedCount++;
                            // Order is NOT marked as processed - will retry on next poll
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
            catch (TokenRenewalException ex)
            {
                _logger.LogCritical(ex, "Token renewal failed - stopping service");
                await _email.TrySendAsync(
                    subject: "CRITICAL: ScheduledPrintService Stopped - Token Renewal Failed",
                    body: $"Service stopped at {DateTime.Now:u} due to authentication failure.\n\n" +
                          $"Error: {ex.Message}\n\n" +
                          $"The service received a 401 Unauthorized response and was unable to renew the authentication token.\n\n" +
                          $"Action required:\n" +
                          $"1. Verify the UserEmail and Password in appsettings.json are correct\n" +
                          $"2. Check if the user account is locked or disabled\n" +
                          $"3. Restart the service after resolving the authentication issue",
                    ct: stoppingToken);
                
                // Stop the service
                _lifetime.StopApplication();
                return;
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
public interface IProcessedItemTracker
{
    bool HasProcessed(string id);
    void MarkProcessed(string id);
}

public class DatabaseOrderTracker : IProcessedItemTracker
{
    private readonly int _apiNumber;
    private readonly ILogger _logger;
    private readonly HashSet<string> _processedIds = new();
    private readonly object _lock = new();
    private readonly string _dbPath;

    public DatabaseOrderTracker(int apiNumber, ILogger logger)
    {
        _apiNumber = apiNumber;
        _logger = logger;
        _dbPath = Path.Combine(AppContext.BaseDirectory, "api_config.db");
        // NOTE: File-based tracker (processed-orders.txt) replaced 2025-11-19 by DB-backed tracker
        //       using ProcessedItem table keyed by ApiNumber for multi-API isolation.
        //       This tracker operates against the runtime copy of api_config.db located in the
        //       application's base directory (bin output). If you inspect the repository root
        //       copy of api_config.db you may not see the ProcessedItem table; open the one at
        //       the logged path instead.

        if (!File.Exists(_dbPath))
        {
            _logger.LogWarning("Processed tracking database not found at expected path: {Path}", _dbPath);
            throw new FileNotFoundException($"Database file not found for processed tracking: {_dbPath}");
        }

        _logger.LogInformation("Initializing processed item tracker for API #{ApiNumber}. Database path: {DbPath}", _apiNumber, _dbPath);

        EnsureTable();
        LoadProcessedIds();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private void EnsureTable()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ProcessedItem (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiNumber INTEGER NOT NULL,
                ItemId TEXT NOT NULL,
                ProcessedAt TEXT NOT NULL,
                UNIQUE(ApiNumber, ItemId)
            );";
        cmd.ExecuteNonQuery();
    }

    private void LoadProcessedIds()
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ItemId FROM ProcessedItem WHERE ApiNumber = @ApiNumber";
            cmd.Parameters.AddWithValue("@ApiNumber", _apiNumber);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetString(0);
                _processedIds.Add(id);
            }
            _logger.LogInformation("Loaded {Count} processed IDs for API #{ApiNumber} from database", _processedIds.Count, _apiNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load processed IDs for API #{ApiNumber}", _apiNumber);
        }
    }

    public void ClearAllForApi()
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ProcessedItem WHERE ApiNumber = @ApiNumber";
            cmd.Parameters.AddWithValue("@ApiNumber", _apiNumber);
            var rows = cmd.ExecuteNonQuery();
            lock (_lock)
            {
                _processedIds.Clear();
            }
            _logger.LogInformation("Cleared {Rows} processed item(s) for API #{ApiNumber}", rows, _apiNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear processed IDs for API #{ApiNumber}", _apiNumber);
        }
    }

    public bool HasProcessed(string id)
    {
        lock (_lock)
        {
            return _processedIds.Contains(id);
        }
    }

    public void MarkProcessed(string id)
    {
        lock (_lock)
        {
            if (_processedIds.Contains(id)) return;
            try
            {
                using var conn = Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO ProcessedItem (ApiNumber, ItemId, ProcessedAt) VALUES (@ApiNumber, @ItemId, @ProcessedAt)";
                cmd.Parameters.AddWithValue("@ApiNumber", _apiNumber);
                cmd.Parameters.AddWithValue("@ItemId", id);
                cmd.Parameters.AddWithValue("@ProcessedAt", DateTime.UtcNow.ToString("u"));
                cmd.ExecuteNonQuery();
                _processedIds.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist processed ID {Id} for API #{ApiNumber}", id, _apiNumber);
            }
        }
    }
}
