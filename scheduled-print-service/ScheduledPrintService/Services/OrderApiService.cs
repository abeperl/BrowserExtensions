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

    public async Task<List<OrderRecord>> GetOrdersListAsync(CancellationToken ct = default)
    {
        try
        {
            // Build the request payload
            var requestPayload = BuildRequestPayload();
            var jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Calling GetOrdersList API...");
            _logger.LogDebug("Request payload: {Payload}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/order/GetOrdersList", content, ct);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("API call successful. Response length: {Length}", responseBody.Length);

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
        return new
        {
            draw = req.Draw,
            columns = new object[]
            {
                new { data = 0, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = 1, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = new[] { 14 }, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = new[] { 4 }, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = new[] { 3 }, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = new[] { 13 }, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = 6, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } },
                new { data = 7, name = "", searchable = true, orderable = true, search = new { value = "", regex = false } }
            },
            order = new[] { new { column = 5, dir = "asc" } },
            start = req.Start,
            length = req.Length,
            search = new { value = "", regex = false },
            clientID = req.ClientID,
            statusName = req.StatusName,
            ChannelId = req.ChannelId,
            CreatedBy = req.CreatedBy,
            PaymentMethod = req.PaymentMethod,
            dateFrom = req.DateFrom,
            dateTo = req.DateTo,
            isdropship = req.IsDropship,
            carrierId = req.CarrierId,
            pickupDate = req.PickupDate,
            NotSchedule = req.NotSchedule,
            isQuickOrder = req.IsQuickOrder,
            BackOrders = req.BackOrders,
            IsPersonalized = req.IsPersonalized,
            clientorderstatusid = req.ClientOrderStatusId,
            clientorderstatusdetailid = req.ClientOrderStatusDetailId
        };
    }

    private List<OrderRecord> ParseOrderRecords(string jsonResponse)
    {
        var records = new List<OrderRecord>();

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Response does not contain 'data' array");
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