using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IPageService
{
    Task<IEnumerable<DataPage>> GetAllPagesAsync(CancellationToken cancellationToken = default);
    Task<DataPage?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DataPage> CreatePageAsync(DataPage page, CancellationToken cancellationToken = default);
    Task<DataPage> UpdatePageAsync(DataPage page, CancellationToken cancellationToken = default);
    Task<bool> DeletePageAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> FetchPageDataAsync(int pageId, CancellationToken cancellationToken = default);
}