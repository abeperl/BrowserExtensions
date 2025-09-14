using System.Net.Http;
using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface IApiService
{
    // Page-based API calls with automatic authentication
    Task<ApiResponse<T>> GetAsync<T>(int pageId);
    Task<ApiResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null);
    Task<ApiResponse<T>> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null);
    Task<ApiResponse<T>> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = null);
    Task<ApiResponse<T>> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null);

    // Raw API calls without authentication
    Task<ApiResponse<T>> GetRawAsync<T>(string url, Dictionary<string, string>? headers = null);
    Task<ApiResponse<T>> PostRawAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null);

    // Utility methods
    Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null);
    Task<bool> TestPageConnectionAsync(int pageId);
    Task<ApiResponse<object>> ExecutePageDataRequestAsync(int pageId);
}