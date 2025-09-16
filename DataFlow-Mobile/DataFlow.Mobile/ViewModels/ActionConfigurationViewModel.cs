using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(PageId), "PageId")]
public partial class ActionConfigurationViewModel : ObservableObject
{
    private readonly IActionService _actionService;
    private readonly IPageService _pageService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Models.Page _currentPage = new();

    [ObservableProperty]
    private ObservableCollection<Models.Action> _pageActions = new();

    [ObservableProperty]
    private ObservableCollection<ActionTrigger> _actionTriggers = new();

    [ObservableProperty]
    private Models.Action _selectedAction = new();

    [ObservableProperty]
    private ActionTrigger _selectedTrigger = new();

    [ObservableProperty]
    private ObservableCollection<string> _actionTypes = new()
    {
        "Navigation", "API Call", "Data Export", "Notification", "Custom Script", "Email", "SMS", "File Download"
    };

    [ObservableProperty]
    private ObservableCollection<string> _triggerTypes = new()
    {
        "Button Click", "Row Selection", "Data Loaded", "Refresh Complete", "Timer", "Gesture", "Voice Command"
    };

    [ObservableProperty]
    private ObservableCollection<string> _navigationTargets = new()
    {
        "External URL", "Other Page", "Modal Dialog", "Bottom Sheet", "Tab Switch"
    };

    [ObservableProperty]
    private ObservableCollection<string> _exportFormats = new()
    {
        "JSON", "CSV", "Excel", "PDF", "XML"
    };

    [ObservableProperty]
    private ObservableCollection<ActionParameter> _actionParameters = new();

    [ObservableProperty]
    private ObservableCollection<ActionCondition> _actionConditions = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isTestingAction;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private int _pageId;

    [ObservableProperty]
    private string _actionScript = string.Empty;

    [ObservableProperty]
    private bool _isAdvancedMode = false;

