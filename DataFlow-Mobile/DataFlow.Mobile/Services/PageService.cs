using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using Microsoft.Extensions.Logging;
using DataFlow.Mobile.Services.Interfaces;

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

    public async Task<IEnumerable<DataPage>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Pages
                .AsNoTracking()
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all pages");
            return Enumerable.Empty<DataPage>();
        }
    }

    public async Task<DataPage?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Pages
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page by ID: {PageId}", id);
            return null;
        }
    }

    public async Task<DataPage> CreatePageAsync(DataPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;
            _context.Pages.Add(page);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created new page: {PageName} (ID: {PageId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating page: {PageName}", page.Name);
            throw;
        }
    }

    public async Task<DataPage> UpdatePageAsync(DataPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            page.UpdatedAt = DateTime.UtcNow;
            _context.Entry(page).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated page: {PageName} (ID: {PageId})", page.Name, page.Id);
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating page: {PageId}", page.Id);
            throw;
        }
    }

    public async Task<bool> DeletePageAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await _context.Pages.FindAsync(new object?[] { id }, cancellationToken);
            if (page == null)
                return false;

            page.IsActive = false;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted page: {PageId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting page: {PageId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<object>> FetchPageDataAsync(int pageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask; // to be implemented with ApiService
            _logger.LogInformation("Fetching data for page: {PageId}", pageId);
            return Enumerable.Empty<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for page: {PageId}", pageId);
            return Enumerable.Empty<object>();
        }
    }
}