namespace DataFlow.Mobile.Services;

public interface IAudioService
{
    Task PlaySoundAsync(string soundName);
    Task<bool> LoadSoundAsync(string soundName, string filePath);
    Task<IEnumerable<string>> GetAvailableSoundsAsync();
    Task SetVolumeAsync(double volume);
    Task<double> GetVolumeAsync();
    Task<bool> IsMutedAsync();
    Task SetMutedAsync(bool muted);
}