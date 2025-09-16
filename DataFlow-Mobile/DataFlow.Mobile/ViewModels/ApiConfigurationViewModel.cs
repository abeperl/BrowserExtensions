using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DataFlow.Mobile.ViewModels;

[QueryProperty(nameof(PageId), "PageId")]
public partial class ApiConfigurationViewModel : ObservableObject
{
    private readonly IPageService _pageService;
    private readonly IApiService _apiService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Models.Page _currentPage = new();

    [ObservableProperty]
    private ObservableCollection<HttpHeader> _requestHeaders = new();

    [ObservableProperty]
    private ObservableCollection<QueryParameter> _queryParameters = new();

    [ObservableProperty]
    private ObservableCollection<string> _httpMethods = new() { "GET", "POST", "PUT", "DELETE", "PATCH" };

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _testSuccessful;

    [ObservableProperty]
    private string _apiResponse = string.Empty;

    [ObservableProperty]
    private int _responseTime;

    [ObservableProperty]
    private int _pageId;

    [ObservableProperty]
    private string _selectedContentType = "application/json";

    [ObservableProperty]
    private ObservableCollection<string> _contentTypes = new()
    {
        "application/json",
        "application/xml",
        "text/plain",
        "application/x-www-form-urlencoded",
        "multipart/form-data"
    };

