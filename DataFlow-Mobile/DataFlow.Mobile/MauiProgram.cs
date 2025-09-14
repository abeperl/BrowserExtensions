using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using DataFlow.Mobile.Services;
using DataFlow.Mobile.Services.Interfaces;

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
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.Add("User-Agent", "DataFlow-Mobile/1.0");
		})
		.AddResilienceHandler("default", builder =>
		{
			builder
				.AddRetry(new HttpRetryStrategyOptions
				{
					MaxRetryAttempts = 3,
					Delay = TimeSpan.FromSeconds(1),
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true
				})
				.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
				{
					FailureRatio = 0.5,
					SamplingDuration = TimeSpan.FromSeconds(30),
					MinimumThroughput = 3,
					BreakDuration = TimeSpan.FromSeconds(30)
				})
				.AddTimeout(TimeSpan.FromSeconds(10));
		});

		// Configure authentication-specific HttpClient
		services.AddHttpClient("AuthApi", client =>
		{
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddResilienceHandler("auth", builder =>
		{
			builder
				.AddRetry(new HttpRetryStrategyOptions
				{
					MaxRetryAttempts = 2,
					Delay = TimeSpan.FromMilliseconds(500),
					BackoffType = DelayBackoffType.Linear
				})
				.AddTimeout(TimeSpan.FromSeconds(5));
		});
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
	}
}
