using System.Net;
using System.Text.Json;
using System.Text;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.Mobile.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly INetworkService _networkService;
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiService> logger,
        IAuthenticationService authService,
        INetworkService networkService,
        IServiceScopeFactory scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _authService = authService;
        _networkService = networkService;
        _scopeFactory = scopeFactory;
    }

    public async Task<ApiResponse<T>> GetAsync<T>(int pageId, CancellationToken cancellationToken = default)
    {
        return await ExecutePageRequestAsync<T>(pageId, HttpMethod.Get, cancellationToken);
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default)
    {
        if (pageId.HasValue)
        {
            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId.Value);
            headers = MergeHeaders(headers, authHeaders);
        }
        return await ExecuteRequestAsync<T>(HttpMethod.Get, url, null, headers, cancellationToken);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default)
    {
        if (pageId.HasValue)
        {
            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId.Value);
            headers = MergeHeaders(headers, authHeaders);
        }
        return await ExecuteRequestAsync<T>(HttpMethod.Post, url, data, headers, cancellationToken);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default)
    {
        if (pageId.HasValue)
        {
            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId.Value);
            headers = MergeHeaders(headers, authHeaders);
        }
        return await ExecuteRequestAsync<T>(HttpMethod.Put, url, data, headers, cancellationToken);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default)
    {
        if (pageId.HasValue)
        {
            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId.Value);
            headers = MergeHeaders(headers, authHeaders);
        }
        return await ExecuteRequestAsync<T>(HttpMethod.Delete, url, null, headers, cancellationToken);
    }

    public async Task<ApiResponse<T>> GetRawAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteRequestAsync<T>(HttpMethod.Get, url, null, headers, cancellationToken);
    }

    public async Task<ApiResponse<T>> PostRawAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteRequestAsync<T>(HttpMethod.Post, url, data, headers, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check network connectivity first
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("No network connection available");
                return false;
            }

            using var client = _httpClientFactory.CreateClient("DataFlowApi");
            var request = new HttpRequestMessage(HttpMethod.Head, url); // Use HEAD for lighter test

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await client.SendAsync(request, cancellationToken);
            _logger.LogInformation("Connection test for {Url}: {StatusCode}", url, response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for URL: {Url}", url);
            return false;
        }
    }

    public async Task<bool> TestPageConnectionAsync(int pageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var authConfig = await _authService.GetAuthConfigAsync(pageId);
            if (authConfig == null)
            {
                _logger.LogWarning("No authentication config found for page: {PageId}", pageId);
                return false;
            }

            // Get the page to test its endpoint
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataFlowDbContext>();
            var page = await context.Pages.FindAsync(new object?[] { pageId }, cancellationToken);

            if (page == null)
            {
                _logger.LogWarning("Page not found: {PageId}", pageId);
                return false;
            }

            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId);
            return await TestConnectionAsync(page.ApiEndpoint, authHeaders, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Page connection test failed for page: {PageId}", pageId);
            return false;
        }
    }

    public async Task<ApiResponse<object>> ExecutePageDataRequestAsync(int pageId, CancellationToken cancellationToken = default)
    {
        return await ExecutePageRequestAsync<object>(pageId, HttpMethod.Get, cancellationToken);
    }

    private async Task<ApiResponse<T>> ExecutePageRequestAsync<T>(int pageId, HttpMethod method, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check network connectivity first
            if (!_networkService.IsConnected)
            {
                _logger.LogWarning("No network connection available for page: {PageId}", pageId);
                return ApiResponse<T>.Error("No network connection available", System.Net.HttpStatusCode.ServiceUnavailable);
            }

            // Get page configuration
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataFlowDbContext>();
            var page = await context.Pages.FindAsync(new object?[] { pageId }, cancellationToken);

            if (page == null)
            {
                _logger.LogError("Page not found: {PageId}", pageId);
                return ApiResponse<T>.Error("Page not found", System.Net.HttpStatusCode.NotFound);
            }

            // Get authentication headers
            var authHeaders = await _authService.GetAuthenticationHeadersAsync(pageId);

            // Parse additional headers from page config
            var pageHeaders = ParseHeaders(page.RequestHeaders);
            var allHeaders = MergeHeaders(pageHeaders, authHeaders);

            _logger.LogInformation("Executing API request for page {PageId}: {Method} {Url}", pageId, method, page.ApiEndpoint);

            return await ExecuteRequestAsync<T>(method, page.ApiEndpoint, null, allHeaders, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing page request for page: {PageId}", pageId);
            return ApiResponse<T>.Error($"Page request failed: {ex.Message}");
        }
    }

    private async Task<ApiResponse<T>> ExecuteRequestAsync<T>(HttpMethod method, string url, object? data, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            // Check network connectivity
            if (!_networkService.IsConnected)
            {
                return ApiResponse<T>.Error("No network connection available", System.Net.HttpStatusCode.ServiceUnavailable);
            }

            using var client = _httpClientFactory.CreateClient("DataFlowApi");
            var request = new HttpRequestMessage(method, url);

            // Add request ID for tracking
            request.Headers.Add("X-Request-ID", requestId);

            if (data != null)
            {
                var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Request {RequestId} body: {Body}", requestId,
                        jsonData?.Length > 2048 ? jsonData.Substring(0, 2048) + "..." : jsonData);
                }
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    try
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add header {HeaderKey}: {HeaderValue}", header.Key, header.Value);
                    }
                }
            }

            _logger.LogInformation("Sending {Method} request to {Url} (ID: {RequestId})", method, url, requestId);

            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Response {RequestId}: {StatusCode} in {ElapsedMs}ms",
                requestId, response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (!string.IsNullOrEmpty(content))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Response {RequestId} body: {Body}", requestId,
                        content?.Length > 2048 ? content.Substring(0, 2048) + "..." : content);
                }
            }

            var responseHeaders = response.Headers.ToDictionary(
                h => h.Key,
                h => string.Join(", ", h.Value)
            );

            if (response.IsSuccessStatusCode)
            {
                T? result = default;

                if (!string.IsNullOrEmpty(content))
                {
                    try
                    {
                        // Handle different content types
                        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";

                        if (contentType.Contains("json"))
                        {
                            result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        }
                        else if (typeof(T) == typeof(string))
                        {
                            result = (T)(object)content;
                        }
                        else
                        {
                            // Try to deserialize as JSON anyway
                            result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize response content as {Type}", typeof(T).Name);

                        // If T is object or dynamic, return the raw content
                        if (typeof(T) == typeof(object))
                        {
                            result = (T)(object)content;
                        }
                        else
                        {
                            return ApiResponse<T>.Error($"Failed to parse response: {ex.Message}", response.StatusCode);
                        }
                    }
                }

                return new ApiResponse<T>
                {
                    IsSuccess = true,
                    Data = result,
                    StatusCode = response.StatusCode,
                    Headers = responseHeaders,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                var errorMessage = $"API request failed: {response.StatusCode}";
                if (!string.IsNullOrEmpty(content))
                {
                    errorMessage += $" - {content}";
                }

                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    StatusCode = response.StatusCode,
                    Headers = responseHeaders,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP request failed for {Method} {Url} (ID: {RequestId})", method, url, requestId);
            return ApiResponse<T>.Error($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            var message = ex.InnerException is TimeoutException ? "Request timeout" : "Request was cancelled";
            _logger.LogError(ex, "{Message} for {Method} {Url} (ID: {RequestId})", message, method, url, requestId);
            return ApiResponse<T>.Error(message, System.Net.HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error for {Method} {Url} (ID: {RequestId})", method, url, requestId);
            return ApiResponse<T>.Error($"Request failed: {ex.Message}");
        }
    }

    private Dictionary<string, string> MergeHeaders(Dictionary<string, string>? headers1, Dictionary<string, string>? headers2)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (headers1 != null)
        {
            foreach (var header in headers1)
            {
                result[header.Key] = header.Value;
            }
        }

        if (headers2 != null)
        {
            foreach (var header in headers2)
            {
                result[header.Key] = header.Value; // headers2 takes precedence
            }
        }

        return result;
    }

    private Dictionary<string, string> ParseHeaders(string? headersJson)
    {
        if (string.IsNullOrEmpty(headersJson))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse headers JSON: {HeadersJson}", headersJson);
            return new Dictionary<string, string>();
        }
    }

    public async Task<ApiResponse<object>> GetDataAsync(DataPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(page.ApiUrl))
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    ErrorMessage = "Page does not have an API URL configured",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            return await GetAsync<object>(page.ApiUrl, null, page.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data for page {PageId}", page.Id);
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorMessage = $"Error getting data: {ex.Message}",
                StatusCode = HttpStatusCode.InternalServerError
            };
        }
    }
}