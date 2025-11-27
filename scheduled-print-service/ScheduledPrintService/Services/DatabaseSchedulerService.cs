using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

/// <summary>
/// Background service that reads schedules from database and executes APIs based on cron expressions
/// </summary>
public class DatabaseSchedulerService : BackgroundService
{
    private readonly ILogger<DatabaseSchedulerService> _logger;
    private readonly IDatabaseApiConfigService _dbConfigService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailNotificationService _email;
    private readonly Dictionary<int, DateTime> _nextRunTimes = new();

    public DatabaseSchedulerService(
        ILogger<DatabaseSchedulerService> logger,
        IDatabaseApiConfigService dbConfigService,
        IServiceProvider serviceProvider,
        IEmailNotificationService email)
    {
        _logger = logger;
        _dbConfigService = dbConfigService;
        _serviceProvider = serviceProvider;
        _email = email;
    }

    /// <summary>
    /// Converts 5-field Unix cron expression to 6-field NCrontab format (with seconds)
    /// </summary>
    private string NormalizeCronExpression(string cronExpression)
    {
        var fields = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length == 5)
        {
            // Standard Unix cron format (minute hour day month dow)
            // Convert to NCrontab format by prepending "0" for seconds
            return $"0 {cronExpression}";
        }
        else if (fields.Length == 6)
        {
            // Already in NCrontab format (second minute hour day month dow)
            return cronExpression;
        }
        else
        {
            // Invalid format - return as-is and let NCrontab throw a proper error
            return cronExpression;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database Scheduler Service starting...");

        try
        {
            // Load schedules from database
            var schedules = _dbConfigService.LoadEnabledSchedules();

            if (schedules.Count == 0)
            {
                _logger.LogWarning("No enabled schedules found in database. Database Scheduler will remain idle.");
                return;
            }

            _logger.LogInformation("Loaded {Count} enabled schedule(s)", schedules.Count);

            // Display schedule information
            foreach (var schedule in schedules)
            {
                var apiNumbers = _dbConfigService.LoadScheduleApiNumbers(schedule.Id);
                schedule.ApiNumbers = apiNumbers;

                _logger.LogInformation("Schedule #{Id}: '{Name}' - Cron: '{Cron}' - APIs: [{Apis}]",
                    schedule.Id,
                    schedule.ScheduleName,
                    schedule.CronExpression,
                    string.Join(", ", apiNumbers));

                // Calculate next run time
                try
                {
                    var normalizedCron = NormalizeCronExpression(schedule.CronExpression);
                    var cronSchedule = CrontabSchedule.Parse(normalizedCron, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
                    var nextRun = cronSchedule.GetNextOccurrence(DateTime.Now);
                    _nextRunTimes[schedule.Id] = nextRun;

                    _logger.LogInformation("  Next execution: {NextRun} ({TimeUntil} from now)",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss"),
                        nextRun - DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid cron expression for Schedule #{Id}: '{Cron}'",
                        schedule.Id, schedule.CronExpression);
                }
            }

            _logger.LogInformation("Database Scheduler Service initialized. Monitoring schedules...");

            // Main scheduling loop - check every minute
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                foreach (var schedule in schedules)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    // Check if it's time to run this schedule
                    if (_nextRunTimes.TryGetValue(schedule.Id, out var nextRun) && now >= nextRun)
                    {
                        _logger.LogInformation("Executing schedule #{Id}: '{Name}'", schedule.Id, schedule.ScheduleName);

                        try
                        {
                            await ExecuteScheduleAsync(schedule, stoppingToken);

                            // Calculate next run time
                            var normalizedCron = NormalizeCronExpression(schedule.CronExpression);
                            var cronSchedule = CrontabSchedule.Parse(normalizedCron, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
                            _nextRunTimes[schedule.Id] = cronSchedule.GetNextOccurrence(DateTime.Now);

                            _logger.LogInformation("Schedule #{Id} completed. Next run: {NextRun}",
                                schedule.Id,
                                _nextRunTimes[schedule.Id].ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing schedule #{Id}: {Message}", schedule.Id, ex.Message);

                            await _email.TrySendAsync(
                                subject: $"Schedule Execution Failed: {schedule.ScheduleName}",
                                body: $"Schedule #{schedule.Id} failed at {DateTime.Now:u}\n\nError: {ex.Message}\n\nStack: {ex.StackTrace}",
                                ct: stoppingToken);

                            // Still calculate next run time even if this one failed
                            var normalizedCron = NormalizeCronExpression(schedule.CronExpression);
                            var cronSchedule = CrontabSchedule.Parse(normalizedCron, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
                            _nextRunTimes[schedule.Id] = cronSchedule.GetNextOccurrence(DateTime.Now);
                        }
                    }
                }

                // Wait 10 seconds before checking again (balances responsiveness vs CPU usage)
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Database Scheduler Service shutting down");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Database Scheduler Service: {Message}", ex.Message);
            throw;
        }
    }

    private async Task ExecuteScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {Count} API(s) for schedule '{Name}'", schedule.ApiNumbers.Count, schedule.ScheduleName);

        var successCount = 0;
        var failureCount = 0;

        foreach (var apiNumber in schedule.ApiNumbers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Executing API #{ApiNumber} (order {Index}/{Total})",
                    apiNumber,
                    schedule.ApiNumbers.IndexOf(apiNumber) + 1,
                    schedule.ApiNumbers.Count);

                await ExecuteApiAsync(apiNumber, cancellationToken);

                successCount++;
                _logger.LogInformation("API #{ApiNumber} completed successfully", apiNumber);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "API #{ApiNumber} failed: {Message}", apiNumber, ex.Message);

                // Continue with next API even if this one failed
                _logger.LogWarning("Continuing with next API despite failure in API #{ApiNumber}", apiNumber);
            }
        }

        _logger.LogInformation("Schedule '{Name}' complete: {Success} succeeded, {Failed} failed",
            schedule.ScheduleName, successCount, failureCount);
    }

    private async Task ExecuteApiAsync(int apiNumber, CancellationToken cancellationToken)
    {
        // Load API configuration from database
        var apiConfig = _dbConfigService.LoadApiConfig(apiNumber);

        if (!apiConfig.Enabled)
        {
            _logger.LogWarning("API #{ApiNumber} is disabled - skipping", apiNumber);
            return;
        }

        _logger.LogInformation("API #{ApiNumber} loaded: {SubActions} sub-actions configured ({Enabled} enabled)",
            apiNumber,
            apiConfig.SubActions.Count,
            apiConfig.SubActions.Count(a => a.Enabled));

        // Create scoped services for this API execution
        using var scope = _serviceProvider.CreateScope();
        var apiService = scope.ServiceProvider.GetRequiredService<IOrderApiService>();
        var actionExecutor = scope.ServiceProvider.GetRequiredService<ISubActionExecutor>();

        // Create tracker for this API
        var tracker = new DatabaseOrderTracker(apiNumber, _logger);

        // Fetch orders from API
        _logger.LogInformation("Calling API #{ApiNumber}: {Endpoint}", apiNumber, apiConfig.PrimaryEndpoint);
        var orders = await apiService.GetOrdersListAsync(apiConfig, cancellationToken);

        if (orders.Count == 0)
        {
            _logger.LogInformation("API #{ApiNumber} returned no orders", apiNumber);
            return;
        }

        _logger.LogInformation("API #{ApiNumber} returned {Count} orders", apiNumber, orders.Count);

        // Execute any batch sub-actions first
        var batchActions = apiConfig.SubActions
            .Where(a => a.Enabled)
            .Where(a => a.Type.Equals("CreatePicklistBatch", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (batchActions.Count > 0)
        {
            var idList = orders.Select(o => o.Id).ToList();
            foreach (var action in batchActions)
            {
                try
                {
                    _logger.LogInformation("Executing batch action: {Name}", action.Name);
                    await actionExecutor.ExecuteBatchCreatePicklistAsync(action, idList, apiConfig, cancellationToken);
                    _logger.LogInformation("Batch action '{Name}' completed", action.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch action '{Name}' failed: {Message}", action.Name, ex.Message);
                    if (!(action.ContinueOnError ?? true))
                    {
                        throw;
                    }
                }
            }
        }

        // Process individual orders
        var processedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var order in orders)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // Check if already processed
            if (tracker.HasProcessed(order.Id))
            {
                // Don't log each skip individually - summary is logged at the end
                skippedCount++;
                continue;
            }

            try
            {
                _logger.LogDebug("Processing order: {OrderId}", order.Id);

                // Execute sub-actions for this order
                await actionExecutor.ExecuteActionsForOrderAsync(order.Id, order.RawData, apiConfig, cancellationToken);

                // Mark as processed
                tracker.MarkProcessed(order.Id);
                processedCount++;

                _logger.LogDebug("Successfully processed order: {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Failed to process order {OrderId}: {Message}", order.Id, ex.Message);
                // Continue with next order
            }
        }

        _logger.LogInformation("API #{ApiNumber} batch complete: {Processed} processed, {Skipped} skipped, {Failed} failed",
            apiNumber, processedCount, skippedCount, failedCount);
    }
}
