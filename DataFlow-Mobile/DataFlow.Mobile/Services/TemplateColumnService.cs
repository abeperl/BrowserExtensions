using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataFlow.Mobile.Services;

public class TemplateColumnService : ITemplateColumnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemplateProcessor _templateProcessor;
    private readonly ILogger<TemplateColumnService> _logger;

    public TemplateColumnService(
        IUnitOfWork unitOfWork,
        ITemplateProcessor templateProcessor,
        ILogger<TemplateColumnService> logger)
    {
        _unitOfWork = unitOfWork;
        _templateProcessor = templateProcessor;
        _logger = logger;
    }

    public async Task<List<TemplateColumn>> GetColumnsByTemplateIdAsync(int templateId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            return await repository.GetAsync(
                tc => tc.TemplateId == templateId,
                orderBy: q => q.OrderBy(tc => tc.SortOrder).ThenBy(tc => tc.PropertyName)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving columns for template {TemplateId}", templateId);
            return [];
        }
    }

    public async Task<List<TemplateColumn>> GetVisibleColumnsByTemplateIdAsync(int templateId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            return await repository.GetAsync(
                tc => tc.TemplateId == templateId && tc.IsVisible,
                orderBy: q => q.OrderBy(tc => tc.SortOrder)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visible columns for template {TemplateId}", templateId);
            return [];
        }
    }

    public async Task<TemplateColumn?> GetColumnByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            return await repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving column with ID {Id}", id);
            return null;
        }
    }

    public async Task<TemplateColumn> CreateColumnAsync(TemplateColumn column)
    {
        try
        {
            // Auto-assign sort order if not specified
            if (column.SortOrder == 0)
            {
                var repository = _unitOfWork.GetRepository<TemplateColumn>();
                var existingColumns = await repository.GetAsync(tc => tc.TemplateId == column.TemplateId);
                column.SortOrder = existingColumns.Count > 0 ? existingColumns.Max(tc => tc.SortOrder) + 1 : 1;
            }

            column.CreatedAt = DateTime.UtcNow;
            column.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            await repository.AddAsync(column);
            await _unitOfWork.SaveChangesAsync();

            return column;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template column");
            throw;
        }
    }

    public async Task<TemplateColumn> UpdateColumnAsync(TemplateColumn column)
    {
        try
        {
            column.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            repository.Update(column);
            await _unitOfWork.SaveChangesAsync();

            return column;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template column with ID {Id}", column.Id);
            throw;
        }
    }

    public async Task<bool> DeleteColumnAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var column = await repository.GetByIdAsync(id);

            if (column == null)
                return false;

            repository.Delete(column);
            await _unitOfWork.SaveChangesAsync();

            // Reorder remaining columns
            await ReorderRemainingColumnsAsync(column.TemplateId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template column with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateColumnVisibilityAsync(int columnId, bool isVisible)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var column = await repository.GetByIdAsync(columnId);

            if (column == null)
                return false;

            column.IsVisible = isVisible;
            column.UpdatedAt = DateTime.UtcNow;

            repository.Update(column);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visibility for column {ColumnId}", columnId);
            throw;
        }
    }

    public async Task<bool> ReorderColumnsAsync(int templateId, List<int> columnIds)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var columns = await repository.GetAsync(tc => tc.TemplateId == templateId && columnIds.Contains(tc.Id));

            for (int i = 0; i < columnIds.Count; i++)
            {
                var column = columns.FirstOrDefault(c => c.Id == columnIds[i]);
                if (column != null)
                {
                    column.SortOrder = i + 1;
                    column.UpdatedAt = DateTime.UtcNow;
                    repository.Update(column);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering columns for template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<List<TemplateColumn>> UpdateMultipleColumnsAsync(List<TemplateColumn> columns)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();

            foreach (var column in columns)
            {
                column.UpdatedAt = DateTime.UtcNow;
                repository.Update(column);
            }

            await _unitOfWork.SaveChangesAsync();
            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating multiple template columns");
            throw;
        }
    }

    public async Task<TemplateColumn> DuplicateColumnAsync(int columnId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var originalColumn = await repository.GetByIdAsync(columnId);

            if (originalColumn == null)
                throw new ArgumentException($"Template column with ID {columnId} not found");

            // Get the next sort order
            var existingColumns = await repository.GetAsync(tc => tc.TemplateId == originalColumn.TemplateId);
            var nextSortOrder = existingColumns.Count > 0 ? existingColumns.Max(tc => tc.SortOrder) + 1 : 1;

            var duplicatedColumn = new TemplateColumn
            {
                TemplateId = originalColumn.TemplateId,
                PropertyName = $"{originalColumn.PropertyName}_copy",
                DisplayName = $"{originalColumn.DisplayName} (Copy)",
                DataType = originalColumn.DataType,
                IsVisible = originalColumn.IsVisible,
                SortOrder = nextSortOrder,
                Width = originalColumn.Width,
                TextAlignment = originalColumn.TextAlignment,
                FontWeight = originalColumn.FontWeight,
                FontSize = originalColumn.FontSize,
                TextColor = originalColumn.TextColor,
                BackgroundColor = originalColumn.BackgroundColor,
                FormatString = originalColumn.FormatString,
                ConditionalFormatting = originalColumn.ConditionalFormatting,
                AllowSorting = originalColumn.AllowSorting,
                AllowFiltering = originalColumn.AllowFiltering,
                FilterType = originalColumn.FilterType,
                WordWrap = originalColumn.WordWrap,
                MaxLines = originalColumn.MaxLines,
                DefaultValue = originalColumn.DefaultValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(duplicatedColumn);
            await _unitOfWork.SaveChangesAsync();

            return duplicatedColumn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating template column with ID {ColumnId}", columnId);
            throw;
        }
    }

    public async Task<List<TemplateColumn>> ResetColumnsToDefaultAsync(int templateId, JsonElement sampleData)
    {
        try
        {
            // Delete existing columns
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var existingColumns = await repository.GetAsync(tc => tc.TemplateId == templateId);

            foreach (var column in existingColumns)
            {
                repository.Delete(column);
            }

            // Generate new columns based on sample data
            var newColumns = await _templateProcessor.AutoGenerateColumnsAsync(sampleData, templateId);

            // Save new columns
            foreach (var column in newColumns)
            {
                await repository.AddAsync(column);
            }

            await _unitOfWork.SaveChangesAsync();

            return newColumns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting columns to default for template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<TemplateColumnSummary> GetColumnSummaryAsync(int templateId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var columns = await repository.GetAsync(tc => tc.TemplateId == templateId);

            var summary = new TemplateColumnSummary
            {
                TotalColumns = columns.Count,
                VisibleColumns = columns.Count(c => c.IsVisible),
                HiddenColumns = columns.Count(c => !c.IsVisible),
                AvailableDataTypes = ["String", "Number", "Date", "Boolean", "Currency", "Percentage"],
                MostUsedDataTypes = columns.GroupBy(c => c.DataType)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList(),
                HasCustomFormatting = columns.Any(c => !string.IsNullOrEmpty(c.FormatString)),
                HasConditionalFormatting = columns.Any(c => !string.IsNullOrEmpty(c.ConditionalFormatting))
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting column summary for template {TemplateId}", templateId);
            return new TemplateColumnSummary();
        }
    }

    private async Task ReorderRemainingColumnsAsync(int templateId)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<TemplateColumn>();
            var remainingColumns = await repository.GetAsync(
                tc => tc.TemplateId == templateId,
                orderBy: q => q.OrderBy(tc => tc.SortOrder)
            );

            for (int i = 0; i < remainingColumns.Count; i++)
            {
                remainingColumns[i].SortOrder = i + 1;
                remainingColumns[i].UpdatedAt = DateTime.UtcNow;
                repository.Update(remainingColumns[i]);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering remaining columns for template {TemplateId}", templateId);
        }
    }
}