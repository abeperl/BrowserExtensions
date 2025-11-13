using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface IPrintTracker
{
    bool HasPrinted(string key);
    void MarkPrinted(string key);
}

public class FilePrintTracker : IPrintTracker
{
    private readonly ILogger<FilePrintTracker> _logger;
    private readonly SchedulerConfig _config;
    private readonly ConcurrentDictionary<string, byte> _printed = new();
    private readonly object _sync = new();
    private bool _loaded;

    public FilePrintTracker(ILogger<FilePrintTracker> logger, IOptions<SchedulerConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public bool HasPrinted(string key)
    {
        EnsureLoaded();
        return _printed.ContainsKey(key);
    }

    public void MarkPrinted(string key)
    {
        EnsureLoaded();
        if (_printed.TryAdd(key, 0))
        {
            lock (_sync)
            {
                File.AppendAllLines(_config.PrintedStorePath, new[] { key });
            }
            _logger.LogInformation("Marked printed: {Key}", key);
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_sync)
        {
            if (_loaded) return;
            var storePath = Path.Combine(DataPaths.DataRoot, Path.GetFileName(_config.PrintedStorePath));
            _config.PrintedStorePath = storePath; // mutate config instance for downstream usage
            if (File.Exists(storePath))
            {
                foreach (var line in File.ReadAllLines(storePath))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _printed.TryAdd(line.Trim(), 0);
                    }
                }
            }
            _loaded = true;
            _logger.LogInformation("Loaded {Count} printed keys", _printed.Count);
        }
    }
}
