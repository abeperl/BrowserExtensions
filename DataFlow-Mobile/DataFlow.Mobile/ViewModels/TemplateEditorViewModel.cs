using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(TemplateId), "TemplateId")]
public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly ITemplateColumnService _columnService;
    private readonly IColorSchemeService _colorSchemeService;
    private readonly ILayoutTemplateService _layoutTemplateService;
    private readonly ITemplateProcessor _templateProcessor;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Template _currentTemplate;

    [ObservableProperty]
    private ObservableCollection<TemplateColumn> _columns = new();

    [ObservableProperty]
    private ObservableCollection<ColorScheme> _availableColorSchemes = new();

    [ObservableProperty]
    private ObservableCollection<LayoutTemplate> _availableLayoutTemplates = new();

    [ObservableProperty]
    private TemplateColumn _selectedColumn;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPreviewMode;

    [ObservableProperty]
    private ProcessedTemplateData _previewData;

    [ObservableProperty]
    private string _sampleJsonData = @"{
    ""items"": [
        {
            ""id"": 1,
            ""name"": ""Sample Item 1"",
            ""description"": ""This is a sample item for template preview"",
            ""price"": 29.99,
            ""category"": ""Electronics"",
            ""inStock"": true,
            ""lastUpdated"": ""2024-01-15T10:30:00Z""
        },
        {
            ""id"": 2,
            ""name"": ""Sample Item 2"",
            ""description"": ""Another sample item with different data"",
            ""price"": 49.95,
            ""category"": ""Accessories"",
            ""inStock"": false,
            ""lastUpdated"": ""2024-01-16T14:45:00Z""
        }
    ]
}";

    [ObservableProperty]
    private int _templateId;

    [ObservableProperty]
    private TemplateColumnSummary _columnSummary;

    public TemplateEditorViewModel(
        ITemplateService templateService,
        ITemplateColumnService columnService,
        IColorSchemeService colorSchemeService,
        ILayoutTemplateService layoutTemplateService,
        ITemplateProcessor templateProcessor,
        INavigationService navigationService)
    {
        _templateService = templateService;
        _columnService = columnService;
        _colorSchemeService = colorSchemeService;
        _layoutTemplateService = layoutTemplateService;
        _templateProcessor = templateProcessor;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task LoadTemplateAsync()
    {
        if (IsLoading || TemplateId <= 0) return;

        try
        {
            IsLoading = true;

            // Load template
            CurrentTemplate = await _templateService.GetTemplateByIdAsync(TemplateId);
            if (CurrentTemplate == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Template not found", "OK");
                await _navigationService.GoBackAsync();
                return;
            }

            // Load columns
            var columns = await _columnService.GetColumnsByTemplateIdAsync(TemplateId);
            Columns.Clear();
            foreach (var column in columns)
            {
                Columns.Add(column);
            }

            // Load available options
            await LoadAvailableOptionsAsync();

            // Load column summary
            ColumnSummary = await _columnService.GetColumnSummaryAsync(TemplateId);

            // Generate preview
            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load template: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        try
        {
            if (CurrentTemplate == null) return;

            await _templateService.UpdateTemplateAsync(CurrentTemplate);

            // Save all columns
            var columnsToUpdate = Columns.Where(c => c.Id > 0).ToList();
            if (columnsToUpdate.Any())
            {
                await _columnService.UpdateMultipleColumnsAsync(columnsToUpdate);
            }

            await Application.Current.MainPage.DisplayAlert("Success", "Template saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save template: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task AddColumnAsync()
    {
        try
        {
            var newColumn = new TemplateColumn
            {
                TemplateId = TemplateId,
                PropertyName = "new_field",
                DisplayName = "New Field",
                DataType = "String",
                IsVisible = true,
                SortOrder = Columns.Count + 1,
                TextAlignment = "Left",
                FontSize = "14",
                TextColor = "#000000",
                AllowSorting = true
            };

            var createdColumn = await _columnService.CreateColumnAsync(newColumn);
            Columns.Add(createdColumn);

            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add column: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteColumnAsync(TemplateColumn column)
    {
        if (column == null) return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Confirm Delete",
            $"Are you sure you want to delete the column '{column.DisplayName}'?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            await _columnService.DeleteColumnAsync(column.Id);
            Columns.Remove(column);

            // Reorder remaining columns
            for (int i = 0; i < Columns.Count; i++)
            {
                Columns[i].SortOrder = i + 1;
            }

            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete column: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ToggleColumnVisibilityAsync(TemplateColumn column)
    {
        if (column == null) return;

        try
        {
            column.IsVisible = !column.IsVisible;
            await _columnService.UpdateColumnVisibilityAsync(column.Id, column.IsVisible);
            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update column visibility: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task MoveColumnUpAsync(TemplateColumn column)
    {
        if (column == null) return;

        var index = Columns.IndexOf(column);
        if (index > 0)
        {
            Columns.Move(index, index - 1);
            await UpdateColumnOrderAsync();
        }
    }

    [RelayCommand]
    public async Task MoveColumnDownAsync(TemplateColumn column)
    {
        if (column == null) return;

        var index = Columns.IndexOf(column);
        if (index < Columns.Count - 1)
        {
            Columns.Move(index, index + 1);
            await UpdateColumnOrderAsync();
        }
    }

    [RelayCommand]
    public async Task TogglePreviewModeAsync()
    {
        IsPreviewMode = !IsPreviewMode;
        if (IsPreviewMode)
        {
            await GeneratePreviewAsync();
        }
    }

    [RelayCommand]
    public async Task AutoGenerateColumnsAsync()
    {
        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Auto-Generate Columns",
            "This will replace all existing columns with auto-generated ones based on sample data. Continue?",
            "Yes", "Cancel");

        if (!confirm) return;

        try
        {
            var sampleData = JsonSerializer.Deserialize<JsonElement>(SampleJsonData);
            var newColumns = await _columnService.ResetColumnsToDefaultAsync(TemplateId, sampleData);

            Columns.Clear();
            foreach (var column in newColumns)
            {
                Columns.Add(column);
            }

            await GeneratePreviewAsync();
            await Application.Current.MainPage.DisplayAlert("Success", "Columns auto-generated successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to auto-generate columns: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading available options: {ex.Message}");
        }
    }

    private async Task UpdateColumnOrderAsync()
    {
        try
        {
            var columnIds = Columns.Select(c => c.Id).ToList();
            await _columnService.ReorderColumnsAsync(TemplateId, columnIds);

            // Update sort order
            for (int i = 0; i < Columns.Count; i++)
            {
                Columns[i].SortOrder = i + 1;
            }

            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating column order: {ex.Message}");
        }
    }

    private async Task GeneratePreviewAsync()
    {
        try
        {
            if (CurrentTemplate == null) return;

            // Update template columns for preview
            CurrentTemplate.Columns = Columns.ToList();

            var sampleData = JsonSerializer.Deserialize<JsonElement>(SampleJsonData);
            PreviewData = await _templateProcessor.ProcessDataAsync(CurrentTemplate, sampleData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating preview: {ex.Message}");
        }
    }

    partial void OnTemplateIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadTemplateAsync());
        }
    }
}