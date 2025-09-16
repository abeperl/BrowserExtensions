using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IColorSchemeService
{
    /// <summary>
    /// Gets all available color schemes (built-in and custom)
    /// </summary>
    Task<List<ColorScheme>> GetAllColorSchemesAsync();

    /// <summary>
    /// Gets a specific color scheme by ID
    /// </summary>
    Task<ColorScheme?> GetColorSchemeByIdAsync(int id);

    /// <summary>
    /// Gets built-in color schemes
    /// </summary>
    Task<List<ColorScheme>> GetBuiltInColorSchemesAsync();

    /// <summary>
    /// Creates a new custom color scheme
    /// </summary>
    Task<ColorScheme> CreateColorSchemeAsync(ColorScheme colorScheme);

    /// <summary>
    /// Updates an existing color scheme
    /// </summary>
    Task<ColorScheme> UpdateColorSchemeAsync(ColorScheme colorScheme);

    /// <summary>
    /// Deletes a color scheme (cannot delete built-in schemes)
    /// </summary>
    Task<bool> DeleteColorSchemeAsync(int id);

    /// <summary>
    /// Duplicates an existing color scheme
    /// </summary>
    Task<ColorScheme> DuplicateColorSchemeAsync(int id, string newName);

    /// <summary>
    /// Gets the default color scheme
    /// </summary>
    Task<ColorScheme> GetDefaultColorSchemeAsync();

    /// <summary>
    /// Initializes built-in color schemes if they don't exist
    /// </summary>
    Task InitializeBuiltInColorSchemesAsync();
}