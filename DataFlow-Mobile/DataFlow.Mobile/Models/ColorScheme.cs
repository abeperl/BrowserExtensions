using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("ColorSchemes")]
public class ColorScheme
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
    public string PrimaryColor { get; set; } = "#007ACC";

    [Required]
    [MaxLength(50)]
    public string SecondaryColor { get; set; } = "#6C757D";

    [Required]
    [MaxLength(50)]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [Required]
    [MaxLength(50)]
    public string SurfaceColor { get; set; } = "#F8F9FA";

    [Required]
    [MaxLength(50)]
    public string TextColor { get; set; } = "#212529";

    [Required]
    [MaxLength(50)]
    public string TextSecondaryColor { get; set; } = "#6C757D";

    [Required]
    [MaxLength(50)]
    public string BorderColor { get; set; } = "#DEE2E6";

    [Required]
    [MaxLength(50)]
    public string SuccessColor { get; set; } = "#28A745";

    [Required]
    [MaxLength(50)]
    public string WarningColor { get; set; } = "#FFC107";

    [Required]
    [MaxLength(50)]
    public string ErrorColor { get; set; } = "#DC3545";

    [Required]
    [MaxLength(50)]
    public string InfoColor { get; set; } = "#17A2B8";

    [Required]
    public bool IsBuiltIn { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Template> Templates { get; set; } = [];

    // Static predefined color schemes
    public static ColorScheme DefaultLight => new()
    {
        Name = "Light Theme",
        Description = "Clean light theme with modern colors",
        PrimaryColor = "#007ACC",
        SecondaryColor = "#6C757D",
        BackgroundColor = "#FFFFFF",
        SurfaceColor = "#F8F9FA",
        TextColor = "#212529",
        TextSecondaryColor = "#6C757D",
        BorderColor = "#DEE2E6",
        IsBuiltIn = true
    };

    public static ColorScheme DefaultDark => new()
    {
        Name = "Dark Theme",
        Description = "Modern dark theme for low-light environments",
        PrimaryColor = "#4FC3F7",
        SecondaryColor = "#9E9E9E",
        BackgroundColor = "#121212",
        SurfaceColor = "#1E1E1E",
        TextColor = "#FFFFFF",
        TextSecondaryColor = "#B0B0B0",
        BorderColor = "#333333",
        IsBuiltIn = true
    };
}