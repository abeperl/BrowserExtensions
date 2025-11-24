using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface ISubActionExecutor
{
    Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, CancellationToken ct = default);
    Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, CancellationToken ct = default);
    Task ExecuteChainedActionsAsync(SubAction sourceAction, string responseBody, CancellationToken ct = default);
}

public class SubActionExecutor : ISubActionExecutor
{
    private readonly ILogger<SubActionExecutor> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApiConfig _config;
    private readonly PdfConfig _pdfConfig;
    private readonly PdfPrintService _printer;
    private readonly PdfBrowserManager _browserManager;
    private readonly ITokenRenewalService _tokenRenewal;
        // Captured page from the most recent navigation-only action (kept alive for printing)
        private PuppeteerSharp.IPage? _capturedPage;
        private string _capturedPageContextId = string.Empty;
        private PuppeteerSharp.IBrowser? _capturedBrowser; // for isolated mode cleanup
        // Last intercepted picklist JSON body (for diagnostics / potential later validation)
        private string? _lastInterceptedPicklistJson;

    public SubActionExecutor(
        ILogger<SubActionExecutor> logger,
        HttpClient httpClient,
        IOptions<ApiConfig> apiConfig,
        IOptions<PdfConfig> pdfConfig,
        PdfPrintService printer,
        PdfBrowserManager browserManager,
        ITokenRenewalService tokenRenewal)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = apiConfig.Value;
        _pdfConfig = pdfConfig.Value;
        _printer = printer;
        _browserManager = browserManager;
        _tokenRenewal = tokenRenewal;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.Timeout = TimeSpan.FromSeconds(200);

        UpdateHttpClientAuth();

