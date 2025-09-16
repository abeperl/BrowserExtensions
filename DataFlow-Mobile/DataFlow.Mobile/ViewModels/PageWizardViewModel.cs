using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(PageId), "PageId")]
public partial class PageWizardViewModel : ObservableObject
{
    private readonly IPageService _pageService;
    private readonly IApiService _apiService;
    private readonly ITemplateService _templateService;
    private readonly ITemplateProcessor _templateProcessor;
    private readonly IColorSchemeService _colorSchemeService;
    private readonly ILayoutTemplateService _layoutTemplateService;
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _totalSteps = 5;

    [ObservableProperty]
    private Models.Page _pageConfiguration = new();

    [ObservableProperty]
    private Template _templateConfiguration = new();

    [ObservableProperty]
    private AuthenticationConfig _authConfiguration = new();

    [ObservableProperty]
    private ObservableCollection<ColorScheme> _availableColorSchemes = new();

    [ObservableProperty]
    private ObservableCollection<LayoutTemplate> _availableLayoutTemplates = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableAuthTypes = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _testSuccessful;

    [ObservableProperty]
    private string _sampleApiResponse = string.Empty;

    [ObservableProperty]
    private ProcessedTemplateData _previewData;

    [ObservableProperty]
    private int _pageId;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _wizardTitle = "Create New Page";

    public PageWizardViewModel(
        IPageService pageService,
        IApiService apiService,
        ITemplateService templateService,
        ITemplateProcessor templateProcessor,
        IColorSchemeService colorSchemeService,
        ILayoutTemplateService layoutTemplateService,
        IAuthenticationService authenticationService,
        INavigationService navigationService)
    {
        _pageService = pageService;
        _apiService = apiService;
        _templateService = templateService;
        _templateProcessor = templateProcessor;
        _colorSchemeService = colorSchemeService;
        _layoutTemplateService = layoutTemplateService;
        _authenticationService = authenticationService;
        _navigationService = navigationService;

        InitializeDefaults();
    }

    [RelayCommand]
    public async Task LoadPageAsync()
    {
        if (PageId > 0)
        {
            IsEditMode = true;
            WizardTitle = "Edit Page";
            await LoadExistingPageAsync();
        }
        else
        {
            IsEditMode = false;
            WizardTitle = "Create New Page";
        }

        await LoadAvailableOptionsAsync();
    }

    [RelayCommand]
    public async Task NextStepAsync()
    {
        if (await ValidateCurrentStepAsync())
        {
            if (CurrentStep < TotalSteps)
            {
                CurrentStep++;
                await OnStepChangedAsync();
            }
            else
            {
                await FinishWizardAsync();
            }
        }
    }

