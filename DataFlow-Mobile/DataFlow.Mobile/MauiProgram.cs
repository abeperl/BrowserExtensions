using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;
using DataFlow.Mobile.ViewModels;
using DataFlow.Mobile.Views.Pages;

namespace DataFlow.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure logging
#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configure HTTP client with resilience
		ConfigureHttpClients(builder.Services);

		// Configure database
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "dataflow.db");
		builder.Services.AddDbContext<DataFlowDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// Register services
		RegisterServices(builder.Services);

		return builder.Build();
	}

	private static void ConfigureHttpClients(IServiceCollection services)
	{
		// Configure default HttpClient with resilience policies
		services.AddHttpClient("DataFlowApi", client =>
		{
			// removed: set timeout via resilience handler
			client.DefaultRequestHeaders.Add("User-Agent", "DataFlow-Mobile/1.0");
		})
		.AddStandardResilienceHandler();

		// Configure authentication-specific HttpClient
		services.AddHttpClient("AuthApi", client =>
		{
			// removed: set timeout via resilience handler
		})
		.AddStandardResilienceHandler();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		// Repository pattern services
		services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// Network service
		services.AddSingleton<INetworkService, NetworkService>();

		// Data services
		services.AddScoped<IPageService, PageService>();
		services.AddScoped<ITemplateService, TemplateService>();
		services.AddScoped<IActionService, ActionService>();
		services.AddScoped<ISettingsService, SettingsService>();

		// API services
		services.AddScoped<IApiService, ApiService>();
		services.AddScoped<IAuthenticationService, AuthenticationService>();

		// Audio service
		services.AddSingleton<IAudioService, AudioService>();

		// Import/Export service
		services.AddScoped<IImportExportService, ImportExportService>();

		// Security services
		services.AddScoped<ISecureStorageService, SecureStorageService>();

		// Backup/Restore service
		services.AddScoped<IBackupRestoreService, BackupRestoreService>();

		// Navigation service
		services.AddSingleton<INavigationService, NavigationService>();

		// Template processing services
		services.AddScoped<ITemplateProcessor, TemplateProcessor>();
		services.AddScoped<ITemplateColumnService, TemplateColumnService>();
		services.AddScoped<IColorSchemeService, ColorSchemeService>();
		services.AddScoped<ILayoutTemplateService, LayoutTemplateService>();

		// ViewModels
		services.AddTransient<HomePageViewModel>();
		services.AddTransient<PageDetailViewModel>();
		services.AddTransient<SettingsPageViewModel>();
		services.AddTransient<AboutPageViewModel>();
		services.AddTransient<TemplateEditorViewModel>();
		services.AddTransient<PageWizardViewModel>();
		services.AddTransient<ApiConfigurationViewModel>();
		services.AddTransient<AdvancedTemplateDesignerViewModel>();
            // Removed registration for ActionConfiguration feature (no implementation)
		services.AddTransient<AudioSettingsViewModel>();

		// Pages
		services.AddTransient<HomePage>();
		services.AddTransient<PageDetailView>();
		services.AddTransient<SettingsPage>();
		services.AddTransient<AboutPage>();
		services.AddTransient<TemplateEditorPage>();
		services.AddTransient<PageWizardPage>();
		services.AddTransient<ApiConfigurationPage>();
		services.AddTransient<AdvancedTemplateDesignerPage>();
            // Removed registration for ActionConfiguration feature (no implementation)
		services.AddTransient<AudioSettingsPage>();
	}
}
