using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface ITemplateService
{
    Task<IEnumerable<Template>> GetAllTemplatesAsync();
    Task<Template?> GetTemplateByIdAsync(int id);
    Task<Template> CreateTemplateAsync(Template template);
    Task<Template> UpdateTemplateAsync(Template template);
    Task<bool> DeleteTemplateAsync(int id);
    Task<Template?> GetTemplateByPageIdAsync(int pageId);
}