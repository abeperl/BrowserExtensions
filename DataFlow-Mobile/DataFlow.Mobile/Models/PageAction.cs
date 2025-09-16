using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace DataFlow.Mobile.Models;

[Table("Actions")]
public class PageAction
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty; // Button, Dropdown, Input, etc.

    [MaxLength(100)]
    public string? Label { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }

    [MaxLength(2000)]
    public string? ApiEndpoint { get; set; }

    [MaxLength(20)]
    public string? HttpMethod { get; set; } = "POST";

    public string? PayloadTemplate { get; set; }

    public string? Parameters { get; set; }

    public string? ValidationRules { get; set; }

    public string? ConfirmationMessage { get; set; }

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public string? SoundEffect { get; set; }

    public bool IsItemLevel { get; set; } = true;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Additional properties for UI binding
    [NotMapped]
    public string DisplayText => !string.IsNullOrEmpty(Label) ? Label : Name;

    [NotMapped]
    public string ButtonColor => !string.IsNullOrEmpty(Color) ? Color : "#007ACC";

    [NotMapped]
    public bool IsEnabled => IsActive;

    [NotMapped]
    public string? Placeholder { get; set; }

    [NotMapped]
    public string? InputValue { get; set; }

    [NotMapped]
    public string? SelectedValue { get; set; }

    [NotMapped]
    public bool ToggleValue { get; set; }

    [NotMapped]
    public ObservableCollection<string> DropdownOptions { get; set; } = new();

    [NotMapped]
    public ObservableCollection<SelectableItem> MultiSelectOptions { get; set; } = new();

    [NotMapped]
    public ObservableCollection<object> SelectedItems { get; set; } = new();

    [NotMapped]
    public string? NavigationTarget { get; set; }

    [NotMapped]
    public string? Conditions { get; set; }

    [NotMapped]
    public string? SuccessSound { get; set; }

    [NotMapped]
    public string? ErrorSound { get; set; }

    // Foreign key
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;
}