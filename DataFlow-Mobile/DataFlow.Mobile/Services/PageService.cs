using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using Microsoft.Extensions.Logging;
using PageModel = DataFlow.Mobile.Models.Page;

namespace DataFlow.Mobile.Services;

public class PageService : IPageService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<PageService> _logger;

    public PageService(DataFlowDbContext context, ILogger<PageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PageModel>> GetAllPageModelsAsync()
    {
        try
        {
            return await _context.PageModels
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all pages");
            return [];
        }
    }

    public async Task<PageModel?> GetPageModelByIdAsync(int id)
    {
        try
        {
            return await _context.PageModels
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page by ID: {PageModelId}", id);
            return null;
        }
    }

    public async Task<PageModel> CreatePageModelAsync(PageModel page)
    {
        try
        {
            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;

            _context.PageModels.Add(page);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new page: {PageModelName} (ID: {PageModelId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating page: {PageModelName}", page.Name);
            throw;
        }
    }

    public async Task<PageModel> UpdatePageModelAsync(PageModel page)
    {
        try
        {
            page.UpdatedAt = DateTime.UtcNow;
            _context.Entry(page).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated page: {PageModelName} (ID: {PageModelId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating page: {PageModelId}", page.Id);
            throw;
        }
    }

    public async Task<bool> DeletePageModelAsync(int id)
    {
        try
        {
            var page = await _context.PageModels.FindAsync(id);
            if (page == null)
                return false;

            page.IsActive = false;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted page: {PageModelId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting page: {PageModelId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<object>> FetchPageModelDataAsync(int pageId)
    {
        try
        {
            // This will be implemented with the ApiService
            // For now, return empty list
            await Task.Delay(1); // Placeholder
            _logger.LogInformation("Fetching data for page: {PageModelId}", pageId);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for page: {PageModelId}", pageId);
            return [];
        }
    }
}