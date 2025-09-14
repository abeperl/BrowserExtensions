namespace DataFlow.Mobile.Models;

public class ActionResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public string? ErrorDetails { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ExecutionTime { get; set; }

    public static ActionResult Success(string? message = null, object? data = null)
    {
        return new ActionResult
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static ActionResult Error(string message, string? errorDetails = null)
    {
        return new ActionResult
        {
            IsSuccess = false,
            Message = message,
            ErrorDetails = errorDetails
        };
    }
}