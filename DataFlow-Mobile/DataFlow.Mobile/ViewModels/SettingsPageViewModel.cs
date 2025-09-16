using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
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
    private ObservableCollection<Models.Page> _pages = new();

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
            var pages = await _pageService.GetAllPagesAsync();

            Pages.Clear();
            foreach (var page in pages)
            {
                Pages.Add(page);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to load pages: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
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

            var newPage = new Models.Page
            {
                Name = pageName,
                Description = "Created from settings",
                ApiEndpoint = apiEndpoint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                Template = new TemplateModel
                {
                    Name = $"{pageName} Template",
                    Layout = "List",
                    BackgroundColor = "#FFFFFF",
                    TextColor = "#000000",
                    CreatedAt = DateTime.UtcNow
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
    public async Task EditPageAsync(Models.Page page)
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
    public async Task DeletePageAsync(Models.Page page)
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
    public async Task DuplicatePageAsync(Models.Page page)
    {
        if (page == null) return;

        try
        {
            var duplicatePage = new Models.Page
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