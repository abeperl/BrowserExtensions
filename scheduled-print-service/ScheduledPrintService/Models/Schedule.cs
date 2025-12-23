namespace ScheduledPrintService.Models;

/// <summary>
/// Represents a schedule that triggers API execution based on a cron expression
/// </summary>
public class Schedule
{
    public int Id { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<int> ApiNumbers { get; set; } = new();

    /// <summary>
    /// Processing mode: "immediate" or "queued"
    /// - immediate: Fetch primary API and process all orders immediately (default, current behavior)
    /// - queued: Fetch primary API once, enqueue all IDs, then process in batches
    /// </summary>
    public string ProcessingMode { get; set; } = "immediate";

    /// <summary>
    /// For queued mode: how many orders to process per batch
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// For queued mode: whether to fetch and enqueue new orders on each run
    /// If false, only processes existing pending orders without fetching new ones
    /// </summary>
    public bool EnqueueNewOrders { get; set; } = true;
}

/// <summary>
/// Represents the assignment of an API to a schedule with execution order
/// </summary>
public class ScheduleApi
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int ApiNumber { get; set; }
    public int ExecutionOrder { get; set; }
}
