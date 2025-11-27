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
