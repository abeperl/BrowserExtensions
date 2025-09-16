using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(PageId), "PageId")]
public partial class PageDetailViewModel : ObservableObject
{
    private readonly IPageService _pageService;
    private readonly IApiService _apiService;
    private readonly IActionService _actionService;
    private readonly IAudioService _audioService;
    private readonly INavigationService _navigationService;
    private readonly ITemplateProcessor _templateProcessor;

    [ObservableProperty]
    private Models.Page _currentPage;

    [ObservableProperty]
    private ObservableCollection<JsonElement> _dataItems = new();

    [ObservableProperty]
    private ProcessedTemplateData _processedData;

    [ObservableProperty]
    private string _layoutType = "List";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _pageId;

    [ObservableProperty]
    private DateTime _lastRefresh;

    public PageDetailViewModel(
        IPageService pageService,
        IApiService apiService,
        IActionService actionService,
        IAudioService audioService,
        INavigationService navigationService,
        ITemplateProcessor templateProcessor)
    {
        _pageService = pageService;
        _apiService = apiService;
        _actionService = actionService;
        _audioService = audioService;
        _navigationService = navigationService;
        _templateProcessor = templateProcessor;
    }

    [RelayCommand]
    public async Task LoadPageDataAsync()
    {
        if (IsLoading || PageId <= 0) return;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            // Load page configuration
            CurrentPage = await _pageService.GetPageByIdAsync(PageId);
            if (CurrentPage == null)
            {
                throw new InvalidOperationException($"Page with ID {PageId} not found");
            }

            // Fetch data from API
            var apiResponse = await _apiService.GetDataAsync(CurrentPage);
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Process data using template system
                if (CurrentPage.Template != null)
                {
                    ProcessedData = await _templateProcessor.ProcessDataAsync(CurrentPage.Template, apiResponse.Data);
                    LayoutType = CurrentPage.Template.LayoutTemplate?.LayoutType ?? "List";
                }
                else
                {
                    // Fallback to simple display
                    DataItems.Clear();
                    if (apiResponse.Data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in apiResponse.Data.EnumerateArray())
                        {
                            DataItems.Add(item);
                        }
                    }
                    else if (apiResponse.Data.ValueKind == JsonValueKind.Object)
                    {
                        DataItems.Add(apiResponse.Data);
                    }
                }

                LastRefresh = DateTime.Now;
            }
            else
            {
                throw new InvalidOperationException(apiResponse.ErrorMessage ?? "Failed to load data");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Error loading page data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            await LoadPageDataAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task ExecuteActionAsync(PageAction action)
    {
        if (action == null || CurrentPage == null) return;

        try
        {
            // Play audio feedback if configured
            if (!string.IsNullOrEmpty(action.AudioFile))
            {
                await _audioService.PlaySoundAsync(action.AudioFile);
            }

            // TODO: Implement ExecuteActionAsync in Phase 7
            throw new NotImplementedException("Action execution will be implemented in Phase 7");

            if (result.IsSuccess)
            {
                // Refresh data if action was successful and requires refresh
                if (action.RefreshAfterExecution)
                {
                    await LoadPageDataAsync();
                }

                // Show success feedback
                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    result.Message ?? "Action completed successfully",
                    "OK");
            }
            else
            {
                // Show error feedback
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    result.Message ?? "Action failed",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to execute action: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task ExecuteItemActionAsync(object parameter)
    {
        if (parameter is not (PageAction action, JsonElement dataItem))
            return;

        try
        {
            // Play audio feedback if configured
            if (!string.IsNullOrEmpty(action.AudioFile))
            {
                await _audioService.PlaySoundAsync(action.AudioFile);
            }

            // TODO: Implement ExecuteActionWithContextAsync in Phase 7
            throw new NotImplementedException("Action execution will be implemented in Phase 7");

            if (result.IsSuccess)
            {
                // Refresh data if action was successful and requires refresh
                if (action.RefreshAfterExecution)
                {
                    await LoadPageDataAsync();
                }

                // Show success feedback
                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    result.Message ?? "Action completed successfully",
                    "OK");
            }
            else
            {
                // Show error feedback
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    result.Message ?? "Action failed",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to execute action: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    partial void OnPageIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadPageDataAsync());
        }
    }

    public string GetPropertyValue(JsonElement item, string propertyName)
    {
        try
        {
            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.GetDecimal().ToString(),
                    JsonValueKind.True => "Yes",
                    JsonValueKind.False => "No",
                    JsonValueKind.Null => string.Empty,
                    _ => property.ToString()
                };
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}