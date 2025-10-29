using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(TemplateId), "TemplateId")]
public partial class AdvancedTemplateDesignerViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly ITemplateProcessor _templateProcessor;
    private readonly IColorSchemeService _colorSchemeService;
    private readonly ILayoutTemplateService _layoutTemplateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Template _currentTemplate = new();

    [ObservableProperty]
    private ObservableCollection<TemplateColumn> _availableColumns = new();

    [ObservableProperty]
    private ObservableCollection<TemplateColumn> _designerColumns = new();

    [ObservableProperty]
    private ObservableCollection<ColorScheme> _colorSchemes = new();

    [ObservableProperty]
    private ObservableCollection<LayoutTemplate> _layoutTemplates = new();

    [ObservableProperty]
    private ColorScheme _selectedColorScheme = new();

    [ObservableProperty]
    private LayoutTemplate _selectedLayoutTemplate = new();

    [ObservableProperty]
    private TemplateColumn _selectedColumn = new();

    [ObservableProperty]
    private string _previewData = string.Empty;

    [ObservableProperty]
    private bool _isPreviewMode = false;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDragMode = false;

    [ObservableProperty]
    private int _templateId;

    [ObservableProperty]
    private string _designerMode = "Visual"; // Visual, Code, Preview

    [ObservableProperty]
    private string _templateCode = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _designerModes = new() { "Visual", "Code", "Preview" };

    [ObservableProperty]
    private ObservableCollection<string> _dataTypes = new()
    {
        "Text", "Number", "Date", "Boolean", "Currency", "Percentage", "Image", "Link", "JSON"
    };

    [ObservableProperty]
    private ObservableCollection<string> _alignmentOptions = new()
    {
        "Left", "Center", "Right", "Justify"
    };

    [ObservableProperty]
    private ObservableCollection<string> _sortDirections = new()
    {
        "None", "Ascending", "Descending"
    };

    [ObservableProperty]
    private ObservableCollection<string> _formatTypes = new()
    {
        "None", "Currency", "Date", "DateTime", "Percentage", "Custom"
    };

    public AdvancedTemplateDesignerViewModel(
        ITemplateService templateService,
        ITemplateProcessor templateProcessor,
        IColorSchemeService colorSchemeService,
        ILayoutTemplateService layoutTemplateService,
        INavigationService navigationService)
    {
        _templateService = templateService;
        _templateProcessor = templateProcessor;
        _colorSchemeService = colorSchemeService;
        _layoutTemplateService = layoutTemplateService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task LoadTemplateAsync()
    {
        if (TemplateId <= 0) return;

        try
        {
            IsLoading = true;

            CurrentTemplate = await _templateService.GetTemplateByIdAsync(TemplateId);
            if (CurrentTemplate == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Template not found", "OK");
                await _navigationService.GoBackAsync();
                return;
            }

            await LoadDesignerDataAsync();
            await LoadColorSchemesAsync();
            await LoadLayoutTemplatesAsync();
            await LoadTemplateColumnsAsync();
            await GeneratePreviewDataAsync();
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
    public async Task AddColumnAsync()
    {
        var newColumn = new TemplateColumn
        {
            Id = DesignerColumns.Count + 1,
            DisplayName = "New Column",
            DataType = "Text",
            IsVisible = true,
            SortOrder = DesignerColumns.Count,
            Width = "100",
            TextAlignment = "Left",
            TemplateId = CurrentTemplate.Id
        };

        DesignerColumns.Add(newColumn);
        SelectedColumn = newColumn;
        await UpdateTemplateColumnsAsync();
    }

    [RelayCommand]
    public async Task RemoveColumnAsync(TemplateColumn column)
    {
        if (column != null)
        {
            DesignerColumns.Remove(column);
            await UpdateTemplateColumnsAsync();
        }
    }

    [RelayCommand]
    public async Task DuplicateColumnAsync(TemplateColumn column)
    {
        if (column != null)
        {
            var duplicated = new TemplateColumn
            {
                Id = DesignerColumns.Count + 1,
                DisplayName = $"{column.DisplayName} Copy",
                DataType = column.DataType,
                FormatString = column.FormatString,
                IsVisible = column.IsVisible,
                AllowSorting = column.AllowSorting,
                AllowFiltering = column.AllowFiltering,
                SortOrder = DesignerColumns.Count,
                Width = column.Width,
                TextAlignment = column.TextAlignment,
                TextColor = column.TextColor,
                BackgroundColor = column.BackgroundColor,
                FontWeight = column.FontWeight,
                TemplateId = CurrentTemplate.Id
            };

            DesignerColumns.Add(duplicated);
            await UpdateTemplateColumnsAsync();
        }
    }

    [RelayCommand]
    public async Task MoveColumnUpAsync(TemplateColumn column)
    {
        if (column != null)
        {
            var index = DesignerColumns.IndexOf(column);
            if (index > 0)
            {
                DesignerColumns.Move(index, index - 1);
                await ReorderColumnsAsync();
            }
        }
    }

    [RelayCommand]
    public async Task MoveColumnDownAsync(TemplateColumn column)
    {
        if (column != null)
        {
            var index = DesignerColumns.IndexOf(column);
            if (index < DesignerColumns.Count - 1)
            {
                DesignerColumns.Move(index, index + 1);
                await ReorderColumnsAsync();
            }
        }
    }

    [RelayCommand]
    public async Task ApplyColorSchemeAsync()
    {
        if (SelectedColorScheme != null)
        {
            CurrentTemplate.ColorSchemeId = SelectedColorScheme.Id;

            // Apply colors to all columns
            foreach (var column in DesignerColumns)
            {
                column.TextColor = SelectedColorScheme.TextColor;
                column.BackgroundColor = SelectedColorScheme.BackgroundColor;
            }

            await UpdateTemplateColumnsAsync();
            await GeneratePreviewDataAsync();
        }
    }

    [RelayCommand]
    public async Task ApplyLayoutTemplateAsync()
    {
        if (SelectedLayoutTemplate != null)
        {
            CurrentTemplate.LayoutTemplateId = SelectedLayoutTemplate.Id;
            await SaveTemplateAsync();
            await GeneratePreviewDataAsync();
        }
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        try
        {
            IsLoading = true;

            await UpdateTemplateColumnsAsync();
            await _templateService.UpdateTemplateAsync(CurrentTemplate);

            await Application.Current.MainPage.DisplayAlert("Success", "Template saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save template: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExportTemplateAsync()
    {
        try
        {
            var templateJson = JsonSerializer.Serialize(CurrentTemplate, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await Clipboard.Default.SetTextAsync(templateJson);
            await Application.Current.MainPage.DisplayAlert("Export Complete", "Template JSON copied to clipboard!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export template: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ImportTemplateAsync()
    {
        var templateJson = await Application.Current.MainPage.DisplayPromptAsync(
            "Import Template",
            "Paste template JSON here:",
            placeholder: "{ \"name\": \"...\", ... }");

        if (string.IsNullOrWhiteSpace(templateJson))
            return;

        try
        {
            var importedTemplate = JsonSerializer.Deserialize<Template>(templateJson);

            // Apply imported settings
            CurrentTemplate.Name = importedTemplate.Name;
            CurrentTemplate.Description = importedTemplate.Description;
            CurrentTemplate.ColorSchemeId = importedTemplate.ColorSchemeId;
            CurrentTemplate.LayoutTemplateId = importedTemplate.LayoutTemplateId;

            await LoadTemplateColumnsAsync();
            await GeneratePreviewDataAsync();

            await Application.Current.MainPage.DisplayAlert("Success", "Template imported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to import template: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task TogglePreviewModeAsync()
    {
        IsPreviewMode = !IsPreviewMode;
        if (IsPreviewMode)
        {
            await GeneratePreviewDataAsync();
        }
    }

    [RelayCommand]
    public async Task GeneratePreviewDataAsync()
    {
        try
        {
            // Generate sample data for preview
            var sampleData = new List<Dictionary<string, object>>();

            for (int i = 1; i <= 5; i++)
            {
                var item = new Dictionary<string, object>();
                foreach (var column in DesignerColumns.Where(c => c.IsVisible))
                {
                    item[column.Name] = GenerateSampleValue(column.DataType, i);
                }
                sampleData.Add(item);
            }

            PreviewData = JsonSerializer.Serialize(sampleData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            PreviewData = $"Error generating preview: {ex.Message}";
        }
    }

    private async Task LoadDesignerDataAsync()
    {
        // Load template configuration data
        if (!string.IsNullOrEmpty(CurrentTemplate.Configuration))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(CurrentTemplate.Configuration);
                // Apply configuration settings
            }
            catch
            {
                // If parsing fails, use defaults
            }
        }
    }

    private async Task LoadColorSchemesAsync()
    {
        var schemes = await _colorSchemeService.GetAllColorSchemesAsync();
        ColorSchemes.Clear();
        foreach (var scheme in schemes)
        {
            ColorSchemes.Add(scheme);
        }

        if (CurrentTemplate.ColorSchemeId.HasValue)
        {
            SelectedColorScheme = ColorSchemes.FirstOrDefault(c => c.Id == CurrentTemplate.ColorSchemeId.Value);
        }
    }

    private async Task LoadLayoutTemplatesAsync()
    {
        var layouts = await _layoutTemplateService.GetAllLayoutTemplatesAsync();
        LayoutTemplates.Clear();
        foreach (var layout in layouts)
        {
            LayoutTemplates.Add(layout);
        }

        if (CurrentTemplate.LayoutTemplateId.HasValue)
        {
            SelectedLayoutTemplate = LayoutTemplates.FirstOrDefault(l => l.Id == CurrentTemplate.LayoutTemplateId.Value);
        }
    }

    private async Task LoadTemplateColumnsAsync()
    {
        var columns = await _templateService.GetTemplateColumnsAsync(CurrentTemplate.Id);
        DesignerColumns.Clear();
        foreach (var column in columns.OrderBy(c => c.Order))
        {
            DesignerColumns.Add(column);
        }
    }

    private async Task UpdateTemplateColumnsAsync()
    {
        try
        {
            var columnsJson = JsonSerializer.Serialize(DesignerColumns);
            CurrentTemplate.ColumnsConfiguration = columnsJson;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update columns: {ex.Message}", "OK");
        }
    }

    private async Task ReorderColumnsAsync()
    {
        for (int i = 0; i < DesignerColumns.Count; i++)
        {
            DesignerColumns[i].SortOrder = i;
        }
        await UpdateTemplateColumnsAsync();
    }

    private object GenerateSampleValue(string dataType, int index)
    {
        return dataType switch
        {
            "Text" => $"Sample Text {index}",
            "Number" => index * 10,
            "Date" => DateTime.Now.AddDays(index).ToString("yyyy-MM-dd"),
            "Boolean" => index % 2 == 0,
            "Currency" => (index * 25.99m).ToString("C"),
            "Percentage" => $"{index * 10}%",
            "Image" => $"https://via.placeholder.com/50x50?text={index}",
            "Link" => $"https://example.com/item/{index}",
            "JSON" => $"{{\"id\": {index}, \"value\": \"data{index}\"}}",
            _ => $"Value {index}"
        };
    }

    partial void OnTemplateIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadTemplateAsync());
        }
    }

    partial void OnSelectedColumnChanged(TemplateColumn value)
    {
        // Update property panels when column selection changes
    }

    partial void OnDesignerModeChanged(string value)
    {
        if (value == "Code")
        {
            TemplateCode = JsonSerializer.Serialize(CurrentTemplate, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        else if (value == "Preview")
        {
            Task.Run(async () => await GeneratePreviewDataAsync());
        }
    }
}