using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface IPageService
{
    Task<IEnumerable<Page>> GetAllPagesAsync();
    Task<Page?> GetPageByIdAsync(int id);
    Task<Page> CreatePageAsync(Page page);
    Task<Page> UpdatePageAsync(Page page);
    Task<bool> DeletePageAsync(int id);
    Task<IEnumerable<object>> FetchPageDataAsync(int pageId);
}