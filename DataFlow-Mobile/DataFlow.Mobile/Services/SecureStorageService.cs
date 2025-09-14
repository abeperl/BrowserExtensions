using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DataFlow.Mobile.Services.Interfaces;

namespace DataFlow.Mobile.Services;

public class SecureStorageService : ISecureStorageService
{
    private readonly ILogger<SecureStorageService> _logger;
    private const string EncryptionKeyName = "DataFlow_Encryption_Key";

    public SecureStorageService(ILogger<SecureStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<string> StoreSecureDataAsync(string key, string data)
    {
        try
        {
            var encryptedData = await EncryptDataAsync(data);
            await SecureStorage.SetAsync(key, encryptedData);

            _logger.LogInformation("Securely stored data for key: {Key}", key);
            return encryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing secure data for key: {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetSecureDataAsync(string key)
    {
        try
        {
            var encryptedData = await SecureStorage.GetAsync(key);
            if (string.IsNullOrEmpty(encryptedData))
            {
                return null;
            }

            return await DecryptDataAsync(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secure data for key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> DeleteSecureDataAsync(string key)
    {
        try
        {
            SecureStorage.Remove(key);
            _logger.LogInformation("Deleted secure data for key: {Key}", key);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secure data for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> HasSecureDataAsync(string key)
    {
        try
        {
            var data = await SecureStorage.GetAsync(key);
            return !string.IsNullOrEmpty(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking secure data for key: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecureDataAsync()
    {
        try
        {
            var result = new Dictionary<string, string>();

            // Note: SecureStorage doesn't provide a way to enumerate keys
            // This would need to be implemented by maintaining a list of keys
            // For now, return empty dictionary with a warning
            _logger.LogWarning("GetAllSecureDataAsync called but SecureStorage doesn't support enumeration");

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all secure data");
            return new Dictionary<string, string>();
        }
    }

    public async Task ClearAllSecureDataAsync()
    {
        try
        {
            SecureStorage.RemoveAll();
            _logger.LogInformation("Cleared all secure data");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all secure data");
            throw;
        }
    }

    public async Task<string> EncryptDataAsync(string plainText)
    {
        try
        {
            var encryptionKey = await GetOrCreateEncryptionKeyAsync();

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(encryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cryptoStream);

            await writer.WriteAsync(plainText);
            await writer.FlushAsync();
            cryptoStream.FlushFinalBlock();

            var encryptedBytes = memoryStream.ToArray();
            var result = Convert.ToBase64String(aes.IV.Concat(encryptedBytes).ToArray());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw;
        }
    }

    public async Task<string> DecryptDataAsync(string encryptedData)
    {
        try
        {
            var encryptionKey = await GetOrCreateEncryptionKeyAsync();
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(encryptionKey);

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var memoryStream = new MemoryStream(cipher);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw;
        }
    }

    public async Task<string> GenerateSecureKeyAsync()
    {
        try
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            var key = Convert.ToBase64String(aes.Key);

            _logger.LogInformation("Generated new secure key");
            return await Task.FromResult(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure key");
            throw;
        }
    }

    public async Task<bool> ValidateSecureKeyAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var keyBytes = Convert.FromBase64String(key);
            return await Task.FromResult(keyBytes.Length == 32); // 256-bit key
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating secure key");
            return false;
        }
    }

    private async Task<string> GetOrCreateEncryptionKeyAsync()
    {
        try
        {
            var existingKey = await SecureStorage.GetAsync(EncryptionKeyName);

            if (!string.IsNullOrEmpty(existingKey) && await ValidateSecureKeyAsync(existingKey))
            {
                return existingKey;
            }

            // Generate new key
            var newKey = await GenerateSecureKeyAsync();
            await SecureStorage.SetAsync(EncryptionKeyName, newKey);

            _logger.LogInformation("Created new encryption key");
            return newKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating encryption key");
            throw;
        }
    }
}