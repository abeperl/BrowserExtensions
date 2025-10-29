using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;

namespace DataFlow.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPageService _pageService;
    private readonly ITemplateService _templateService;
    private readonly IImportExportService _importExportService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<DataPage> _pages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SettingsPageViewModel(
        IPageService pageService,
        ITemplateService templateService,
        IImportExportService importExportService,
        INavigationService navigationService)
    {
        _pageService = pageService;
        _templateService = templateService;
        _importExportService = importExportService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task LoadPagesAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Android.Util.Log.Info("DataFlow", "LoadPagesAsync: Starting to load pages");

            // Temporary: Just create some sample data instead of calling the service
            Pages.Clear();
            Pages.Add(new DataPage
            {
                Id = 1,
                Name = "Sample Page",
                Description = "This is a test page",
                ApiEndpoint = "https://api.example.com",
                CreatedAt = DateTime.Now,
                IsActive = true
            });

            Android.Util.Log.Info("DataFlow", "LoadPagesAsync: Successfully added sample pages");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("DataFlow", $"LoadPagesAsync: Error - {ex}");
            try
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to load pages: {ex.Message}",
                    "OK");
            }
            catch (Exception alertEx)
            {
                Android.Util.Log.Error("DataFlow", $"LoadPagesAsync: Alert error - {alertEx}");
            }
        }
        finally
        {
            IsLoading = false;
            Android.Util.Log.Info("DataFlow", "LoadPagesAsync: Finished");
        }
    }

    [RelayCommand]
    public async Task CreateNewPageAsync()
    {
        // For now, create a simple page. In Phase 6, we'll implement the full page editor
        try
        {
            var pageName = await Application.Current.MainPage.DisplayPromptAsync(
                "New Page",
                "Enter page name:",
                placeholder: "My API Page");

            if (string.IsNullOrWhiteSpace(pageName))
                return;

            var apiEndpoint = await Application.Current.MainPage.DisplayPromptAsync(
                "API Endpoint",
                "Enter API endpoint URL:",
                placeholder: "https://api.example.com/data");

            if (string.IsNullOrWhiteSpace(apiEndpoint))
                return;

            var newPage = new DataPage
            {
                Name = pageName,
                Description = "Created from settings",
                ApiEndpoint = apiEndpoint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                Template = new Template
                {
                    Name = $"{pageName} Template",
                    LayoutType = "List",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _pageService.CreatePageAsync(newPage);
            await LoadPagesAsync();

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Page created successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to create page: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task EditPageAsync(DataPage page)
    {
        if (page == null) return;

        // Simple edit for now - just name and endpoint
        var newName = await Application.Current.MainPage.DisplayPromptAsync(
            "Edit Page",
            "Page name:",
            initialValue: page.Name);

        if (string.IsNullOrWhiteSpace(newName))
            return;

        var newEndpoint = await Application.Current.MainPage.DisplayPromptAsync(
            "Edit Page",
            "API endpoint:",
            initialValue: page.ApiEndpoint);

        if (string.IsNullOrWhiteSpace(newEndpoint))
            return;

        try
        {
            page.Name = newName;
            page.ApiEndpoint = newEndpoint;
            page.UpdatedAt = DateTime.UtcNow;

            await _pageService.UpdatePageAsync(page);
            await LoadPagesAsync();

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Page updated successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to update page: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task DeletePageAsync(DataPage page)
    {
        if (page == null) return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Confirm Delete",
            $"Are you sure you want to delete '{page.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            await _pageService.DeletePageAsync(page.Id);
            await LoadPagesAsync();

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Page deleted successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to delete page: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task DuplicatePageAsync(DataPage page)
    {
        if (page == null) return;

        try
        {
            var duplicatePage = new DataPage
            {
                Name = $"{page.Name} (Copy)",
                Description = page.Description,
                ApiEndpoint = page.ApiEndpoint,
                AuthConfig = page.AuthConfig,
                Template = page.Template,
                Actions = page.Actions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = page.IsActive
            };

            await _pageService.CreatePageAsync(duplicatePage);
            await LoadPagesAsync();

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Page duplicated successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to duplicate page: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task ExportSettingsAsync()
    {
        try
        {
            var exportData = await _importExportService.ExportAllDataAsync();

            // For now, just show the JSON - in a real app, you'd save to file or share
            await Application.Current.MainPage.DisplayAlert(
                "Export Complete",
                $"Configuration exported successfully.\n\nData size: {exportData.Length} characters",
                "OK");

            // TODO: Implement file saving or sharing functionality
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to export settings: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task ImportSettingsAsync()
    {
        try
        {
            // For now, just show placeholder - in a real app, you'd open file picker
            await Application.Current.MainPage.DisplayAlert(
                "Import Settings",
                "Import functionality will be available in Phase 9.\n\nFor now, you can create pages manually.",
                "OK");

            // TODO: Implement file picker and import functionality
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to import settings: {ex.Message}",
                "OK");
        }
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
                p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                p.ApiEndpoint.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

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