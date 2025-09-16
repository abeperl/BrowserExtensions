using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DataFlow.Mobile.ViewModels;

public class AudioSettingsViewModel : BaseViewModel
{
    private readonly IAudioService _audioService;
    private readonly ILogger<AudioSettingsViewModel> _logger;

    private bool _isAudioEnabled = true;
    private double _volume = 0.8;
    private ObservableCollection<SoundSetting> _actionSounds = new();
    private ObservableCollection<string> _availableSounds = new();
    private string _audioStatus = "Unknown";
    private int _loadedSoundsCount = 0;
    private string _lastPlayedSound = "None";

    public AudioSettingsViewModel(IAudioService audioService, ILogger<AudioSettingsViewModel> logger)
    {
        _audioService = audioService;
        _logger = logger;

        // Initialize commands
        TestVolumeCommand = new Command(async () => await TestVolumeAsync());
        TestSoundCommand = new Command<SoundSetting>(async setting => await TestSoundAsync(setting));
        TestBuiltInSoundCommand = new Command<string>(async soundName => await TestBuiltInSoundAsync(soundName));
        ReloadSoundsCommand = new Command(async () => await ReloadSoundsAsync());
        ClearCacheCommand = new Command(async () => await ClearCacheAsync());
        ResetToDefaultsCommand = new Command(async () => await ResetToDefaultsAsync());

        // Initialize action sounds
        InitializeActionSounds();

        // Load current settings
        _ = LoadSettingsAsync();
    }

