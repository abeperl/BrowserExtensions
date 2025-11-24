using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface IOrderApiService
{
    Task<List<OrderRecord>> GetOrdersListAsync(CancellationToken ct = default);
}

public class OrderRecord
{
    public string Id { get; set; } = string.Empty;
    public JsonElement RawData { get; set; }
}

public class OrderApiService : IOrderApiService
{
    private readonly ILogger<OrderApiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApiConfig _config;
    private readonly ITokenRenewalService _tokenRenewal;

    public OrderApiService(
        ILogger<OrderApiService> logger,
        HttpClient httpClient,
        IOptions<ApiConfig> apiConfig,
        ITokenRenewalService tokenRenewal)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = apiConfig.Value;
        _tokenRenewal = tokenRenewal;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.Timeout = TimeSpan.FromSeconds(200);

        UpdateHttpClientAuth();

        // Add custom headers
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
            using var req = requestFactory();
            try
            {
                var resp = await _httpClient.SendAsync(req, ct);
                
                // Check for 401 Unauthorized - token may have expired
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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
                            
                            // Don't count this as an attempt, retry immediately with new token
                            resp.Dispose();
                            continue;
                        }
                        else
                        {
                            _logger.LogCritical("Failed to renew token after 401 Unauthorized - service will stop");
                            throw new TokenRenewalException("Unable to renew authentication token after receiving 401 Unauthorized");
                        }
                    }
                    
                    lastResponse = resp;
                }
                else if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                {
                    return resp;
                }
                else if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)resp.StatusCode >= 500)
                {
                    lastResponse = resp;
                }
                else
                {
                    return resp;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error on attempt {Attempt}/{Max}", attempt, attempts);
            }

            if (attempt == attempts) break;
            var jitter = rng.Next(0, baseDelay);
            var delay = Math.Min(maxDelay, (int)(baseDelay * Math.Pow(2, attempt - 1)) + jitter);
            _logger.LogWarning("Retrying in {Delay}ms (attempt {Next}/{Max})", delay, attempt + 1, attempts);
            try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { break; }
        }

        return lastResponse ?? new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Request failed after retries.")
        };
    }

    public async Task<List<OrderRecord>> GetOrdersListAsync(CancellationToken ct = default)
    {
        try
        {
            // Determine primary endpoint and method from configuration
            var endpoint = string.IsNullOrWhiteSpace(_config.PrimaryEndpoint) ? "/api/order/GetOrdersList" : _config.PrimaryEndpoint;
            var methodString = string.IsNullOrWhiteSpace(_config.PrimaryHttpMethod) ? "POST" : _config.PrimaryHttpMethod;
            var httpMethod = new HttpMethod(methodString.ToUpperInvariant());

            // Build request payload: prefer raw PrimaryPayload if supplied; otherwise serialize DefaultRequest
            string? jsonContent = null;
            if (!string.IsNullOrWhiteSpace(_config.PrimaryPayload) && _config.PrimaryPayload != "{}")
            {
                jsonContent = _config.PrimaryPayload;
            }
            else
            {
                var requestPayload = BuildRequestPayload();
                jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }

            _logger.LogInformation("Calling primary endpoint {Endpoint} ({Method})...", endpoint, httpMethod);
            _logger.LogDebug("Request payload: {Payload}", jsonContent);

            Func<HttpRequestMessage> factory = () =>
            {
                var req = new HttpRequestMessage(httpMethod, endpoint);
                if (httpMethod != HttpMethod.Get && jsonContent is not null)
                {
                    req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }
                return req;
            };

            var response = await SendWithRetryAsync(factory, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("API call successful. Response length: {Length}", responseBody.Length);
            _logger.LogDebug("Response body: {Body}", responseBody);

            // Log first 500 chars of response for debugging
            var preview = responseBody.Length > 500 ? responseBody.Substring(0, 500) : responseBody;
            _logger.LogInformation("Response preview: {Preview}...", preview);

            // Parse JSON and extract order records
            var orders = ParseOrderRecords(responseBody);
            _logger.LogInformation("Extracted {Count} order records", orders.Count);
            
            if (orders.Count > 0)
            {
                _logger.LogInformation("First record ID: {Id}", orders[0].Id);
            }

            return orders;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling primary endpoint {Endpoint}", _config.PrimaryEndpoint);
            throw;
        }
    }

    private object BuildRequestPayload()
    {
        var req = _config.DefaultRequest;
        var dateFrom = string.IsNullOrWhiteSpace(req.DateFrom) ? "2018-01-07" : req.DateFrom;
        var dateTo = string.IsNullOrWhiteSpace(req.DateTo) ? DateTime.Today.ToString("yyyy-MM-dd") : req.DateTo;

        // Use configured columns if provided, else fallback to a minimal column set
        object columnsObject;
        if (_config.OrdersListColumns.HasValue)
        {
            columnsObject = _config.OrdersListColumns.Value;
        }
        else
        {
            columnsObject = new object[]
            {
                new { data = 0, name = "", searchable = true, orderable = false, search = new { value = "", regex = false } },
                new { data = 1, name = "", searchable = true, orderable = false, search = new { value = "", regex = false } }
            };
        }

        return new
        {
            draw = req.Draw,
            columns = columnsObject,
            order = new[] { new { column = 5, dir = "asc" } },
            start = req.Start,
            length = req.Length,
            search = new { value = "", regex = false },
            clientID = req.ClientID,
            Customer = req.Customer,
            statusName = req.StatusName,
            ChannelId = req.ChannelId,
            CreatedBy = req.CreatedBy,
            PaymentMethod = string.IsNullOrEmpty(req.PaymentMethodString) ? req.PaymentMethod.ToString() : req.PaymentMethodString,
            dateFrom = dateFrom,
            dateTo = dateTo,
            isdropship = req.IsDropship,
            carrierId = req.CarrierId,
            pickupDate = req.PickupDate,
            NotSchedule = req.NotSchedule,
            isQuickOrder = req.IsQuickOrder,
            clientorderstatusid = req.ClientOrderStatusId.ToString(),
            clientorderstatusdetailid = req.ClientOrderStatusDetailId.ToString(),
            orderTypeFulfillment = req.OrderTypeFulfillment,
            CutOffOrders = req.CutOffOrders,
            BackOrders = req.BackOrders,
            IsPersonalized = req.IsPersonalized,
            InStock = req.InStock
        };
    }

    private List<OrderRecord> ParseOrderRecords(string jsonResponse)
    {
        var records = new List<OrderRecord>();

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Support both shapes:
            // 1) { "data": [ ... ] }
            // 2) { "data": { "data": [ ... ] } }
            JsonElement dataElement;
            if (root.TryGetProperty("data", out var topData))
            {
                if (topData.ValueKind == JsonValueKind.Array)
                {
                    dataElement = topData;
                }
                else if (topData.ValueKind == JsonValueKind.Object && topData.TryGetProperty("data", out var nestedData) && nestedData.ValueKind == JsonValueKind.Array)
                {
                    dataElement = nestedData;
                }
                else
                {
                    _logger.LogWarning("Response 'data' property found but not an array. Kind={Kind}", topData.ValueKind);
                    return records;
                }
            }
            else
            {
                _logger.LogWarning("Response does not contain 'data' property at root. Sample: {Sample}",
                    jsonResponse.Length > 300 ? jsonResponse.Substring(0, 300) + "..." : jsonResponse);
                return records;
            }

            foreach (var item in dataElement.EnumerateArray())
            {
                try
                {
                    // Extract ID using configured JSON path
                    var id = ExtractIdFromJsonPath(item, _config.IdJsonPath);

                    if (!string.IsNullOrEmpty(id))
                    {
                        records.Add(new OrderRecord
                        {
                            Id = id,
                            RawData = item.Clone()
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Could not extract ID from record using path: {Path}", _config.IdJsonPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse individual record");
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response");
        }

        return records;
    }

    private string ExtractIdFromJsonPath(JsonElement element, string jsonPath)
    {
        try
        {
            // Simple JSON path parser (supports array indices like "[0]" or property names)
            if (jsonPath.StartsWith("[") && jsonPath.EndsWith("]"))
            {
                // Array index
                var indexStr = jsonPath.Trim('[', ']');
                if (int.TryParse(indexStr, out var index) && element.ValueKind == JsonValueKind.Array)
                {
                    var array = element.EnumerateArray().ToList();
                    if (index < array.Count)
                    {
                        return array[index].ToString();
                    }
                }
            }
            else
            {
                // Property name
                if (element.TryGetProperty(jsonPath, out var propElement))
                {
                    return propElement.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting ID from path: {Path}", jsonPath);
        }

        return string.Empty;
    }
}