using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScheduledPrintService.Models;
using ScheduledPrintService.Services;
using Serilog;

// Ensure working directory is the executable directory
try { Directory.SetCurrentDirectory(AppContext.BaseDirectory); } catch { }

// Parse command-line arguments
var manualMode = false;
int? apiNumber = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--manual" || args[i] == "-m")
    {
        manualMode = true;
    }
    else if ((args[i] == "--api-number" || args[i] == "-a") && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var num))
        {
            apiNumber = num;
            i++; // Skip next arg since we consumed it
        }
    }
}

var builder = Host.CreateApplicationBuilder(args);
// Ensure we load appsettings.json from the executable directory as well (when run from repo root)
var exeDir = AppContext.BaseDirectory;
builder.Configuration.AddJsonFile(Path.Combine(exeDir, "appsettings.json"), optional: true, reloadOnChange: true);

// Configure Serilog (console + file)
var logDir = DataPaths.EnsureDir("logs");
var logPathPattern = Path.Combine(logDir, "scheduled-print-service-.log");
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(path: logPathPattern, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Announce file logging location
var todayLog = Path.Combine(logDir, $"scheduled-print-service-{DateTime.Now:yyyyMMdd}.log");
Log.Information("File logging enabled. Directory: {Dir}. Today log: {File}", logDir, todayLog);

// Bind configuration sections
builder.Services.Configure<PdfConfig>(builder.Configuration.GetSection("Pdf"));
builder.Services.Configure<PrinterConfig>(builder.Configuration.GetSection("Printer"));
builder.Services.Configure<DemoConfig>(builder.Configuration.GetSection("Demo"));
builder.Services.Configure<SchedulerConfig>(builder.Configuration.GetSection("Scheduler"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));

// API config: load from database if manual mode with API number, otherwise from appsettings.json
if (manualMode && apiNumber.HasValue)
{
    // Register database service for loading API config
    builder.Services.AddSingleton<IDatabaseApiConfigService, DatabaseApiConfigService>();
    
    // Load API config from database and register as singleton
    var tempServiceProvider = builder.Services.BuildServiceProvider();
    var dbService = tempServiceProvider.GetRequiredService<IDatabaseApiConfigService>();
    var apiConfig = dbService.LoadApiConfig(apiNumber.Value);
    
    // Load credentials from appsettings.json (not stored in database for security)
    var fallbackUserEmail = builder.Configuration.GetSection("Api:UserEmail").Get<string>() ?? string.Empty;
    var fallbackPassword = builder.Configuration.GetSection("Api:Password").Get<string>() ?? string.Empty;
    
    builder.Services.Configure<ApiConfig>(options =>
    {
        options.Enabled = apiConfig.Enabled;
        options.BaseUrl = apiConfig.BaseUrl;
        options.BearerToken = apiConfig.BearerToken;
        options.WarehouseId = apiConfig.WarehouseId;
        options.Cookies = apiConfig.Cookies;
        options.DefaultRequest = apiConfig.DefaultRequest;
        options.ManualMode = true;
        options.SubActions = apiConfig.SubActions;
        options.ProcessedIdsPath = apiConfig.ProcessedIdsPath;
        options.ApiNumber = apiConfig.ApiNumber;
        options.PrimaryEndpoint = apiConfig.PrimaryEndpoint;
        options.PrimaryHttpMethod = apiConfig.PrimaryHttpMethod;
        options.PrimaryPayload = apiConfig.PrimaryPayload;
        // Credentials from appsettings.json for token renewal
        options.UserEmail = fallbackUserEmail;
        options.Password = fallbackPassword;
    });
    Log.Information("Manual mode enabled with API #{ApiNumber} from database", apiNumber.Value);
}
else
{
    // Load from appsettings.json (legacy mode)
    builder.Services.Configure<ApiConfig>(builder.Configuration.GetSection("Api"));
}

// Diagnostic logging of key flags
var demoEnabled = builder.Configuration.GetSection("Demo").GetValue<bool>("Enabled");
var schedulerEnabled = builder.Configuration.GetSection("Scheduler").GetValue<bool>("Enabled");
var apiEnabled = manualMode && apiNumber.HasValue ? true : builder.Configuration.GetSection("Api").GetValue<bool>("Enabled");
Log.Information("Config Flags => Demo.Enabled={DemoEnabled} Scheduler.Enabled={SchedulerEnabled} Api.Enabled={ApiEnabled}", demoEnabled, schedulerEnabled, apiEnabled);
Log.Information("Env:SCHEDULED_PRINT_DATA_ROOT => {EnvOverride}", Environment.GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT"));
Log.Information("DataRoot => {DataRoot}", DataPaths.DataRoot);

// Core services
builder.Services.AddSingleton<PdfBrowserManager>();
builder.Services.AddSingleton<PdfPrintService>();
// Choose printer implementation based on config at runtime
builder.Services.AddSingleton<IPdfPrinter>(sp =>
{
    var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PrinterConfig>>().Value;
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return cfg.Mode.Equals("Windows", StringComparison.OrdinalIgnoreCase)
        ? new WindowsPdfPrinter(loggerFactory.CreateLogger<WindowsPdfPrinter>(), sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PrinterConfig>>())
        : new FilePdfPrinter(loggerFactory.CreateLogger<FilePdfPrinter>(), sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PrinterConfig>>());
});

// HTTP fetcher with resilience handler in future; basic client now
builder.Services.AddHttpClient<IHtmlFetcher, HttpHtmlFetcher>();
builder.Services.AddSingleton<IPrintTracker, FilePrintTracker>();
builder.Services.AddSingleton<IEmailNotificationService, EmailNotificationService>();

// Token renewal service (singleton to maintain state)
builder.Services.AddSingleton<ITokenRenewalService, TokenRenewalService>();

// API services
builder.Services.AddHttpClient<IOrderApiService, OrderApiService>();
builder.Services.AddHttpClient<ISubActionExecutor, SubActionExecutor>();

// Hosted services: demo (optional), scheduler (conditional), and API polling
builder.Services.AddHostedService<DemoRunnerService>();
var schedulerFeatureEnabled = builder.Configuration.GetSection("Scheduler").GetValue<bool>("Enabled");
if (schedulerFeatureEnabled)
{
    builder.Services.AddHostedService<PrintSchedulerService>();
}
else
{
    Log.Information("PrintSchedulerService disabled by config; not registering.");
}
builder.Services.AddHostedService<ApiPollSchedulerService>();

// Enable Windows Service integration (no console window when installed)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Scheduled Print Service";
});

var host = builder.Build();

if (OperatingSystem.IsWindows())
{
    host.Run();
}
else
{
    await host.RunAsync();
}