    public bool IsAudioEnabled
    {
        get => _isAudioEnabled;
        set
        {
            if (SetProperty(ref _isAudioEnabled, value))
            {
                _ = UpdateAudioEnabledAsync(value);
            }
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                _ = UpdateVolumeAsync(value);
                OnPropertyChanged(nameof(VolumePercentage));
            }
        }
    }

    public int VolumePercentage => (int)(_volume * 100);

    public ObservableCollection<SoundSetting> ActionSounds
    {
        get => _actionSounds;
        set => SetProperty(ref _actionSounds, value);
    }

    public ObservableCollection<string> AvailableSounds
    {
        get => _availableSounds;
        set => SetProperty(ref _availableSounds, value);
    }

    public string AudioStatus
    {
        get => _audioStatus;
        set => SetProperty(ref _audioStatus, value);
    }

    public int LoadedSoundsCount
    {
        get => _loadedSoundsCount;
        set => SetProperty(ref _loadedSoundsCount, value);
    }

    public string LastPlayedSound
    {
        get => _lastPlayedSound;
        set => SetProperty(ref _lastPlayedSound, value);
    }

    public ICommand TestVolumeCommand { get; }
    public ICommand TestSoundCommand { get; }
    public ICommand TestBuiltInSoundCommand { get; }
    public ICommand ReloadSoundsCommand { get; }
    public ICommand ClearCacheCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }

    private async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;

            // Load current audio settings
            _isAudioEnabled = !await _audioService.IsMutedAsync();
            _volume = await _audioService.GetVolumeAsync();

            // Load available sounds
            await ReloadSoundsAsync();

            // Update status
            await UpdateStatusAsync();

            OnPropertyChanged(nameof(IsAudioEnabled));
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(VolumePercentage));

            _logger.LogInformation("Audio settings loaded - Enabled: {IsEnabled}, Volume: {Volume}", _isAudioEnabled, _volume);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audio settings");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateAudioEnabledAsync(bool isEnabled)
    {
        try
        {
            await _audioService.SetMutedAsync(!isEnabled);
            await UpdateStatusAsync();

            _logger.LogInformation("Audio enabled set to: {IsEnabled}", isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating audio enabled state");
        }
    }

    private async Task UpdateVolumeAsync(double volume)
    {
        try
        {
            await _audioService.SetVolumeAsync(volume);
            _logger.LogInformation("Volume updated to: {Volume}", volume);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating volume");
        }
    }

    private async Task TestVolumeAsync()
    {
        try
        {
            await _audioService.PlaySoundAsync("success");
            LastPlayedSound = $"success ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing volume");
        }
    }

    private async Task TestSoundAsync(SoundSetting? setting)
    {
        try
        {
            if (setting?.IsEnabled == true && !string.IsNullOrEmpty(setting.SelectedSound))
            {
                await _audioService.PlaySoundAsync(setting.SelectedSound);
                LastPlayedSound = $"{setting.SelectedSound} ({DateTime.Now:HH:mm:ss})";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing sound: {SoundName}", setting?.SelectedSound);
        }
    }

    private async Task TestBuiltInSoundAsync(string? soundName)
    {
        try
        {
            if (!string.IsNullOrEmpty(soundName))
            {
                await _audioService.PlaySoundAsync(soundName);
                LastPlayedSound = $"{soundName} ({DateTime.Now:HH:mm:ss})";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing built-in sound: {SoundName}", soundName);
        }
    }

    private async Task ReloadSoundsAsync()
    {
        try
        {
            IsLoading = true;

            var sounds = await _audioService.GetAvailableSoundsAsync();
            AvailableSounds.Clear();

            foreach (var sound in sounds)
            {
                AvailableSounds.Add(sound);
            }

            LoadedSoundsCount = AvailableSounds.Count;

            _logger.LogInformation("Reloaded {Count} sounds", LoadedSoundsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading sounds");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearCacheAsync()
    {
        try
        {
            IsLoading = true;

            await _audioService.ClearCacheAsync();
            await _audioService.PreloadBuiltInSoundsAsync();
            await ReloadSoundsAsync();

            _logger.LogInformation("Audio cache cleared and rebuilt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing audio cache");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        try
        {
            IsLoading = true;

            // Reset to default values
            await _audioService.SetVolumeAsync(0.8);
            await _audioService.SetMutedAsync(false);

            // Reset action sound settings
            foreach (var actionSound in ActionSounds)
            {
                actionSound.IsEnabled = true;
                actionSound.SelectedSound = actionSound.DefaultSound;
            }

            // Reload settings
            await LoadSettingsAsync();

            _logger.LogInformation("Audio settings reset to defaults");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting audio settings");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            var isMuted = await _audioService.IsMutedAsync();
            var volume = await _audioService.GetVolumeAsync();

            AudioStatus = isMuted ? "Muted" : $"Enabled ({volume:P0})";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating audio status");
            AudioStatus = "Error";
        }
    }

    private void InitializeActionSounds()
    {
        ActionSounds = new ObservableCollection<SoundSetting>
        {
            new("Success", "Play when action completes successfully", "success", true),
            new("Error", "Play when action fails or encounters error", "error", true),
            new("Button Click", "Play when button is pressed", "button_click", true),
            new("Data Refresh", "Play when data is refreshed", "refresh", true),
            new("Navigation", "Play when navigating between pages", "swipe", true),
            new("Toggle", "Play when toggle switch is used", "toggle_on", true),
            new("Input Focus", "Play when input field gains focus", "input_focus", false),
            new("Dropdown", "Play when dropdown is opened", "dropdown_open", false),
            new("Action Complete", "Play when any action is completed", "action_complete", false),
            new("Network Error", "Play when network connection fails", "network_error", true)
        };
    }
}

public class SoundSetting : BaseViewModel
{
    private bool _isEnabled;
    private string _selectedSound;

    public SoundSetting(string displayName, string description, string defaultSound, bool isEnabled = true)
    {
        DisplayName = displayName;
        Description = description;
        DefaultSound = defaultSound;
        _selectedSound = defaultSound;
        _isEnabled = isEnabled;
    }

    public string DisplayName { get; }
    public string Description { get; }
    public string DefaultSound { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string SelectedSound
    {
        get => _selectedSound;
        set => SetProperty(ref _selectedSound, value);
    }
}