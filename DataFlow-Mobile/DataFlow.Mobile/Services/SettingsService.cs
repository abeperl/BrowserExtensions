using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DataFlow.Mobile.Services;

public class SettingsService : ISettingsService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(DataFlowDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        // This returns a composite settings object
        // For now, return a default one
        return new AppSettings { Key = "Composite", Value = "Default" };
    }

    public async Task<AppSettings> UpdateSettingsAsync(AppSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.Entry(settings).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<T?> GetSettingAsync<T>(string key)
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting?.Value == null)
                return default;

            return JsonSerializer.Deserialize<T>(setting.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting: {Key}", key);
            return default;
        }
    }

    public async Task<bool> SetSettingAsync<T>(string key, T value)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value);
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new AppSettings
                {
                    Key = key,
                    Value = jsonValue,
                    DataType = typeof(T).Name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = jsonValue;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> DeleteSettingAsync(string key)
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
                return false;

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting: {Key}", key);
            return false;
        }
    }
}