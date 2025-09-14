using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Settings")]
public class SettingModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = "String";

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsUserConfigurable { get; set; } = true;

    [Required]
    public bool IsSecure { get; set; } = false;

    public string? DefaultValue { get; set; }

    public string? ValidationRules { get; set; }

    [Required]
    public int SortOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}