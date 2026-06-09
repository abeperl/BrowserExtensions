using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("LayoutTemplates")]
public class LayoutTemplate
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
    public string LayoutType { get; set; } = "List";

    [Required]
    public int ColumnsPerRow { get; set; } = 1;

    [Required]
    public int ItemSpacing { get; set; } = 8;

    [Required]
    public int ItemPadding { get; set; } = 15;

    [Required]
    public int BorderRadius { get; set; } = 8;

    [Required]
    public bool ShowShadows { get; set; } = true;

    [Required]
    public bool ShowBorders { get; set; } = false;

    [Required]
    [MaxLength(50)]
    public string ShadowColor { get; set; } = "#00000020";

    [Required]
    public int ShadowOffset { get; set; } = 2;

    [Required]
    public int ShadowBlur { get; set; } = 4;

    [Required]
    [MaxLength(50)]
    public string BorderColor { get; set; } = "#DEE2E6";

    [Required]
    public int BorderWidth { get; set; } = 1;

    [Required]
    public bool EnableHover { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string HoverColor { get; set; } = "#F8F9FA";

    [Required]
    public bool EnableSelection { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string SelectionColor { get; set; } = "#E3F2FD";

    [Required]
    public bool IsBuiltIn { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Additional properties for compatibility
    public string? Configuration { get; set; }

    // Navigation properties
    public ICollection<Template> Templates { get; set; } = [];

    // Static predefined layout templates
    public static LayoutTemplate DefaultList => new()
    {
        Name = "Simple List",
        Description = "Standard list layout with minimal styling",
        LayoutType = "List",
        ColumnsPerRow = 1,
        ItemSpacing = 8,
        ItemPadding = 15,
        BorderRadius = 8,
        ShowShadows = true,
        IsBuiltIn = true
    };

    public static LayoutTemplate CompactList => new()
    {
        Name = "Compact List",
        Description = "Dense list layout for more items per screen",
        LayoutType = "List",
        ColumnsPerRow = 1,
        ItemSpacing = 4,
        ItemPadding = 10,
        BorderRadius = 4,
        ShowShadows = false,
        ShowBorders = true,
        IsBuiltIn = true
    };

    public static LayoutTemplate CardGrid => new()
    {
        Name = "Card Grid",
        Description = "Grid layout with card-style items",
        LayoutType = "Grid",
        ColumnsPerRow = 2,
        ItemSpacing = 12,
        ItemPadding = 20,
        BorderRadius = 12,
        ShowShadows = true,
        IsBuiltIn = true
    };
}