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
        _logger.LogInformation("Executing {Count} sub-actions for order {OrderId}", _config.SubActions.Count, orderId);

        for (int i = 0; i < _config.SubActions.Count; i++)
        {
            var action = _config.SubActions[i];
            var actionNum = i + 1;

            try
            {
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

        _logger.LogInformation("All sub-actions completed for order {OrderId}", orderId);
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

            default:
                _logger.LogWarning("Unknown action type: {Type}", action.Type);
                break;
        }
    }

    private async Task ExecuteCallApiAsync(SubAction action, string orderId, CancellationToken ct)
    {
        var endpoint = ReplaceTokens(action.Endpoint, orderId);
        _logger.LogDebug("Calling API: {Method} {Endpoint}", action.Method, endpoint);

        var request = new HttpRequestMessage(new HttpMethod(action.Method), endpoint);

        // Add custom headers
        foreach (var header in action.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, ReplaceTokens(header.Value, orderId));
        }

        // Add request body if provided
        if (!string.IsNullOrEmpty(action.RequestBody))
        {
            var body = ReplaceTokens(action.RequestBody, orderId);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            _logger.LogDebug("Request body: {Body}", body);
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Response: {Response}", responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody);
    }

    private async Task ExecuteGetHtmlAndPrintAsync(SubAction action, string orderId, CancellationToken ct)
    {
        var endpoint = ReplaceTokens(action.Endpoint, orderId);
        _logger.LogDebug("Fetching HTML from: {Method} {Endpoint}", action.Method, endpoint);

        var request = new HttpRequestMessage(new HttpMethod(action.Method), endpoint);

        // Add custom headers
        foreach (var header in action.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, ReplaceTokens(header.Value, orderId));
        }

        // Add request body if provided
        if (!string.IsNullOrEmpty(action.RequestBody))
        {
            var body = ReplaceTokens(action.RequestBody, orderId);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);

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
