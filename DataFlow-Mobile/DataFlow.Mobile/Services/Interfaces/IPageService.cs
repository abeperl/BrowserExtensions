using DataFlow.Mobile.Models;
using PageModelModel = DataFlow.Mobile.Models.PageModel;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IPageModelService
{
    Task<IEnumerable<PageModel>> GetAllPageModelsAsync();
    Task<PageModel?> GetPageModelByIdAsync(int id);
    Task<PageModel> CreatePageModelAsync(PageModel page);
    Task<PageModel> UpdatePageModelAsync(PageModel page);
    Task<bool> DeletePageModelAsync(int id);
    Task<IEnumerable<object>> FetchPageModelDataAsync(int pageId);
}