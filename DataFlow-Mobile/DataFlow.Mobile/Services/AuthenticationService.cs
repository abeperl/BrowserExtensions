using System.Text;
using System.Text.Json;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Mobile.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataFlowDbContext _context;
    private readonly ISecureStorageService _secureStorage;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IHttpClientFactory httpClientFactory,
        DataFlowDbContext context,
        ISecureStorageService secureStorage)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _secureStorage = secureStorage;
    }

    public async Task<TokenResponse?> GetTokenAsync(int pageId)
    {
        try
        {
            var config = await GetAuthConfigAsync(pageId);
            if (config == null)
                return null;

            // Check if we have a cached token
            var cachedToken = await GetCachedTokenAsync(config);
            if (cachedToken != null && !cachedToken.IsExpired)
            {
                return cachedToken;
            }

            // Try to refresh token if available
            if (!string.IsNullOrEmpty(cachedToken?.RefreshToken))
            {
                var refreshedToken = await RefreshOAuth2TokenAsync(config);
                if (refreshedToken != null)
                {
                    return refreshedToken;
                }
            }

            // Authenticate from scratch
            return config.AuthenticationType switch
            {
                "Bearer" => await AuthenticateBearerAsync(config),
                "OAuth2" => await AuthenticateOAuth2Async(config),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token for page: {PageId}", pageId);
            return null;
        }
    }

    public async Task<bool> AuthenticateAsync(AuthenticationConfig config)
    {
        try
        {
            var result = config.AuthenticationType switch
            {
                "Bearer" => await AuthenticateBearerAsync(config) != null,
                "OAuth2" => await AuthenticateOAuth2Async(config) != null,
                "ApiKey" => true, // API keys don't require authentication
                "Basic" => true, // Basic auth is applied per request
                _ => false
            };

            if (result)
            {
                await SaveAuthConfigAsync(config);
                _logger.LogInformation("Authentication successful for page: {PageId}", config.PageId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for page: {PageId}", config.PageId);
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(int pageId)
    {
        try
        {
            var config = await GetAuthConfigAsync(pageId);
            if (config == null)
                return false;

            var refreshedToken = await RefreshOAuth2TokenAsync(config);
            return refreshedToken != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed for page: {PageId}", pageId);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAuthenticationHeadersAsync(int pageId)
    {
        try
        {
            var config = await GetAuthConfigAsync(pageId);
            if (config == null)
                return new Dictionary<string, string>();

            return config.AuthenticationType switch
            {
                "Bearer" => await GetBearerHeadersAsync(config),
                "OAuth2" => await GetBearerHeadersAsync(config),
                "ApiKey" => await GetApiKeyHeadersAsync(config),
                "Basic" => await GetBasicAuthHeadersAsync(config),
                _ => new Dictionary<string, string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auth headers for page: {PageId}", pageId);
            return new Dictionary<string, string>();
        }
    }

    public async Task<bool> ValidateTokenAsync(int pageId)
    {
        try
        {
            var config = await GetAuthConfigAsync(pageId);
            if (config == null)
                return false;

            return await IsTokenValidAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed for page: {PageId}", pageId);
            return false;
        }
    }

    public async Task ClearTokenAsync(int pageId)
    {
        try
        {
            var config = await GetAuthConfigAsync(pageId);
            if (config != null)
            {
                config.AccessToken = null;
                config.RefreshToken = null;
                config.TokenExpiry = null;
                await SaveAuthConfigAsync(config);

                // Clear from secure storage
                await _secureStorage.DeleteSecureDataAsync($"token_{pageId}");
                _logger.LogInformation("Token cleared for page: {PageId}", pageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing token for page: {PageId}", pageId);
        }
    }

    public async Task<TokenResponse?> AuthenticateBearerAsync(AuthenticationConfig config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.TokenEndpoint) || string.IsNullOrEmpty(config.Username) || string.IsNullOrEmpty(config.Password))
            {
                _logger.LogWarning("Missing required fields for Bearer authentication");
                return null;
            }

            using var client = _httpClientFactory.CreateClient("AuthApi");

            var requestData = new
            {
                username = config.Username,
                password = config.Password
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(config.TokenEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                if (tokenData != null && tokenData.TryGetValue("access_token", out var tokenValue))
                {
                    var tokenResponse = new TokenResponse
                    {
                        AccessToken = tokenValue.ToString() ?? string.Empty,
                        TokenType = tokenData.TryGetValue("token_type", out var typeValue) ? typeValue.ToString() ?? "Bearer" : "Bearer",
                        ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresValue) && int.TryParse(expiresValue.ToString(), out var expires) ? expires : 3600,
                        RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshValue) ? refreshValue.ToString() : null
                    };

                    await CacheTokenAsync(config, tokenResponse);
                    return tokenResponse;
                }
            }

            _logger.LogWarning("Bearer authentication failed for page: {PageId}", config.PageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bearer authentication error for page: {PageId}", config.PageId);
            return null;
        }
    }

    public async Task<TokenResponse?> AuthenticateOAuth2Async(AuthenticationConfig config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.TokenEndpoint) || string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
            {
                _logger.LogWarning("Missing required fields for OAuth2 authentication");
                return null;
            }

            using var client = _httpClientFactory.CreateClient("AuthApi");

            // Client credentials grant
            var requestData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", config.ClientId),
                new("client_secret", config.ClientSecret)
            };

            if (!string.IsNullOrEmpty(config.Scope))
            {
                requestData.Add(new("scope", config.Scope));
            }

            var content = new FormUrlEncodedContent(requestData);
            var response = await client.PostAsync(config.TokenEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                if (tokenData != null && tokenData.TryGetValue("access_token", out var tokenValue))
                {
                    var tokenResponse = new TokenResponse
                    {
                        AccessToken = tokenValue.ToString() ?? string.Empty,
                        TokenType = tokenData.TryGetValue("token_type", out var typeValue) ? typeValue.ToString() ?? "Bearer" : "Bearer",
                        ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresValue) && int.TryParse(expiresValue.ToString(), out var expires) ? expires : 3600,
                        RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshValue) ? refreshValue.ToString() : null,
                        Scope = tokenData.TryGetValue("scope", out var scopeValue) ? scopeValue.ToString() : null
                    };

                    await CacheTokenAsync(config, tokenResponse);
                    return tokenResponse;
                }
            }

            _logger.LogWarning("OAuth2 authentication failed for page: {PageId}", config.PageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth2 authentication error for page: {PageId}", config.PageId);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetApiKeyHeadersAsync(AuthenticationConfig config)
    {
        var headers = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(config.ApiKey) && !string.IsNullOrEmpty(config.ApiKeyHeader))
        {
            headers[config.ApiKeyHeader] = config.ApiKey;
        }

        return await Task.FromResult(headers);
    }

    public async Task<Dictionary<string, string>> GetBasicAuthHeadersAsync(AuthenticationConfig config)
    {
        var headers = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
            headers["Authorization"] = $"Basic {credentials}";
        }

        return await Task.FromResult(headers);
    }

    public async Task<bool> IsTokenValidAsync(AuthenticationConfig config)
    {
        try
        {
            var token = await GetCachedTokenAsync(config);
            return token != null && !token.IsExpired;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsTokenExpiredAsync(AuthenticationConfig config)
    {
        try
        {
            var token = await GetCachedTokenAsync(config);
            return token?.IsExpired ?? true;
        }
        catch
        {
            return true;
        }
    }

    public async Task<TokenResponse?> RefreshOAuth2TokenAsync(AuthenticationConfig config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.RefreshToken))
                return null;

            using var client = _httpClientFactory.CreateClient("AuthApi");

            var requestData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "refresh_token"),
                new("refresh_token", config.RefreshToken),
                new("client_id", config.ClientId ?? string.Empty),
                new("client_secret", config.ClientSecret ?? string.Empty)
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await client.PostAsync(config.TokenEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                if (tokenData != null && tokenData.TryGetValue("access_token", out var tokenValue))
                {
                    var tokenResponse = new TokenResponse
                    {
                        AccessToken = tokenValue.ToString() ?? string.Empty,
                        TokenType = tokenData.TryGetValue("token_type", out var typeValue) ? typeValue.ToString() ?? "Bearer" : "Bearer",
                        ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresValue) && int.TryParse(expiresValue.ToString(), out var expires) ? expires : 3600,
                        RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshValue) ? refreshValue.ToString() : config.RefreshToken
                    };

                    await CacheTokenAsync(config, tokenResponse);
                    _logger.LogInformation("Token refreshed successfully for page: {PageId}", config.PageId);
                    return tokenResponse;
                }
            }

            _logger.LogWarning("Token refresh failed for page: {PageId}", config.PageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error for page: {PageId}", config.PageId);
            return null;
        }
    }

    public async Task<AuthenticationConfig?> GetAuthConfigAsync(int pageId)
    {
        try
        {
            return await _context.AuthenticationConfigs
                .FirstOrDefaultAsync(a => a.PageId == pageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auth config for page: {PageId}", pageId);
            return null;
        }
    }

    public async Task<bool> SaveAuthConfigAsync(AuthenticationConfig config)
    {
        try
        {
            config.UpdatedAt = DateTime.UtcNow;

            var existing = await _context.AuthenticationConfigs
                .FirstOrDefaultAsync(a => a.PageId == config.PageId);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(config);
            }
            else
            {
                config.CreatedAt = DateTime.UtcNow;
                _context.AuthenticationConfigs.Add(config);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving auth config for page: {PageId}", config.PageId);
            return false;
        }
    }

    public async Task<bool> TestAuthenticationAsync(AuthenticationConfig config)
    {
        try
        {
            var result = config.AuthenticationType switch
            {
                "Bearer" => await AuthenticateBearerAsync(config) != null,
                "OAuth2" => await AuthenticateOAuth2Async(config) != null,
                "ApiKey" => !string.IsNullOrEmpty(config.ApiKey),
                "Basic" => !string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password),
                _ => false
            };

            _logger.LogInformation("Authentication test result for page {PageId}: {Result}", config.PageId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication test failed for page: {PageId}", config.PageId);
            return false;
        }
    }

    private async Task<Dictionary<string, string>> GetBearerHeadersAsync(AuthenticationConfig config)
    {
        var headers = new Dictionary<string, string>();
        var token = await GetCachedTokenAsync(config);

        if (token != null && !token.IsExpired)
        {
            headers["Authorization"] = $"{token.TokenType} {token.AccessToken}";
        }

        return headers;
    }

    private async Task<TokenResponse?> GetCachedTokenAsync(AuthenticationConfig config)
    {
        try
        {
            var tokenJson = await _secureStorage.GetSecureDataAsync($"token_{config.PageId}");
            if (string.IsNullOrEmpty(tokenJson))
                return null;

            return JsonSerializer.Deserialize<TokenResponse>(tokenJson);
        }
        catch
        {
            return null;
        }
    }

    private async Task CacheTokenAsync(AuthenticationConfig config, TokenResponse token)
    {
        try
        {
            var tokenJson = JsonSerializer.Serialize(token);
            await _secureStorage.StoreSecureDataAsync($"token_{config.PageId}", tokenJson);

            // Also update the config
            config.AccessToken = token.AccessToken;
            config.RefreshToken = token.RefreshToken;
            config.TokenExpiry = token.ExpiresAt;
            await SaveAuthConfigAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching token for page: {PageId}", config.PageId);
        }
    }
}