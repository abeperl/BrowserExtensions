using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("AuthenticationConfigs")]
public class AuthenticationConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PageId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AuthenticationType { get; set; } = string.Empty; // Bearer, ApiKey, Basic, OAuth

    [MaxLength(2000)]
    public string? TokenEndpoint { get; set; }

    [MaxLength(200)]
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? Password { get; set; }

    [MaxLength(500)]
    public string? ApiKey { get; set; }

    [MaxLength(100)]
    public string? ApiKeyHeader { get; set; } = "X-API-Key";

    [MaxLength(200)]
    public string? ClientId { get; set; }

    [MaxLength(500)]
    public string? ClientSecret { get; set; }

    [MaxLength(200)]
    public string? Scope { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiry { get; set; }

    public string? AdditionalHeaders { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? TokenValue { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public DataPage Page { get; set; } = null!;
}