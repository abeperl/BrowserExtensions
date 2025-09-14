using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task<AppSettings> UpdateSettingsAsync(AppSettings settings);
    Task<T?> GetSettingAsync<T>(string key);
    Task<bool> SetSettingAsync<T>(string key, T value);
    Task<bool> DeleteSettingAsync(string key);
}