    [RelayCommand]
    public async Task PreviousStepAsync()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            await OnStepChangedAsync();
        }
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(PageConfiguration.ApiEndpoint))
        {
            TestResult = "Please enter an API endpoint URL";
            TestSuccessful = false;
            return;
        }

        try
        {
            IsTestingConnection = true;
            TestResult = "Testing connection...";

            // Create a temporary page for testing
            var testPage = new Models.Page
            {
                Name = "Test",
                ApiEndpoint = PageConfiguration.ApiEndpoint,
                ApiMethod = PageConfiguration.ApiMethod,
                RequestHeaders = PageConfiguration.RequestHeaders,
                RequestParameters = PageConfiguration.RequestParameters
            };

            // Apply authentication if configured
            if (AuthConfiguration.Id > 0 || !string.IsNullOrEmpty(AuthConfiguration.AuthenticationType))
            {
                testPage.Authentication = AuthConfiguration;
            }

            var response = await _apiService.GetDataAsync(testPage);

            if (response.IsSuccess)
            {
                TestSuccessful = true;
                TestResult = "✅ Connection successful!";
                SampleApiResponse = JsonSerializer.Serialize(response.Data, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                TestSuccessful = false;
                TestResult = $"❌ Connection failed: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            TestSuccessful = false;
            TestResult = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    public async Task AutoGenerateTemplateAsync()
    {
        if (string.IsNullOrEmpty(SampleApiResponse))
        {
            await Application.Current.MainPage.DisplayAlert(
                "No Sample Data",
                "Please test the API connection first to get sample data for template generation.",
                "OK");
            return;
        }

        try
        {
            var sampleData = JsonSerializer.Deserialize<JsonElement>(SampleApiResponse);

            // Create basic template
            TemplateConfiguration.Name = $"{PageConfiguration.Name} Template";
            TemplateConfiguration.Description = $"Auto-generated template for {PageConfiguration.Name}";

            // Set default color scheme and layout
            TemplateConfiguration.ColorScheme = AvailableColorSchemes.FirstOrDefault(cs => cs.Name == "Light Theme");
            TemplateConfiguration.LayoutTemplate = AvailableLayoutTemplates.FirstOrDefault(lt => lt.Name == "Simple List");

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Template configuration generated! Review the settings and proceed to preview.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to generate template: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task GeneratePreviewAsync()
    {
        if (string.IsNullOrEmpty(SampleApiResponse))
        {
            await TestConnectionAsync();
            if (!TestSuccessful) return;
        }

        try
        {
            var sampleData = JsonSerializer.Deserialize<JsonElement>(SampleApiResponse);

            // Create temporary template with current configuration
            var tempTemplate = new Template
            {
                Name = TemplateConfiguration.Name,
                Description = TemplateConfiguration.Description,
                ColorScheme = TemplateConfiguration.ColorScheme,
                LayoutTemplate = TemplateConfiguration.LayoutTemplate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Auto-generate columns if none exist
            if (!tempTemplate.Columns.Any())
            {
                var autoColumns = await _templateProcessor.AutoGenerateColumnsAsync(sampleData, 0);
                tempTemplate.Columns = autoColumns.Take(5).ToList(); // Show first 5 columns
            }

            PreviewData = await _templateProcessor.ProcessDataAsync(tempTemplate, sampleData);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Preview Error",
                $"Failed to generate preview: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task CancelWizardAsync()
    {
        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Cancel Setup",
            "Are you sure you want to cancel? Any unsaved changes will be lost.",
            "Yes, Cancel",
            "Continue Setup");

        if (confirm)
        {
            await _navigationService.GoBackAsync();
        }
    }

    private void InitializeDefaults()
    {
        PageConfiguration = new Models.Page
        {
            Name = string.Empty,
            Description = string.Empty,
            ApiEndpoint = string.Empty,
            ApiMethod = "GET",
            IsActive = true,
            RefreshIntervalSeconds = 300,
            AutoRefresh = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        TemplateConfiguration = new Template
        {
            Name = string.Empty,
            Description = string.Empty,
            ShowHeaders = true,
            AllowSorting = true,
            AllowFiltering = false,
            ItemsPerPage = 50,
            EnablePagination = true,
            EnablePullToRefresh = true,
            SpacingSize = 8,
            BorderRadius = 4,
            ShowShadows = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        AuthConfiguration = new AuthenticationConfig
        {
            AuthenticationType = "None",
            IsActive = false
        };

        AvailableAuthTypes = new ObservableCollection<string>
        {
            "None",
            "Bearer Token",
            "API Key",
            "Basic Authentication",
            "OAuth 2.0"
        };
    }

    private async Task LoadExistingPageAsync()
    {
        try
        {
            IsLoading = true;

            var existingPage = await _pageService.GetPageByIdAsync(PageId);
            if (existingPage != null)
            {
                PageConfiguration = existingPage;
                TemplateConfiguration = existingPage.Template ?? new Template();
                AuthConfiguration = existingPage.Authentication ?? new AuthenticationConfig();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to load page: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAvailableOptionsAsync()
    {
        try
        {
            var colorSchemes = await _colorSchemeService.GetAllColorSchemesAsync();
            AvailableColorSchemes.Clear();
            foreach (var scheme in colorSchemes)
            {
                AvailableColorSchemes.Add(scheme);
            }

            var layoutTemplates = await _layoutTemplateService.GetAllLayoutTemplatesAsync();
            AvailableLayoutTemplates.Clear();
            foreach (var layout in layoutTemplates)
            {
                AvailableLayoutTemplates.Add(layout);
            }

            // Set defaults if nothing selected
            if (TemplateConfiguration.ColorScheme == null && AvailableColorSchemes.Any())
            {
                TemplateConfiguration.ColorScheme = AvailableColorSchemes.First();
            }

            if (TemplateConfiguration.LayoutTemplate == null && AvailableLayoutTemplates.Any())
            {
                TemplateConfiguration.LayoutTemplate = AvailableLayoutTemplates.First();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading options: {ex.Message}");
        }
    }

    private async Task<bool> ValidateCurrentStepAsync()
    {
        switch (CurrentStep)
        {
            case 1: // Basic Information
                if (string.IsNullOrWhiteSpace(PageConfiguration.Name))
                {
                    await ShowValidationErrorAsync("Please enter a page name");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(PageConfiguration.ApiEndpoint))
                {
                    await ShowValidationErrorAsync("Please enter an API endpoint URL");
                    return false;
                }
                break;

            case 2: // API Configuration
                if (!TestSuccessful && !IsEditMode)
                {
                    var continueAnyway = await Application.Current.MainPage.DisplayAlert(
                        "API Not Tested",
                        "The API connection hasn't been tested successfully. Continue anyway?",
                        "Continue",
                        "Test First");
                    return continueAnyway;
                }
                break;

            case 3: // Authentication
                if (AuthConfiguration.AuthenticationType != "None")
                {
                    if (string.IsNullOrEmpty(AuthConfiguration.TokenValue) &&
                        string.IsNullOrEmpty(AuthConfiguration.Username))
                    {
                        await ShowValidationErrorAsync("Please configure authentication credentials");
                        return false;
                    }
                }
                break;

            case 4: // Template Design
                if (string.IsNullOrWhiteSpace(TemplateConfiguration.Name))
                {
                    TemplateConfiguration.Name = $"{PageConfiguration.Name} Template";
                }
                break;
        }

        return true;
    }

    private async Task OnStepChangedAsync()
    {
        switch (CurrentStep)
        {
            case 4: // Template Design step
                await GeneratePreviewAsync();
                break;
        }
    }

    private async Task FinishWizardAsync()
    {
        try
        {
            IsLoading = true;

            // Save template first
            if (TemplateConfiguration.Id == 0)
            {
                TemplateConfiguration = await _templateService.CreateTemplateAsync(TemplateConfiguration);
            }
            else
            {
                TemplateConfiguration = await _templateService.UpdateTemplateAsync(TemplateConfiguration);
            }

            // Save authentication if configured
            if (AuthConfiguration.AuthenticationType != "None")
            {
                if (AuthConfiguration.Id == 0)
                {
                    AuthConfiguration.PageId = PageConfiguration.Id;
                    AuthConfiguration = await _authenticationService.CreateAuthenticationAsync(AuthConfiguration);
                }
                else
                {
                    AuthConfiguration = await _authenticationService.UpdateAuthenticationAsync(AuthConfiguration);
                }
            }

            // Link template and auth to page
            PageConfiguration.TemplateId = TemplateConfiguration.Id;
            PageConfiguration.AuthenticationId = AuthConfiguration.Id > 0 ? AuthConfiguration.Id : null;

            // Save page
            if (IsEditMode)
            {
                await _pageService.UpdatePageAsync(PageConfiguration);
            }
            else
            {
                await _pageService.CreatePageAsync(PageConfiguration);
            }

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                $"Page '{PageConfiguration.Name}' {(IsEditMode ? "updated" : "created")} successfully!",
                "OK");

            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to save page: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowValidationErrorAsync(string message)
    {
        await Application.Current.MainPage.DisplayAlert("Validation Error", message, "OK");
    }

    partial void OnPageIdChanged(int value)
    {
        Task.Run(async () => await LoadPageAsync());
    }

    public string GetStepTitle()
    {
        return CurrentStep switch
        {
            1 => "Basic Information",
            2 => "API Configuration",
            3 => "Authentication",
            4 => "Template Design",
            5 => "Review & Finish",
            _ => "Setup"
        };
    }

    public string GetStepDescription()
    {
        return CurrentStep switch
        {
            1 => "Enter basic page details and API endpoint",
            2 => "Configure API settings and test connection",
            3 => "Set up authentication if required",
            4 => "Design data presentation template",
            5 => "Review settings and complete setup",
            _ => ""
        };
    }
}