        // Add standard headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("WarehouseId", _config.WarehouseId.ToString());
        _httpClient.DefaultRequestHeaders.Add("Origin", _config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_config.BaseUrl}/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
    }

    private void UpdateHttpClientAuth()
    {
        // Get current token and cookies from renewal service
        var token = _tokenRenewal.GetCurrentToken();
        var cookies = _tokenRenewal.GetCurrentCookies();

        // Update Bearer token
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // Update cookies
        _httpClient.DefaultRequestHeaders.Remove("Cookie");
        if (cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
    }

    public async Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, CancellationToken ct = default)
    {
        _logger.LogDebug("Executing {Count} sub-actions for order {OrderId}", _config.SubActions.Count, orderId);

        for (int i = 0; i < _config.SubActions.Count; i++)
        {
            var action = _config.SubActions[i];
            var actionNum = i + 1;

            try
            {
                if (!action.Enabled)
                {
                    // Skip silently when disabled
                    continue;
                }

                // Skip batch action in per-order context without logging
                if (action.Type.Equals("CreatePicklistBatch", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _logger.LogInformation("[{Num}/{Total}] {ActionName} for order {OrderId}",
                    actionNum, _config.SubActions.Count, action.Name, orderId);

                await ExecuteActionAsync(action, orderId, orderData, ct);

                _logger.LogInformation("[{Num}/{Total}] {ActionName} completed successfully",
                    actionNum, _config.SubActions.Count, action.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Num}/{Total}] {ActionName} failed: {Message}",
                    actionNum, _config.SubActions.Count, action.Name, ex.Message);

                if (!action.ContinueOnError)
                {
                    _logger.LogWarning("Stopping action chain due to error (ContinueOnError=false)");
                    throw;
                }

                _logger.LogInformation("Continuing to next action (ContinueOnError=true)");
            }
        }

        _logger.LogDebug("All sub-actions completed for order {OrderId}", orderId);
    }

    private async Task ExecuteActionAsync(SubAction action, string orderId, JsonElement orderData, CancellationToken ct)
    {
        switch (action.Type.ToLowerInvariant())
        {
            case "callapi":
                await ExecuteCallApiAsync(action, orderId, ct);
                break;

            case "gethtmlandprint":
                await ExecuteGetHtmlAndPrintAsync(action, orderId, ct);
                break;

            case "geturlandprint":
                await ExecuteGetUrlAndPrintAsync(action, orderId, new Dictionary<string, object>(), ct);
                break;

            case "navigateonly":
                await ExecuteNavigateOnlyAsync(action, orderId, null, ct);
                break;

            case "printcapturedhtml":
                await ExecutePrintCapturedHtmlAsync(action, orderId, ct);
                break;

            case "delay":
                await ExecuteDelayAsync(action, ct);
                break;

            case "createpicklistbatch":
                // Batch action is intended to run at batch scope, not per order. Log and ignore here.
                _logger.LogDebug("Batch action '{ActionName}' encountered in per-order context; skipping.", action.Name);
                break;

            default:
                _logger.LogWarning("Unknown action type: {Type}", action.Type);
                break;
        }
    }

    public async Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, CancellationToken ct = default)
    {
        // Prepare endpoint
        var endpoint = action.Endpoint;

        // Convert order IDs to integers as required by API payload
        var idList = new List<int>();
        foreach (var id in orderIds)
        {
            if (int.TryParse(id, out var parsed))
            {
                idList.Add(parsed);
            }
            else
            {
                _logger.LogWarning("Skipping non-integer order ID: {Id}", id);
            }
        }

        if (idList.Count == 0)
        {
            _logger.LogInformation("No valid order IDs supplied for batch picklist creation.");
            return;
        }

        var batchSize = action.BatchSize > 0 ? action.BatchSize : 10;
        var batches = idList.Chunk(batchSize).ToList();
        _logger.LogInformation("Creating pending order picklists in {BatchCount} batch(es) of up to {BatchSize}.", batches.Count, batchSize);

        for (var i = 0; i < batches.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var batch = batches[i];
            var payload = new
            {
                orderId = batch,
                QuickShip = action.QuickShip,
                ForceCreatePicklist = action.ForceCreatePicklist
            };

            var json = JsonSerializer.Serialize(payload);
            Func<HttpRequestMessage> factory = () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return req;
            };

            _logger.LogInformation("[Batch {Current}/{Total}] POST {Endpoint} with {Count} ids", i + 1, batches.Count, endpoint, batch.Length);
            _logger.LogDebug("Payload: {Payload}", json);

            var httpCt = _config.ManualMode ? CancellationToken.None : ct;
            var response = await SendWithRetryAsync(factory, httpCt);
            var respBody = await response.Content.ReadAsStringAsync(httpCt);
            _logger.LogDebug("Batch API response: {Body}", respBody);

            try
            {
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("[Batch {Current}/{Total}] Success. Response length: {Len}", i + 1, batches.Count, respBody.Length);

                // Execute chained actions if configured
                await ExecuteChainedActionsAsync(action, respBody, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Batch {Current}/{Total}] Failed with status {Status}. Body: {Body}", i + 1, batches.Count, (int)response.StatusCode, Truncate(respBody, 1000));
                throw;
            }

            // Small pacing delay to avoid server throttling
            await Task.Delay(100, ct);
        }
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var attempts = Math.Max(1, _config.RetryMaxAttempts);
        var baseDelay = Math.Max(50, _config.RetryBaseDelayMs);
        var maxDelay = Math.Max(baseDelay, _config.RetryMaxDelayMs);
        var rng = new Random();
        var tokenRenewed = false;

        HttpResponseMessage? lastResponse = null;
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            lastResponse?.Dispose();
            using var request = requestFactory();

            try
            {
                var response = await _httpClient.SendAsync(request, ct);
                
                // Check for 401 Unauthorized - token may have expired
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Received 401 Unauthorized - token may have expired");
                    
                    // Only attempt token renewal once per retry cycle
                    if (!tokenRenewed)
                    {
                        _logger.LogInformation("Attempting to renew authentication token");
                        tokenRenewed = await _tokenRenewal.RenewTokenAsync(ct);
                        
                        if (tokenRenewed)
                        {
                            _logger.LogInformation("Token renewed successfully, updating HTTP client");
                            UpdateHttpClientAuth();
                            
                            // Don't count this as an attempt, retry immediately
                            lastResponse = response;
                            continue;
                        }
                        else
                        {
                            _logger.LogCritical("Failed to renew token after 401 Unauthorized - service will stop");
                            throw new TokenRenewalException("Unable to renew authentication token after receiving 401 Unauthorized");
                        }
                    }
                    
                    lastResponse = response;
                }
                else if ((int)response.StatusCode < 200)
                {
                    lastResponse = response;
                }
                else if ((int)response.StatusCode < 300)
                {
                    return response; // success
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    lastResponse = response;
                }
                else
                {
                    return response; // non-retriable error
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP request error on attempt {Attempt}/{MaxAttempts}", attempt, attempts);
            }
            catch (TaskCanceledException ex)
            {
                if (ct.IsCancellationRequested)
                {
                    // Respect caller cancellation
                    throw;
                }
                _logger.LogWarning(ex, "HTTP request timed out/was canceled by transport on attempt {Attempt}/{MaxAttempts}", attempt, attempts);
            }

            if (attempt == attempts) break;

            var jitter = rng.Next(0, baseDelay);
            var delay = Math.Min(maxDelay, (int)(baseDelay * Math.Pow(2, attempt - 1)) + jitter);
            _logger.LogWarning("Retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", delay, attempt + 1, attempts);
            try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { break; }
        }

        return lastResponse ?? new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Request failed after retries.")
        };
    }

    private async Task ExecuteCallApiAsync(SubAction action, string orderId, CancellationToken ct)
    {
        var endpoint = ReplaceTokens(action.Endpoint, orderId);
        _logger.LogDebug("Calling API: {Method} {Endpoint}", action.Method, endpoint);

        Func<HttpRequestMessage> factory = () =>
        {
            var req = new HttpRequestMessage(new HttpMethod(action.Method), endpoint);

            // Add custom headers
            foreach (var header in action.Headers)
            {
                req.Headers.TryAddWithoutValidation(header.Key, ReplaceTokens(header.Value, orderId));
            }

            // Add request body if provided
            if (!string.IsNullOrEmpty(action.RequestBody))
            {
                var body = ReplaceTokens(action.RequestBody, orderId);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                _logger.LogDebug("Request body: {Body}", body);
            }
            return req;
        };

        var response = await SendWithRetryAsync(factory, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Sub-action API response: {Body}", responseBody);
    }

    private async Task ExecuteGetHtmlAndPrintAsync(SubAction action, string orderId, CancellationToken ct)
    {
        var endpoint = ReplaceTokens(action.Endpoint, orderId);
        _logger.LogDebug("Fetching HTML from: {Method} {Endpoint}", action.Method, endpoint);

        Func<HttpRequestMessage> factory = () =>
        {
            var req = new HttpRequestMessage(new HttpMethod(action.Method), endpoint);

            // Add custom headers
            foreach (var header in action.Headers)
            {
                req.Headers.TryAddWithoutValidation(header.Key, ReplaceTokens(header.Value, orderId));
            }

            // Add request body if provided
            if (!string.IsNullOrEmpty(action.RequestBody))
            {
                var body = ReplaceTokens(action.RequestBody, orderId);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            return req;
        };

        var response = await SendWithRetryAsync(factory, ct);

        // Treat 404 (or other non-success) as non-fatal when ContinueOnError = true
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && action.ContinueOnError)
            {
                var bodySnippet = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("HTML endpoint returned 404 Not Found for {Endpoint}. Skipping print. Body: {BodySnippet}", endpoint, Truncate(bodySnippet, 500));
                return; // Skip further processing
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // Allow upper layer to decide based on ContinueOnError
                throw new HttpRequestException($"HTML fetch failed for {endpoint}: {(int)response.StatusCode} {response.StatusCode}", ex);
            }
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Sub-action API response: {Body}", responseBody);

        // Extract HTML from JSON response if path specified
        string htmlContent;
        if (!string.IsNullOrEmpty(action.HtmlJsonPath))
        {
            htmlContent = ExtractHtmlFromJson(responseBody, action.HtmlJsonPath);
            _logger.LogDebug("Extracted HTML from JSON path: {Path}", action.HtmlJsonPath);
        }
        else
        {
            // Assume response is HTML directly
            htmlContent = responseBody;
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new InvalidOperationException("Received empty HTML content");
        }

        _logger.LogInformation("Printing HTML for order {OrderId} (length: {Length})", orderId, htmlContent.Length);

        // Print the HTML
        var jobName = $"{action.Name}-{orderId}";
        await _printer.PrintHtmlAsync(htmlContent, jobName: jobName, ct);

        _logger.LogInformation("Successfully printed {JobName}", jobName);
    }

    private async Task ExecuteDelayAsync(SubAction action, CancellationToken ct)
    {
        var delayMs = action.DelayMilliseconds;
        _logger.LogDebug("Delaying for {Ms}ms", delayMs);
        await Task.Delay(delayMs, ct);
    }

    public async Task ExecuteChainedActionsAsync(SubAction sourceAction, string responseBody, CancellationToken ct = default)
    {
        // Find chained actions (actions with UseChainedInput that follow this action)
        var sourceIndex = _config.SubActions.IndexOf(sourceAction);
        if (sourceIndex == -1)
        {
            _logger.LogDebug("Source action not found in config, skipping chained execution");
            return;
        }

        var chainedActions = _config.SubActions
            .Skip(sourceIndex + 1)
            .Where(a => a.Enabled && a.UseChainedInput)
            .ToList();

        if (chainedActions.Count == 0)
        {
            _logger.LogDebug("No chained actions configured after '{ActionName}'", sourceAction.Name);
            return;
        }

        _logger.LogInformation("Found {Count} chained action(s) to execute", chainedActions.Count);

        // Extract data from response based on configuration
        var chainedData = ExtractChainedData(sourceAction, responseBody);

        if (chainedData.Count == 0)
        {
            _logger.LogWarning("No data extracted from response for chaining");
            return;
        }

        _logger.LogInformation("Extracted {Count} item(s) for chained execution", chainedData.Count);

        // Execute chained actions for each extracted item
        foreach (var data in chainedData)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var chainedAction in chainedActions)
            {
                try
                {
                    _logger.LogInformation("Executing chained action '{ActionName}' with context data", chainedAction.Name);
                    await ExecuteChainedActionAsync(chainedAction, data, ct);
                    _logger.LogInformation("Chained action '{ActionName}' completed successfully", chainedAction.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chained action '{ActionName}' failed: {Message}", chainedAction.Name, ex.Message);
                    if (!chainedAction.ContinueOnError)
                    {
                        _logger.LogWarning("Stopping chained execution due to error (ContinueOnError=false)");
                        throw;
                    }
                }
            }
        }
    }

    private List<Dictionary<string, object>> ExtractChainedData(SubAction sourceAction, string responseBody)
    {
        var result = new List<Dictionary<string, object>>();

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Navigate to the array based on ChainedArrayJsonPath (e.g., "data")
            var arrayElement = root;
            if (!string.IsNullOrEmpty(sourceAction.ChainedArrayJsonPath))
            {
                var parts = sourceAction.ChainedArrayJsonPath.Split('.');
                foreach (var part in parts)
                {
                    if (arrayElement.TryGetProperty(part, out var element))
                    {
                        arrayElement = element;
                    }
                    else
                    {
                        _logger.LogWarning("JSON path '{Path}' not found in response", sourceAction.ChainedArrayJsonPath);
                        return result;
                    }
                }
            }

            if (arrayElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Expected array at path '{Path}', got {Kind}", sourceAction.ChainedArrayJsonPath ?? "root", arrayElement.ValueKind);
                return result;
            }

            // Extract each item from the array
            foreach (var item in arrayElement.EnumerateArray())
            {
                var itemData = new Dictionary<string, object>();

                // Extract all properties from the item
                foreach (var prop in item.EnumerateObject())
                {
                    var value = ExtractJsonValue(prop.Value);
                    if (value != null)
                    {
                        itemData[prop.Name] = value;
                    }
                }

                if (itemData.Count > 0)
                {
                    result.Add(itemData);
                }
            }

            _logger.LogDebug("Extracted {Count} items from response", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract chained data from response");
        }

        return result;
    }

    private object? ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private async Task ExecuteChainedActionAsync(SubAction action, Dictionary<string, object> context, CancellationToken ct)
    {
        switch (action.Type.ToLowerInvariant())
        {
            case "geturlandprint":
                await ExecuteGetUrlAndPrintAsync(action, string.Empty, context, ct);
                break;

            case "navigateonly":
                await ExecuteNavigateOnlyAsync(action, string.Empty, context, ct);
                break;

            case "printcapturedpage":
            case "printcapturedhtml":
                await ExecutePrintCapturedHtmlAsync(action, string.Empty, ct);
                break;

            case "callapi":
                await ExecuteCallApiWithContextAsync(action, context, ct);
                break;

            case "delay":
                await ExecuteDelayAsync(action, ct);
                break;

            default:
                _logger.LogWarning("Chained action type '{Type}' not supported", action.Type);
                break;
        }
    }

    private async Task ExecuteGetUrlAndPrintAsync(SubAction action, string orderId, Dictionary<string, object> context, CancellationToken ct)
    {
        // Build URL with context substitution
        var url = ReplaceTokensWithContext(action.Endpoint, orderId, context);
        _logger.LogInformation("Fetching URL with Puppeteer: {Url}", url);

        // Get current authentication credentials
        var token = _tokenRenewal.GetCurrentToken();
        var cookies = _tokenRenewal.GetCurrentCookies();

        await using var page = await _browserManager.NewPageAsync(ct, token, cookies);
        
        // Navigate to the page
        _logger.LogDebug("Navigating to {Url}", url);
        // Strategy: First load root application shell, then set hash route via script.
        // This avoids referrerPolicy issues seen when directly navigating to hash URLs.
        var rootUrl = _config.BaseUrl.TrimEnd('/') + "/";
        _logger.LogDebug("Loading application shell {RootUrl} before hash navigation", rootUrl);
        try
        {
            await page.GoToAsync(rootUrl, new PuppeteerSharp.NavigationOptions
            {
                WaitUntil = new[] { PuppeteerSharp.WaitUntilNavigation.Load },
                Timeout = 60000
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("Root page load timed out ({Message}), proceeding with hash navigation anyway", ex.Message);
        }
        _logger.LogDebug("Setting SPA hash route via script to {Url}", url);
        await page.EvaluateExpressionAsync($"window.location.href='{url.Replace("'","%27")}'");

        // Critical: Wait for hash navigation to complete and route to render
        // Hash changes are asynchronous in SPAs - give framework time to detect and render new route
        _logger.LogDebug("Waiting for SPA framework to detect hash change and start rendering");
        await Task.Delay(1500, ct); // Give route detection time

        // Wait additional time for SPA to load data
        if (action.WaitForNetworkIdleMs > 0)
        {
            _logger.LogDebug("Waiting {Ms}ms for network idle", action.WaitForNetworkIdleMs);
            await Task.Delay(action.WaitForNetworkIdleMs, ct);
        }

        // Make hidden fields visible if requested
        if (action.MakeHiddenVisible)
        {
            _logger.LogDebug("Making hidden fields visible");
            await page.EvaluateExpressionAsync(@"
                Array.from(document.querySelectorAll('input[type=""hidden""]')).forEach(el => {
                    el.type = 'text';
                    el.style.display = 'block';
                });
            ");
        }

        // Generate PDF
        _logger.LogInformation("Generating PDF from page");
        var pdfBytes = await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
        {
            PrintBackground = true,
            Format = PuppeteerSharp.Media.PaperFormat.Letter
        });

        // Directly send generated PDF bytes to printer (avoid re-rendering & losing dynamic XHR state)
        var jobName = $"{action.Name}-{ExtractJobNameFromContext(context)}";
        _logger.LogInformation("Printing PDF: {JobName} ({Size} bytes)", jobName, pdfBytes.Length);
        await _printer.PrintPdfBytesAsync(pdfBytes, jobName, ct);
    }

    // New: Navigate only, keep page alive for subsequent print action (supports chained context & enhanced diagnostics)
    private async Task ExecuteNavigateOnlyAsync(SubAction action, string orderId, Dictionary<string, object>? context, CancellationToken ct)
    {
        var rawEndpoint = action.Endpoint;
        var url = context != null ? ReplaceTokensWithContext(rawEndpoint, orderId, context) : ReplaceTokens(rawEndpoint, orderId);

        // Detect unresolved tokens (e.g. {pickListId} that did not substitute)
        var unresolved = Regex.Matches(url, @"\{[^}]+\}").Select(m => m.Value).Distinct().ToList();
        if (unresolved.Count > 0)
        {
            _logger.LogWarning("Unresolved token(s) in navigation URL after substitution: {Tokens} RawEndpoint={Raw} OrderId={OrderId}", string.Join(",", unresolved), rawEndpoint, orderId);
        }

        _logger.LogInformation("Navigating (capture only) to URL: {Url} (raw endpoint: {RawEndpoint})", url, rawEndpoint);

        // Get current authentication credentials
        var token = _tokenRenewal.GetCurrentToken();
        var cookies = _tokenRenewal.GetCurrentCookies();

        // Don't use 'await using' - we need to keep the page alive
        // Dispose any previously captured page defensively to avoid multi-page interference
        if (_capturedPage != null)
        {
            try
            {
                await _capturedPage.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed disposing previous captured page (non-fatal)");
            }
            finally
            {
                _capturedPage = null;
                _capturedPageContextId = string.Empty;
            }
        }
        PuppeteerSharp.IPage page;
        if (_pdfConfig.IsolateBrowserPerPicklist)
        {
            _logger.LogDebug("Isolated browser mode enabled; launching dedicated Chromium instance for picklist navigation");
            page = await _browserManager.NewIsolatedBrowserPageAsync(ct, token, cookies);
            _capturedBrowser = page.Browser;
        }
        else
        {
            page = await _browserManager.NewPageAsync(ct, token, cookies);
            _capturedBrowser = null; // ensure null when not isolated
        }

        // Attach response listener EARLY (before any navigation) to avoid missing initial picklist XHR
        var picklistIdForDetectionEarly = (context != null && context.TryGetValue("pickListId", out var earlyCtxId) ? earlyCtxId?.ToString() : orderId) ?? string.Empty;
        var apiDetailsUrlFragmentEarly = "/api/Picklist/GetPickListDetails";
        bool apiDataObservedEarly = false;
        string? interceptedJsonEarly = null;
        var apiDetectionTcsEarly = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Response += async (_, e) =>
        {
            try
            {
                var resp = e.Response;
                var rUrl = resp.Url;
                var method = resp.Request?.Method?.ToString() ?? string.Empty;
                if (!apiDataObservedEarly && rUrl.Contains(apiDetailsUrlFragmentEarly, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrEmpty(picklistIdForDetectionEarly) || rUrl.Contains($"picklistid={picklistIdForDetectionEarly}", StringComparison.OrdinalIgnoreCase)) && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    if (resp.Status == System.Net.HttpStatusCode.OK)
                    {
                        string body = string.Empty;
                        try { body = await resp.TextAsync(); } catch { }
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            var lower = body.ToLowerInvariant();
                            bool looksSuccess = lower.Contains("\"responsecode\":0") || lower.Contains("\"responsetype\":\"success\"");
                            bool hasKeys = lower.Contains("picklistnumber") || lower.Contains("picklistid") || lower.Contains("\"picklist\"");
                            if (looksSuccess && hasKeys)
                            {
                                apiDataObservedEarly = true;
                                interceptedJsonEarly = body;
                                _logger.LogInformation("(Early) Observed picklist details API response id {Id} length {Len}", picklistIdForDetectionEarly, body.Length);
                                apiDetectionTcsEarly.TrySetResult(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Early response listener error (non-fatal)");
            }
        };

        // Production: removed verbose console diagnostic hooks.

        // Navigate to base URL first to establish domain context
        var rootUrl = _config.BaseUrl.TrimEnd('/') + "/";
        _logger.LogDebug("Loading application shell {RootUrl} to establish domain context", rootUrl);
        try
        {
            await page.GoToAsync(rootUrl, new PuppeteerSharp.NavigationOptions
            {
                WaitUntil = new[] { PuppeteerSharp.WaitUntilNavigation.DOMContentLoaded },
                Timeout = 30000
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("Root page load timed out ({Message}), continuing anyway", ex.Message);
        }

        // CRITICAL: Inject auth token into storage IMMEDIATELY after domain is loaded
        // This ensures the token is available when we navigate to the hash route
        try
        {
            var storageToken = _tokenRenewal.GetCurrentToken();
            if (!string.IsNullOrWhiteSpace(storageToken))
            {
                await page.EvaluateFunctionAsync("t => { try { localStorage.setItem('token', t); sessionStorage.setItem('token', t); } catch(e) {} }", storageToken);
                _logger.LogDebug("Injected token into localStorage/sessionStorage after domain load");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to inject token into storage (non-fatal)");
        }

        // Wait for SPA framework readiness before setting hash (prevents early hash assignment before route handlers register)
        try
        {
            _logger.LogDebug("Waiting for SPA framework (tf.page) readiness before hash route");
            await page.WaitForFunctionAsync("() => window.tf && window.tf.page && typeof window.tf.page === 'object'", new PuppeteerSharp.WaitForFunctionOptions { Timeout = 15000 });
            _logger.LogDebug("SPA framework ready (tf.page detected)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SPA framework not detected within pre-route timeout ({Message}); proceeding with hash navigation anyway", ex.Message);
        }

        // Pre-seed global variable before and after hash navigation to ensure SPA JavaScript can access it
        try
        {
            var seedId = (context != null && context.TryGetValue("pickListId", out var ctxId) ? ctxId?.ToString() : orderId) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(seedId))
            {
                // Seed before navigation
                await page.EvaluateExpressionAsync($"try{{ window.picklist_id='{seedId}'; }}catch(e){{}} ");
            }
        }
        catch { /* non-fatal */ }

        // Set the hash to trigger SPA routing
        await page.EvaluateExpressionAsync($"window.location.href='{url.Replace("'","%27")}'");

        // Re-seed after hash change with small delay to ensure SPA reads the variable
        try
        {
            var reSeedId = (context != null && context.TryGetValue("pickListId", out var ctxId2) ? ctxId2?.ToString() : orderId) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(reSeedId))
            {
                await Task.Delay(100); // Small delay for hash change to register
                await page.EvaluateExpressionAsync($"try{{ window.picklist_id='{reSeedId}'; }}catch(e){{}} ");
                await Task.Delay(200); // Allow SPA time to read variable before making API calls
            }
        }
        catch { /* non-fatal */ }

        // If configured, attempt auto-click of a row matching the id to trigger details XHR
        if (!string.IsNullOrWhiteSpace(_pdfConfig.AutoClickRowSelector))
        {
            try
            {
                _logger.LogDebug("Attempting auto-click using selector {Selector} for id {Id}", _pdfConfig.AutoClickRowSelector, orderId);
                // Wait briefly for rows to render (not cancellation token – critical SPA step)
                await Task.Delay(500);
                bool clickResult = await page.EvaluateFunctionAsync<bool>("(sel,id) => { try { const rows = Array.from(document.querySelectorAll(sel)); for(const r of rows){ const txt = (r.innerText||'').trim(); if(txt.includes(id)){ r.click(); return true; } } } catch(e){} return false; }", _pdfConfig.AutoClickRowSelector, orderId);
                if (clickResult)
                {
                    _logger.LogInformation("Auto-click dispatched for row containing id {Id}", orderId);
                }
                else
                {
                    _logger.LogWarning("Auto-click failed to find row containing id {Id} with selector {Selector}", orderId, _pdfConfig.AutoClickRowSelector);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Auto-click logic error (non-fatal)");
            }
        }

        // Verify effective href after assignment (diagnostic for home page issue)
        try
        {
            var effectiveHref = await page.EvaluateExpressionAsync<string>("window.location.href");
            if (!string.Equals(effectiveHref, url, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Effective window.location.href ({Effective}) does not match expected URL ({Expected}) - SPA may have redirected to home or token substitution failed", effectiveHref, url);
                // Second attempt after short delay if mismatch persists (route race mitigation)
                await Task.Delay(1500); // Increased delay for production stability
                await page.EvaluateExpressionAsync($"window.location.href='{url.Replace("'","%27")}'");
                var retryHref = await page.EvaluateExpressionAsync<string>("window.location.href");
                if (!string.Equals(retryHref, url, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Retry href assignment still mismatched ({RetryHref}); giving up and continuing with diagnostics", retryHref);
                }
                else
                {
                    _logger.LogInformation("Retry href assignment succeeded; effective href now matches target URL");
                }
            }
            else
            {
                _logger.LogDebug("Effective window.location.href matches expected URL");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read effective window.location.href (non-fatal)");
        }

        // Production: removed hashchange listener instrumentation.

        // Optimized picklist data readiness detection (using early listener variables)
        var picklistIdForDetection = picklistIdForDetectionEarly; // reuse
        var detectionTimeoutMs = Math.Max(5000, _pdfConfig.DataLoadRetryMs > 0 ? _pdfConfig.DataLoadRetryMs : 10000);
        _logger.LogDebug("Awaiting picklist API (timeout {Ms}ms, id {Id})", detectionTimeoutMs, picklistIdForDetection);
        var detectionCompleted = await Task.WhenAny(apiDetectionTcsEarly.Task, Task.Delay(detectionTimeoutMs));

        if (detectionCompleted == apiDetectionTcsEarly.Task && apiDetectionTcsEarly.Task.IsCompletedSuccessfully)
        {
            _logger.LogInformation("Picklist data readiness confirmed early; skipping fallback waits");
            if (!string.IsNullOrWhiteSpace(interceptedJsonEarly))
            {
                try
                {
                    using var doc = JsonDocument.Parse(interceptedJsonEarly);
                    var root = doc.RootElement;
                    int itemCount = 0;
                    foreach (var candidate in new[] {"data", "items", "pickListDetails"})
                    {
                        if (root.TryGetProperty(candidate, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            itemCount = arr.GetArrayLength();
                            break;
                        }
                    }
                    string? pickListNumber = null;
                    if (root.TryGetProperty("pickListNumber", out var numProp) && numProp.ValueKind == JsonValueKind.String)
                    {
                        pickListNumber = numProp.GetString();
                    }
                    if (string.IsNullOrWhiteSpace(pickListNumber) && root.TryGetProperty("pickListId", out var idProp))
                    {
                        pickListNumber = idProp.ToString();
                    }
                    _logger.LogInformation("Picklist JSON validation summary: PickListNumberOrId={PickListNumber} ItemCount={ItemCount}", pickListNumber ?? "(n/a)", itemCount);
                    _lastInterceptedPicklistJson = interceptedJsonEarly.Length > 5000 ? interceptedJsonEarly.Substring(0, 5000) + $"...(truncated {interceptedJsonEarly.Length} chars)" : interceptedJsonEarly;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed parsing intercepted picklist JSON (non-fatal)");
                }
            }
        }
        else
        {
            _logger.LogWarning("Picklist API not observed within {Ms}ms; using fallback timing", detectionTimeoutMs);

            _logger.LogDebug("Fallback: waiting 1500ms for hash route rendering");
            await Task.Delay(1500); // Don't pass ct – critical for route detection

            if (action.WaitForNetworkIdleMs > 0)
            {
                _logger.LogDebug("Fallback: waiting {Ms}ms for SPA data loading", action.WaitForNetworkIdleMs);
                await Task.Delay(action.WaitForNetworkIdleMs); // Don't pass ct
            }

            try
            {
                _logger.LogDebug("Fallback: waiting for network idle");
                await page.WaitForNetworkIdleAsync(new PuppeteerSharp.WaitForNetworkIdleOptions
                {
                    IdleTime = 1000,
                    Timeout = 30000
                });
                _logger.LogDebug("Fallback: network became idle successfully");
            }
            catch (Exception ex) when (ex is PuppeteerSharp.WaitTaskTimeoutException or TaskCanceledException)
            {
                _logger.LogWarning("Fallback: network idle wait interrupted ({Type}) - continuing: {Message}", ex.GetType().Name, ex.Message);
            }

            _logger.LogDebug("Fallback: final 1000ms stabilization delay");
            await Task.Delay(1000); // shorter final delay

            // Manual fetch fallback if still no data observed
            if (!apiDataObservedEarly && !string.IsNullOrWhiteSpace(picklistIdForDetection))
            {
                try
                {
                    _logger.LogDebug("Manual fetch fallback for picklist id {Id}", picklistIdForDetection);
                    var manualJson = await page.EvaluateExpressionAsync<string>($"fetch('/api/Picklist/GetPickListDetails?picklistid={picklistIdForDetection}').then(r=>r.text()).catch(_=>'')");
                    if (!string.IsNullOrWhiteSpace(manualJson))
                    {
                        var lower = manualJson.ToLowerInvariant();
                        bool looksSuccess = lower.Contains("\"responsecode\":0") || lower.Contains("\"responsetype\":\"success\"");
                        bool hasKeys = lower.Contains("picklistnumber") || lower.Contains("picklistid") || lower.Contains("\"picklist\"");
                        if (looksSuccess && hasKeys)
                        {
                            _logger.LogInformation("Manual fetch obtained picklist details (length {Len})", manualJson.Length);
                            _lastInterceptedPicklistJson = manualJson.Length > 5000 ? manualJson.Substring(0,5000)+$"...(truncated {manualJson.Length} chars)" : manualJson;
                        }
                        else
                        {
                            _logger.LogWarning("Manual fetch content failed validation; success={Success} hasKeys={HasKeys} length={Len}", looksSuccess, hasKeys, manualJson.Length);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Manual fetch returned empty body for picklist id {Id}", picklistIdForDetection);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Manual fetch fallback failed (non-fatal)");
                }
            }
        }

        // Optional selector readiness (from PdfConfig) before capturing page
        if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
        {
            try
            {
                _logger.LogDebug("Waiting for selector '{Selector}' before capturing page", _pdfConfig.WaitForSelector);
                await page.WaitForSelectorAsync(_pdfConfig.WaitForSelector, new PuppeteerSharp.WaitForSelectorOptions
                {
                    Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
                });
                var escaped = _pdfConfig.WaitForSelector.Replace("'", "\\'");
                _logger.LogDebug("Ensuring selector '{Selector}' has populated text", _pdfConfig.WaitForSelector);
                await page.WaitForFunctionAsync($"() => (document.querySelector('{escaped}')?.textContent || '').trim().length > 0", new PuppeteerSharp.WaitForFunctionOptions
                {
                    Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Selector readiness check failed; continuing anyway");
            }
        }

        // Additional selector presence waits (presence only, not content)
        if (_pdfConfig.AdditionalWaitSelectors?.Count > 0)
        {
            foreach (var sel in _pdfConfig.AdditionalWaitSelectors.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                try
                {
                    _logger.LogDebug("Waiting for additional selector '{Selector}'", sel);
                    await page.WaitForSelectorAsync(sel, new PuppeteerSharp.WaitForSelectorOptions
                    {
                        Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Additional selector '{Selector}' not found within timeout; continuing", sel);
                }
            }
        }

        // Data rows readiness (indicates XHR-rendered data present)
        if (_pdfConfig.MinimumDataRows > 0 && !string.IsNullOrWhiteSpace(_pdfConfig.DataReadyRowSelector))
        {
            try
            {
                var elapsed = 0;
                var interval = 500;
                var target = _pdfConfig.MinimumDataRows;
                var selector = _pdfConfig.DataReadyRowSelector;
                var escapedRowSel = selector.Replace("'", "\\'");
                int currentCount = await page.EvaluateFunctionAsync<int>("sel => document.querySelectorAll(sel).length", selector);
                if (currentCount < target)
                {
                    _logger.LogDebug("Initial row count {Current} below minimum {Min}; entering polling loop", currentCount, target);
                    while (elapsed < _pdfConfig.DataLoadRetryMs && currentCount < target)
                    {
                        try
                        {
                            await page.WaitForFunctionAsync($"() => document.querySelectorAll('{escapedRowSel}').length >= {target}", new PuppeteerSharp.WaitForFunctionOptions { Timeout = interval });
                        }
                        catch { /* swallow timeout for short interval */ }
                        currentCount = await page.EvaluateFunctionAsync<int>("sel => document.querySelectorAll(sel).length", selector);
                        _logger.LogDebug("Row polling: t={Elapsed}ms count={Count}", elapsed, currentCount);
                        if (currentCount >= target) break;
                        elapsed += interval;
                    }
                }
                _logger.LogInformation("Data row diagnostic: Selector={Selector} FinalCount={Count} PolledMs={Elapsed}", selector, currentCount, elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate data row readiness selector '{Selector}'", _pdfConfig.DataReadyRowSelector);
            }
        }

        // Selector & data container outerHTML diagnostics (truncated)
        try
        {
            if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
            {
                var selHtml = await page.EvaluateFunctionAsync<string>("sel => { const el=document.querySelector(sel); if(!el) return ''; const html=el.outerHTML; return html.length>500? html.substring(0,500)+'...('+html.length+' chars)': html; }", _pdfConfig.WaitForSelector);
                if (!string.IsNullOrEmpty(selHtml))
                {
                    _logger.LogDebug("OuterHTML snippet for WaitForSelector '{Selector}': {HtmlSnippet}", _pdfConfig.WaitForSelector, selHtml);
                }
            }
            if (!string.IsNullOrWhiteSpace(_pdfConfig.DataReadyRowSelector))
            {
                var rowParentHtml = await page.EvaluateFunctionAsync<string>("rowSel => { const row=document.querySelector(rowSel); if(!row) return ''; const parent=row.closest('table')||row.parentElement; if(!parent) return ''; const html=parent.outerHTML; return html.length>500? html.substring(0,500)+'...('+html.length+' chars)': html; }", _pdfConfig.DataReadyRowSelector);
                if (!string.IsNullOrEmpty(rowParentHtml))
                {
                    _logger.LogDebug("OuterHTML snippet for data container (from row selector '{Selector}'): {HtmlSnippet}", _pdfConfig.DataReadyRowSelector, rowParentHtml);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed capturing outerHTML diagnostics");
        }

        // Optional stabilization delay after everything is present
        if (_pdfConfig.PostSelectorStableMs > 0)
        {
            _logger.LogDebug("Post-selector stabilization delay {Ms}ms", _pdfConfig.PostSelectorStableMs);
            await Task.Delay(_pdfConfig.PostSelectorStableMs);
        }

        // Diagnostic screenshot after navigation if enabled
        if (_pdfConfig.CaptureDiagnosticScreenshot)
        {
            try
            {
                var shotsDir = DataPaths.EnsureDir("screenshots");
                var shotPath = Path.Combine(shotsDir, $"nav_{DateTime.Now:yyyyMMdd_HHmmssfff}_{(string.IsNullOrWhiteSpace(orderId) ? picklistIdForDetection : orderId)}.png");
                await page.ScreenshotAsync(shotPath, new PuppeteerSharp.ScreenshotOptions { FullPage = true });
                _logger.LogInformation("Captured navigation diagnostic screenshot: {Path}", shotPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture navigation diagnostic screenshot");
            }
        }

        // Diagnostic: log HTML length & presence of selector
        try
        {
            var html = await page.GetContentAsync();
            var length = html.Length;
            bool selectorPresent = false;
            if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
            {
                selectorPresent = await page.EvaluateFunctionAsync<bool>("sel => !!document.querySelector(sel)", _pdfConfig.WaitForSelector);
            }
            _logger.LogInformation("Navigation diagnostic: HTML length={Length} SelectorPresent={SelectorPresent}", length, selectorPresent);
            if (!string.IsNullOrWhiteSpace(_lastInterceptedPicklistJson))
            {
                _logger.LogDebug("Navigation diagnostic: Stored intercepted picklist JSON snippet length={Len}", _lastInterceptedPicklistJson.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture navigation diagnostic HTML");
        }

        // Store the live page for subsequent print action (don't dispose it)
        _capturedPage = page;
        _capturedPageContextId = string.IsNullOrWhiteSpace(picklistIdForDetection) ? orderId : picklistIdForDetection;
        _logger.LogInformation("Page fully loaded and ready for printing (keeping browser page alive)");
    }

    // New: Print previously captured page
    private async Task ExecutePrintCapturedHtmlAsync(SubAction action, string orderId, CancellationToken ct)
    {
        if (_capturedPage == null)
        {
            _logger.LogWarning("No captured page available to print. Skipping print action.");
            return;
        }

        var jobName = string.IsNullOrWhiteSpace(_capturedPageContextId)
            ? action.Name
            : $"{action.Name}-{_capturedPageContextId}";

        _logger.LogInformation("Printing from live browser page. Job: {JobName}", jobName);
        
        // Re-verify selector readiness if configured (defensive check; page may have updated since capture)
        if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
        {
            try
            {
                bool selectorPresent = await _capturedPage.EvaluateFunctionAsync<bool>("sel => !!document.querySelector(sel)", _pdfConfig.WaitForSelector);
                _logger.LogDebug("Print diagnostic: Selector '{Selector}' present={Present}", _pdfConfig.WaitForSelector, selectorPresent);
                if (selectorPresent)
                {
                    var escaped = _pdfConfig.WaitForSelector.Replace("'", "\\'");
                    await _capturedPage.WaitForFunctionAsync($"() => (document.querySelector('{escaped}')?.textContent || '').trim().length > 0", new PuppeteerSharp.WaitForFunctionOptions
                    {
                        Timeout = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Selector verification during print failed; continuing");
            }
        }

        // Log HTML length prior to PDF generation
        try
        {
            var html = await _capturedPage.GetContentAsync();
            _logger.LogInformation("Print diagnostic: HTML length before PDF={Length}", html.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get HTML content length before PDF generation");
        }

        // Row count & outerHTML diagnostics before PDF (may have changed since navigation)
        try
        {
            if (_pdfConfig.MinimumDataRows > 0 && !string.IsNullOrWhiteSpace(_pdfConfig.DataReadyRowSelector))
            {
                var currentCount = await _capturedPage.EvaluateFunctionAsync<int>("sel => document.querySelectorAll(sel).length", _pdfConfig.DataReadyRowSelector);
                _logger.LogInformation("Pre-print data row diagnostic: Selector={Selector} Count={Count}", _pdfConfig.DataReadyRowSelector, currentCount);
            }
            if (!string.IsNullOrWhiteSpace(_pdfConfig.DataReadyRowSelector))
            {
                var rowParentHtml = await _capturedPage.EvaluateFunctionAsync<string>("rowSel => { const row=document.querySelector(rowSel); if(!row) return ''; const parent=row.closest('table')||row.parentElement; if(!parent) return ''; const html=parent.outerHTML; return html.length>500? html.substring(0,500)+'...('+html.length+' chars)': html; }", _pdfConfig.DataReadyRowSelector);
                if (!string.IsNullOrEmpty(rowParentHtml))
                {
                    _logger.LogDebug("Pre-print outerHTML snippet for data container: {HtmlSnippet}", rowParentHtml);
                }
            }
            if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
            {
                var selHtml = await _capturedPage.EvaluateFunctionAsync<string>("sel => { const el=document.querySelector(sel); if(!el) return ''; const html=el.outerHTML; return html.length>500? html.substring(0,500)+'...('+html.length+' chars)': html; }", _pdfConfig.WaitForSelector);
                if (!string.IsNullOrEmpty(selHtml))
                {
                    _logger.LogDebug("Pre-print outerHTML snippet for WaitForSelector '{Selector}': {HtmlSnippet}", _pdfConfig.WaitForSelector, selHtml);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed capturing pre-print outerHTML diagnostics");
        }

        // Optional diagnostic screenshot before PDF
        if (_pdfConfig.CaptureDiagnosticScreenshot)
        {
            try
            {
                var shotsDir = DataPaths.EnsureDir("screenshots");
                var shotPath = Path.Combine(shotsDir, $"preprint_{DateTime.Now:yyyyMMdd_HHmmssfff}_{orderId}.png");
                await _capturedPage.ScreenshotAsync(shotPath, new PuppeteerSharp.ScreenshotOptions { FullPage = true });
                _logger.LogInformation("Captured pre-print diagnostic screenshot: {Path}", shotPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture pre-print diagnostic screenshot");
            }
        }

        // Generate PDF directly from the live page (preserves CSS and JavaScript state)
        var pdfBytes = await CreatePdfFromPageAsync(_capturedPage, ct);
        
        // Save the PDF to disk (MUST succeed)
        var outputDir = DataPaths.EnsureDir("out");
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        var filename = $"{timestamp}_{jobName}.pdf";
        var filePath = Path.Combine(outputDir, filename);
        await File.WriteAllBytesAsync(filePath, pdfBytes, ct);
        _logger.LogInformation("PDF saved to {Path}", filePath);

        // Send to printer if configured (MUST succeed - exception will trigger retry)
        await _printer.PrintPdfBytesAsync(pdfBytes, jobName, ct);
        _logger.LogInformation("PDF sent to printer: {JobName}", jobName);

        if (!string.IsNullOrWhiteSpace(_lastInterceptedPicklistJson))
        {
            _logger.LogDebug("Print diagnostic: Intercepted picklist JSON snippet length={Len}", _lastInterceptedPicklistJson.Length);
        }

        _logger.LogInformation("Successfully processed page (saved and printed)");

        // Dispose the page and clear reference
        await _capturedPage.DisposeAsync();
        _capturedPage = null;
        if (_capturedBrowser != null && _pdfConfig.IsolateBrowserPerPicklist)
        {
            try
            {
                await _capturedBrowser.CloseAsync();
                _capturedBrowser.Dispose();
                _logger.LogDebug("Closed isolated browser instance for picklist context {ContextId}", _capturedPageContextId);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed closing isolated browser (non-fatal)");
            }
            finally
            {
                _capturedBrowser = null;
            }
        }
        _capturedPageContextId = string.Empty;
        _lastInterceptedPicklistJson = null;
    }

    private async Task<byte[]> CreatePdfFromPageAsync(PuppeteerSharp.IPage page, CancellationToken ct)
    {
        var marginInches = _pdfConfig.MarginMillimeters / 25.4;

        var pdfOptions = new PuppeteerSharp.PdfOptions
        {
            PrintBackground = _pdfConfig.PrintBackground,
            Landscape = _pdfConfig.Landscape,
            MarginOptions = new PuppeteerSharp.Media.MarginOptions
            {
                Top = $"{marginInches}in",
                Right = $"{marginInches}in",
                Bottom = $"{marginInches}in",
                Left = $"{marginInches}in"
            }
        };

        if (_pdfConfig.PageWidthInches.HasValue && _pdfConfig.PageHeightInches.HasValue)
        {
            pdfOptions.Width = $"{_pdfConfig.PageWidthInches.Value}in";
            pdfOptions.Height = $"{_pdfConfig.PageHeightInches.Value}in";
        }

        _logger.LogInformation("Generating PDF (Landscape={Landscape}, Bg={Bg})", pdfOptions.Landscape, pdfOptions.PrintBackground);
        var bytes = await page.PdfDataAsync(pdfOptions);
        _logger.LogInformation("Generated PDF with {Length} bytes", bytes.Length);
        return bytes;
    }

    private async Task ExecuteCallApiWithContextAsync(SubAction action, Dictionary<string, object> context, CancellationToken ct)
    {
        var endpoint = ReplaceTokensWithContext(action.Endpoint, string.Empty, context);
        _logger.LogDebug("Calling API with context: {Method} {Endpoint}", action.Method, endpoint);

        Func<HttpRequestMessage> factory = () =>
        {
            var req = new HttpRequestMessage(new HttpMethod(action.Method), endpoint);

            // Add custom headers
            foreach (var header in action.Headers)
            {
                req.Headers.TryAddWithoutValidation(header.Key, ReplaceTokensWithContext(header.Value, string.Empty, context));
            }

            // Add request body if provided
            if (!string.IsNullOrEmpty(action.RequestBody))
            {
                var body = ReplaceTokensWithContext(action.RequestBody, string.Empty, context);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                _logger.LogDebug("Request body: {Body}", body);
            }
            return req;
        };

        var response = await SendWithRetryAsync(factory, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Chained API response: {Body}", responseBody);
    }

    private string ExtractJobNameFromContext(Dictionary<string, object> context)
    {
        if (context.TryGetValue("pickListId", out var id))
        {
            return id.ToString() ?? "unknown";
        }
        if (context.TryGetValue("orderId", out var orderId))
        {
            return orderId.ToString() ?? "unknown";
        }
        return "unknown";
    }

    private string ReplaceTokensWithContext(string input, string orderId, Dictionary<string, object> context)
    {
        var result = input;

        // Replace order ID tokens
        if (!string.IsNullOrEmpty(orderId))
        {
            result = result.Replace("{id}", orderId, StringComparison.OrdinalIgnoreCase)
                           .Replace("{orderId}", orderId, StringComparison.OrdinalIgnoreCase);
        }

        // Replace context tokens (e.g., {pickListId}, {orderNumber})
        foreach (var kvp in context)
        {
            var token = $"{{{kvp.Key}}}";
            var value = kvp.Value?.ToString() ?? string.Empty;
            result = result.Replace(token, value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private string ReplaceTokens(string input, string orderId)
    {
        return input.Replace("{id}", orderId, StringComparison.OrdinalIgnoreCase)
                    .Replace("{orderId}", orderId, StringComparison.OrdinalIgnoreCase);
    }

    private string ExtractHtmlFromJson(string jsonResponse, string jsonPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Simple JSON path parsing (supports property names like "html" or "data.html")
            var parts = jsonPath.Split('.');
            var current = root;

            foreach (var part in parts)
            {
                if (current.TryGetProperty(part, out var element))
                {
                    current = element;
                }
                else
                {
                    _logger.LogWarning("JSON path not found: {Path}", jsonPath);
                    return string.Empty;
                }
            }

            return current.GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract HTML from JSON");
            return string.Empty;
        }
    }
}
