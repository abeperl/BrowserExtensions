using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface ITemplateColumnService
{
    /// <summary>
    /// Gets all columns for a specific template
    /// </summary>
    Task<List<TemplateColumn>> GetColumnsByTemplateIdAsync(int templateId);

    /// <summary>
    /// Gets visible columns for a template, ordered by sort order
    /// </summary>
    Task<List<TemplateColumn>> GetVisibleColumnsByTemplateIdAsync(int templateId);

    /// <summary>
    /// Gets a specific template column by ID
    /// </summary>
    Task<TemplateColumn?> GetColumnByIdAsync(int id);

    /// <summary>
    /// Creates a new template column
    /// </summary>
    Task<TemplateColumn> CreateColumnAsync(TemplateColumn column);

    /// <summary>
    /// Updates an existing template column
    /// </summary>
    Task<TemplateColumn> UpdateColumnAsync(TemplateColumn column);

    /// <summary>
    /// Deletes a template column
    /// </summary>
    Task<bool> DeleteColumnAsync(int id);

    /// <summary>
    /// Updates column visibility
    /// </summary>
    Task<bool> UpdateColumnVisibilityAsync(int columnId, bool isVisible);

    /// <summary>
    /// Reorders columns for a template
    /// </summary>
    Task<bool> ReorderColumnsAsync(int templateId, List<int> columnIds);

    /// <summary>
    /// Updates multiple columns at once
    /// </summary>
    Task<List<TemplateColumn>> UpdateMultipleColumnsAsync(List<TemplateColumn> columns);

    /// <summary>
    /// Duplicates a column
    /// </summary>
    Task<TemplateColumn> DuplicateColumnAsync(int columnId);

    /// <summary>
    /// Resets columns to auto-generated defaults based on sample data
    /// </summary>
    Task<List<TemplateColumn>> ResetColumnsToDefaultAsync(int templateId, System.Text.Json.JsonElement sampleData);

    /// <summary>
    /// Gets column configuration summary for a template
    /// </summary>
    Task<TemplateColumnSummary> GetColumnSummaryAsync(int templateId);
}

