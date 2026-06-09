using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public interface IImportExportService
{
    Task<string> ExportAllConfigurationAsync();
    Task<string> ExportAllDataAsync();
    Task<string> ExportPageConfigurationAsync(int pageId);
    Task<bool> ImportConfigurationAsync(string jsonData, bool overwrite = false);
    Task<bool> ValidateImportDataAsync(string jsonData);
    Task<ImportResult> PreviewImportAsync(string jsonData);
    Task<bool> BackupDatabaseAsync();
    Task<bool> RestoreDatabaseAsync(string backupPath);
}