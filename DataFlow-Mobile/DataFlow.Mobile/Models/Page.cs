using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Pages")]
public class DataPage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(2000)]
    public string ApiEndpoint { get; set; } = string.Empty;

    // Alias for backward compatibility
    [NotMapped]
    public string ApiUrl => ApiEndpoint;

    [Required]
    [MaxLength(20)]
    public string ApiMethod { get; set; } = "GET";

    public string? RequestHeaders { get; set; }

    public string? RequestParameters { get; set; }

    [Required]
    public int TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public Template Template { get; set; } = null!;

    public int? AuthenticationId { get; set; }

    [ForeignKey(nameof(AuthenticationId))]
    public AuthenticationConfig? Authentication { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public int RefreshIntervalSeconds { get; set; } = 300;

    [Required]
    public bool AutoRefresh { get; set; } = false;

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required]
    public int SortOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PageAction> Actions { get; set; } = [];

    // Navigation property for authentication configuration
    public AuthenticationConfig? AuthConfig { get; set; }
}