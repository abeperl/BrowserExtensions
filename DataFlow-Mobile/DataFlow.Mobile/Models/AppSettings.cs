using System.ComponentModel.DataAnnotations;

namespace DataFlow.Mobile.Models;

public class AppSettings
{
    public int Id { get; set; }

    [Required]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? DataType { get; set; } = "String";

    public string? Category { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}