namespace DataFlow.Mobile.Services.Interfaces;

public interface ISecureStorageService
{
    Task<string> StoreSecureDataAsync(string key, string data);
    Task<string?> GetSecureDataAsync(string key);
    Task<bool> DeleteSecureDataAsync(string key);
    Task<bool> HasSecureDataAsync(string key);
    Task<Dictionary<string, string>> GetAllSecureDataAsync();
    Task ClearAllSecureDataAsync();

    // Encryption-specific methods
    Task<string> EncryptDataAsync(string plainText);
    Task<string> DecryptDataAsync(string encryptedData);

    // Key management
    Task<string> GenerateSecureKeyAsync();
    Task<bool> ValidateSecureKeyAsync(string key);
}