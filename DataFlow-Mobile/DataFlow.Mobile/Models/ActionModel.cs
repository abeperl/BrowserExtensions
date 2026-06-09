using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Actions")]
public class ActionModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PageId { get; set; }

    [ForeignKey(nameof(PageId))]
    public DataPage Page { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = "Button";

    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; } = "#007ACC";

    [Required]
    [MaxLength(2000)]
    public string ApiEndpoint { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HttpMethod { get; set; } = "POST";

    public string? RequestHeaders { get; set; }

    public string? JsonPayloadTemplate { get; set; }

    public string? ValidationRules { get; set; }

    [Required]
    public bool RequiresConfirmation { get; set; } = false;

    [MaxLength(200)]
    public string? ConfirmationMessage { get; set; }

    [Required]
    public bool IsEnabled { get; set; } = true;

    [Required]
    public int SortOrder { get; set; } = 0;

    [Required]
    public bool ShowInList { get; set; } = true;

    [Required]
    public bool ShowInDetail { get; set; } = true;

    public int? AudioConfigId { get; set; }

    [ForeignKey(nameof(AudioConfigId))]
    public AudioConfigModel? AudioConfig { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}