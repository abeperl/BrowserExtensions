using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;

namespace DataFlow.Mobile.ViewModels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly IPageService _pageService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<Models.Page> _pages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HomePageViewModel(IPageService pageService, INavigationService navigationService)
    {
        _pageService = pageService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task LoadPagesAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var pages = await _pageService.GetAllPagesAsync();

            Pages.Clear();
            foreach (var page in pages)
            {
                Pages.Add(page);
            }
        }
        catch (Exception ex)
        {
            // TODO: Add error handling and user notification
            System.Diagnostics.Debug.WriteLine($"Error loading pages: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshPagesAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            await LoadPagesAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task OpenPageAsync(Models.Page page)
    {
        if (page == null) return;

        var parameters = new Dictionary<string, object>
        {
            { "PageId", page.Id }
        };

        await _navigationService.NavigateToAsync("pagedetail", parameters);
    }

    [RelayCommand]
    public async Task SearchPagesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPagesAsync();
            return;
        }

        try
        {
            var allPages = await _pageService.GetAllPagesAsync();
            var filteredPages = allPages.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);

            Pages.Clear();
            foreach (var page in filteredPages)
            {
                Pages.Add(page);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching pages: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task NavigateToSettingsAsync()
    {
        await _navigationService.NavigateToAsync("settings");
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        Task.Run(async () =>
        {
            await Task.Delay(300);
            if (SearchText == value)
            {
                await SearchPagesAsync();
            }
        });
    }
}