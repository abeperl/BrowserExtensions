using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Services.Interfaces;
using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public class BackupRestoreService : IBackupRestoreService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<BackupRestoreService> _logger;
    private readonly string _backupDirectory;

    public BackupRestoreService(
        DataFlowDbContext context,
        ILogger<BackupRestoreService> logger)
    {
        _context = context;
        _logger = logger;
        _backupDirectory = Path.Combine(FileSystem.AppDataDirectory, "Backups");

        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<string> CreateBackupAsync(string? backupName = null)
    {
        try
        {
            backupName ??= $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var backupPath = Path.Combine(_backupDirectory, $"{backupName}.json");

            var backupData = new
            {
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                Pages = await _context.Pages
                    .Include(p => p.Template)
                    .Include(p => p.Actions)
                    .Include(p => p.Authentication)
                    .ToListAsync(),
                Templates = await _context.Templates.ToListAsync(),
                Actions = await _context.Actions.ToListAsync(),
                AuthenticationConfigs = await _context.AuthenticationConfigs.ToListAsync(),
                Settings = await _context.Settings.ToListAsync(),
                AudioConfigs = await _context.AudioConfigs.ToListAsync()
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(backupData, jsonOptions);
            await File.WriteAllTextAsync(backupPath, json);

            _logger.LogInformation("Created backup at: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup: {BackupName}", backupName);
            throw;
        }
    }

    public async Task<bool> RestoreFromBackupAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                return false;
            }

            if (!await ValidateBackupAsync(backupPath))
            {
                _logger.LogError("Backup validation failed: {BackupPath}", backupPath);
                return false;
            }

            var json = await File.ReadAllTextAsync(backupPath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Start transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear existing data
                _context.AudioConfigs.RemoveRange(await _context.AudioConfigs.ToListAsync());
                _context.Actions.RemoveRange(await _context.Actions.ToListAsync());
                _context.AuthenticationConfigs.RemoveRange(await _context.AuthenticationConfigs.ToListAsync());
                _context.Pages.RemoveRange(await _context.Pages.ToListAsync());
                _context.Templates.RemoveRange(await _context.Templates.ToListAsync());
                _context.Settings.RemoveRange(await _context.Settings.Where(s => s.Category != "System").ToListAsync());

                await _context.SaveChangesAsync();

                // Restore data
                if (root.TryGetProperty("templates", out var templatesElement))
                {
                    var templates = JsonSerializer.Deserialize<List<Template>>(templatesElement.GetRawText(), jsonOptions);
                    if (templates != null)
                    {
                        _context.Templates.AddRange(templates);
                        await _context.SaveChangesAsync();
                    }
                }

                if (root.TryGetProperty("authenticationConfigs", out var authConfigsElement))
                {
                    var authConfigs = JsonSerializer.Deserialize<List<AuthenticationConfig>>(authConfigsElement.GetRawText(), jsonOptions);
                    if (authConfigs != null)
                    {
                        _context.AuthenticationConfigs.AddRange(authConfigs);
                        await _context.SaveChangesAsync();
                    }
                }

                if (root.TryGetProperty("pages", out var pagesElement))
                {
                    var pages = JsonSerializer.Deserialize<List<DataPage>>(pagesElement.GetRawText(), jsonOptions);
                    if (pages != null)
                    {
                        _context.Pages.AddRange(pages);
                        await _context.SaveChangesAsync();
                    }
                }

                if (root.TryGetProperty("actions", out var actionsElement))
                {
                    var actions = JsonSerializer.Deserialize<List<PageAction>>(actionsElement.GetRawText(), jsonOptions);
                    if (actions != null)
                    {
                        _context.Actions.AddRange(actions);
                        await _context.SaveChangesAsync();
                    }
                }

                if (root.TryGetProperty("settings", out var settingsElement))
                {
                    var settings = JsonSerializer.Deserialize<List<AppSettings>>(settingsElement.GetRawText(), jsonOptions);
                    if (settings != null)
                    {
                        _context.Settings.AddRange(settings);
                        await _context.SaveChangesAsync();
                    }
                }

                if (root.TryGetProperty("audioConfigs", out var audioConfigsElement))
                {
                    var audioConfigs = JsonSerializer.Deserialize<List<AudioConfigModel>>(audioConfigsElement.GetRawText(), jsonOptions);
                    if (audioConfigs != null)
                    {
                        _context.AudioConfigs.AddRange(audioConfigs);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully restored from backup: {BackupPath}", backupPath);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring from backup: {BackupPath}", backupPath);
            return false;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        return await RestoreFromBackupAsync(backupPath);
    }

    public async Task<bool> DeleteBackupAsync(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                _logger.LogInformation("Deleted backup: {BackupPath}", backupPath);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup: {BackupPath}", backupPath);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetBackupListAsync()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            return await Task.FromResult(backupFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup list");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> ValidateBackupAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
                return false;

            var json = await File.ReadAllTextAsync(backupPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Check for required properties
            return root.TryGetProperty("version", out _) &&
                   root.TryGetProperty("createdAt", out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup: {BackupPath}", backupPath);
            return false;
        }
    }

    public async Task<long> GetBackupSizeAsync(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                var fileInfo = new FileInfo(backupPath);
                return await Task.FromResult(fileInfo.Length);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup size: {BackupPath}", backupPath);
            return 0;
        }
    }

    public async Task<DateTime> GetBackupDateAsync(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                var fileInfo = new FileInfo(backupPath);
                return await Task.FromResult(fileInfo.CreationTime);
            }
            return DateTime.MinValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup date: {BackupPath}", backupPath);
            return DateTime.MinValue;
        }
    }

    public async Task<bool> EnableAutoBackupAsync(TimeSpan interval)
    {
        try
        {
            // This would typically involve setting up a background service
            // For now, we'll just store the setting
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "AutoBackupEnabled");
            if (setting == null)
            {
                setting = new AppSettings
                {
                    Key = "AutoBackupEnabled",
                    Value = "true",
                    DataType = "Boolean",
                    Category = "Backup"
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = "true";
            }

            var intervalSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "AutoBackupInterval");
            if (intervalSetting == null)
            {
                intervalSetting = new AppSettings
                {
                    Key = "AutoBackupInterval",
                    Value = interval.TotalHours.ToString(),
                    DataType = "Double",
                    Category = "Backup"
                };
                _context.Settings.Add(intervalSetting);
            }
            else
            {
                intervalSetting.Value = interval.TotalHours.ToString();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Enabled auto-backup with interval: {Interval}", interval);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling auto-backup");
            return false;
        }
    }

    public async Task<bool> DisableAutoBackupAsync()
    {
        try
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "AutoBackupEnabled");
            if (setting != null)
            {
                setting.Value = "false";
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Disabled auto-backup");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling auto-backup");
            return false;
        }
    }

    public async Task<bool> IsAutoBackupEnabledAsync()
    {
        try
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "AutoBackupEnabled");
            return setting?.Value == "true";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-backup status");
            return false;
        }
    }

    public async Task<int> CleanupOldBackupsAsync(int keepCount = 5)
    {
        try
        {
            var backupFiles = await GetBackupListAsync();
            var filesToDelete = backupFiles.Skip(keepCount).ToList();

            int deletedCount = 0;
            foreach (var file in filesToDelete)
            {
                if (await DeleteBackupAsync(file))
                {
                    deletedCount++;
                }
            }

            _logger.LogInformation("Cleaned up {DeletedCount} old backups", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups");
            return 0;
        }
    }

    public async Task<int> CleanupBackupsOlderThanAsync(TimeSpan age)
    {
        try
        {
            var cutoffDate = DateTime.Now - age;
            var backupFiles = await GetBackupListAsync();

            int deletedCount = 0;
            foreach (var file in backupFiles)
            {
                var backupDate = await GetBackupDateAsync(file);
                if (backupDate < cutoffDate)
                {
                    if (await DeleteBackupAsync(file))
                    {
                        deletedCount++;
                    }
                }
            }

            _logger.LogInformation("Cleaned up {DeletedCount} old backups older than {Age}", deletedCount, age);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups by age");
            return 0;
        }
    }
}