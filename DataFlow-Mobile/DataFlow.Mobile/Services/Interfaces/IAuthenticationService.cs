using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface IAuthenticationService
{
    // Token management
    Task<TokenResponse?> GetTokenAsync(int pageId);
    Task<bool> AuthenticateAsync(AuthenticationConfig config);
    Task<bool> RefreshTokenAsync(int pageId);
    Task<Dictionary<string, string>> GetAuthenticationHeadersAsync(int pageId);
    Task<bool> ValidateTokenAsync(int pageId);
    Task ClearTokenAsync(int pageId);

    // Authentication type-specific methods
    Task<TokenResponse?> AuthenticateBearerAsync(AuthenticationConfig config);
    Task<TokenResponse?> AuthenticateOAuth2Async(AuthenticationConfig config);
    Task<Dictionary<string, string>> GetApiKeyHeadersAsync(AuthenticationConfig config);
    Task<Dictionary<string, string>> GetBasicAuthHeadersAsync(AuthenticationConfig config);

    // Token refresh and lifecycle
    Task<bool> IsTokenValidAsync(AuthenticationConfig config);
    Task<bool> IsTokenExpiredAsync(AuthenticationConfig config);
    Task<TokenResponse?> RefreshOAuth2TokenAsync(AuthenticationConfig config);

    // Configuration management
    Task<AuthenticationConfig?> GetAuthConfigAsync(int pageId);
    Task<bool> SaveAuthConfigAsync(AuthenticationConfig config);
    Task<bool> TestAuthenticationAsync(AuthenticationConfig config);
}