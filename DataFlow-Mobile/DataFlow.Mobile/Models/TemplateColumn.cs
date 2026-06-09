using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("TemplateColumns")]
public class TemplateColumn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public Template Template { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DataType { get; set; } = "String";

    [Required]
    public bool IsVisible { get; set; } = true;

    [Required]
    public int SortOrder { get; set; } = 0;

    [MaxLength(20)]
    public string? Width { get; set; } = "Auto";

    [MaxLength(20)]
    public string TextAlignment { get; set; } = "Left";

    [MaxLength(50)]
    public string? FontWeight { get; set; } = "Normal";

    [MaxLength(50)]
    public string? FontSize { get; set; } = "14";

    [MaxLength(50)]
    public string? TextColor { get; set; } = "#000000";

    [MaxLength(50)]
    public string? BackgroundColor { get; set; }

    [MaxLength(200)]
    public string? FormatString { get; set; }

    [MaxLength(500)]
    public string? ConditionalFormatting { get; set; }

    [Required]
    public bool AllowSorting { get; set; } = true;

    [Required]
    public bool AllowFiltering { get; set; } = false;

    [MaxLength(20)]
    public string? FilterType { get; set; } = "Contains";

    [Required]
    public bool WordWrap { get; set; } = false;

    [Required]
    public int MaxLines { get; set; } = 1;

    [MaxLength(200)]
    public string? DefaultValue { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Additional properties for compatibility
    public string Name => DisplayName;
    public int Order => SortOrder;
}