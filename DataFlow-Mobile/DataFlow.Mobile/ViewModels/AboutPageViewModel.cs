using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace DataFlow.Mobile.ViewModels;

public partial class AboutPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _appName = "DataFlow Mobile";

    [ObservableProperty]
    private string _appVersion;

    [ObservableProperty]
    private string _buildDate;

    [ObservableProperty]
    private string _frameworkVersion;

    [ObservableProperty]
    private string _deviceInfo;

    public AboutPageViewModel()
    {
        LoadAppInfo();
    }

    private void LoadAppInfo()
    {
        try
        {
            // Get app version
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            AppVersion = $"{version?.Major}.{version?.Minor}.{version?.Build}";

            // Get build date (approximate from assembly)
            var buildDateTime = new DateTime(2000, 1, 1)
                .AddDays(version?.Build ?? 0)
                .AddSeconds((version?.Revision ?? 0) * 2);
            BuildDate = buildDateTime.ToString("MMM dd, yyyy");

            // Get framework version
            FrameworkVersion = Environment.Version.ToString();

            // Get device info
            DeviceInfo = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading app info: {ex.Message}");
            AppVersion = "1.0.0";
            BuildDate = DateTime.Now.ToString("MMM dd, yyyy");
            FrameworkVersion = ".NET 8";
            DeviceInfo = "Unknown Device";
        }
    }

    [RelayCommand]
    public async Task OpenGitHubAsync()
    {
        try
        {
            // Replace with your actual GitHub repository URL
            var uri = new Uri("https://github.com/yourusername/dataflow-mobile");
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Could not open GitHub: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task SendFeedbackAsync()
    {
        try
        {
            var subject = Uri.EscapeDataString("DataFlow Mobile Feedback");
            var body = Uri.EscapeDataString($"App Version: {AppVersion}\nDevice: {DeviceInfo}\n\nFeedback:\n");
            var emailUri = $"mailto:feedback@dataflow.com?subject={subject}&body={body}";

            await Browser.Default.OpenAsync(emailUri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Could not open email client: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    public async Task ViewLicensesAsync()
    {
        await Application.Current.MainPage.DisplayAlert(
            "Open Source Licenses",
            "• .NET MAUI - MIT License\n" +
            "• CommunityToolkit.Mvvm - MIT License\n" +
            "• CommunityToolkit.Maui - MIT License\n" +
            "• Microsoft.EntityFrameworkCore - MIT License\n" +
            "• System.Text.Json - MIT License\n\n" +
            "Full license texts available in source code.",
            "OK");
    }

    [RelayCommand]
    public async Task ViewPrivacyPolicyAsync()
    {
        await Application.Current.MainPage.DisplayAlert(
            "Privacy Policy",
            "DataFlow Mobile respects your privacy:\n\n" +
            "• All data is stored locally on your device\n" +
            "• API credentials are encrypted using platform secure storage\n" +
            "• No personal data is transmitted to our servers\n" +
            "• API calls are made directly from your device\n" +
            "• App usage analytics are not collected\n\n" +
            "For questions, contact: privacy@dataflow.com",
            "OK");
    }

    [RelayCommand]
    public async Task ShareAppAsync()
    {
        try
        {
            var shareRequest = new ShareTextRequest
            {
                Text = "Check out DataFlow Mobile - Dynamic API Data Visualization & Management Platform! " +
                       "Create custom dashboards for any REST API with beautiful templates and interactive actions.",
                Title = "Share DataFlow Mobile"
            };

            await Share.Default.RequestAsync(shareRequest);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Could not share app: {ex.Message}",
                "OK");
        }
    }
}