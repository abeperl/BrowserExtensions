using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using ScheduledPrintService.Models;
using System.Linq;

namespace ScheduledPrintService.Services;

public class PdfBrowserManager : IAsyncDisposable
{
    private readonly ILogger<PdfBrowserManager> _logger;
    private readonly PdfConfig _config;
    private readonly ApiConfig _apiConfig;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PdfBrowserManager(ILogger<PdfBrowserManager> logger, IOptions<PdfConfig> options, IOptions<ApiConfig> apiConfig)
    {
        _logger = logger;
        _config = options.Value;
        _apiConfig = apiConfig.Value;
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
            var chromiumVersion = await _browser.GetVersionAsync();
            var puppeteerVersion = typeof(PuppeteerSharp.Puppeteer).Assembly.GetName().Version?.ToString();
            _logger.LogInformation("Chromium launched. Version: {ChromiumVersion}. PuppeteerSharp version: {PuppeteerVersion}", chromiumVersion, puppeteerVersion);
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IPage> NewPageAsync(CancellationToken ct, string? bearerToken = null, Dictionary<string, string>? cookies = null)
    {
        var browser = await GetOrCreateBrowserAsync(ct);
        var page = await browser.NewPageAsync();
        page.DefaultNavigationTimeout = (int)TimeSpan.FromSeconds(_config.NavigationTimeoutSeconds).TotalMilliseconds;

        // DO NOT inject Authorization header - it breaks CORS for fonts/images
        // Token is injected into localStorage by SubActionExecutor, page uses it from there
        // Only set User-Agent to appear as normal browser
        await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36"
        });
        _logger.LogDebug("Configured page headers (token will be used from localStorage by page's JavaScript)");

        if (cookies != null && cookies.Count > 0)
        {
            var cookieParams = cookies.Select(kvp => new CookieParam
            {
                Name = kvp.Key,
                Value = kvp.Value,
                Domain = new Uri(_apiConfig.BaseUrl ?? "https://localhost").Host,
                Path = "/",
                HttpOnly = false,
                Secure = true
            }).ToArray();

            await page.SetCookieAsync(cookieParams);
            _logger.LogDebug("Injected {Count} cookie(s) into page", cookies.Count);
        }

        // Console message diagnostics (always enabled for error tracking)
        page.Console += (_, args) =>
        {
            try
            {
                var type = args.Message.Type.ToString().ToUpperInvariant();
                var text = args.Message.Text;
                if (type == "ERROR" || type == "WARNING")
                {
                    _logger.LogWarning("[CONSOLE {Type}] {Message}", type, Truncate(text, 500));
                }
                else if (_config.DebugLogNetworkResponses)
                {
                    _logger.LogDebug("[CONSOLE {Type}] {Message}", type, Truncate(text, 300));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Console message logging error");
            }
        };

        // Network response diagnostics
        if (_config.DebugLogNetworkResponses)
        {
            bool useRegex = _config.NetworkLogUrlPattern.StartsWith("/") && _config.NetworkLogUrlPattern.EndsWith("/") && _config.NetworkLogUrlPattern.Length > 2;
            System.Text.RegularExpressions.Regex? regex = null;
            if (useRegex)
            {
                var pattern = _config.NetworkLogUrlPattern.Substring(1, _config.NetworkLogUrlPattern.Length - 2);
                try { regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled); } catch { regex = null; }
            }
            page.Response += async (_, args) =>
            {
                try
                {
                    var response = args.Response;
                    if (response == null) return;
                    var url = response.Url;
                    bool match = false;
                    if (regex != null) match = regex.IsMatch(url); else match = url.Contains(_config.NetworkLogUrlPattern, StringComparison.OrdinalIgnoreCase);
                    if (match)
                    {
                        var status = response.Status; // HttpStatusCode enum
                        var statusCode = (int)status;
                        string bodySnippet = string.Empty;
                        var reqMethod = response.Request?.Method?.ToString();
                        if (statusCode >= 200 && statusCode < 300 && string.Equals(reqMethod, "GET", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var txt = await response.TextAsync();
                                bodySnippet = txt.Length > 300 ? txt.Substring(0, 300) + $"...({txt.Length} chars)" : txt;
                            }
                            catch { /* ignore body read issues */ }
                        }
                        // Trace level to reduce log verbosity - only shown if explicitly enabled
                        _logger.LogTrace("[NET] {Status} {Url}", statusCode, Truncate(url, 200));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Network response logging error");
                }
            };
        }

        return page;
    }

