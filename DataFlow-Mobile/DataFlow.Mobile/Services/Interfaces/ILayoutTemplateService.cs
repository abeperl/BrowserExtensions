using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface ILayoutTemplateService
{
    /// <summary>
    /// Gets all available layout templates (built-in and custom)
    /// </summary>
    Task<List<LayoutTemplate>> GetAllLayoutTemplatesAsync();

    /// <summary>
    /// Gets a specific layout template by ID
    /// </summary>
    Task<LayoutTemplate?> GetLayoutTemplateByIdAsync(int id);

    /// <summary>
    /// Gets built-in layout templates
    /// </summary>
    Task<List<LayoutTemplate>> GetBuiltInLayoutTemplatesAsync();

    /// <summary>
    /// Creates a new custom layout template
    /// </summary>
    Task<LayoutTemplate> CreateLayoutTemplateAsync(LayoutTemplate layoutTemplate);

    /// <summary>
    /// Updates an existing layout template
    /// </summary>
    Task<LayoutTemplate> UpdateLayoutTemplateAsync(LayoutTemplate layoutTemplate);

    /// <summary>
    /// Deletes a layout template (cannot delete built-in templates)
    /// </summary>
    Task<bool> DeleteLayoutTemplateAsync(int id);

    /// <summary>
    /// Duplicates an existing layout template
    /// </summary>
    Task<LayoutTemplate> DuplicateLayoutTemplateAsync(int id, string newName);

    /// <summary>
    /// Gets the default layout template
    /// </summary>
    Task<LayoutTemplate> GetDefaultLayoutTemplateAsync();

    /// <summary>
    /// Initializes built-in layout templates if they don't exist
    /// </summary>
    Task InitializeBuiltInLayoutTemplatesAsync();

    /// <summary>
    /// Gets layout templates by type
    /// </summary>
    Task<List<LayoutTemplate>> GetLayoutTemplatesByTypeAsync(string layoutType);
}