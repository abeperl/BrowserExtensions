using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface ISubActionExecutor
{
    Task ExecuteActionsForOrderAsync(string orderId, JsonElement orderData, CancellationToken ct = default);
    Task ExecuteBatchCreatePicklistAsync(SubAction action, IEnumerable<string> orderIds, CancellationToken ct = default);
}

public class SubActionExecutor : ISubActionExecutor
{
    private readonly ILogger<SubActionExecutor> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApiConfig _config;
    private readonly PdfPrintService _printer;

    public SubActionExecutor(
        ILogger<SubActionExecutor> logger,
        HttpClient httpClient,
        IOptions<ApiConfig> apiConfig,
        PdfPrintService printer)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = apiConfig.Value;
        _printer = printer;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.Timeout = TimeSpan.FromSeconds(200);

        // Add Bearer token
        if (!string.IsNullOrEmpty(_config.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.BearerToken);
        }

        // Add standard headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("WarehouseId", _config.WarehouseId.ToString());
        _httpClient.DefaultRequestHeaders.Add("Origin", _config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_config.BaseUrl}/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

        // Add cookies
        if (_config.Cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", _config.Cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
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
                QuickShip = action.QuickShip
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

        HttpResponseMessage? lastResponse = null;
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            lastResponse?.Dispose();
            using var request = requestFactory();

            try
            {
                var response = await _httpClient.SendAsync(request, ct);
                if ((int)response.StatusCode < 200)
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
        response.EnsureSuccessStatusCode();

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
