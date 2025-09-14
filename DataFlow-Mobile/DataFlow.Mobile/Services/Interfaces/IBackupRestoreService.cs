namespace DataFlow.Mobile.Services.Interfaces;

public interface IBackupRestoreService
{
    Task<string> CreateBackupAsync(string? backupName = null);
    Task<bool> RestoreFromBackupAsync(string backupPath);
    Task<bool> DeleteBackupAsync(string backupPath);
    Task<IEnumerable<string>> GetBackupListAsync();
    Task<bool> ValidateBackupAsync(string backupPath);
    Task<long> GetBackupSizeAsync(string backupPath);
    Task<DateTime> GetBackupDateAsync(string backupPath);

    // Auto-backup functionality
    Task<bool> EnableAutoBackupAsync(TimeSpan interval);
    Task<bool> DisableAutoBackupAsync();
    Task<bool> IsAutoBackupEnabledAsync();

    // Cleanup old backups
    Task<int> CleanupOldBackupsAsync(int keepCount = 5);
    Task<int> CleanupBackupsOlderThanAsync(TimeSpan age);
}