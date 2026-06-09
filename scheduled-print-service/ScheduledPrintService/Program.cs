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

// Initialize DataPaths with configuration
DataPaths.Initialize(builder.Configuration);

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
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));

// Register database service for loading API configs (used by manual mode and database scheduler)
builder.Services.AddSingleton<IDatabaseApiConfigService, DatabaseApiConfigService>();

// API config: load from database if manual mode with API number
if (manualMode && apiNumber.HasValue)
{
    
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

// Diagnostic logging
var demoEnabled = builder.Configuration.GetSection("Demo").GetValue<bool>("Enabled");
Log.Information("Config Flags => Demo.Enabled={DemoEnabled}", demoEnabled);
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

builder.Services.AddSingleton<IEmailNotificationService, EmailNotificationService>();

// Token renewal service (singleton to maintain state)
builder.Services.AddSingleton<ITokenRenewalService, TokenRenewalService>();

// Pending orders service for queue-based processing
builder.Services.AddSingleton<IPendingOrdersService, PendingOrdersService>();

// API services
builder.Services.AddHttpClient<IOrderApiService, OrderApiService>();
builder.Services.AddHttpClient<ISubActionExecutor, SubActionExecutor>();

// Hosted services: demo (optional) and database scheduler
builder.Services.AddHostedService<DemoRunnerService>();

// Database scheduler service (reads schedules from database and executes APIs)
builder.Services.AddHostedService<DatabaseSchedulerService>();

// Enable Windows Service integration (no console window when installed)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Scheduled Print Service";
});

var host = builder.Build();

// If manual mode with a specific API number, execute that API immediately and exit
if (manualMode && apiNumber.HasValue)
{
    using var scope = host.Services.CreateScope();
    var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseApiConfigService>();
    var apiService = scope.ServiceProvider.GetRequiredService<IOrderApiService>();
    var actionExecutor = scope.ServiceProvider.GetRequiredService<ISubActionExecutor>();

    var apiNum = apiNumber.Value;
    Log.Information("Manual mode: Executing API #{ApiNumber} now", apiNum);

    // Load API configuration
    var apiConfig = dbService.LoadApiConfig(apiNum);

    Log.Information("API #{ApiNumber} loaded: {SubActions} sub-actions configured ({Enabled} enabled)",
        apiNum,
        apiConfig.SubActions.Count,
        apiConfig.SubActions.Count(a => a.Enabled));

    // Fetch orders from API
    var orders = await apiService.GetOrdersListAsync(apiConfig);
    if (orders.Count == 0)
    {
        Log.Information("API #{ApiNumber} returned no orders", apiNum);
    }
    else
    {
        Log.Information("API #{ApiNumber} returned {Count} orders", apiNum, orders.Count);
        var processed = 0;
        var failed = 0;
        foreach (var order in orders)
        {
            try
            {
                Log.Debug("Processing order: {OrderId}", order.Id);
                var anyActionSucceeded = await actionExecutor.ExecuteActionsForOrderAsync(order.Id, order.RawData, apiConfig);
                if (anyActionSucceeded)
                {
                    processed++;
                    Log.Debug("Successfully processed order: {OrderId}", order.Id);
                }
                else
                {
                    failed++;
                    Log.Warning("Order {OrderId} not marked as processed: no actions succeeded", order.Id);
                }
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error(ex, "Failed to process order {OrderId}: {Message}", order.Id, ex.Message);
            }
        }
        Log.Information("Manual mode complete for API #{ApiNumber}: {Processed} processed, {Failed} failed",
            apiNum, processed, failed);

        // Execute any post-batch sub-actions (folder-based actions that should run AFTER all orders)
        var postBatchActions = apiConfig.SubActions
            .Where(a => a.Enabled)
            .Where(a => !string.IsNullOrWhiteSpace(a.LocalJsonFolderPath))
            .ToList();

        if (postBatchActions.Count > 0)
        {
            Log.Information("API #{ApiNumber}: Executing {Count} post-batch actions", apiNum, postBatchActions.Count);
            foreach (var action in postBatchActions)
            {
                try
                {
                    Log.Information("Executing post-batch action: {Name} ({Type})", action.Name, action.Type);
                    await actionExecutor.ExecutePostBatchActionAsync(action, apiConfig);
                    Log.Information("Post-batch action '{Name}' completed", action.Name);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Post-batch action '{Name}' failed: {Message}", action.Name, ex.Message);
                    if (!(action.ContinueOnError ?? true))
                    {
                        throw;
                    }
                }
            }
        }
    }

    // Exit without starting hosted services
    return;
}

if (OperatingSystem.IsWindows())
{
    host.Run();
}
else
{
    await host.RunAsync();
}
