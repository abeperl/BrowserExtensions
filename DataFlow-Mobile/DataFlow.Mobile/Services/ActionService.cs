using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Mobile.Services;

public class ActionService : IActionService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<ActionService> _logger;

    public ActionService(DataFlowDbContext context, ILogger<ActionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PageAction>> GetActionsByPageIdAsync(int pageId)
    {
        try
        {
            return await _context.Actions
                .Where(a => a.PageId == pageId && a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting actions for page: {PageId}", pageId);
            return [];
        }
    }

    public async Task<PageAction?> GetActionByIdAsync(int id)
    {
        try
        {
            return await _context.Actions.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting action by ID: {ActionId}", id);
            return null;
        }
    }

    public async Task<PageAction> CreateActionAsync(PageAction action)
    {
        try
        {
            action.CreatedAt = DateTime.UtcNow;
            action.UpdatedAt = DateTime.UtcNow;

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new action: {ActionName} (ID: {ActionId})", action.Name, action.Id);
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating action: {ActionName}", action.Name);
            throw;
        }
    }

    public async Task<PageAction> UpdateActionAsync(PageAction action)
    {
        try
        {
            action.UpdatedAt = DateTime.UtcNow;
            _context.Entry(action).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated action: {ActionName} (ID: {ActionId})", action.Name, action.Id);
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action: {ActionId}", action.Id);
            throw;
        }
    }

    public async Task<bool> DeleteActionAsync(int id)
    {
        try
        {
            var action = await _context.Actions.FindAsync(id);
            if (action == null)
                return false;

            action.IsActive = false;
            action.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted action: {ActionId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action: {ActionId}", id);
            return false;
        }
    }

    public async Task<ActionResult> ExecuteActionAsync(int actionId, object? data = null)
    {
        try
        {
            // Placeholder implementation
            // This will be properly implemented in Phase 7
            await Task.Delay(1);
            _logger.LogInformation("Executing action: {ActionId}", actionId);
            return ActionResult.Success("Action executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action: {ActionId}", actionId);
            return ActionResult.Error($"Action execution failed: {ex.Message}");
        }
    }
}