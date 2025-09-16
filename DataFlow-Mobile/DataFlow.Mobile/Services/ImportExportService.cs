using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace DataFlow.Mobile.Services;

public class ImportExportService : IImportExportService
{
    private readonly ILogger<ImportExportService> _logger;
    private readonly DataFlowDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IBackupRestoreService _backupService;

    public ImportExportService(
        ILogger<ImportExportService> logger,
        DataFlowDbContext context,
        ISettingsService settingsService,
        IBackupRestoreService backupService)
    {
        _logger = logger;
        _context = context;
        _settingsService = settingsService;
        _backupService = backupService;
    }

    public async Task<string> ExportAllConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Starting full configuration export");

            var exportData = new ExportData
            {
                ExportedAt = DateTime.UtcNow,
                Version = "1.0.0",
                AppVersion = AppInfo.Current.VersionString,
                Pages = await ExportPagesAsync(),
                Templates = await ExportTemplatesAsync(),
                ColorSchemes = await ExportColorSchemesAsync(),
                LayoutTemplates = await ExportLayoutTemplatesAsync(),
                Actions = await ExportActionsAsync(),
                Settings = await ExportSettingsAsync(),
                AudioConfigs = await ExportAudioConfigsAsync()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonData = JsonSerializer.Serialize(exportData, options);
            _logger.LogInformation("Full configuration export completed - Size: {Size} bytes", jsonData.Length);

            return jsonData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all configuration");
            throw;
        }
    }

    public async Task<string> ExportPageConfigurationAsync(int pageId)
    {
        try
        {
            _logger.LogInformation("Exporting page configuration for page: {PageId}", pageId);

            var page = await _context.Pages
                .Include(p => p.Template)
                .Include(p => p.Actions)
                .FirstOrDefaultAsync(p => p.Id == pageId);

            if (page == null)
            {
                throw new InvalidOperationException($"Page with ID {pageId} not found");
            }

            var exportData = new PageExportData
            {
                ExportedAt = DateTime.UtcNow,
                Version = "1.0.0",
                Page = page,
                Actions = page.Actions?.ToList() ?? new List<PageAction>(),
                Template = page.Template
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            var jsonData = JsonSerializer.Serialize(exportData, options);
            _logger.LogInformation("Page configuration export completed for page: {PageId}", pageId);

            return jsonData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting page configuration for page: {PageId}", pageId);
            throw;
        }
    }

    public async Task<bool> ImportConfigurationAsync(string jsonData, bool overwrite = false)
    {
        try
        {
            _logger.LogInformation("Starting configuration import, overwrite: {Overwrite}", overwrite);

            if (!await ValidateImportDataAsync(jsonData))
            {
                _logger.LogWarning("Import data validation failed");
                return false;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importData = JsonSerializer.Deserialize<ExportData>(jsonData, options);
            if (importData == null)
            {
                _logger.LogError("Failed to deserialize import data");
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create backup before import
                await _backupService.CreateBackupAsync($"pre_import_{DateTime.UtcNow:yyyyMMdd_HHmmss}");

                // Import in order of dependencies
                await ImportColorSchemesAsync(importData.ColorSchemes, overwrite);
                await ImportLayoutTemplatesAsync(importData.LayoutTemplates, overwrite);
                await ImportTemplatesAsync(importData.Templates, overwrite);
                await ImportPagesAsync(importData.Pages, overwrite);
                await ImportActionsAsync(importData.Actions, overwrite);
                await ImportAudioConfigsAsync(importData.AudioConfigs, overwrite);
                await ImportSettingsAsync(importData.Settings, overwrite);

                await transaction.CommitAsync();
                _logger.LogInformation("Configuration import completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during configuration import, transaction rolled back");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration");
            return false;
        }
    }

    public async Task<bool> ValidateImportDataAsync(string jsonData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonData))
                return false;

            var document = JsonDocument.Parse(jsonData);
            var root = document.RootElement;

            // Check for required properties
            if (!root.TryGetProperty("version", out _) ||
                !root.TryGetProperty("exportedAt", out _))
            {
                _logger.LogWarning("Import data missing required metadata");
                return false;
            }

            // Try to deserialize to validate structure
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importData = JsonSerializer.Deserialize<ExportData>(jsonData, options);
            if (importData == null)
            {
                _logger.LogWarning("Failed to deserialize import data");
                return false;
            }

            _logger.LogInformation("Import data validation passed");
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in import data");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating import data");
            return false;
        }
    }

    public async Task<ImportResult> PreviewImportAsync(string jsonData)
    {
        try
        {
            var result = new ImportResult { IsValid = false };

            if (!await ValidateImportDataAsync(jsonData))
            {
                result.ErrorMessage = "Invalid import data format";
                return result;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importData = JsonSerializer.Deserialize<ExportData>(jsonData, options);
            if (importData == null)
            {
                result.ErrorMessage = "Failed to parse import data";
                return result;
            }

            result.IsValid = true;
            result.Summary = new ImportSummary
            {
                PagesCount = importData.Pages?.Count ?? 0,
                TemplatesCount = importData.Templates?.Count ?? 0,
                ActionsCount = importData.Actions?.Count ?? 0,
                SettingsCount = importData.Settings?.Count ?? 0,
                ColorSchemesCount = importData.ColorSchemes?.Count ?? 0,
                LayoutTemplatesCount = importData.LayoutTemplates?.Count ?? 0,
                AudioConfigsCount = importData.AudioConfigs?.Count ?? 0,
                ExportedAt = importData.ExportedAt,
                Version = importData.Version,
                AppVersion = importData.AppVersion
            };

            // Check for conflicts
            result.Conflicts = await DetectConflictsAsync(importData);

            _logger.LogInformation("Import preview completed - {PagesCount} pages, {TemplatesCount} templates",
                result.Summary.PagesCount, result.Summary.TemplatesCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing import");
            return new ImportResult
            {
                IsValid = false,
                ErrorMessage = $"Preview failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> BackupDatabaseAsync()
    {
        try
        {
            var backupName = $"full_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            await _backupService.CreateBackupAsync(backupName);
            _logger.LogInformation("Database backup completed: {BackupName}", backupName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup");
            return false;
        }
    }

    public async Task<bool> RestoreDatabaseAsync(string backupPath)
    {
        try
        {
            await _backupService.RestoreBackupAsync(backupPath);
            _logger.LogInformation("Database restored from: {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database from: {BackupPath}", backupPath);
            return false;
        }
    }

    public async Task<byte[]> ExportAsFileAsync()
    {
        try
        {
            var jsonData = await ExportAllConfigurationAsync();

            using var memoryStream = new MemoryStream();
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);

            // Add configuration file
            var configEntry = archive.CreateEntry("configuration.json");
            using var configStream = configEntry.Open();
            var configBytes = Encoding.UTF8.GetBytes(jsonData);
            await configStream.WriteAsync(configBytes);

            // Add metadata file
            var metadata = new
            {
                ExportedAt = DateTime.UtcNow,
                AppVersion = AppInfo.Current.VersionString,
                Platform = DeviceInfo.Current.Platform.ToString(),
                Device = DeviceInfo.Current.Model
            };

            var metadataEntry = archive.CreateEntry("metadata.json");
            using var metadataStream = metadataEntry.Open();
            var metadataBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            await metadataStream.WriteAsync(metadataBytes);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting as file");
            throw;
        }
    }

    public async Task<bool> ImportFromFileAsync(byte[] fileData, bool overwrite = false)
    {
        try
        {
            using var memoryStream = new MemoryStream(fileData);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            var configEntry = archive.GetEntry("configuration.json");
            if (configEntry == null)
            {
                _logger.LogError("Configuration file not found in import archive");
                return false;
            }

            using var configStream = configEntry.Open();
            using var reader = new StreamReader(configStream);
            var jsonData = await reader.ReadToEndAsync();

            return await ImportConfigurationAsync(jsonData, overwrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing from file");
            return false;
        }
    }

    // Private helper methods
    private async Task<List<Page>> ExportPagesAsync()
    {
        return await _context.Pages
            .Include(p => p.Template)
            .Include(p => p.Actions)
            .ToListAsync();
    }

    private async Task<List<Template>> ExportTemplatesAsync()
    {
        return await _context.Templates.ToListAsync();
    }

    private async Task<List<ColorScheme>> ExportColorSchemesAsync()
    {
        return await _context.ColorSchemes.ToListAsync();
    }

    private async Task<List<LayoutTemplate>> ExportLayoutTemplatesAsync()
    {
        return await _context.LayoutTemplates.ToListAsync();
    }

    private async Task<List<PageAction>> ExportActionsAsync()
    {
        return await _context.Actions.ToListAsync();
    }

    private async Task<List<AudioConfigModel>> ExportAudioConfigsAsync()
    {
        return await _context.AudioConfigs.ToListAsync();
    }

    private async Task<Dictionary<string, object>> ExportSettingsAsync()
    {
        // Export app settings (this would depend on your settings implementation)
        return new Dictionary<string, object>
        {
            { "ExportVersion", "1.0.0" },
            { "ExportedAt", DateTime.UtcNow }
        };
    }

    private async Task ImportColorSchemesAsync(List<ColorScheme>? schemes, bool overwrite)
    {
        if (schemes == null) return;

        foreach (var scheme in schemes)
        {
            var existing = await _context.ColorSchemes.FirstOrDefaultAsync(c => c.Name == scheme.Name);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.PrimaryColor = scheme.PrimaryColor;
                existing.SecondaryColor = scheme.SecondaryColor;
                existing.BackgroundColor = scheme.BackgroundColor;
                existing.TextColor = scheme.TextColor;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                scheme.Id = 0; // Reset ID
                scheme.CreatedAt = DateTime.UtcNow;
                scheme.UpdatedAt = DateTime.UtcNow;
                _context.ColorSchemes.Add(scheme);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportLayoutTemplatesAsync(List<LayoutTemplate>? templates, bool overwrite)
    {
        if (templates == null) return;

        foreach (var template in templates)
        {
            var existing = await _context.LayoutTemplates.FirstOrDefaultAsync(t => t.Name == template.Name);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.LayoutType = template.LayoutType;
                existing.Configuration = template.Configuration;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                template.Id = 0; // Reset ID
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                _context.LayoutTemplates.Add(template);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportTemplatesAsync(List<Template>? templates, bool overwrite)
    {
        if (templates == null) return;

        foreach (var template in templates)
        {
            var existing = await _context.Templates.FirstOrDefaultAsync(t => t.Name == template.Name);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.Configuration = template.Configuration;
                existing.ColorSchemeId = template.ColorSchemeId;
                existing.LayoutTemplateId = template.LayoutTemplateId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                template.Id = 0; // Reset ID
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                _context.Templates.Add(template);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportPagesAsync(List<Page>? pages, bool overwrite)
    {
        if (pages == null) return;

        foreach (var page in pages)
        {
            var existing = await _context.Pages.FirstOrDefaultAsync(p => p.Name == page.Name);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.Description = page.Description;
                existing.ApiEndpoint = page.ApiEndpoint;
                existing.ApiMethod = page.ApiMethod;
                existing.RequestHeaders = page.RequestHeaders;
                existing.RequestParameters = page.RequestParameters;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                page.Id = 0; // Reset ID
                page.CreatedAt = DateTime.UtcNow;
                page.UpdatedAt = DateTime.UtcNow;
                _context.Pages.Add(page);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportActionsAsync(List<PageAction>? actions, bool overwrite)
    {
        if (actions == null) return;

        foreach (var action in actions)
        {
            // Find the corresponding page
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Name == action.Page?.Name);
            if (page == null) continue;

            var existing = await _context.Actions.FirstOrDefaultAsync(a => a.Name == action.Name && a.PageId == page.Id);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.Description = action.Description;
                existing.ActionType = action.ActionType;
                existing.ApiEndpoint = action.ApiEndpoint;
                existing.Parameters = action.Parameters;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                action.Id = 0; // Reset ID
                action.PageId = page.Id;
                action.CreatedAt = DateTime.UtcNow;
                action.UpdatedAt = DateTime.UtcNow;
                _context.Actions.Add(action);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportAudioConfigsAsync(List<AudioConfigModel>? configs, bool overwrite)
    {
        if (configs == null) return;

        foreach (var config in configs)
        {
            var existing = await _context.AudioConfigs.FirstOrDefaultAsync(a => a.Name == config.Name);
            if (existing != null && !overwrite) continue;

            if (existing != null)
            {
                // Update existing
                existing.FilePath = config.FilePath;
                existing.Volume = config.Volume;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                config.Id = 0; // Reset ID
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;
                _context.AudioConfigs.Add(config);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportSettingsAsync(Dictionary<string, object>? settings, bool overwrite)
    {
        if (settings == null) return;

        foreach (var setting in settings)
        {
            // Import application settings
            await _settingsService.SetSettingAsync(setting.Key, setting.Value);
        }
    }

    private async Task<List<ImportConflict>> DetectConflictsAsync(ExportData importData)
    {
        var conflicts = new List<ImportConflict>();

        // Check for page name conflicts
        if (importData.Pages != null)
        {
            foreach (var page in importData.Pages)
            {
                var existing = await _context.Pages.FirstOrDefaultAsync(p => p.Name == page.Name);
                if (existing != null)
                {
                    conflicts.Add(new ImportConflict
                    {
                        Type = "Page",
                        Name = page.Name,
                        Description = $"Page '{page.Name}' already exists"
                    });
                }
            }
        }

        // Check for template name conflicts
        if (importData.Templates != null)
        {
            foreach (var template in importData.Templates)
            {
                var existing = await _context.Templates.FirstOrDefaultAsync(t => t.Name == template.Name);
                if (existing != null)
                {
                    conflicts.Add(new ImportConflict
                    {
                        Type = "Template",
                        Name = template.Name,
                        Description = $"Template '{template.Name}' already exists"
                    });
                }
            }
        }

        return conflicts;
    }

    // Supporting classes
    public class ExportData
    {
        public DateTime ExportedAt { get; set; }
        public string Version { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public List<Page>? Pages { get; set; }
        public List<Template>? Templates { get; set; }
        public List<ColorScheme>? ColorSchemes { get; set; }
        public List<LayoutTemplate>? LayoutTemplates { get; set; }
        public List<PageAction>? Actions { get; set; }
        public List<AudioConfigModel>? AudioConfigs { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class PageExportData
    {
        public DateTime ExportedAt { get; set; }
        public string Version { get; set; } = string.Empty;
        public Page? Page { get; set; }
        public List<PageAction>? Actions { get; set; }
        public Template? Template { get; set; }
    }

    public class ImportConflict
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}