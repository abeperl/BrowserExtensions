using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("AudioConfigs")]
public class AudioConfigModel
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
    public string EventType { get; set; } = "Success";

    [Required]
    [MaxLength(200)]
    public string AudioFileName { get; set; } = string.Empty;

    [Required]
    public double Volume { get; set; } = 1.0;

    [Required]
    public bool IsEnabled { get; set; } = true;

    [Required]
    public bool EnableHapticFeedback { get; set; } = true;

    [MaxLength(50)]
    public string? HapticPattern { get; set; } = "Medium";

    [Required]
    public bool IsBuiltIn { get; set; } = true;

    [Required]
    public bool IsDefault { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ActionModel> Actions { get; set; } = [];
}