    public ActionConfigurationViewModel(
        IActionService actionService,
        IPageService pageService,
        INavigationService navigationService)
    {
        _actionService = actionService;
        _pageService = pageService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task LoadPageAsync()
    {
        if (PageId <= 0) return;

        try
        {
            IsLoading = true;

            CurrentPage = await _pageService.GetPageByIdAsync(PageId);
            if (CurrentPage == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Page not found", "OK");
                await _navigationService.GoBackAsync();
                return;
            }

            await LoadPageActionsAsync();
            await LoadActionTriggersAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load page: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddActionAsync()
    {
        var newAction = new Models.Action
        {
            Id = PageActions.Count + 1,
            Name = "New Action",
            Type = "Button Click",
            PageId = CurrentPage.Id,
            IsEnabled = true,
            Order = PageActions.Count,
            CreatedAt = DateTime.UtcNow
        };

        PageActions.Add(newAction);
        SelectedAction = newAction;
        await SaveActionsAsync();
    }

    [RelayCommand]
    public async Task RemoveActionAsync(Models.Action action)
    {
        if (action != null)
        {
            var result = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete",
                $"Are you sure you want to delete the action '{action.Name}'?",
                "Delete", "Cancel");

            if (result)
            {
                PageActions.Remove(action);
                await SaveActionsAsync();
            }
        }
    }

    [RelayCommand]
    public async Task DuplicateActionAsync(Models.Action action)
    {
        if (action != null)
        {
            var duplicated = new Models.Action
            {
                Id = PageActions.Count + 1,
                Name = $"{action.Name} Copy",
                Type = action.Type,
                Configuration = action.Configuration,
                PageId = CurrentPage.Id,
                IsEnabled = action.IsEnabled,
                Order = PageActions.Count,
                CreatedAt = DateTime.UtcNow
            };

            PageActions.Add(duplicated);
            await SaveActionsAsync();
        }
    }

    [RelayCommand]
    public async Task AddTriggerAsync()
    {
        var newTrigger = new ActionTrigger
        {
            Id = ActionTriggers.Count + 1,
            Name = "New Trigger",
            Type = "Button Click",
            ActionId = SelectedAction?.Id ?? 0,
            IsEnabled = true,
            Order = ActionTriggers.Count
        };

        ActionTriggers.Add(newTrigger);
        SelectedTrigger = newTrigger;
    }

    [RelayCommand]
    public async Task RemoveTriggerAsync(ActionTrigger trigger)
    {
        if (trigger != null)
        {
            ActionTriggers.Remove(trigger);
        }
    }

    [RelayCommand]
    public async Task AddParameterAsync()
    {
        var newParameter = new ActionParameter
        {
            Id = ActionParameters.Count + 1,
            Name = "Parameter",
            Value = "Value",
            Type = "String",
            ActionId = SelectedAction?.Id ?? 0,
            IsRequired = false
        };

        ActionParameters.Add(newParameter);
    }

    [RelayCommand]
    public async Task RemoveParameterAsync(ActionParameter parameter)
    {
        if (parameter != null)
        {
            ActionParameters.Remove(parameter);
        }
    }

    [RelayCommand]
    public async Task AddConditionAsync()
    {
        var newCondition = new ActionCondition
        {
            Id = ActionConditions.Count + 1,
            Field = "field_name",
            Operator = "equals",
            Value = "value",
            ActionId = SelectedAction?.Id ?? 0
        };

        ActionConditions.Add(newCondition);
    }

    [RelayCommand]
    public async Task RemoveConditionAsync(ActionCondition condition)
    {
        if (condition != null)
        {
            ActionConditions.Remove(condition);
        }
    }

    [RelayCommand]
    public async Task TestActionAsync()
    {
        if (SelectedAction == null)
        {
            TestResult = "Please select an action to test";
            return;
        }

        try
        {
            IsTestingAction = true;
            TestResult = "Testing action...";

            // Simulate action execution based on type
            await Task.Delay(1000); // Simulate processing time

            switch (SelectedAction.Type)
            {
                case "Navigation":
                    TestResult = "✅ Navigation action would redirect to configured URL";
                    break;
                case "API Call":
                    TestResult = "✅ API call would be executed with configured parameters";
                    break;
                case "Data Export":
                    TestResult = "✅ Data would be exported in configured format";
                    break;
                case "Notification":
                    TestResult = "✅ Notification would be displayed to user";
                    break;
                case "Custom Script":
                    TestResult = "✅ Custom script would be executed";
                    break;
                case "Email":
                    TestResult = "✅ Email would be sent to configured recipients";
                    break;
                case "SMS":
                    TestResult = "✅ SMS would be sent to configured number";
                    break;
                case "File Download":
                    TestResult = "✅ File would be downloaded to device";
                    break;
                default:
                    TestResult = "✅ Action would be executed successfully";
                    break;
            }
        }
        catch (Exception ex)
        {
            TestResult = $"❌ Test failed: {ex.Message}";
        }
        finally
        {
            IsTestingAction = false;
        }
    }

    [RelayCommand]
    public async Task SaveActionsAsync()
    {
        try
        {
            IsLoading = true;

            // Save action parameters and conditions to configuration
            if (SelectedAction != null)
            {
                var config = new
                {
                    parameters = ActionParameters.ToList(),
                    conditions = ActionConditions.ToList(),
                    triggers = ActionTriggers.Where(t => t.ActionId == SelectedAction.Id).ToList(),
                    script = ActionScript
                };

                SelectedAction.Configuration = JsonSerializer.Serialize(config);
                SelectedAction.UpdatedAt = DateTime.UtcNow;
            }

            // Save all actions
            foreach (var action in PageActions)
            {
                if (action.Id > 0)
                {
                    await _actionService.UpdateActionAsync(action);
                }
                else
                {
                    await _actionService.CreateActionAsync(action);
                }
            }

            await Application.Current.MainPage.DisplayAlert("Success", "Actions saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save actions: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ImportActionsAsync()
    {
        var actionsJson = await Application.Current.MainPage.DisplayPromptAsync(
            "Import Actions",
            "Paste actions JSON here:",
            placeholder: "[{\"name\": \"...\", \"type\": \"...\"}]");

        if (string.IsNullOrWhiteSpace(actionsJson))
            return;

        try
        {
            var importedActions = JsonSerializer.Deserialize<List<Models.Action>>(actionsJson);

            foreach (var action in importedActions)
            {
                action.Id = 0; // Reset ID for new entries
                action.PageId = CurrentPage.Id;
                action.CreatedAt = DateTime.UtcNow;
                PageActions.Add(action);
            }

            await SaveActionsAsync();
            await Application.Current.MainPage.DisplayAlert("Success", "Actions imported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to import actions: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ExportActionsAsync()
    {
        try
        {
            var actionsJson = JsonSerializer.Serialize(PageActions, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await Clipboard.Default.SetTextAsync(actionsJson);
            await Application.Current.MainPage.DisplayAlert("Export Complete", "Actions JSON copied to clipboard!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export actions: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task GenerateActionScriptAsync()
    {
        if (SelectedAction == null) return;

        try
        {
            // Generate a basic script template based on action type
            ActionScript = SelectedAction.Type switch
            {
                "Navigation" => GenerateNavigationScript(),
                "API Call" => GenerateApiCallScript(),
                "Data Export" => GenerateDataExportScript(),
                "Notification" => GenerateNotificationScript(),
                "Custom Script" => GenerateCustomScript(),
                _ => "// Custom action script\nfunction executeAction(data, parameters) {\n    // Your code here\n    console.log('Action executed:', data);\n    return { success: true, message: 'Action completed' };\n}"
            };

            await Application.Current.MainPage.DisplayAlert("Success", "Action script template generated!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to generate script: {ex.Message}", "OK");
        }
    }

    private async Task LoadPageActionsAsync()
    {
        var actions = await _actionService.GetActionsByPageIdAsync(CurrentPage.Id);
        PageActions.Clear();
        foreach (var action in actions.OrderBy(a => a.Order))
        {
            PageActions.Add(action);
        }
    }

    private async Task LoadActionTriggersAsync()
    {
        ActionTriggers.Clear();
        ActionParameters.Clear();
        ActionConditions.Clear();

        if (SelectedAction?.Configuration != null)
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(SelectedAction.Configuration);

                if (config.ContainsKey("triggers"))
                {
                    var triggers = JsonSerializer.Deserialize<List<ActionTrigger>>(config["triggers"].ToString());
                    foreach (var trigger in triggers)
                    {
                        ActionTriggers.Add(trigger);
                    }
                }

                if (config.ContainsKey("parameters"))
                {
                    var parameters = JsonSerializer.Deserialize<List<ActionParameter>>(config["parameters"].ToString());
                    foreach (var parameter in parameters)
                    {
                        ActionParameters.Add(parameter);
                    }
                }

                if (config.ContainsKey("conditions"))
                {
                    var conditions = JsonSerializer.Deserialize<List<ActionCondition>>(config["conditions"].ToString());
                    foreach (var condition in conditions)
                    {
                        ActionConditions.Add(condition);
                    }
                }

                if (config.ContainsKey("script"))
                {
                    ActionScript = config["script"].ToString();
                }
            }
            catch
            {
                // If parsing fails, start with empty collections
            }
        }
    }

    private string GenerateNavigationScript()
    {
        return @"// Navigation Action Script
function executeNavigation(data, parameters) {
    const url = parameters.targetUrl || 'https://example.com';
    const target = parameters.target || '_blank';

    if (target === 'modal') {
        // Open in modal dialog
        showModal(url);
    } else if (target === 'current') {
        // Navigate in current window
        window.location.href = url;
    } else {
        // Open in new window/tab
        window.open(url, target);
    }

    return { success: true, message: 'Navigation executed' };
}";
    }

    private string GenerateApiCallScript()
    {
        return @"// API Call Action Script
async function executeApiCall(data, parameters) {
    const endpoint = parameters.endpoint || '';
    const method = parameters.method || 'GET';
    const headers = parameters.headers || {};

    try {
        const response = await fetch(endpoint, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                ...headers
            },
            body: method !== 'GET' ? JSON.stringify(data) : undefined
        });

        const result = await response.json();
        return { success: true, data: result };
    } catch (error) {
        return { success: false, error: error.message };
    }
}";
    }

    private string GenerateDataExportScript()
    {
        return @"// Data Export Action Script
function executeDataExport(data, parameters) {
    const format = parameters.format || 'json';
    const filename = parameters.filename || 'export';

    switch (format.toLowerCase()) {
        case 'csv':
            exportToCsv(data, filename);
            break;
        case 'json':
            exportToJson(data, filename);
            break;
        case 'excel':
            exportToExcel(data, filename);
            break;
        default:
            exportToJson(data, filename);
    }

    return { success: true, message: 'Data exported successfully' };
}";
    }

    private string GenerateNotificationScript()
    {
        return @"// Notification Action Script
function executeNotification(data, parameters) {
    const title = parameters.title || 'Notification';
    const message = parameters.message || 'Action completed';
    const type = parameters.type || 'info';

    // Show notification
    showNotification({
        title: title,
        message: message,
        type: type,
        duration: parameters.duration || 3000
    });

    return { success: true, message: 'Notification shown' };
}";
    }

    private string GenerateCustomScript()
    {
        return @"// Custom Action Script
function executeCustomAction(data, parameters) {
    // Access row data
    const rowData = data.selectedRow || {};

    // Access page parameters
    const pageParams = parameters || {};

    // Your custom logic here
    console.log('Executing custom action with data:', rowData);
    console.log('Parameters:', pageParams);

    // Example: Process data
    const result = processData(rowData, pageParams);

    // Return result
    return {
        success: true,
        message: 'Custom action completed',
        data: result
    };
}

function processData(data, params) {
    // Implement your custom data processing logic
    return data;
}";
    }

    partial void OnPageIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadPageAsync());
        }
    }

    partial void OnSelectedActionChanged(Models.Action value)
    {
        if (value != null)
        {
            Task.Run(async () => await LoadActionTriggersAsync());
        }
    }
}

// Supporting models for action configuration
public class ActionTrigger : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _configuration = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private int _actionId;
}

public class ActionParameter : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _type = "String";

    [ObservableProperty]
    private bool _isRequired = false;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _actionId;
}

public class ActionCondition : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _field = string.Empty;

    [ObservableProperty]
    private string _operator = "equals";

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _logicOperator = "AND";

    [ObservableProperty]
    private int _actionId;
}