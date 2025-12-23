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
    Task<List<OrderRecord>> GetOrdersListAsync(ApiConfig apiConfig, CancellationToken ct = default);
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

            // Log cookie update with token verification
            var cookieToken = cookies.ContainsKey("token") ? cookies["token"] : null;
            if (!string.IsNullOrEmpty(cookieToken))
            {
                // _logger.LogDebug("Updated Cookie header with token (cookie token length: {CookieLength}, matches auth: {Matches})",
                //     cookieToken.Length,
                //     cookieToken == token);
            }
            else
            {
                _logger.LogWarning("Cookie header updated but 'token' cookie is missing or empty!");
            }
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, ApiConfig apiConfig, CancellationToken ct)
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
                        _logger.LogInformation("Attempting to renew authentication token (forcing fresh token from server)");
                        // Force refresh to bypass cached token since server rejected it with 401
                        // CRITICAL: Pass the correct baseUrl for this API (apiConfig.BaseUrl, not _config.BaseUrl)
                        tokenRenewed = await _tokenRenewal.RenewTokenAsync(apiConfig.BaseUrl, ct, forceRefresh: true);

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

    public Task<List<OrderRecord>> GetOrdersListAsync(CancellationToken ct = default)
    {
        return GetOrdersListAsync(_config, ct);
    }

    public async Task<List<OrderRecord>> GetOrdersListAsync(ApiConfig apiConfig, CancellationToken ct = default)
    {
        try
        {
            // Temporarily configure HttpClient for this API config
            _httpClient.BaseAddress = new Uri(apiConfig.BaseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.Timeout = TimeSpan.FromSeconds(200);

            // Update auth headers for this API
            if (!string.IsNullOrWhiteSpace(apiConfig.BearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiConfig.BearerToken);
            }

            // Add custom headers from database configuration (replaces hardcoded logic)
            foreach (var header in apiConfig.CustomHeaders)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            // Fallback: Add WarehouseId if present and not in custom headers (backward compatibility)
            if (apiConfig.WarehouseId > 0 && !apiConfig.CustomHeaders.ContainsKey("WarehouseId"))
            {
                _httpClient.DefaultRequestHeaders.Add("WarehouseId", apiConfig.WarehouseId.ToString());
            }

            // Add cookies
            if (apiConfig.Cookies.Count > 0)
            {
                var cookieString = string.Join("; ", apiConfig.Cookies.Select(kv => $"{kv.Key}={kv.Value}"));
                _httpClient.DefaultRequestHeaders.Add("Cookie", cookieString);
            }

            // Determine primary endpoint and method from configuration
            var endpoint = string.IsNullOrWhiteSpace(apiConfig.PrimaryEndpoint) ? "/api/order/GetOrdersList" : apiConfig.PrimaryEndpoint;
            var methodString = string.IsNullOrWhiteSpace(apiConfig.PrimaryHttpMethod) ? "POST" : apiConfig.PrimaryHttpMethod;
            var httpMethod = new HttpMethod(methodString.ToUpperInvariant());

            // Build request payload: prefer raw PrimaryPayload if supplied; otherwise serialize DefaultRequest
            string? jsonContent = null;
            if (!string.IsNullOrWhiteSpace(apiConfig.PrimaryPayload) && apiConfig.PrimaryPayload != "{}")
            {
                jsonContent = apiConfig.PrimaryPayload;
            }
            else
            {
                var requestPayload = BuildRequestPayload(apiConfig);
                jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }

            _logger.LogInformation("API #{ApiNumber}: Calling primary endpoint {Endpoint} ({Method})...", apiConfig.ApiNumber, endpoint, httpMethod);
            _logger.LogDebug("API #{ApiNumber}: Request payload: {Payload}", apiConfig.ApiNumber, jsonContent);

            Func<HttpRequestMessage> factory = () =>
            {
                var req = new HttpRequestMessage(httpMethod, endpoint);
                if (httpMethod != HttpMethod.Get && jsonContent is not null)
                {
                    req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }
                return req;
            };

            var response = await SendWithRetryAsync(factory, apiConfig, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("API #{ApiNumber}: API call successful. Response length: {Length}", apiConfig.ApiNumber, responseBody.Length);

            // Log first 500 chars of response for debugging (truncate to avoid log bloat)
            var responsePreview = responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody;
            _logger.LogDebug("API #{ApiNumber}: Response body: {Body}", apiConfig.ApiNumber, responsePreview);

            _logger.LogInformation("API #{ApiNumber}: About to parse JSON response...", apiConfig.ApiNumber);

            // Parse JSON and extract order records
            var orders = ParseOrderRecords(responseBody, apiConfig);
            _logger.LogInformation("API #{ApiNumber}: Extracted {Count} order records", apiConfig.ApiNumber, orders.Count);

            if (orders.Count > 0)
            {
                _logger.LogInformation("API #{ApiNumber}: First record ID: {Id}", apiConfig.ApiNumber, orders[0].Id);
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
            _logger.LogError(ex, "Unexpected error calling primary endpoint {Endpoint}", apiConfig.PrimaryEndpoint);
            throw;
        }
    }

    private object BuildRequestPayload()
    {
        return BuildRequestPayload(_config);
    }

    private object BuildRequestPayload(ApiConfig config)
    {
        var req = config.DefaultRequest;
        var dateFrom = string.IsNullOrWhiteSpace(req.DateFrom) ? "2018-01-07" : req.DateFrom;
        var dateTo = string.IsNullOrWhiteSpace(req.DateTo) ? DateTime.Today.ToString("yyyy-MM-dd") : req.DateTo;

        // Use configured columns if provided, else fallback to a minimal column set
        object columnsObject;
        if (config.OrdersListColumns.HasValue)
        {
            columnsObject = config.OrdersListColumns.Value;
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
        return ParseOrderRecords(jsonResponse, _config);
    }

    private List<OrderRecord> ParseOrderRecords(string jsonResponse, ApiConfig config)
    {
        var records = new List<OrderRecord>();

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Navigate to the array based on PrimaryFilterArrayJsonPath configuration
            // If not configured, default to "data" for backward compatibility
            JsonElement dataElement = root;
            var arrayPath = !string.IsNullOrEmpty(config.PrimaryFilterArrayJsonPath)
                ? config.PrimaryFilterArrayJsonPath
                : "data";

            if (!string.IsNullOrEmpty(arrayPath))
            {
                var parts = arrayPath.Split('.');
                foreach (var part in parts)
                {
                    if (dataElement.TryGetProperty(part, out var element))
                    {
                        dataElement = element;
                    }
                    else
                    {
                        _logger.LogWarning("JSON path '{Path}' not found in response. Sample: {Sample}",
                            arrayPath,
                            jsonResponse.Length > 300 ? jsonResponse.Substring(0, 300) + "..." : jsonResponse);
                        return records;
                    }
                }
            }

            if (dataElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Expected array at path '{Path}', got {Kind}. Sample: {Sample}",
                    arrayPath ?? "root",
                    dataElement.ValueKind,
                    jsonResponse.Length > 300 ? jsonResponse.Substring(0, 300) + "..." : jsonResponse);
                return records;
            }

            var totalItems = 0;
            var filteredOutItems = 0;

            foreach (var item in dataElement.EnumerateArray())
            {
                totalItems++;

                try
                {
                    // Apply primary API filter if configured (BEFORE extracting ID)
                    if (!string.IsNullOrEmpty(config.PrimaryFilterType))
                    {
                        if (!ApplyPrimaryFilter(item, config))
                        {
                            filteredOutItems++;
                            continue; // Skip this item
                        }
                    }

                    // Extract ID using configured JSON path
                    var id = ExtractIdFromJsonPath(item, config.IdJsonPath);

                    if (!string.IsNullOrEmpty(id))
                    {
                        records.Add(new OrderRecord
                        {
                            Id = id,
                            RawData = item.Clone()
                        });

                        if (totalItems <= 3)
                        {
                            _logger.LogDebug("ParseOrderRecords: Extracted ID={Id} from item #{Num}", id, totalItems);
                        }
                    }
                    else
                    {
                        if (totalItems <= 3)
                        {
                            _logger.LogWarning("Could not extract ID from record #{Num} using path: {Path}. Item properties: {Props}",
                                totalItems, config.IdJsonPath, string.Join(", ", item.EnumerateObject().Select(p => p.Name)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse individual record #{Num}", totalItems);
                }
            }

            // Only log parsing details if there were items to process
            if (totalItems > 0 || filteredOutItems > 0)
            {
                _logger.LogDebug("ParseOrderRecords: Processed {Total} items, filtered out {Filtered}, extracted {Extracted} records",
                    totalItems, filteredOutItems, records.Count);

                if (filteredOutItems > 0)
                {
                    _logger.LogInformation("Primary API filter applied: {Total} total items, {Filtered} filtered out, {Kept} kept",
                        totalItems, filteredOutItems, records.Count);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ParseOrderRecords");
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
                // Array index or numeric property name
                var indexStr = jsonPath.Trim('[', ']');

                if (element.ValueKind == JsonValueKind.Array)
                {
                    // True array - use numeric index
                    if (int.TryParse(indexStr, out var index))
                    {
                        var array = element.EnumerateArray().ToList();
                        if (index < array.Count)
                        {
                            return array[index].ToString();
                        }
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    // Object with numeric string properties (e.g., {"0": "value", "22": "id"})
                    // Try as property name first
                    if (element.TryGetProperty(indexStr, out var propElement))
                    {
                        return propElement.ToString();
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

    private bool StartsWithAny(string fieldValue, string commaSeparatedValues)
    {
        // Split comma-separated values and check if field starts with any of them
        var values = commaSeparatedValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var value in values)
        {
            if (fieldValue.StartsWith(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private bool ApplyPrimaryFilter(JsonElement item, ApiConfig config)
    {
        try
        {
            var filterType = config.PrimaryFilterType!.ToLowerInvariant();
            var filterValue = config.PrimaryFilterValue ?? string.Empty;

            // Get field value - handle both direct fields and array indices
            string fieldValue;

            if (config.PrimaryFilterArrayIndex.HasValue)
            {
                // Handle array index filtering (e.g., data[x][17])
                var arrayIndex = config.PrimaryFilterArrayIndex.Value;

                if (item.ValueKind == JsonValueKind.Array)
                {
                    var array = item.EnumerateArray().ToList();
                    if (arrayIndex < array.Count)
                    {
                        fieldValue = array[arrayIndex].ToString();
                    }
                    else
                    {
                        _logger.LogDebug("Array index {Index} out of bounds (array length: {Length})", arrayIndex, array.Count);
                        return true; // Allow through if index is out of bounds
                    }
                }
                else
                {
                    _logger.LogWarning("Filter configured for array index but item is not an array. Kind={Kind}", item.ValueKind);
                    return true; // Allow through if misconfigured
                }
            }
            else if (!string.IsNullOrEmpty(config.PrimaryFilterField))
            {
                // Handle field-based filtering
                if (item.TryGetProperty(config.PrimaryFilterField, out var fieldElement))
                {
                    fieldValue = fieldElement.ToString();
                }
                else
                {
                    _logger.LogDebug("Field {Field} not found in item", config.PrimaryFilterField);
                    return true; // Allow through if field not found
                }
            }
            else
            {
                _logger.LogWarning("Primary filter configuration missing both PrimaryFilterArrayIndex and PrimaryFilterField");
                return true; // Allow through if misconfigured
            }

            // Apply filter based on filter type
            return filterType switch
            {
                "startswith" => fieldValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                "notstartswith" => !fieldValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                "startswithany" => StartsWithAny(fieldValue, filterValue),
                "notstartswithany" => !StartsWithAny(fieldValue, filterValue),
                "contains" => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                "notcontains" => !fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                "equals" => fieldValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                "notequals" => !fieldValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                "notempty" => !string.IsNullOrWhiteSpace(fieldValue),
                "empty" => string.IsNullOrWhiteSpace(fieldValue),
                _ => true // Unknown filter type - allow through
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error applying primary filter");
            return true; // Allow through on error
        }
    }
}