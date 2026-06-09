using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface ITokenRenewalService
{
    Task<bool> RenewTokenAsync(string baseUrl, CancellationToken ct = default, bool forceRefresh = false);
    string GetCurrentToken();
    Dictionary<string, string> GetCurrentCookies();
}

public class TokenRenewalService : ITokenRenewalService
{
    private readonly ILogger<TokenRenewalService> _logger;
    private readonly ApiConfig _config;
    private readonly HttpClient _httpClient;
    private readonly IDatabaseApiConfigService _dbConfigService;
    private readonly object _lock = new();
    private string _currentToken;
    private Dictionary<string, string> _currentCookies;

    public TokenRenewalService(
        ILogger<TokenRenewalService> logger,
        IOptions<ApiConfig> apiConfig,
        IHttpClientFactory httpClientFactory,
        IDatabaseApiConfigService dbConfigService)
    {
        _logger = logger;
        _config = apiConfig.Value;
        _dbConfigService = dbConfigService;
        _currentToken = _config.BearerToken;
        _currentCookies = new Dictionary<string, string>(_config.Cookies);

        // Create a separate HttpClient for authentication (doesn't use the configured token)
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public string GetCurrentToken()
    {
        lock (_lock)
        {
            return _currentToken;
        }
    }

    public Dictionary<string, string> GetCurrentCookies()
    {
        lock (_lock)
        {
            return new Dictionary<string, string>(_currentCookies);
        }
    }

    public async Task<bool> RenewTokenAsync(string baseUrl, CancellationToken ct = default, bool forceRefresh = false)
    {
        // Load credentials from database using the provided baseUrl (not _config.BaseUrl which may be wrong in multi-API scenarios)
        var (username, password, cachedToken, tokenExpiresAt) = _dbConfigService.LoadAuthCredentials(baseUrl);

        // Check if cached token is still valid (but skip cache if forceRefresh is true)
        if (!forceRefresh && !string.IsNullOrEmpty(cachedToken) && tokenExpiresAt.HasValue && tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation("Using cached token, expires at {ExpiresAt}", tokenExpiresAt);
            lock (_lock)
            {
                _currentToken = cachedToken;
            }
            return true;
        }

        if (forceRefresh)
        {
            _logger.LogInformation("Force refresh requested - bypassing cached token and fetching fresh token from server");
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Cannot renew token: Username or Password not found in database for BaseUrl={BaseUrl}", baseUrl);
            return false;
        }

        _logger.LogInformation("Attempting to renew authentication token for user: {Email}", username);

        try
        {
            // Different sites use different payload field names
            // malchus.3plnext.com uses: UserName, UserPassword
            // mj.3plnext.com uses: userEmail, Password
            object loginPayload;
            if (baseUrl.Contains("malchus.3plnext.com", StringComparison.OrdinalIgnoreCase))
            {
                loginPayload = new
                {
                    UserName = username,
                    UserPassword = password
                };
            }
            else
            {
                loginPayload = new
                {
                    userEmail = username,
                    Password = password
                };
            }

            var requestContent = new StringContent(
                JsonSerializer.Serialize(loginPayload),
                Encoding.UTF8,
                "application/json");

            // Create request with absolute URL (baseUrl parameter may differ from _httpClient.BaseAddress)
            var loginUrl = new Uri(new Uri(baseUrl), "/api/account/login");
            using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
            {
                Content = requestContent
            };

            // Add headers as per the curl command
            request.Headers.Clear();
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "null");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Origin", baseUrl);
            request.Headers.Add("Pragma", "no-cache");
            request.Headers.Add("Referer", $"{baseUrl}/");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            // Add custom headers from database (replaces hardcoded logic)
            var customHeaders = _dbConfigService.LoadCustomHeaders(baseUrl);
            foreach (var header in customHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            // Fallback: Add WarehouseId if not in custom headers (backward compatibility)
            if (!customHeaders.ContainsKey("WarehouseId") && _config.WarehouseId > 0)
            {
                request.Headers.Add("WarehouseId", _config.WarehouseId.ToString());
            }

            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");

            // Add cookie for isRefreshedToken
            request.Headers.Add("Cookie", "isRefreshedToken=false; chargingCard=; paymentinprocess=");

            _logger.LogDebug("Sending login request to {LoginUrl}", loginUrl);

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Login failed with status {Status}: {Body}",
                    (int)response.StatusCode, responseBody);
                return false;
            }

            _logger.LogDebug("Login response: {Body}", responseBody);

            // Parse response to extract token
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            string? newToken = null;
            Dictionary<string, string>? newCookies = null;
            string? userDataJson = null;

            // Try to extract token from response - different APIs use different formats

            // Format 1: malchus.3plnext.com - {"response": {"result": {"token": "...", "userInfo": {...}}}}
            if (root.TryGetProperty("response", out var responseElement))
            {
                if (responseElement.TryGetProperty("result", out var resultElement))
                {
                    if (resultElement.TryGetProperty("token", out var tokenElement))
                    {
                        newToken = tokenElement.GetString();
                    }

                    // Extract userInfo if available for cookie
                    if (resultElement.TryGetProperty("userInfo", out var userInfoElement))
                    {
                        userDataJson = userInfoElement.GetRawText();
                    }
                }
            }
            // Format 2: mj.3plnext.com - {"data": {"token": "...", "userInfo": {...}}}
            else if (root.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("token", out var tokenElement))
                {
                    newToken = tokenElement.GetString();
                }

                // Extract userInfo if available for cookie
                if (dataElement.TryGetProperty("userInfo", out var userInfoElement))
                {
                    userDataJson = userInfoElement.GetRawText();
                }
            }
            // Format 3: Fallback - try root level for backward compatibility
            else if (root.TryGetProperty("token", out var tokenElement))
            {
                newToken = tokenElement.GetString();

                if (root.TryGetProperty("userData", out var userDataElement))
                {
                    userDataJson = userDataElement.GetRawText();
                }
            }

            if (string.IsNullOrEmpty(newToken))
            {
                _logger.LogError("Token not found in login response");
                return false;
            }

            // Update cookies
            newCookies = new Dictionary<string, string>
            {
                ["isRefreshedToken"] = "false",
                ["token"] = newToken
            };

            if (!string.IsNullOrEmpty(userDataJson))
            {
                newCookies["userData"] = userDataJson;
            }

            // Update stored token and cookies
            lock (_lock)
            {
                _currentToken = newToken;
                _currentCookies = newCookies;
                _logger.LogInformation("Successfully renewed authentication token");
            }

            // Save token to database (expires in 24 hours - adjust as needed)
            var expiresAt = DateTime.UtcNow.AddHours(24);
            _dbConfigService.UpdateAuthToken(baseUrl, newToken, expiresAt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew token: {Message}", ex.Message);
            return false;
        }
    }
}
