using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DataFlow.Mobile.Views.Controls;

public partial class DynamicActionView : ContentView
{
    public static readonly BindableProperty ActionsProperty =
        BindableProperty.Create(nameof(Actions), typeof(ObservableCollection<PageAction>), typeof(DynamicActionView), new ObservableCollection<PageAction>());

    public static readonly BindableProperty CurrentDataProperty =
        BindableProperty.Create(nameof(CurrentData), typeof(object), typeof(DynamicActionView), null);

    public static readonly BindableProperty PageIdProperty =
        BindableProperty.Create(nameof(PageId), typeof(int), typeof(DynamicActionView), 0);

    private readonly IActionService _actionService;
    private readonly ILogger<DynamicActionView> _logger;

    public DynamicActionView()
    {
        InitializeComponent();
        _actionService = ServiceHelper.GetService<IActionService>();
        _logger = ServiceHelper.GetService<ILogger<DynamicActionView>>();

        BindingContext = new DynamicActionViewModel(_actionService, _logger);

        // Subscribe to property changes
        PropertyChanged += OnPropertyChanged;
    }

    public ObservableCollection<PageAction> Actions
    {
        get => (ObservableCollection<PageAction>)GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    public object CurrentData
    {
        get => GetValue(CurrentDataProperty);
        set => SetValue(CurrentDataProperty, value);
    }

    public int PageId
    {
        get => (int)GetValue(PageIdProperty);
        set => SetValue(PageIdProperty, value);
    }

    private async void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PageId) && PageId > 0)
        {
            await LoadActionsAsync();
        }
    }

    private async Task LoadActionsAsync()
    {
        try
        {
            var actions = await _actionService.GetActionsByPageIdAsync(PageId);
            Actions.Clear();

            foreach (var action in actions)
            {
                Actions.Add(action);
            }

            if (BindingContext is DynamicActionViewModel viewModel)
            {
                viewModel.Actions = Actions;
                viewModel.CurrentData = CurrentData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading actions for page: {PageId}", PageId);
        }
    }

    public async Task RefreshActionsAsync()
    {
        await LoadActionsAsync();
    }
}

public class DynamicActionViewModel : BaseViewModel
{
    private readonly IActionService _actionService;
    private readonly ILogger _logger;
    private ObservableCollection<PageAction> _actions = new();
    private object? _currentData;
    private bool _isExecutingAction;
    private string _executionStatus = string.Empty;
    private ActionResult? _actionResult;

    public DynamicActionViewModel(IActionService actionService, ILogger logger)
    {
        _actionService = actionService;
        _logger = logger;
        ExecuteActionCommand = new Command<PageAction>(async action => await ExecuteActionAsync(action));
    }

    public ObservableCollection<PageAction> Actions
    {
        get => _actions;
        set => SetProperty(ref _actions, value);
    }

    public object? CurrentData
    {
        get => _currentData;
        set => SetProperty(ref _currentData, value);
    }

    public bool IsExecutingAction
    {
        get => _isExecutingAction;
        set => SetProperty(ref _isExecutingAction, value);
    }

    public string ExecutionStatus
    {
        get => _executionStatus;
        set => SetProperty(ref _executionStatus, value);
    }

    public ActionResult? ActionResult
    {
        get => _actionResult;
        set => SetProperty(ref _actionResult, value);
    }

    public ICommand ExecuteActionCommand { get; }

    private async Task ExecuteActionAsync(PageAction action)
    {
        if (action == null || IsExecutingAction) return;

        try
        {
            IsExecutingAction = true;
            ExecutionStatus = $"Executing {action.Name}...";
            ActionResult = null;

            // Prepare action data based on action type
            var actionData = PrepareActionData(action);

            // Execute the action
            var result = await _actionService.ExecuteActionAsync(action.Id, actionData);

            // Handle the result
            ActionResult = result;
            ExecutionStatus = result.IsSuccess ? "Action completed successfully" : "Action failed";

            // Handle navigation if needed
            if (result.IsSuccess && action.ActionType.Equals("Navigation", StringComparison.OrdinalIgnoreCase))
            {
                await HandleNavigationAsync(result.Data?.ToString() ?? action.NavigationTarget);
            }

            // Auto-hide result after delay
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ActionResult = null;
                    ExecutionStatus = string.Empty;
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action: {ActionName}", action.Name);
            ActionResult = DataFlow.Mobile.Models.ActionResult.Error($"Execution failed: {ex.Message}");
            ExecutionStatus = "Action failed";
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    private object PrepareActionData(PageAction action)
    {
        var actionData = new Dictionary<string, object>();

        // Add current data context
        if (CurrentData != null)
        {
            actionData["contextData"] = CurrentData;
        }

        // Add action-specific data based on type
        switch (action.ActionType.ToLower())
        {
            case "input":
                actionData["inputValue"] = action.InputValue ?? string.Empty;
                break;

            case "dropdown":
                actionData["selectedValue"] = action.SelectedValue ?? string.Empty;
                break;

            case "toggle":
                actionData["toggleValue"] = action.ToggleValue.ToString();
                break;

            case "multiselect":
                actionData["selectedItems"] = action.SelectedItems?.ToList() ?? new List<object>();
                break;
        }

        return actionData;
    }

    private async Task HandleNavigationAsync(string? navigationTarget)
    {
        try
        {
            if (string.IsNullOrEmpty(navigationTarget)) return;

            // Handle different navigation types
            if (navigationTarget.StartsWith("http"))
            {
                // External URL
                await Browser.OpenAsync(navigationTarget);
            }
            else
            {
                // Internal navigation
                await Shell.Current.GoToAsync(navigationTarget);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling navigation to: {Target}", navigationTarget);
        }
    }
}

// Helper class to get services from the DI container
public static class ServiceHelper
{
    public static T GetService<T>() where T : class
    {
        // Get service from MAUI's built-in service provider
        return Application.Current?.Handler?.MauiContext?.Services.GetService<T>()
               ?? throw new InvalidOperationException($"Unable to resolve service {typeof(T).Name}");
    }
}