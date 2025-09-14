using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface IActionService
{
    Task<IEnumerable<PageAction>> GetActionsByPageIdAsync(int pageId);
    Task<PageAction?> GetActionByIdAsync(int id);
    Task<PageAction> CreateActionAsync(PageAction action);
    Task<PageAction> UpdateActionAsync(PageAction action);
    Task<bool> DeleteActionAsync(int id);
    Task<ActionResult> ExecuteActionAsync(int actionId, object? data = null);
}