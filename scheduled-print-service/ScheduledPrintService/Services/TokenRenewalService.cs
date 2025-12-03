using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface ITokenRenewalService
{
    Task<bool> RenewTokenAsync(CancellationToken ct = default, bool forceRefresh = false);
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

    public async Task<bool> RenewTokenAsync(CancellationToken ct = default, bool forceRefresh = false)
    {
        // Load credentials from database
        var (username, password, cachedToken, tokenExpiresAt) = _dbConfigService.LoadAuthCredentials(_config.BaseUrl);

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
            _logger.LogWarning("Cannot renew token: Username or Password not found in database for BaseUrl={BaseUrl}", _config.BaseUrl);
            return false;
        }

        _logger.LogInformation("Attempting to renew authentication token for user: {Email}", username);

        try
        {
            var loginPayload = new
            {
                userEmail = username,
                Password = password
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(loginPayload),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/login")
            {
                Content = requestContent
            };

            // Add headers as per the curl command
            request.Headers.Clear();
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "null");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Origin", _config.BaseUrl);
            request.Headers.Add("Pragma", "no-cache");
            request.Headers.Add("Referer", $"{_config.BaseUrl}/");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("WarehouseId", _config.WarehouseId.ToString());
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

            // Add cookie for isRefreshedToken
            request.Headers.Add("Cookie", "isRefreshedToken=false");

            _logger.LogDebug("Sending login request to /api/account/login");

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

            // Try to extract token from response
            // Expected format: {"data": {"token": "...", "userInfo": {...}}}
            if (root.TryGetProperty("data", out var dataElement))
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
            // Fallback: try root level for backward compatibility
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
            _dbConfigService.UpdateAuthToken(_config.BaseUrl, newToken, expiresAt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew token: {Message}", ex.Message);
            return false;
        }
    }
}
