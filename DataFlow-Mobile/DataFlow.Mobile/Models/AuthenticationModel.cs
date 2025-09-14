using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Authentications")]
public class AuthenticationModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string AuthType { get; set; } = "BearerToken";

    [MaxLength(1000)]
    public string? BearerToken { get; set; }

    [MaxLength(200)]
    public string? ApiKey { get; set; }

    [MaxLength(100)]
    public string? ApiKeyHeader { get; set; } = "X-API-Key";

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(200)]
    public string? Password { get; set; }

    [MaxLength(1000)]
    public string? OAuthClientId { get; set; }

    [MaxLength(1000)]
    public string? OAuthClientSecret { get; set; }

    [MaxLength(2000)]
    public string? OAuthTokenUrl { get; set; }

    [MaxLength(2000)]
    public string? OAuthScope { get; set; }

    public string? CustomHeaders { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime? TokenExpiresAt { get; set; }

    [Required]
    public bool AutoRefreshToken { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PageModel> Pages { get; set; } = [];
}