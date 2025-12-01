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
    Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, ApiConfig apiConfig, CancellationToken ct = default);
    Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, CancellationToken ct = default);
    Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, ApiConfig apiConfig, CancellationToken ct = default);
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
        // Temporary config override for database-driven execution
        private ApiConfig? _tempApiConfig = null;
        // Captured page from the most recent navigation-only action (kept alive for printing)
        private PuppeteerSharp.IPage? _capturedPage;
        private string _capturedPageContextId = string.Empty;
        private PuppeteerSharp.IBrowser? _capturedBrowser; // for isolated mode cleanup
        // Last intercepted picklist JSON body (for diagnostics / potential later validation)
        private string? _lastInterceptedPicklistJson;
        // Last saved PDF path from SaveCapturedHtml action (for PrintSavedPdf action)
        private string? _lastSavedPdfPath;

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

    /// <summary>
    /// Gets the active API configuration (temporary override or default)
    /// </summary>
    private ApiConfig GetActiveConfig() => _tempApiConfig ?? _config;

    /// <summary>
    /// Updates HttpClient headers for a specific API configuration (without changing BaseAddress)
    /// </summary>
    private void UpdateHttpClientForApiConfig(ApiConfig config)
    {
        // NOTE: Cannot change BaseAddress after first request - HttpClient limitation
        // All our APIs use the same BaseUrl anyway, so we just update headers

        _httpClient.DefaultRequestHeaders.Clear();

        // Update Bearer token
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrWhiteSpace(config.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.BearerToken);
        }

        // Add standard headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("WarehouseId", config.WarehouseId.ToString());
        _httpClient.DefaultRequestHeaders.Add("Origin", config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{config.BaseUrl}/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

        // Add cookies
        if (config.Cookies.Count > 0)
        {
            var cookieString = string.Join("; ", config.Cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookieString);
        }
    }

    public async Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, CancellationToken ct = default)
    {
        var activeConfig = GetActiveConfig();
        _logger.LogDebug("Executing {Count} sub-actions for order {OrderId}", activeConfig.SubActions.Count, orderId);

        for (int i = 0; i < activeConfig.SubActions.Count; i++)
        {
            var action = activeConfig.SubActions[i];
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

                // Apply filter if configured (skip action if filter doesn't match)
                if (!string.IsNullOrEmpty(action.ChainedFilterType) && !string.IsNullOrEmpty(action.ChainedFilterField))
                {
                    var orderDict = JsonElementToDictionary(orderData);
                    if (!ApplyChainedFilter(orderDict, action))
                    {
                        _logger.LogDebug("[{Num}/{Total}] {ActionName} skipped for order {OrderId} due to filter: {FilterType} on field {Field}",
                            actionNum, activeConfig.SubActions.Count, action.Name, orderId, action.ChainedFilterType, action.ChainedFilterField);
                        continue; // Skip this action for this order
                    }
                }

                _logger.LogInformation("[{Num}/{Total}] {ActionName} for order {OrderId}",
                    actionNum, activeConfig.SubActions.Count, action.Name, orderId);

                await ExecuteActionAsync(action, orderId, orderData, ct);

                _logger.LogInformation("[{Num}/{Total}] {ActionName} completed successfully",
                    actionNum, activeConfig.SubActions.Count, action.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Num}/{Total}] {ActionName} failed: {Message}",
                    actionNum, activeConfig.SubActions.Count, action.Name, ex.Message);

                if (!(action.ContinueOnError ?? true))
                {
                    _logger.LogWarning("Stopping action chain due to error (ContinueOnError=false)");
                    throw;
                }

                _logger.LogInformation("Continuing to next action (ContinueOnError=true)");
            }
        }

        _logger.LogDebug("All sub-actions completed for order {OrderId}", orderId);
    }

    public async Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, ApiConfig apiConfig, CancellationToken ct = default)
    {
        // Store previous temp config (for nested calls)
        var previousTemp = _tempApiConfig;
        try
        {
            // Set temporary config override
            _tempApiConfig = apiConfig;

            // Update HTTP client headers for this API config (but not BaseAddress - can't change after first use)
            UpdateHttpClientForApiConfig(apiConfig);

            // Execute actions (will use GetActiveConfig() which returns apiConfig)
            await ExecuteActionsForOrderAsync(orderId, orderData, ct);
        }
        finally
        {
            // Restore previous state
            _tempApiConfig = previousTemp;
            UpdateHttpClientAuth(); // Just update auth, don't touch BaseAddress
        }
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
                var context = JsonElementToDictionary(orderData);
                await ExecuteGetUrlAndPrintAsync(action, orderId, context, ct);
                break;

            case "navigateonly":
                var navContext = JsonElementToDictionary(orderData);
                await ExecuteNavigateOnlyAsync(action, orderId, navContext, ct);
                break;

            case "printcapturedhtml":
                await ExecutePrintCapturedHtmlAsync(action, orderId, ct);
                break;

            case "savecapturedhtml":
                await ExecuteSaveCapturedHtmlAsync(action, orderId, ct);
                break;

            case "printsavedpdf":
                await ExecutePrintSavedPdfAsync(action, orderId, ct);
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
        var activeConfig = GetActiveConfig();

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

        var batchSize = action.BatchSize ?? 10;
        var batches = idList.Chunk(batchSize).ToList();
        _logger.LogInformation("Creating pending order picklists in {BatchCount} batch(es) of up to {BatchSize}.", batches.Count, batchSize);

        for (var i = 0; i < batches.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var batch = batches[i];
            var payload = new
            {
                orderId = batch,
                QuickShip = action.QuickShip ?? false,
                ForceCreatePicklist = action.ForceCreatePicklist ?? false
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

            var httpCt = activeConfig.ManualMode ? CancellationToken.None : ct;
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

    public async Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, ApiConfig apiConfig, CancellationToken ct = default)
    {
        // Store previous temp config (for nested calls)
        var previousTemp = _tempApiConfig;
        try
        {
            // Set temporary config override
            _tempApiConfig = apiConfig;

            // Update HTTP client headers for this API config (but not BaseAddress - can't change after first use)
            UpdateHttpClientForApiConfig(apiConfig);

            // Execute batch action (will use GetActiveConfig() which returns apiConfig)
            await ExecuteBatchCreatePicklistAsync(action, orderIds, ct);
        }
        finally
        {
            // Restore previous state
            _tempApiConfig = previousTemp;
            UpdateHttpClientAuth(); // Just update auth, don't touch BaseAddress
        }
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var activeConfig = GetActiveConfig();
        var attempts = Math.Max(1, activeConfig.RetryMaxAttempts);
        var baseDelay = Math.Max(50, activeConfig.RetryBaseDelayMs);
        var maxDelay = Math.Max(baseDelay, activeConfig.RetryMaxDelayMs);
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
                        _logger.LogInformation("Attempting to renew authentication token (forcing fresh token from server)");
                        // Force refresh to bypass cached token since server rejected it with 401
                        tokenRenewed = await _tokenRenewal.RenewTokenAsync(ct, forceRefresh: true);

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
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && (action.ContinueOnError ?? true))
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
        var printerName = GetActiveConfig().PrinterName;
        await _printer.PrintHtmlAsync(htmlContent, jobName, printerName, ct);

        _logger.LogInformation("Successfully printed {JobName} to printer {Printer}", jobName, printerName ?? "default");
    }

    private async Task ExecuteDelayAsync(SubAction action, CancellationToken ct)
    {
        var delayMs = action.DelayMilliseconds ?? 1000;
        _logger.LogDebug("Delaying for {Ms}ms", delayMs);
        await Task.Delay(delayMs, ct);
    }

    public async Task ExecuteChainedActionsAsync(SubAction sourceAction, string responseBody, CancellationToken ct = default)
    {
        var activeConfig = GetActiveConfig();

        // Find chained actions (actions with UseChainedInput that follow this action)
        var sourceIndex = activeConfig.SubActions.IndexOf(sourceAction);
        if (sourceIndex == -1)
        {
            _logger.LogDebug("Source action not found in config, skipping chained execution");
            return;
        }

        var chainedActions = activeConfig.SubActions
            .Skip(sourceIndex + 1)
            .Where(a => a.Enabled && (a.UseChainedInput == true))
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
                    if (!(chainedAction.ContinueOnError ?? true))
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

                // Check if item is an array or object
                if (item.ValueKind == JsonValueKind.Array)
                {
                    // Item is an array - extract each element by index
                    var arrayItems = item.EnumerateArray().ToList();
                    for (int i = 0; i < arrayItems.Count; i++)
                    {
                        var value = ExtractJsonValue(arrayItems[i]);
                        if (value != null)
                        {
                            itemData[i.ToString()] = value;
                        }
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    // Item is an object - extract all properties
                    foreach (var prop in item.EnumerateObject())
                    {
                        var value = ExtractJsonValue(prop.Value);
                        if (value != null)
                        {
                            itemData[prop.Name] = value;
                        }
                    }
                }
                else
                {
                    // Item is a primitive value - store as index 0
                    var value = ExtractJsonValue(item);
                    if (value != null)
                    {
                        itemData["0"] = value;
                    }
                }

                // Apply filter if configured
                if (!string.IsNullOrEmpty(sourceAction.ChainedFilterType) &&
                    (sourceAction.ChainedFilterArrayIndex.HasValue || !string.IsNullOrEmpty(sourceAction.ChainedFilterField)))
                {
                    if (!ApplyChainedFilter(itemData, sourceAction))
                    {
                        _logger.LogDebug("Item filtered out by {FilterType}",
                            sourceAction.ChainedFilterType);
                        continue; // Skip this item
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

    private bool ApplyChainedFilter(Dictionary<string, object> itemData, SubAction sourceAction)
    {
        var filterField = sourceAction.ChainedFilterField!;
        var filterType = sourceAction.ChainedFilterType!.ToLowerInvariant();
        var filterValue = sourceAction.ChainedFilterValue ?? string.Empty;

        // Get field value - handle both direct fields and array indices
        string fieldValue;

        if (sourceAction.ChainedFilterArrayIndex.HasValue)
        {
            // Handle array index filtering (e.g., data[x][17])
            // Item is an array, get value at specified index
            var arrayIndex = sourceAction.ChainedFilterArrayIndex.Value;

            // Check if itemData contains an array representation
            // When ChainedItemFieldPath is like "[0]", the entire item is the array
            if (itemData.Count == 1 && itemData.ContainsKey("__array__"))
            {
                // Special case: entire item is array
                var arrayObj = itemData["__array__"];
                if (arrayObj is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
                {
                    var arrayElements = jsonArray.EnumerateArray().ToList();
                    if (arrayIndex >= 0 && arrayIndex < arrayElements.Count)
                    {
                        fieldValue = ExtractJsonValue(arrayElements[arrayIndex])?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        _logger.LogDebug("Array index {Index} out of bounds (length: {Length})", arrayIndex, arrayElements.Count);
                        return false;
                    }
                }
                else
                {
                    _logger.LogDebug("Expected array for index filtering, got {Type}", arrayObj?.GetType().Name ?? "null");
                    return false;
                }
            }
            else
            {
                // Try to get indexed value from item data
                // Look for numeric keys (0, 1, 2, ..., 17, etc.)
                var indexKey = arrayIndex.ToString();
                if (itemData.TryGetValue(indexKey, out var indexedValue))
                {
                    fieldValue = indexedValue?.ToString() ?? string.Empty;
                }
                else
                {
                    _logger.LogDebug("Array index key '{Key}' not found in item data", indexKey);
                    return false;
                }
            }
        }
        else if (!string.IsNullOrEmpty(filterField))
        {
            // Handle normal field filtering
            if (!itemData.TryGetValue(filterField, out var fieldValueObj))
            {
                _logger.LogDebug("Filter field '{Field}' not found in item data", filterField);
                return false; // Field not found, filter out
            }
            fieldValue = fieldValueObj?.ToString() ?? string.Empty;
        }
        else
        {
            _logger.LogWarning("Filter configuration missing both ChainedFilterField and ChainedFilterArrayIndex");
            return true; // Allow through if misconfigured
        }

        return filterType switch
        {
            "notempty" => !string.IsNullOrWhiteSpace(fieldValue),

            "contains" => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),

            "notcontains" => !fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),

            "notstartswith" => !fieldValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),

            "startswith" => fieldValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),

            "equals" => fieldValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),

            "notequals" => !fieldValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),

            // Special filter: must be a file path (contains .html and doesn't start with {)
            "isfilepath" => !string.IsNullOrWhiteSpace(fieldValue) &&
                           fieldValue.Contains(".html", StringComparison.OrdinalIgnoreCase) &&
                           !fieldValue.TrimStart().StartsWith("{"),

            _ => true // Unknown filter type, allow item through
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

            case "savecapturedhtml":
                await ExecuteSaveCapturedHtmlAsync(action, string.Empty, ct);
                break;

            case "printsavedpdf":
                await ExecutePrintSavedPdfAsync(action, string.Empty, ct);
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
        var activeConfig = GetActiveConfig();

        // Build URL with context substitution
        var url = ReplaceTokensWithContext(action.Endpoint, orderId, context);
        _logger.LogInformation("Fetching URL with Puppeteer: {Url}", url);

        // Get current authentication credentials
        var token = _tokenRenewal.GetCurrentToken();
        var cookies = _tokenRenewal.GetCurrentCookies();

        await using var page = await _browserManager.NewPageAsync(ct, token, cookies);

        // Navigate to the page
        _logger.LogDebug("Navigating to {Url}", url);

        // Detect if this is a direct file path (e.g., /Store/CustomForms/file.html) vs SPA hash route
        var isDirectFile = url.Contains(".html", StringComparison.OrdinalIgnoreCase) &&
                          !url.Contains("#") &&
                          (url.Contains("/") || url.Contains("\\"));

        if (isDirectFile)
        {
            // Direct navigation to static HTML file - no SPA shell loading needed
            _logger.LogDebug("Direct file navigation detected (contains .html), navigating directly to {Url}", url);
            try
            {
                await page.GoToAsync(url, new PuppeteerSharp.NavigationOptions
                {
                    WaitUntil = new[] { PuppeteerSharp.WaitUntilNavigation.Load },
                    Timeout = 60000
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning("Direct page load timed out ({Message})", ex.Message);
            }
        }
        else
        {
            // SPA hash navigation - load shell first, then navigate via hash
            // Strategy: First load root application shell, then set hash route via script.
            // This avoids referrerPolicy issues seen when directly navigating to hash URLs.
            var rootUrl = activeConfig.BaseUrl.TrimEnd('/') + "/";
            _logger.LogDebug("SPA navigation detected, loading application shell {RootUrl} before hash navigation", rootUrl);
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
        }

        // Wait additional time for SPA to load data
        var waitMs = action.WaitForNetworkIdleMs ?? 3000;
        if (waitMs > 0)
        {
            _logger.LogDebug("Waiting {Ms}ms for network idle", waitMs);
            await Task.Delay(waitMs, ct);
        }

        // Make hidden fields visible if requested
        if (action.MakeHiddenVisible == true)
        {
            _logger.LogDebug("Making hidden fields visible");
            await page.EvaluateExpressionAsync(@"
                Array.from(document.querySelectorAll('input[type=""hidden""]')).forEach(el => {
                    el.type = 'text';
                    el.style.display = 'block';
                });
            ");
        }

        // Force portrait orientation via CSS and hide "short items" table from PDF output
        _logger.LogDebug("Injecting PDF print styles (portrait orientation + hiding short items)");
        await page.EvaluateExpressionAsync(@"
            // Inject CSS to force portrait mode for PDF printing
            const style = document.createElement('style');
            style.textContent = `
                @page {
                    size: portrait !important;
                    margin: 0.4in;
                }
                @media print {
                    body {
                        width: 8.5in !important;
                        max-width: 8.5in !important;
                    }
                }
                .customer-highlight {
                    background-color: #000 !important;
                    color: #fff !important;
                    font-weight: bold !important;
                    font-size: 1.4em !important;
                    padding: 8px 12px !important;
                    display: inline-block !important;
                    margin: 4px 0 !important;
                    border-radius: 4px !important;
                }
            `;
            document.head.appendChild(style);

            // Find and style CUSTOMER field (label + value)
            let customerStyledCount = 0;
            const allElements = Array.from(document.querySelectorAll('*'));
            allElements.forEach(el => {
                const text = el.textContent?.trim() || '';

                // Method 1: Look for element containing 'CUSTOMER:' label combined with value
                if (text.startsWith('CUSTOMER:') && text.length < 100) {
                    el.classList.add('customer-highlight');
                    customerStyledCount++;
                }
                // Method 2: Look for the value separately if it's in a different element (previous sibling check)
                else if (el.previousElementSibling) {
                    const prevText = el.previousElementSibling.textContent?.trim() || '';
                    if (prevText === 'CUSTOMER:' && text.length > 2 && text.length < 100 && !text.includes(':')) {
                        el.classList.add('customer-highlight');
                        customerStyledCount++;
                    }
                }
                // Method 3: Look for parent element that contains CUSTOMER: label
                else if (el.parentElement) {
                    const parentText = el.parentElement.textContent?.trim() || '';
                    if (parentText.includes('CUSTOMER:') && text.length > 2 && text.length < 100 && !text.includes(':') && text !== 'CUSTOMER:') {
                        // Check if this is the customer value (not another label)
                        const allText = parentText.replace(/\s+/g, ' ');
                        if (allText.match(/CUSTOMER:\s*/ + text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))) {
                            el.classList.add('customer-highlight');
                            customerStyledCount++;
                        }
                    }
                }
            });

            // Find and hide any table or section containing 'short items' text
            const elementsToHide = Array.from(document.querySelectorAll('*')).filter(el => {
                const text = (el.textContent || '').toLowerCase();
                const tag = el.tagName.toLowerCase();
                // Look for tables, divs, or sections that contain 'short items' in their text content
                if ((tag === 'table' || tag === 'div' || tag === 'section') &&
                    (text.includes('short items') || text.includes('shortitems'))) {
                    // Make sure we're not matching a parent that contains the actual short items table
                    const hasShortItemsHeader = Array.from(el.querySelectorAll('*')).some(child => {
                        const childText = (child.textContent || '').toLowerCase();
                        return childText.trim() === 'short items' || childText.includes('short items');
                    });
                    return hasShortItemsHeader || text.match(/short\s*items/i);
                }
                return false;
            });

            elementsToHide.forEach(el => {
                el.style.display = 'none';
            });

            console.log('PDF print styles injected. Hidden Short Items elements: ' + elementsToHide.length + ', Styled customer elements: ' + customerStyledCount);
        ");

        // Generate PDF
        _logger.LogInformation("Generating PDF from page (portrait mode)");
        var pdfBytes = await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
        {
            PrintBackground = true,
            Format = PuppeteerSharp.Media.PaperFormat.Letter,
            Landscape = false
        });

        // Directly send generated PDF bytes to printer (avoid re-rendering & losing dynamic XHR state)
        var jobName = $"{action.Name}-{ExtractJobNameFromContext(context)}";
        var printerName = GetActiveConfig().PrinterName;
        _logger.LogInformation("Printing PDF: {JobName} ({Size} bytes) to printer {Printer}", jobName, pdfBytes.Length, printerName ?? "default");
        await _printer.PrintPdfBytesAsync(pdfBytes, jobName, printerName, ct);
    }

    // New: Navigate only, keep page alive for subsequent print action (supports chained context & enhanced diagnostics)
    private async Task ExecuteNavigateOnlyAsync(SubAction action, string orderId, Dictionary<string, object>? context, CancellationToken ct)
    {
        var activeConfig = GetActiveConfig();
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

        // Track JavaScript console errors that could prevent Knockout bindings
        var jsErrors = new System.Collections.Concurrent.ConcurrentBag<string>();
        page.Console += (_, args) =>
        {
            try
            {
                var type = args.Message.Type.ToString().ToUpperInvariant();
                if (type == "ERROR")
                {
                    var msg = args.Message.Text;
                    jsErrors.Add(msg);
                    _logger.LogWarning("[JS ERROR] {Message}", msg.Length > 500 ? msg.Substring(0, 500) + "..." : msg);
                }
            }
            catch { }
        };

        // Attach response listener EARLY (before any navigation) to avoid missing initial picklist XHR
        var picklistIdForDetectionEarly = (context != null && context.TryGetValue("pickListId", out var earlyCtxId) ? earlyCtxId?.ToString() : orderId) ?? string.Empty;
        var apiDetailsUrlFragmentEarly = "/api/Picklist/GetPickListDetails";
        bool apiDataObservedEarly = false;
        string? interceptedJsonEarly = null;
        var apiDetectionTcsEarly = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Also detect the PicklistDetail.json config file (CRITICAL for Knockout bindings)
        var configFileUrlFragment = "/configs/PicklistDetail.json";
        bool configFileObserved = false;
        var configDetectionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Response += async (_, e) =>
        {
            try
            {
                var resp = e.Response;
                var rUrl = resp.Url;
                var method = resp.Request?.Method?.ToString() ?? string.Empty;

                // Detect GetPickListDetails API
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

                // Detect PicklistDetail.json config file (CRITICAL - controls which Knockout bindings are active)
                if (!configFileObserved && rUrl.Contains(configFileUrlFragment, StringComparison.OrdinalIgnoreCase))
                {
                    if (resp.Status == System.Net.HttpStatusCode.OK)
                    {
                        string configBody = string.Empty;
                        try { configBody = await resp.TextAsync(); } catch { }
                        if (!string.IsNullOrWhiteSpace(configBody))
                        {
                            configFileObserved = true;
                            _logger.LogInformation("(Early) Observed PicklistDetail.json config file - length {Len}", configBody.Length);
                            configDetectionTcs.TrySetResult(true);
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

        // Detect if this is a direct file path (e.g., /Store/CustomForms/file.html) vs SPA hash route
        var isDirectFile = url.Contains(".html", StringComparison.OrdinalIgnoreCase) &&
                          !url.Contains("#") &&
                          (url.Contains("/") || url.Contains("\\"));

        if (isDirectFile)
        {
            // Direct navigation to static HTML file - no SPA shell loading needed
            _logger.LogInformation("Direct file navigation detected (contains .html), navigating directly to {Url}", url);
            try
            {
                await page.GoToAsync(url, new PuppeteerSharp.NavigationOptions
                {
                    WaitUntil = new[] { PuppeteerSharp.WaitUntilNavigation.Load },
                    Timeout = 60000
                });
                _logger.LogInformation("Direct navigation completed successfully");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning("Direct page load timed out ({Message})", ex.Message);
            }

            // Wait for network idle if configured
            var waitMs = action.WaitForNetworkIdleMs ?? 3000;
            if (waitMs > 0)
            {
                _logger.LogDebug("Waiting {Ms}ms for network idle after direct navigation", waitMs);
                await Task.Delay(waitMs, ct);
            }
        }
        else
        {
            // SPA hash navigation - load shell first, then navigate via hash
            _logger.LogDebug("SPA navigation detected, loading application shell before hash navigation");

            // Navigate to base URL first to establish domain context
            var rootUrl = activeConfig.BaseUrl.TrimEnd('/') + "/";
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

            // CRITICAL: Inject auth token and userData into storage IMMEDIATELY after domain is loaded
            // This ensures the token and userData are available when we navigate to the hash route
            try
            {
                var storageToken = _tokenRenewal.GetCurrentToken();
                var currentCookies = _tokenRenewal.GetCurrentCookies();

                if (!string.IsNullOrWhiteSpace(storageToken))
                {
                    await page.EvaluateFunctionAsync("t => { try { localStorage.setItem('token', t); sessionStorage.setItem('token', t); } catch(e) {} }", storageToken);
                    _logger.LogDebug("Injected token into localStorage/sessionStorage after domain load");
                }

                // Also inject userData if available (Angular app needs this)
                if (currentCookies.TryGetValue("userData", out var userData) && !string.IsNullOrWhiteSpace(userData))
                {
                    await page.EvaluateFunctionAsync("u => { try { localStorage.setItem('userData', u); sessionStorage.setItem('userData', u); } catch(e) {} }", userData);
                    _logger.LogDebug("Injected userData into localStorage/sessionStorage after domain load");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to inject auth data into storage (non-fatal)");
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
            _logger.LogDebug("Awaiting BOTH picklist API AND config file (timeout {Ms}ms, id {Id})", detectionTimeoutMs, picklistIdForDetection);
    
            // Wait for BOTH the API data AND the config file (both required for Knockout bindings)
            var bothResourcesTask = Task.WhenAll(apiDetectionTcsEarly.Task, configDetectionTcs.Task);
            var detectionCompleted = await Task.WhenAny(bothResourcesTask, Task.Delay(detectionTimeoutMs));
    
            if (detectionCompleted == bothResourcesTask && bothResourcesTask.IsCompletedSuccessfully)
            {
                _logger.LogInformation("BOTH picklist API data AND config file confirmed - Knockout bindings should work; skipping fallback waits");
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
                        // Store FULL JSON for manual DOM population (don't truncate!)
                        _lastInterceptedPicklistJson = interceptedJsonEarly;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed parsing intercepted picklist JSON (non-fatal)");
                    }
                }
            }
            else
            {
                if (!apiDataObservedEarly && !configFileObserved)
                {
                    _logger.LogWarning("NEITHER picklist API nor config file observed within {Ms}ms; using fallback timing", detectionTimeoutMs);
                }
                else if (!apiDataObservedEarly)
                {
                    _logger.LogWarning("Picklist API not observed within {Ms}ms (config file OK); using fallback timing", detectionTimeoutMs);
                }
                else if (!configFileObserved)
                {
                    _logger.LogWarning("Config file PicklistDetail.json not observed within {Ms}ms (API OK) - Knockout bindings may fail; using fallback timing", detectionTimeoutMs);
                }
    
                _logger.LogDebug("Fallback: waiting 1500ms for hash route rendering");
                await Task.Delay(1500); // Don't pass ct – critical for route detection
    
                var fallbackWaitMs = action.WaitForNetworkIdleMs ?? 3000;
                if (fallbackWaitMs > 0)
                {
                    _logger.LogDebug("Fallback: waiting {Ms}ms for SPA data loading", fallbackWaitMs);
                    await Task.Delay(fallbackWaitMs); // Don't pass ct
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
                        // Fetch with authentication token and WarehouseId from localStorage
                        var manualJson = await page.EvaluateExpressionAsync<string>($@"
                            (async () => {{
                                try {{
                                    const token = localStorage.getItem('token') || sessionStorage.getItem('token');
                                    const userDataStr = localStorage.getItem('userData') || sessionStorage.getItem('userData');
                                    let warehouseId = '1';
    
                                    if (userDataStr) {{
                                        try {{
                                            const userData = JSON.parse(userDataStr);
                                            warehouseId = (userData.defaultWarehouseId || userData.warehouseId || 1).toString();
                                        }} catch(e) {{ }}
                                    }}
    
                                    const resp = await fetch('/api/Picklist/GetPickListDetails?picklistid={picklistIdForDetection}', {{
                                        headers: {{
                                            'Authorization': token ? 'Bearer ' + token : '',
                                            'Content-Type': 'application/json',
                                            'WarehouseId': warehouseId
                                        }}
                                    }});
                                    const text = await resp.text();
                                    return JSON.stringify({{ status: resp.status, statusText: resp.statusText, body: text }});
                                }} catch(e) {{
                                    return JSON.stringify({{ error: e.toString() }});
                                }}
                            }})()
                        ");
                        if (!string.IsNullOrWhiteSpace(manualJson))
                        {
                            // Parse the wrapper JSON that contains status and body
                            try
                            {
                                using var jsonDoc = JsonDocument.Parse(manualJson);
                                var root = jsonDoc.RootElement;
    
                                if (root.TryGetProperty("error", out var errorProp))
                                {
                                    _logger.LogError("Manual fetch JavaScript error: {Error}", errorProp.GetString());
                                }
                                else if (root.TryGetProperty("status", out var statusProp) && root.TryGetProperty("body", out var bodyProp))
                                {
                                    var httpStatus = statusProp.GetInt32();
                                    var statusText = root.TryGetProperty("statusText", out var stProp) ? stProp.GetString() : "";
                                    var responseBody = bodyProp.GetString() ?? "";
    
                                    _logger.LogInformation("Manual fetch HTTP {Status} {StatusText}, body length: {Len}", httpStatus, statusText, responseBody.Length);
    
                                    if (httpStatus >= 200 && httpStatus < 300 && !string.IsNullOrWhiteSpace(responseBody))
                                    {
                                        var lower = responseBody.ToLowerInvariant();
                                        bool looksSuccess = lower.Contains("\"responsecode\":0") || lower.Contains("\"responsetype\":\"success\"");
                                        bool hasKeys = lower.Contains("picklistnumber") || lower.Contains("picklistid") || lower.Contains("\"picklist\"");
    
                                        if (looksSuccess && hasKeys)
                                        {
                                            _logger.LogInformation("Manual fetch obtained valid picklist details");
                                            // Store FULL JSON for manual DOM population (don't truncate!)
                                            _lastInterceptedPicklistJson = responseBody;
    
                                            // Populate the DOM using tf.binder.scatter() - same as the page's GetPickListDetail() function
                                            try
                                            {
                                                _logger.LogInformation("Attempting to populate DOM using tf.binder.scatter()");
                                                // Pass JSON as function parameter to avoid escaping issues
                                                var populateResult = await page.EvaluateFunctionAsync<string>(@"(jsonString) => {
                                                    try {
                                                        const data = JSON.parse(jsonString);
    
                                                        if (data.responseCode == 0 && data.data && data.data.PickList) {
                                                            // Process picklist items (same as GetPickListDetail)
                                                            var TotalQty = 0;
                                                            var TotalCtns = 0;
                                                            var OrderNo = '';
    
                                                            for (var i = 0; i < data.data.PickListItems.length; i++) {
                                                                if (data.data.PickListItems[i].IsCase == 0) {
                                                                    data.data.PickListItems[i].IsCase = 'piece(s)';
                                                                } else {
                                                                    data.data.PickListItems[i].IsCase = 'Case(s)';
                                                                }
                                                                if (data.data.PickListItems[i].TotalCtns == null) {
                                                                    data.data.PickListItems[i].TotalCtns = 1;
                                                                }
                                                                TotalQty += parseFloat(data.data.PickListItems[i].Quantity);
                                                                TotalCtns += parseFloat(data.data.PickListItems[i].TotalCtns);
                                                                if (OrderNo == '') {
                                                                    OrderNo += data.data.PickListItems[i].OrderNumber;
                                                                }
                                                            }
    
                                                            Object.assign(data.data.PickList, { TotalQty: TotalQty });
                                                            Object.assign(data.data.PickList, { TotalCtns: Math.ceil(TotalCtns) });
                                                            Object.assign(data.data.PickList, { OrderNo: OrderNo });
    
                                                            if (data.data.PickList.Priority == null || data.data.PickList.Priority == '') {
                                                                data.data.PickList.Priority = 1;
                                                            }
    
                                                            // Scatter data to DOM using tf.binder
                                                            if (typeof window.tf !== 'undefined' && window.tf.binder && window.tf.binder.scatter) {
                                                                window.tf.binder.scatter(data.data, '.data-scatter');
                                                                window.tf.binder.scatter(data.data.PickList, '.data-data-scatter');
    
                                                                // Generate barcodes if function exists
                                                                if (typeof GenerateOrderNoBarcode === 'function') {
                                                                    GenerateOrderNoBarcode();
                                                                }
    
                                                                return 'Success: Data scattered to DOM';
                                                            } else {
                                                                return 'Warning: tf.binder.scatter not available';
                                                            }
                                                        } else {
                                                            return 'Error: Invalid data structure - responseCode=' + (data.responseCode || 'missing');
                                                        }
                                                    } catch(e) {
                                                        return 'Error: ' + e.toString();
                                                    }
                                                }", responseBody);
                                                _logger.LogInformation("DOM population result: {Result}", populateResult);
                                            }
                                            catch (Exception popEx)
                                            {
                                                _logger.LogWarning(popEx, "Failed to populate DOM with picklist data");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Manual fetch body failed validation; success={Success} hasKeys={HasKeys}", looksSuccess, hasKeys);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Manual fetch returned HTTP {Status}, body may be error message", httpStatus);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Manual fetch response has unexpected structure: {Preview}",
                                        manualJson.Length > 200 ? manualJson.Substring(0, 200) + "..." : manualJson);
                                }
                            }
                            catch (JsonException jex)
                            {
                                _logger.LogError(jex, "Failed to parse manual fetch wrapper JSON");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Manual fetch returned empty response for picklist id {Id}", picklistIdForDetection);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Manual fetch fallback failed (non-fatal)");
                    }
                }
            }
    
            // Knockout.js binding readiness checks (critical for data population)
            try
            {
                _logger.LogDebug("Checking if Knockout.js is available on page");
                var koExists = await page.EvaluateExpressionAsync<bool>("typeof window.ko !== 'undefined' && window.ko !== null");
                if (koExists)
                {
                    _logger.LogInformation("Knockout.js detected on page - waiting for bindings to be applied");
    
                    // Wait for Knockout.js to finish applying bindings (check that binding context exists)
                    if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
                    {
                        var escaped = _pdfConfig.WaitForSelector.Replace("'", "\\'");
                        try
                        {
                            _logger.LogDebug("Waiting for Knockout binding context on selector '{Selector}'", _pdfConfig.WaitForSelector);
                            await page.WaitForFunctionAsync($"() => {{ const el = document.querySelector('{escaped}'); return el && window.ko && window.ko.dataFor && window.ko.dataFor(el) !== undefined; }}",
                                new PuppeteerSharp.WaitForFunctionOptions { Timeout = 10000 });
                            _logger.LogInformation("Knockout binding context confirmed for selector '{Selector}'", _pdfConfig.WaitForSelector);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Knockout binding context wait timed out - bindings may not be applied yet");
                        }
                    }
    
                    // Diagnostic: Check view model state
                    try
                    {
                        var vmDiagnostic = await page.EvaluateFunctionAsync<string>(@"(sel) => {
                            try {
                                const el = document.querySelector(sel);
                                if (!el || !window.ko || !window.ko.dataFor) return 'ko.dataFor not available';
                                const vm = window.ko.dataFor(el);
                                if (!vm) return 'No view model bound';
                                const keys = Object.keys(vm).filter(k => !k.startsWith('_')).slice(0, 10);
                                return 'VM keys: ' + keys.join(', ');
                            } catch(e) {
                                return 'Error: ' + e.message;
                            }
                        }", _pdfConfig.WaitForSelector);
                        _logger.LogInformation("Knockout view model diagnostic: {Diagnostic}", vmDiagnostic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get Knockout view model diagnostic");
                    }
                }
                else
                {
                    _logger.LogWarning("Knockout.js NOT detected on page - data binding may not work as expected");
    
                    // Check what JavaScript frameworks/libraries ARE available
                    try
                    {
                        var availableLibs = await page.EvaluateExpressionAsync<string>(@"
                            (() => {
                                const libs = [];
                                if (typeof jQuery !== 'undefined') libs.push('jQuery v' + jQuery.fn.jquery);
                                if (typeof $ !== 'undefined') libs.push('$ (jQuery or similar)');
                                if (typeof window.ko !== 'undefined') libs.push('Knockout');
                                if (typeof window.tf !== 'undefined') libs.push('tf (SPA framework)');
                                if (typeof Vue !== 'undefined') libs.push('Vue');
                                if (typeof React !== 'undefined') libs.push('React');
                                if (typeof Angular !== 'undefined') libs.push('Angular');
                                if (typeof Backbone !== 'undefined') libs.push('Backbone');
                                return libs.length > 0 ? libs.join(', ') : 'None detected';
                            })()
                        ");
                        _logger.LogInformation("Available JavaScript libraries on page: {Libs}", availableLibs);
                    }
                    catch (Exception libEx)
                    {
                        _logger.LogDebug(libEx, "Failed to detect available JavaScript libraries");
                    }
    
                    // Check if page data is accessible via tf.page or global variables
                    try
                    {
                        var tfDataCheck = await page.EvaluateExpressionAsync<string>(@"
                            (() => {
                                if (typeof window.tf === 'undefined') return 'tf framework not available';
                                if (typeof window.tf.page === 'undefined') return 'tf.page not available';
                                if (typeof window.tf.page.viewModel !== 'undefined') return 'tf.page.viewModel exists (type: ' + typeof window.tf.page.viewModel + ')';
                                if (typeof window.tf.page.data !== 'undefined') return 'tf.page.data exists (type: ' + typeof window.tf.page.data + ')';
                                return 'tf available but no viewModel or data found';
                            })()
                        ");
                        _logger.LogInformation("SPA framework data check: {Check}", tfDataCheck);
    
                        // Try to access the picklist data from tf.page.viewModel if it exists
                        var picklistDataCheck = await page.EvaluateExpressionAsync<string>(@"
                            (() => {
                                try {
                                    if (window.tf && window.tf.page && window.tf.page.viewModel) {
                                        const vm = window.tf.page.viewModel;
                                        const keys = Object.keys(vm).slice(0, 20);
                                        let result = 'ViewModel keys: ' + keys.join(', ');
                                        if (vm.PickListNumber) result += ' | PickListNumber=' + vm.PickListNumber;
                                        if (vm.PicklistNumber) result += ' | PicklistNumber=' + vm.PicklistNumber;
                                        if (vm.picklistNumber) result += ' | picklistNumber=' + vm.picklistNumber;
                                        return result;
                                    }
                                    return 'No viewModel accessible';
                                } catch(e) {
                                    return 'Error accessing viewModel: ' + e.message;
                                }
                            })()
                        ");
                        _logger.LogInformation("ViewModel picklist data check: {Check}", picklistDataCheck);
                    }
                    catch (Exception tfEx)
                    {
                        _logger.LogDebug(tfEx, "Failed to check tf.page data");
                    }
    
                    // DISABLED: Manual DOM population - Let the page render naturally with its own JavaScript
                    // The page's tf framework will populate all fields, including barcodes
                    _logger.LogInformation("Trusting page to render naturally - wait times configured to allow full rendering");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Knockout.js detection failed");
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
    
            // Final page readiness diagnostic - check for critical content elements
            try
            {
                var contentCheck = await page.EvaluateExpressionAsync<string>(@"
                    (() => {
                        const checks = [];
                        // Check for barcodes
                        const svgs = document.querySelectorAll('svg');
                        const canvases = document.querySelectorAll('canvas');
                        const barcodeImgs = document.querySelectorAll('img[src*=""barcode""], img[alt*=""barcode""]');
                        checks.push(`SVG elements: ${svgs.length}`);
                        checks.push(`Canvas elements: ${canvases.length}`);
                        checks.push(`Barcode images: ${barcodeImgs.length}`);
    
                        // Check table rows
                        const tableRows = document.querySelectorAll('table#KortHyvdds tbody tr');
                        checks.push(`Table rows: ${tableRows.length}`);
    
                        // Check if PicklistNumber has content
                        const picklistNum = document.querySelector('#PicklistNumber, [data-bind*=""PickListNumber""]');
                        checks.push(`PicklistNumber populated: ${picklistNum && picklistNum.textContent.trim().length > 0}`);
    
                        return checks.join('; ');
                    })()
                ");
                _logger.LogInformation("Page content readiness check: {Check}", contentCheck);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Content readiness check failed");
            }
    
            // Report JavaScript errors summary
            if (jsErrors.Count > 0)
            {
                _logger.LogWarning("Detected {Count} JavaScript error(s) during navigation - these may prevent data rendering:", jsErrors.Count);
                foreach (var err in jsErrors.Take(5))
                {
                    _logger.LogWarning("  - {Error}", err.Length > 200 ? err.Substring(0, 200) + "..." : err);
                }
            }
            else
            {
                _logger.LogInformation("No JavaScript console errors detected during navigation");
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
        } // End of SPA navigation path

        // Store the live page for subsequent print action (don't dispose it) - common to both paths
        _capturedPage = page;
        _capturedPageContextId = context != null && context.TryGetValue("pickListId", out var ctxPicklistId) ? ctxPicklistId?.ToString() ?? orderId : orderId;
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

        // Force portrait orientation via CSS and hide "Short Items" section
        _logger.LogDebug("Injecting PDF print styles (portrait orientation + hiding Short Items)");
        try
        {
            await _capturedPage.EvaluateExpressionAsync(@"
                // Inject CSS to force portrait mode for PDF printing
                const style = document.createElement('style');
                style.textContent = `
                    @page {
                        size: portrait !important;
                        margin: 0.4in;
                    }
                    @media print {
                        body {
                            width: 8.5in !important;
                            max-width: 8.5in !important;
                        }
                    }
                    .customer-highlight {
                        background-color: #000 !important;
                        color: #fff !important;
                        font-weight: bold !important;
                        font-size: 1.4em !important;
                        padding: 8px 12px !important;
                        display: inline-block !important;
                        margin: 4px 0 !important;
                        border-radius: 4px !important;
                    }
                `;
                document.head.appendChild(style);

                // Find and style CUSTOMER field (label + value)
                let customerStyledCount = 0;
                const allElements = Array.from(document.querySelectorAll('*'));
                allElements.forEach(el => {
                    const text = el.textContent?.trim() || '';

                    // Method 1: Look for element containing 'CUSTOMER:' label combined with value
                    if (text.startsWith('CUSTOMER:') && text.length < 100) {
                        el.classList.add('customer-highlight');
                        customerStyledCount++;
                    }
                    // Method 2: Look for the value separately if it's in a different element (previous sibling check)
                    else if (el.previousElementSibling) {
                        const prevText = el.previousElementSibling.textContent?.trim() || '';
                        if (prevText === 'CUSTOMER:' && text.length > 2 && text.length < 100 && !text.includes(':')) {
                            el.classList.add('customer-highlight');
                            customerStyledCount++;
                        }
                    }
                    // Method 3: Look for parent element that contains CUSTOMER: label
                    else if (el.parentElement) {
                        const parentText = el.parentElement.textContent?.trim() || '';
                        if (parentText.includes('CUSTOMER:') && text.length > 2 && text.length < 100 && !text.includes(':') && text !== 'CUSTOMER:') {
                            // Check if this is the customer value (not another label)
                            const allText = parentText.replace(/\s+/g, ' ');
                            if (allText.match(/CUSTOMER:\s*/ + text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))) {
                                el.classList.add('customer-highlight');
                                customerStyledCount++;
                            }
                        }
                    }
                });

                // Hide any element with heading or title containing 'Short Items' or 'ShortItems'
                const shortItemsElements = [];

                // Method 1: Find by heading text (h1-h6 tags)
                document.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach(heading => {
                    const text = heading.textContent.trim().toLowerCase().replace(/\s+/g, '');
                    if (text === 'shortitems' || text.includes('shortitems')) {
                        // Hide the heading and its next sibling (likely the table)
                        heading.style.display = 'none';
                        if (heading.nextElementSibling) {
                            heading.nextElementSibling.style.display = 'none';
                        }
                        // Also try to hide parent if it's a wrapper
                        const parent = heading.closest('div, section, article');
                        if (parent && parent.textContent.trim().toLowerCase().replace(/\s+/g, '').includes('shortitems')) {
                            parent.style.display = 'none';
                        }
                        shortItemsElements.push(heading);
                    }
                });

                // Method 2: Find tables that have empty body (Short Items table has headers but no data)
                document.querySelectorAll('table').forEach(table => {
                    const headers = Array.from(table.querySelectorAll('th, thead td')).map(h => h.textContent.trim().toLowerCase());
                    const hasShortItemsHeaders = headers.some(h => h.includes('order') && h.includes('sku') && h.includes('product'));
                    const tbody = table.querySelector('tbody');
                    const hasNoDataRows = !tbody || tbody.querySelectorAll('tr').length === 0 ||
                                          (tbody.querySelectorAll('tr').length === 1 && !tbody.textContent.trim());

                    // If it looks like the Short Items table (has expected headers and no data)
                    if (hasShortItemsHeaders && hasNoDataRows) {
                        table.style.display = 'none';
                        shortItemsElements.push(table);

                        // Also hide any preceding heading
                        let prev = table.previousElementSibling;
                        while (prev && prev.tagName && prev.tagName.match(/^H[1-6]$/)) {
                            const text = prev.textContent.trim().toLowerCase().replace(/\s+/g, '');
                            if (text.includes('shortitems')) {
                                prev.style.display = 'none';
                            }
                            break;
                        }
                    }
                });

                console.log('PDF print styles injected. Hidden Short Items elements: ' + shortItemsElements.length + ', Styled customer elements: ' + customerStyledCount);
            ");
            _logger.LogInformation("Portrait orientation CSS + Short Items hiding applied");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject PDF print styles (continuing with PDF generation)");
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
        var printerName = GetActiveConfig().PrinterName;
        await _printer.PrintPdfBytesAsync(pdfBytes, jobName, printerName, ct);
        _logger.LogInformation("PDF sent to printer {Printer}: {JobName}", printerName ?? "default", jobName);

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

    private async Task ExecuteSaveCapturedHtmlAsync(SubAction action, string orderId, CancellationToken ct)
    {
        if (_capturedPage == null)
        {
            _logger.LogWarning("No captured page available to save. Skipping save action.");
            return;
        }

        var jobName = string.IsNullOrWhiteSpace(_capturedPageContextId)
            ? action.Name
            : $"{action.Name}-{_capturedPageContextId}";

        _logger.LogInformation("Saving PDF from live browser page. Job: {JobName}", jobName);

        // Re-verify selector readiness if configured (defensive check; page may have updated since capture)
        if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
        {
            try
            {
                bool selectorPresent = await _capturedPage.EvaluateFunctionAsync<bool>("sel => !!document.querySelector(sel)", _pdfConfig.WaitForSelector);
                _logger.LogDebug("Save diagnostic: Selector '{Selector}' present={Present}", _pdfConfig.WaitForSelector, selectorPresent);
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
                _logger.LogWarning(ex, "Selector verification during save failed; continuing");
            }
        }

        // Optional diagnostic screenshot before PDF
        if (_pdfConfig.CaptureDiagnosticScreenshot)
        {
            try
            {
                var shotsDir = DataPaths.EnsureDir("screenshots");
                var shotPath = Path.Combine(shotsDir, $"presave_{DateTime.Now:yyyyMMdd_HHmmssfff}_{orderId}.png");
                await _capturedPage.ScreenshotAsync(shotPath, new PuppeteerSharp.ScreenshotOptions { FullPage = true });
                _logger.LogInformation("Captured pre-save diagnostic screenshot: {Path}", shotPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture pre-save diagnostic screenshot");
            }
        }

        // Force portrait orientation via CSS and hide "Short Items" section
        _logger.LogDebug("Injecting PDF save styles (portrait orientation + hiding Short Items)");
        try
        {
            await _capturedPage.EvaluateExpressionAsync(@"
                // Inject CSS to force portrait mode for PDF printing
                const style = document.createElement('style');
                style.textContent = `
                    @page {
                        size: portrait;
                        margin: 0.5in;
                    }
                    @media print {
                        body {
                            -webkit-print-color-adjust: exact;
                            print-color-adjust: exact;
                        }
                    }
                    .customer-highlight {
                        background-color: #000 !important;
                        color: #fff !important;
                        font-weight: bold !important;
                        font-size: 1.4em !important;
                        padding: 8px 12px !important;
                        display: inline-block !important;
                        margin: 4px 0 !important;
                        border-radius: 4px !important;
                        border: 2px solid #000 !important;
                    }
                `;
                document.head.appendChild(style);

                // Find and style CUSTOMER field (label + value)
                let customerStyledCount = 0;

                // Method 1: Target the exact structure - span with data-bind='Customers'
                const customerValueSpan = document.querySelector('span[data-bind=""Customers""]');
                if (customerValueSpan) {
                    customerValueSpan.classList.add('customer-highlight');
                    customerStyledCount++;
                    console.log('Applied customer-highlight to span[data-bind=""Customers""]:', customerValueSpan.textContent);
                }

                // Method 2: Find label containing 'Customer' text and get next sibling span
                const allLabels = Array.from(document.querySelectorAll('label'));
                allLabels.forEach(label => {
                    const labelText = label.textContent?.trim() || '';
                    if (labelText.toLowerCase().includes('customer') && labelText.includes(':')) {
                        const nextSibling = label.nextElementSibling;
                        if (nextSibling && nextSibling.tagName === 'SPAN' && nextSibling.classList.contains('form-control')) {
                            nextSibling.classList.add('customer-highlight');
                            customerStyledCount++;
                            console.log('Applied customer-highlight to label sibling span:', nextSibling.textContent);
                        }
                    }
                });

                // Method 3: Fallback - search all elements for the patterns (case-insensitive)
                const allElements = Array.from(document.querySelectorAll('*'));
                allElements.forEach(el => {
                    const text = el.textContent?.trim() || '';

                    // Look for element containing 'CUSTOMER:' or 'Customer:' label combined with value
                    if ((text.toUpperCase().startsWith('CUSTOMER:') || text.match(/^Customer\s*:/i)) && text.length < 100) {
                        el.classList.add('customer-highlight');
                        customerStyledCount++;
                    }
                    // Look for the value separately if it's in a different element (previous sibling check)
                    else if (el.previousElementSibling) {
                        const prevText = el.previousElementSibling.textContent?.trim() || '';
                        if ((prevText.toUpperCase() === 'CUSTOMER:' || prevText.match(/^Customer\s*:$/i)) && text.length > 2 && text.length < 100 && !text.includes(':')) {
                            el.classList.add('customer-highlight');
                            customerStyledCount++;
                        }
                    }
                    // Look for parent element that contains CUSTOMER: label
                    else if (el.parentElement) {
                        const parentText = el.parentElement.textContent?.trim() || '';
                        if ((parentText.toUpperCase().includes('CUSTOMER:') || parentText.match(/Customer\s*:/i)) && text.length > 2 && text.length < 100 && !text.includes(':') && !text.match(/^Customer\s*:?$/i)) {
                            // Check if this is the customer value (not another label)
                            const allText = parentText.replace(/\s+/g, ' ');
                            const escapedText = text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                            if (allText.match(new RegExp('Customer\\s*:\\s*' + escapedText, 'i'))) {
                                el.classList.add('customer-highlight');
                                customerStyledCount++;
                            }
                        }
                    }
                });

                // Hide any element with heading or title containing 'Short Items' or 'ShortItems'
                const shortItemsElements = [];

                // Method 1: Find by heading text (h1-h6 tags)
                document.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach(heading => {
                    const text = heading.textContent.trim().toLowerCase().replace(/\s+/g, '');
                    if (text === 'shortitems' || text.includes('shortitems')) {
                        // Hide the heading and its next sibling (likely the table)
                        heading.style.display = 'none';
                        if (heading.nextElementSibling) {
                            heading.nextElementSibling.style.display = 'none';
                        }
                        // Also try to hide parent if it's a wrapper
                        const parent = heading.closest('div, section, article');
                        if (parent && parent.textContent.trim().toLowerCase().replace(/\s+/g, '').includes('shortitems')) {
                            parent.style.display = 'none';
                        }
                        shortItemsElements.push(heading);
                    }
                });

                // Method 2: Find tables that have empty body (Short Items table has headers but no data)
                document.querySelectorAll('table').forEach(table => {
                    const headers = Array.from(table.querySelectorAll('th, thead td')).map(h => h.textContent.trim().toLowerCase());
                    const hasShortItemsHeaders = headers.some(h => h.includes('order') && h.includes('sku') && h.includes('product'));
                    const tbody = table.querySelector('tbody');
                    const hasNoDataRows = !tbody || tbody.querySelectorAll('tr').length === 0 ||
                                          (tbody.querySelectorAll('tr').length === 1 && !tbody.textContent.trim());

                    // If it looks like the Short Items table (has expected headers and no data)
                    if (hasShortItemsHeaders && hasNoDataRows) {
                        table.style.display = 'none';
                        shortItemsElements.push(table);

                        // Also hide any preceding heading
                        let prev = table.previousElementSibling;
                        while (prev && prev.tagName && prev.tagName.match(/^H[1-6]$/)) {
                            const text = prev.textContent.trim().toLowerCase().replace(/\s+/g, '');
                            if (text.includes('shortitems')) {
                                prev.style.display = 'none';
                            }
                            break;
                        }
                    }
                });

                console.log('PDF save styles injected. Hidden Short Items elements: ' + shortItemsElements.length + ', Styled customer elements: ' + customerStyledCount);
            ");
            _logger.LogInformation("Portrait orientation CSS + Short Items hiding applied");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject PDF save styles (continuing with PDF generation)");
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

        // Store the file path for potential printing later
        _lastSavedPdfPath = filePath;

        _logger.LogInformation("Successfully saved page to PDF");

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

    private async Task ExecutePrintSavedPdfAsync(SubAction action, string orderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_lastSavedPdfPath))
        {
            _logger.LogWarning("No saved PDF path available. Skipping print action.");
            return;
        }

        if (!File.Exists(_lastSavedPdfPath))
        {
            _logger.LogWarning("Saved PDF file not found at {Path}. Skipping print action.", _lastSavedPdfPath);
            _lastSavedPdfPath = null;
            return;
        }

        var jobName = string.IsNullOrWhiteSpace(_capturedPageContextId)
            ? action.Name
            : $"{action.Name}-{_capturedPageContextId}";

        _logger.LogInformation("Printing saved PDF. Job: {JobName}, Path: {Path}", jobName, _lastSavedPdfPath);

        // Read the PDF file
        var pdfBytes = await File.ReadAllBytesAsync(_lastSavedPdfPath, ct);

        // Send to printer if configured (MUST succeed - exception will trigger retry)
        var printerName = GetActiveConfig().PrinterName;
        await _printer.PrintPdfBytesAsync(pdfBytes, jobName, printerName, ct);
        _logger.LogInformation("PDF sent to printer {Printer}: {JobName}", printerName ?? "default", jobName);

        _logger.LogInformation("Successfully printed saved PDF");

        // Clear the saved path reference
        _lastSavedPdfPath = null;
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

        // When explicit Width/Height are set, Landscape property is ignored by PuppeteerSharp
        // Only use explicit dimensions if Landscape=true, otherwise use standard Letter format
        if (_pdfConfig.Landscape && _pdfConfig.PageWidthInches.HasValue && _pdfConfig.PageHeightInches.HasValue)
        {
            // For landscape with custom dimensions, set width (larger) and height (smaller)
            pdfOptions.Width = $"{_pdfConfig.PageHeightInches.Value}in";  // Swap for landscape
            pdfOptions.Height = $"{_pdfConfig.PageWidthInches.Value}in";  // Swap for landscape
        }
        else if (!_pdfConfig.Landscape)
        {
            // For portrait, use standard Letter format instead of explicit dimensions
            // This ensures the Landscape=false setting is respected
            pdfOptions.Format = PuppeteerSharp.Media.PaperFormat.Letter;
        }
        else if (_pdfConfig.PageWidthInches.HasValue && _pdfConfig.PageHeightInches.HasValue)
        {
            // Fallback: custom dimensions without landscape
            pdfOptions.Width = $"{_pdfConfig.PageWidthInches.Value}in";
            pdfOptions.Height = $"{_pdfConfig.PageHeightInches.Value}in";
        }

        _logger.LogInformation("Generating PDF (Landscape={Landscape}, Format={Format}, W={W}, H={H}, Bg={Bg})",
            pdfOptions.Landscape,
            pdfOptions.Format?.ToString() ?? "Custom",
            pdfOptions.Width ?? "Auto",
            pdfOptions.Height ?? "Auto",
            pdfOptions.PrintBackground);
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

    private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => string.Empty,
                    _ => property.Value.GetRawText()
                };
            }
        }

        return dict;
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
