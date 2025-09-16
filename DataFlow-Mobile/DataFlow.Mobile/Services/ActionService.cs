using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataFlow.Mobile.Services;

public class ActionService : IActionService
{
    private readonly DataFlowDbContext _context;
    private readonly ILogger<ActionService> _logger;
    private readonly IApiService _apiService;
    private readonly IAudioService _audioService;

    public ActionService(
        DataFlowDbContext context,
        ILogger<ActionService> logger,
        IApiService apiService,
        IAudioService audioService)
    {
        _context = context;
        _logger = logger;
        _apiService = apiService;
        _audioService = audioService;
    }

    public async Task<IEnumerable<PageAction>> GetActionsByPageIdAsync(int pageId)
    {
        try
        {
            return await _context.Actions
                .Where(a => a.PageId == pageId && a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting actions for page: {PageId}", pageId);
            return [];
        }
    }

    public async Task<PageAction?> GetActionByIdAsync(int id)
    {
        try
        {
            return await _context.Actions.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting action by ID: {ActionId}", id);
            return null;
        }
    }

    public async Task<PageAction> CreateActionAsync(PageAction action)
    {
        try
        {
            action.CreatedAt = DateTime.UtcNow;
            action.UpdatedAt = DateTime.UtcNow;

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new action: {ActionName} (ID: {ActionId})", action.Name, action.Id);
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating action: {ActionName}", action.Name);
            throw;
        }
    }

    public async Task<PageAction> UpdateActionAsync(PageAction action)
    {
        try
        {
            action.UpdatedAt = DateTime.UtcNow;
            _context.Entry(action).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated action: {ActionName} (ID: {ActionId})", action.Name, action.Id);
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action: {ActionId}", action.Id);
            throw;
        }
    }

    public async Task<bool> DeleteActionAsync(int id)
    {
        try
        {
            var action = await _context.Actions.FindAsync(id);
            if (action == null)
                return false;

            action.IsActive = false;
            action.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted action: {ActionId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action: {ActionId}", id);
            return false;
        }
    }

    public async Task<ActionResult> ExecuteActionAsync(int actionId, object? data = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var action = await _context.Actions
                .Include(a => a.Page)
                .FirstOrDefaultAsync(a => a.Id == actionId && a.IsActive);

            if (action == null)
            {
                return ActionResult.Error("Action not found or inactive");
            }

            _logger.LogInformation("Executing action: {ActionName} (ID: {ActionId}) of type: {ActionType}",
                action.Name, actionId, action.ActionType);

            // Evaluate conditions before executing
            if (!await EvaluateActionConditionsAsync(action, data))
            {
                _logger.LogInformation("Action conditions not met for action: {ActionId}", actionId);
                return ActionResult.Error("Action conditions not met");
            }

            // Execute action based on type
            var result = action.ActionType.ToLower() switch
            {
                "button" => await ExecuteButtonActionAsync(action, data),
                "dropdown" => await ExecuteDropdownActionAsync(action, data),
                "input" => await ExecuteInputActionAsync(action, data),
                "toggle" => await ExecuteToggleActionAsync(action, data),
                "multiselect" => await ExecuteMultiSelectActionAsync(action, data),
                "navigation" => await ExecuteNavigationActionAsync(action, data),
                "api_call" => await ExecuteApiCallActionAsync(action, data),
                _ => ActionResult.Error($"Unknown action type: {action.ActionType}")
            };

            // Play audio feedback if configured
            if (result.IsSuccess && !string.IsNullOrEmpty(action.SuccessSound))
            {
                await _audioService.PlaySoundAsync(action.SuccessSound);
            }
            else if (!result.IsSuccess && !string.IsNullOrEmpty(action.ErrorSound))
            {
                await _audioService.PlaySoundAsync(action.ErrorSound);
            }

            // Log execution result
            var executionTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("Action {ActionId} completed in {ExecutionTime}ms with result: {IsSuccess}",
                actionId, executionTime.TotalMilliseconds, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action: {ActionId}", actionId);
            return ActionResult.Error($"Action execution failed: {ex.Message}");
        }
    }

    private async Task<bool> EvaluateActionConditionsAsync(PageAction action, object? data)
    {
        try
        {
            if (string.IsNullOrEmpty(action.Conditions))
                return true;

            var conditions = JsonSerializer.Deserialize<List<ActionCondition>>(action.Conditions);
            if (conditions == null || !conditions.Any())
                return true;

            foreach (var condition in conditions)
            {
                if (!await EvaluateConditionAsync(condition, data))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating action conditions for action: {ActionId}", action.Id);
            return false;
        }
    }

    private async Task<bool> EvaluateConditionAsync(ActionCondition condition, object? data)
    {
        try
        {
            if (data == null) return true;

            var dataJson = JsonSerializer.Serialize(data);
            var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);

            var fieldValue = ExtractFieldValue(dataElement, condition.Field);

            return condition.Operator.ToLower() switch
            {
                "equals" => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                "not_equals" => !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                "contains" => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                "greater" => CompareNumbers(fieldValue, condition.Value) > 0,
                "less" => CompareNumbers(fieldValue, condition.Value) < 0,
                "empty" => string.IsNullOrEmpty(fieldValue),
                "not_empty" => !string.IsNullOrEmpty(fieldValue),
                _ => true
            };
        }
        catch
        {
            return false;
        }
    }

    private async Task<ActionResult> ExecuteButtonActionAsync(PageAction action, object? data)
    {
        try
        {
            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Success("Button clicked successfully");
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var response = await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload);

            if (response.IsSuccess)
            {
                return ActionResult.Success("Button action completed successfully", response.Data);
            }
            else
            {
                return ActionResult.Error($"API call failed: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Button action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteDropdownActionAsync(PageAction action, object? data)
    {
        try
        {
            var selectedValue = ExtractSelectedValue(data, "selectedValue");

            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Success($"Dropdown selection: {selectedValue}", selectedValue);
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var response = await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload);

            return response.IsSuccess
                ? ActionResult.Success("Dropdown action completed successfully", response.Data)
                : ActionResult.Error($"API call failed: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Dropdown action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteInputActionAsync(PageAction action, object? data)
    {
        try
        {
            var inputValue = ExtractSelectedValue(data, "inputValue");

            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Success($"Input submitted: {inputValue}", inputValue);
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var response = await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload);

            return response.IsSuccess
                ? ActionResult.Success("Input action completed successfully", response.Data)
                : ActionResult.Error($"API call failed: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Input action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteToggleActionAsync(PageAction action, object? data)
    {
        try
        {
            var toggleValue = ExtractSelectedValue(data, "toggleValue");
            var isEnabled = bool.TryParse(toggleValue, out var boolValue) && boolValue;

            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Success($"Toggle {(isEnabled ? "enabled" : "disabled")}", isEnabled);
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var response = await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload);

            return response.IsSuccess
                ? ActionResult.Success("Toggle action completed successfully", response.Data)
                : ActionResult.Error($"API call failed: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Toggle action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteMultiSelectActionAsync(PageAction action, object? data)
    {
        try
        {
            var selectedItems = ExtractSelectedItems(data);

            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Success($"Selected {selectedItems.Count} items", selectedItems);
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var response = await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload);

            return response.IsSuccess
                ? ActionResult.Success("Multi-select action completed successfully", response.Data)
                : ActionResult.Error($"API call failed: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Multi-select action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteNavigationActionAsync(PageAction action, object? data)
    {
        try
        {
            if (string.IsNullOrEmpty(action.NavigationTarget))
            {
                return ActionResult.Error("Navigation target not specified");
            }

            // Navigation will be handled by the UI layer
            return ActionResult.Success($"Navigate to: {action.NavigationTarget}", action.NavigationTarget);
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Navigation action failed: {ex.Message}");
        }
    }

    private async Task<ActionResult> ExecuteApiCallActionAsync(PageAction action, object? data)
    {
        try
        {
            if (string.IsNullOrEmpty(action.ApiEndpoint))
            {
                return ActionResult.Error("API endpoint not specified");
            }

            var payload = await GenerateActionPayloadAsync(action, data);
            var httpMethod = action.HttpMethod?.ToUpper() ?? "POST";

            var response = httpMethod switch
            {
                "GET" => await _apiService.GetRawAsync<object>(action.ApiEndpoint),
                "POST" => await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload),
                "PUT" => await _apiService.PutAsync<object>(action.ApiEndpoint, payload),
                "DELETE" => await _apiService.DeleteAsync<object>(action.ApiEndpoint),
                _ => await _apiService.PostRawAsync<object>(action.ApiEndpoint, payload)
            };

            return response.IsSuccess
                ? ActionResult.Success("API call completed successfully", response.Data)
                : ActionResult.Error($"API call failed: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"API call action failed: {ex.Message}");
        }
    }

    private async Task<object> GenerateActionPayloadAsync(PageAction action, object? data)
    {
        try
        {
            var payload = new Dictionary<string, object>();

            // Add base data if provided
            if (data != null)
            {
                var dataJson = JsonSerializer.Serialize(data);
                var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);

                if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        payload[property.Name] = property.Value;
                    }
                }
            }

            // Add action parameters
            if (!string.IsNullOrEmpty(action.Parameters))
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(action.Parameters);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        payload[param.Key] = await ProcessParameterValueAsync(param.Value?.ToString() ?? "", data);
                    }
                }
            }

            // Add metadata
            payload["_action_id"] = action.Id;
            payload["_action_name"] = action.Name;
            payload["_timestamp"] = DateTime.UtcNow;

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating action payload for action: {ActionId}", action.Id);
            return new { error = "Failed to generate payload" };
        }
    }

    private async Task<object> ProcessParameterValueAsync(string parameterValue, object? data)
    {
        try
        {
            // Process variable substitutions like {{field_name}}
            var result = parameterValue;
            var matches = Regex.Matches(parameterValue, @"\{\{([^}]+)\}\}");

            foreach (Match match in matches)
            {
                var fieldName = match.Groups[1].Value;
                var fieldValue = ExtractFieldValue(data, fieldName);
                result = result.Replace(match.Value, fieldValue);
            }

            return result;
        }
        catch
        {
            return parameterValue;
        }
    }

    private string ExtractFieldValue(object? data, string fieldPath)
    {
        try
        {
            if (data == null) return string.Empty;

            var dataJson = JsonSerializer.Serialize(data);
            var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);

            var parts = fieldPath.Split('.');
            var current = dataElement;

            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return string.Empty;
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString() ?? string.Empty,
                JsonValueKind.Number => current.GetDecimal().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => current.ToString()
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractSelectedValue(object? data, string key)
    {
        try
        {
            if (data == null) return string.Empty;
            return ExtractFieldValue(data, key);
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<object> ExtractSelectedItems(object? data)
    {
        try
        {
            if (data == null) return [];

            var selectedItems = ExtractFieldValue(data, "selectedItems");
            if (string.IsNullOrEmpty(selectedItems)) return [];

            return JsonSerializer.Deserialize<List<object>>(selectedItems) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private int CompareNumbers(string value1, string value2)
    {
        if (decimal.TryParse(value1, out var num1) && decimal.TryParse(value2, out var num2))
        {
            return num1.CompareTo(num2);
        }
        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<string>> GetAvailableActionTypesAsync()
    {
        return await Task.FromResult(new[]
        {
            "Button",
            "Dropdown",
            "Input",
            "Toggle",
            "MultiSelect",
            "Navigation",
            "API_Call"
        });
    }

    public async Task<ActionResult> ValidateActionAsync(PageAction action, object? sampleData = null)
    {
        try
        {
            var validationErrors = new List<string>();

            // Basic validation
            if (string.IsNullOrEmpty(action.Name))
                validationErrors.Add("Action name is required");

            if (string.IsNullOrEmpty(action.ActionType))
                validationErrors.Add("Action type is required");

            // Type-specific validation
            switch (action.ActionType.ToLower())
            {
                case "api_call":
                    if (string.IsNullOrEmpty(action.ApiEndpoint))
                        validationErrors.Add("API endpoint is required for API call actions");
                    break;

                case "navigation":
                    if (string.IsNullOrEmpty(action.NavigationTarget))
                        validationErrors.Add("Navigation target is required for navigation actions");
                    break;
            }

            // Validate JSON fields
            if (!string.IsNullOrEmpty(action.Parameters))
            {
                try
                {
                    JsonSerializer.Deserialize<Dictionary<string, object>>(action.Parameters);
                }
                catch
                {
                    validationErrors.Add("Invalid JSON format in parameters");
                }
            }

            if (!string.IsNullOrEmpty(action.Conditions))
            {
                try
                {
                    JsonSerializer.Deserialize<List<ActionCondition>>(action.Conditions);
                }
                catch
                {
                    validationErrors.Add("Invalid JSON format in conditions");
                }
            }

            return validationErrors.Any()
                ? ActionResult.Error($"Validation failed: {string.Join(", ", validationErrors)}")
                : ActionResult.Success("Action validation passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating action: {ActionId}", action.Id);
            return ActionResult.Error($"Validation error: {ex.Message}");
        }
    }

    private class ActionCondition
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}