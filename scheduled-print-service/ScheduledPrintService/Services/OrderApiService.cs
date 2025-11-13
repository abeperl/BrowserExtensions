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

    public OrderApiService(
        ILogger<OrderApiService> logger,
        HttpClient httpClient,
        IOptions<ApiConfig> apiConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = apiConfig.Value;

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

        // Add custom headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("WarehouseId", _config.WarehouseId.ToString());
        _httpClient.DefaultRequestHeaders.Add("Origin", _config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_config.BaseUrl}/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

        // Add cookies if configured
        if (_config.Cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", _config.Cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
    }

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
            using var req = requestFactory();
            try
            {
                var resp = await _httpClient.SendAsync(req, ct);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                {
                    return resp;
                }
                if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)resp.StatusCode >= 500)
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
            // Build the request payload
            var requestPayload = BuildRequestPayload();
            var jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });

            _logger.LogInformation("Calling GetOrdersList API...");
            _logger.LogDebug("Request payload: {Payload}", jsonContent);

            Func<HttpRequestMessage> factory = () =>
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                return new HttpRequestMessage(HttpMethod.Post, "/api/order/GetOrdersList") { Content = content };
            };

            var response = await SendWithRetryAsync(factory, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("API call successful. Response length: {Length}", responseBody.Length);
            _logger.LogDebug("Response body: {Body}", responseBody);

            // Parse JSON and extract order records
            var orders = ParseOrderRecords(responseBody);
            _logger.LogInformation("Extracted {Count} order records", orders.Count);

            return orders;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling GetOrdersList API");
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