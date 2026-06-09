using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Templates")]
public class TemplateModel
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

    public string? FieldMappings { get; set; }

    public string? ColumnSettings { get; set; }

    public string? ColorScheme { get; set; }

    [MaxLength(50)]
    public string Layout { get; set; } = "List";

    [MaxLength(7)]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [MaxLength(7)]
    public string TextColor { get; set; } = "#000000";

    [MaxLength(50)]
    public string? FontFamily { get; set; } = "Default";

    [Required]
    public int FontSize { get; set; } = 14;

    public string? CustomStyles { get; set; }

    [Required]
    public bool ShowHeaders { get; set; } = true;

    [Required]
    public bool AllowSorting { get; set; } = true;

    [Required]
    public bool AllowFiltering { get; set; } = false;

    [Required]
    public int ItemsPerPage { get; set; } = 50;

    [Required]
    public bool EnablePagination { get; set; } = true;

    [Required]
    public bool EnablePullToRefresh { get; set; } = true;

    [Required]
    public int SpacingSize { get; set; } = 8;

    [Required]
    public int BorderRadius { get; set; } = 4;

    [Required]
    public bool ShowShadows { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DataPage> Pages { get; set; } = [];
}