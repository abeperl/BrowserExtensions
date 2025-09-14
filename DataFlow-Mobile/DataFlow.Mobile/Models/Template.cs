using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Mobile.Models;

[Table("Templates")]
public class Template
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? FieldMappings { get; set; }

    public string? VisibleColumns { get; set; }

    [MaxLength(50)]
    public string? ColorScheme { get; set; }

    public string? FontSettings { get; set; }

    [MaxLength(20)]
    public string? LayoutType { get; set; } = "List";

    public string? CustomStyling { get; set; }

    [Required]
    public bool ShowHeaders { get; set; } = true;

    [Required]
    public bool AllowSorting { get; set; } = true;

    [Required]
    public bool AllowFiltering { get; set; } = false;

    [Required]
    [Range(1, 1000)]
    public int ItemsPerPage { get; set; } = 50;

    [Required]
    public bool EnablePagination { get; set; } = true;

    [Required]
    public bool EnablePullToRefresh { get; set; } = true;

    [Required]
    [Range(0, 50)]
    public int SpacingSize { get; set; } = 8;

    [Required]
    [Range(0, 25)]
    public int BorderRadius { get; set; } = 4;

    [Required]
    public bool ShowShadows { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Page> Pages { get; set; } = [];
}