using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IApiService
{
    // Page-based API calls with automatic authentication
    Task<ApiResponse<T>> GetAsync<T>(int pageId, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null, CancellationToken cancellationToken = default);

    // Raw API calls without authentication
    Task<ApiResponse<T>> GetRawAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostRawAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    // Utility methods
    Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<bool> TestPageConnectionAsync(int pageId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> ExecutePageDataRequestAsync(int pageId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetDataAsync(DataPage page, CancellationToken cancellationToken = default);
}
