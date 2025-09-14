namespace DataFlow.Mobile.Models;

public enum AuthenticationType
{
    None = 0,
    Bearer = 1,
    ApiKey = 2,
    Basic = 3,
    OAuth2 = 4,
    Custom = 5
}

public enum OAuth2GrantType
{
    AuthorizationCode = 1,
    ClientCredentials = 2,
    ResourceOwnerPassword = 3,
    RefreshToken = 4
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // 5-minute buffer
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}