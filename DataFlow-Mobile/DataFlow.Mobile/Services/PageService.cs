using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using Microsoft.Extensions.Logging;

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

    public async Task<IEnumerable<Page>> GetAllPagesAsync()
    {
        try
        {
            return await _context.Pages
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

    public async Task<Page?> GetPageByIdAsync(int id)
    {
        try
        {
            return await _context.Pages
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page by ID: {PageId}", id);
            return null;
        }
    }

    public async Task<Page> CreatePageAsync(Page page)
    {
        try
        {
            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;

            _context.Pages.Add(page);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new page: {PageName} (ID: {PageId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating page: {PageName}", page.Name);
            throw;
        }
    }

    public async Task<Page> UpdatePageAsync(Page page)
    {
        try
        {
            page.UpdatedAt = DateTime.UtcNow;
            _context.Entry(page).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated page: {PageName} (ID: {PageId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating page: {PageId}", page.Id);
            throw;
        }
    }

    public async Task<bool> DeletePageAsync(int id)
    {
        try
        {
            var page = await _context.Pages.FindAsync(id);
            if (page == null)
                return false;

            page.IsActive = false;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted page: {PageId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting page: {PageId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<object>> FetchPageDataAsync(int pageId)
    {
        try
        {
            // This will be implemented with the ApiService
            // For now, return empty list
            await Task.Delay(1); // Placeholder
            _logger.LogInformation("Fetching data for page: {PageId}", pageId);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for page: {PageId}", pageId);
            return [];
        }
    }
}