    /// <summary>
    /// Creates a brand new isolated Chromium instance and returns a prepared page with auth headers/cookies.
    /// Caller is responsible for disposing the returned page AND the browser (page.Browser) after use.
    /// </summary>
    public async Task<IPage> NewIsolatedBrowserPageAsync(CancellationToken ct, string? bearerToken = null, Dictionary<string, string>? cookies = null)
    {
        // Reuse download logic but do NOT assign to shared _browser field.
        string? executablePath = _config.ChromiumExecutablePath;
        if (_config.ChromiumDownloadMode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            var cacheDir = DataPaths.EnsureDir(_config.CacheDirectory);
            var fetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = cacheDir });
            var rev = await fetcher.DownloadAsync(BrowserTag.Stable);
            executablePath = rev.GetExecutablePath();
            _logger.LogDebug("(Isolated) Chromium executable ready at {Path}", executablePath);
        }
        else if (_config.ChromiumDownloadMode.Equals("External", StringComparison.OrdinalIgnoreCase) || _config.ChromiumDownloadMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                throw new InvalidOperationException("Isolated browser launch failed: ChromiumExecutablePath invalid for current mode.");
            }
        }

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            ExecutablePath = executablePath,
            Args = new[] {"--no-sandbox","--disable-gpu","--disable-dev-shm-usage","--no-first-run","--no-zygote"}
        };
        var isolatedBrowser = await Puppeteer.LaunchAsync(launchOptions);
        var page = await isolatedBrowser.NewPageAsync();
        page.DefaultNavigationTimeout = (int)TimeSpan.FromSeconds(_config.NavigationTimeoutSeconds).TotalMilliseconds;

        // DO NOT inject Authorization header - it breaks CORS for fonts/images
        // Token should be injected into localStorage by caller, page uses it from there
        var headers = new Dictionary<string,string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36"
        };
        await page.SetExtraHttpHeadersAsync(headers);
        _logger.LogDebug("(Isolated) Configured page headers (token will be used from localStorage by page's JavaScript)");

        if (cookies != null && cookies.Count > 0)
        {
            var cookieParams = cookies.Select(kvp => new CookieParam
            {
                Name = kvp.Key,
                Value = kvp.Value,
                Domain = new Uri(_apiConfig.BaseUrl ?? "https://localhost").Host,
                Path = "/",
                HttpOnly = false,
                Secure = true
            }).ToArray();
            await page.SetCookieAsync(cookieParams);
            _logger.LogDebug("(Isolated) Injected {Count} cookie(s) into page", cookies.Count);
        }

        // Console + network diagnostics mirror shared configuration
        page.Console += (_, args) =>
        {
            try
            {
                var type = args.Message.Type.ToString().ToUpperInvariant();
                var text = args.Message.Text;
                if (type == "ERROR" || type == "WARNING")
                {
                    _logger.LogWarning("(Isolated)[CONSOLE {Type}] {Message}", type, Truncate(text, 500));
                }
                else if (_config.DebugLogNetworkResponses)
                {
                    _logger.LogDebug("(Isolated)[CONSOLE {Type}] {Message}", type, Truncate(text, 300));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "(Isolated) Console message logging error");
            }
        };

        if (_config.DebugLogNetworkResponses)
        {
            bool useRegex = _config.NetworkLogUrlPattern.StartsWith("/") && _config.NetworkLogUrlPattern.EndsWith("/") && _config.NetworkLogUrlPattern.Length > 2;
            System.Text.RegularExpressions.Regex? regex = null;
            if (useRegex)
            {
                var pattern = _config.NetworkLogUrlPattern.Substring(1, _config.NetworkLogUrlPattern.Length - 2);
                try { regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled); } catch { regex = null; }
            }
            page.Response += async (_, args) =>
            {
                try
                {
                    var response = args.Response;
                    if (response == null) return;
                    var url = response.Url;
                    bool match = regex != null ? regex.IsMatch(url) : url.Contains(_config.NetworkLogUrlPattern, StringComparison.OrdinalIgnoreCase);
                    if (match)
                    {
                        var statusCode = (int)response.Status;
                        string bodySnippet = string.Empty;
                        var reqMethod = response.Request?.Method?.ToString();
                        if (statusCode is >=200 and <300 && string.Equals(reqMethod, "GET", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var txt = await response.TextAsync();
                                bodySnippet = txt.Length > 300 ? txt.Substring(0,300)+$"...({txt.Length} chars)" : txt;
                            }
                            catch { }
                        }
                        // Trace level to reduce log verbosity - only shown if explicitly enabled
                        _logger.LogTrace("(Isolated)[NET] {Status} {Url}", statusCode, Truncate(url,200));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "(Isolated) Network response logging error");
                }
            };
        }

        return page;
    }

    private static string Truncate(string value, int max) => string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";

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