    public ApiConfigurationViewModel(
        IPageService pageService,
        IApiService apiService,
        INavigationService navigationService)
    {
        _pageService = pageService;
        _apiService = apiService;
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

            // Parse existing headers
            await LoadRequestHeadersAsync();

            // Parse existing parameters
            await LoadQueryParametersAsync();
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
    public async Task AddHeaderAsync()
    {
        RequestHeaders.Add(new HttpHeader
        {
            Name = string.Empty,
            Value = string.Empty,
            IsEnabled = true
        });
    }

    [RelayCommand]
    public async Task RemoveHeaderAsync(HttpHeader header)
    {
        if (header != null)
        {
            RequestHeaders.Remove(header);
            await UpdateHeadersJsonAsync();
        }
    }

    [RelayCommand]
    public async Task AddParameterAsync()
    {
        QueryParameters.Add(new QueryParameter
        {
            Name = string.Empty,
            Value = string.Empty,
            IsEnabled = true
        });
    }

    [RelayCommand]
    public async Task RemoveParameterAsync(QueryParameter parameter)
    {
        if (parameter != null)
        {
            QueryParameters.Remove(parameter);
            await UpdateParametersJsonAsync();
        }
    }

    [RelayCommand]
    public async Task TestApiAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPage.ApiEndpoint))
        {
            TestResult = "Please enter an API endpoint URL";
            TestSuccessful = false;
            return;
        }

        try
        {
            IsTesting = true;
            TestResult = "Testing API connection...";

            var startTime = DateTime.Now;

            // Update page configuration with current values
            await UpdateHeadersJsonAsync();
            await UpdateParametersJsonAsync();

            var response = await _apiService.GetDataAsync(CurrentPage);

            var endTime = DateTime.Now;
            ResponseTime = (int)(endTime - startTime).TotalMilliseconds;

            if (response.IsSuccess)
            {
                TestSuccessful = true;
                TestResult = $"✅ Success! Response received in {ResponseTime}ms";

                if (response.Data.HasValue)
                {
                    ApiResponse = JsonSerializer.Serialize(response.Data.Value, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
            }
            else
            {
                TestSuccessful = false;
                TestResult = $"❌ Failed: {response.ErrorMessage}";
                ApiResponse = response.ErrorMessage ?? "Unknown error";
            }
        }
        catch (Exception ex)
        {
            TestSuccessful = false;
            TestResult = $"❌ Error: {ex.Message}";
            ApiResponse = ex.ToString();
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    public async Task SaveConfigurationAsync()
    {
        try
        {
            IsLoading = true;

            await UpdateHeadersJsonAsync();
            await UpdateParametersJsonAsync();

            CurrentPage.UpdatedAt = DateTime.UtcNow;
            await _pageService.UpdatePageAsync(CurrentPage);

            await Application.Current.MainPage.DisplayAlert("Success", "API configuration saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save configuration: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ImportFromCurlAsync()
    {
        var curlCommand = await Application.Current.MainPage.DisplayPromptAsync(
            "Import from cURL",
            "Paste your cURL command here:",
            placeholder: "curl -X GET \"https://api.example.com/data\" -H \"Authorization: Bearer token\"");

        if (string.IsNullOrWhiteSpace(curlCommand))
            return;

        try
        {
            var parsedConfig = ParseCurlCommand(curlCommand);

            CurrentPage.ApiEndpoint = parsedConfig.Url;
            CurrentPage.ApiMethod = parsedConfig.Method;

            // Update headers
            RequestHeaders.Clear();
            foreach (var header in parsedConfig.Headers)
            {
                RequestHeaders.Add(header);
            }

            // Update parameters
            QueryParameters.Clear();
            foreach (var param in parsedConfig.Parameters)
            {
                QueryParameters.Add(param);
            }

            await UpdateHeadersJsonAsync();
            await UpdateParametersJsonAsync();

            await Application.Current.MainPage.DisplayAlert("Success", "cURL command imported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to parse cURL command: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ExportToCurlAsync()
    {
        try
        {
            var curlCommand = GenerateCurlCommand();
            await Clipboard.Default.SetTextAsync(curlCommand);

            await Application.Current.MainPage.DisplayAlert(
                "Export Complete",
                "cURL command copied to clipboard!",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export to cURL: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ValidateJsonAsync(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            return;

        try
        {
            JsonSerializer.Deserialize<JsonElement>(jsonText);
            // If we get here, JSON is valid
        }
        catch (JsonException ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Invalid JSON",
                $"JSON syntax error: {ex.Message}",
                "OK");
        }
    }

    private async Task LoadRequestHeadersAsync()
    {
        RequestHeaders.Clear();

        if (!string.IsNullOrEmpty(CurrentPage.RequestHeaders))
        {
            try
            {
                var headersDict = JsonSerializer.Deserialize<Dictionary<string, string>>(CurrentPage.RequestHeaders);
                foreach (var header in headersDict)
                {
                    RequestHeaders.Add(new HttpHeader
                    {
                        Name = header.Key,
                        Value = header.Value,
                        IsEnabled = true
                    });
                }
            }
            catch
            {
                // If parsing fails, add default headers
                AddDefaultHeaders();
            }
        }
        else
        {
            AddDefaultHeaders();
        }
    }

    private async Task LoadQueryParametersAsync()
    {
        QueryParameters.Clear();

        if (!string.IsNullOrEmpty(CurrentPage.RequestParameters))
        {
            try
            {
                var paramsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(CurrentPage.RequestParameters);
                foreach (var param in paramsDict)
                {
                    QueryParameters.Add(new QueryParameter
                    {
                        Name = param.Key,
                        Value = param.Value?.ToString() ?? string.Empty,
                        IsEnabled = true
                    });
                }
            }
            catch
            {
                // If parsing fails, leave empty
            }
        }
    }

    private void AddDefaultHeaders()
    {
        RequestHeaders.Add(new HttpHeader { Name = "Content-Type", Value = SelectedContentType, IsEnabled = true });
        RequestHeaders.Add(new HttpHeader { Name = "Accept", Value = "application/json", IsEnabled = true });
        RequestHeaders.Add(new HttpHeader { Name = "User-Agent", Value = "DataFlow-Mobile/1.0", IsEnabled = true });
    }

    private async Task UpdateHeadersJsonAsync()
    {
        var enabledHeaders = RequestHeaders.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Name));
        var headersDict = enabledHeaders.ToDictionary(h => h.Name, h => h.Value);
        CurrentPage.RequestHeaders = JsonSerializer.Serialize(headersDict);
    }

    private async Task UpdateParametersJsonAsync()
    {
        var enabledParams = QueryParameters.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Name));
        var paramsDict = enabledParams.ToDictionary(p => p.Name, p => p.Value);
        CurrentPage.RequestParameters = JsonSerializer.Serialize(paramsDict);
    }

    private CurlParseResult ParseCurlCommand(string curlCommand)
    {
        var result = new CurlParseResult();

        // Basic parsing - can be enhanced
        var parts = curlCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim('\"');

            if (part == "-X" && i + 1 < parts.Length)
            {
                result.Method = parts[i + 1].Trim('\"');
                i++;
            }
            else if (part == "-H" && i + 1 < parts.Length)
            {
                var header = parts[i + 1].Trim('\"');
                var colonIndex = header.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = header.Substring(0, colonIndex).Trim();
                    var value = header.Substring(colonIndex + 1).Trim();
                    result.Headers.Add(new HttpHeader { Name = name, Value = value, IsEnabled = true });
                }
                i++;
            }
            else if (part.StartsWith("http"))
            {
                var urlParts = part.Split('?');
                result.Url = urlParts[0];

                if (urlParts.Length > 1)
                {
                    var queryString = urlParts[1];
                    var paramPairs = queryString.Split('&');
                    foreach (var pair in paramPairs)
                    {
                        var keyValue = pair.Split('=');
                        if (keyValue.Length == 2)
                        {
                            result.Parameters.Add(new QueryParameter
                            {
                                Name = Uri.UnescapeDataString(keyValue[0]),
                                Value = Uri.UnescapeDataString(keyValue[1]),
                                IsEnabled = true
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    private string GenerateCurlCommand()
    {
        var curl = $"curl -X {CurrentPage.ApiMethod}";

        // Add URL
        var url = CurrentPage.ApiEndpoint;
        if (QueryParameters.Any(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Name)))
        {
            var queryString = string.Join("&",
                QueryParameters.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Name))
                              .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value)}"));
            url += "?" + queryString;
        }
        curl += $" \"{url}\"";

        // Add headers
        foreach (var header in RequestHeaders.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Name)))
        {
            curl += $" -H \"{header.Name}: {header.Value}\"";
        }

        return curl;
    }

    partial void OnPageIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadPageAsync());
        }
    }

    partial void OnSelectedContentTypeChanged(string value)
    {
        var contentTypeHeader = RequestHeaders.FirstOrDefault(h => h.Name == "Content-Type");
        if (contentTypeHeader != null)
        {
            contentTypeHeader.Value = value;
        }
    }
}

public class HttpHeader : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}

public class QueryParameter : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}

public class CurlParseResult
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public List<HttpHeader> Headers { get; set; } = new();
    public List<QueryParameter> Parameters { get; set; } = new();
}