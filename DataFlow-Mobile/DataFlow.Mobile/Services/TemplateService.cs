using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Mobile.Services;

public class TemplateService : ITemplateService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(DataFlowDbContext context, ILogger<TemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Template>> GetAllTemplatesAsync()
    {
        try
        {
            return await _context.Templates
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all templates");
            return [];
        }
    }

    public async Task<Template?> GetTemplateByIdAsync(int id)
    {
        try
        {
            return await _context.Templates.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template by ID: {TemplateId}", id);
            return null;
        }
    }

    public async Task<Template> CreateTemplateAsync(Template template)
    {
        try
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new template: {TemplateName} (ID: {TemplateId})", template.Name, template.Id);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template: {TemplateName}", template.Name);
            throw;
        }
    }

    public async Task<Template> UpdateTemplateAsync(Template template)
    {
        try
        {
            template.UpdatedAt = DateTime.UtcNow;
            _context.Entry(template).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated template: {TemplateName} (ID: {TemplateId})", template.Name, template.Id);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template: {TemplateId}", template.Id);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
                return false;

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted template: {TemplateId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template: {TemplateId}", id);
            return false;
        }
    }

    public async Task<Template?> GetTemplateByPageIdAsync(int pageId)
    {
        try
        {
            var page = await _context.Pages
                .Include(p => p.Template)
                .FirstOrDefaultAsync(p => p.Id == pageId);

            return page?.Template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template for page: {PageId}", pageId);
            return null;
        }
    }
}