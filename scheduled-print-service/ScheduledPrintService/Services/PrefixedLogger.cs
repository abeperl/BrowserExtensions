using Microsoft.Extensions.Logging;

namespace ScheduledPrintService.Services;

/// <summary>
/// Logger wrapper that automatically adds API number prefix to all log messages
/// </summary>
public class PrefixedLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _innerLogger;
    private readonly Func<string> _prefixProvider;

    public PrefixedLogger(ILogger<T> innerLogger, Func<string> prefixProvider)
    {
        _innerLogger = innerLogger;
        _prefixProvider = prefixProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Get the original message
        var originalMessage = formatter(state, exception);

        // Add prefix
        var prefix = _prefixProvider();
        var prefixedMessage = $"{prefix} {originalMessage}";

        // Create a new formatter that returns the prefixed message
        _innerLogger.Log(logLevel, eventId, state, exception, (s, e) => prefixedMessage);
    }
}