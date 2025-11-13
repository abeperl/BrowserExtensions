using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class PdfBrowserManager : IAsyncDisposable
{
    private readonly ILogger<PdfBrowserManager> _logger;
    private readonly PdfConfig _config;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PdfBrowserManager(ILogger<PdfBrowserManager> logger, IOptions<PdfConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public async Task<IBrowser> GetOrCreateBrowserAsync(CancellationToken ct)
    {
        if (_browser != null && !_browser.IsClosed) return _browser;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_browser != null && !_browser.IsClosed) return _browser;

            string? executablePath = _config.ChromiumExecutablePath;

            if (_config.ChromiumDownloadMode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                var cacheDir = DataPaths.EnsureDir(_config.CacheDirectory);
                var fetcherOptions = new BrowserFetcherOptions
                {
                    Path = cacheDir
                };
                var fetcher = new BrowserFetcher(fetcherOptions);
                var rev = await fetcher.DownloadAsync(BrowserTag.Stable);
                executablePath = rev.GetExecutablePath();
                _logger.LogInformation("Chromium downloaded at {Path}", executablePath);
            }
            else if (_config.ChromiumDownloadMode.Equals("External", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    throw new InvalidOperationException("ChromiumExecutablePath must be set and exist when ChromiumDownloadMode=External.");
                }
            }
            else if (_config.ChromiumDownloadMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    throw new InvalidOperationException("ChromiumDownloadMode=Disabled but no ChromiumExecutablePath provided.");
                }
            }

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--no-first-run",
                    "--no-zygote"
                }
            };

            _browser = await Puppeteer.LaunchAsync(launchOptions);
            _logger.LogInformation("Chromium launched. Version: {Version}", await _browser.GetVersionAsync());
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IPage> NewPageAsync(CancellationToken ct)
    {
        var browser = await GetOrCreateBrowserAsync(ct);
        var page = await browser.NewPageAsync();
        page.DefaultNavigationTimeout = (int)TimeSpan.FromSeconds(_config.NavigationTimeoutSeconds).TotalMilliseconds;
        return page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            try
            {
                await _browser.CloseAsync();
                _browser.Dispose();
            }
            catch
            {
                // ignore
            }
        }
        _initLock.Dispose();
    }
}
