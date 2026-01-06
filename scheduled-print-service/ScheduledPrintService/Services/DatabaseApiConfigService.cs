using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ScheduledPrintService.Models;
using System.Text.Json;

namespace ScheduledPrintService.Services;

public interface IDatabaseApiConfigService
{
    bool IsApiEnabled(int apiNumber);
    ApiConfig LoadApiConfig(int apiNumber);
    List<Schedule> LoadEnabledSchedules();
    List<int> LoadScheduleApiNumbers(int scheduleId);
    (string? username, string? password, string? token, DateTime? tokenExpiresAt) LoadAuthCredentials(string baseUrl);
    void UpdateAuthToken(string baseUrl, string token, DateTime expiresAt);
    Dictionary<string, string> LoadCustomHeaders(string baseUrl);
}

public class DatabaseApiConfigService : IDatabaseApiConfigService
{
    private readonly ILogger<DatabaseApiConfigService> _logger;
    private readonly string _dbPath;

    public DatabaseApiConfigService(ILogger<DatabaseApiConfigService> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(AppContext.BaseDirectory, "api_config.db");

        if (!File.Exists(_dbPath))
        {
            throw new FileNotFoundException($"Database file not found: {_dbPath}");
        }
    }

    public bool IsApiEnabled(int apiNumber)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT IsEnabled FROM PrimaryApi WHERE ApiNumber = @ApiNumber";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        var result = cmd.ExecuteScalar();
        return result != null && Convert.ToBoolean(result);
    }

    public ApiConfig LoadApiConfig(int apiNumber)
    {
        _logger.LogInformation("Loading API configuration for ApiNumber={ApiNumber} from database", apiNumber);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // Ensure backward compatibility with new columns
        EnsurePrimaryApiColumnsExist(connection);

        // Load primary API
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, Headers, Params, Payload, IsEnabled, PrinterName, Configuration, LocalJsonFilePath
            FROM PrimaryApi
            WHERE ApiNumber = @ApiNumber";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException($"API #{apiNumber} not found in database");
        }

        var config = new ApiConfig
        {
            Enabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
            BaseUrl = reader.GetString(reader.GetOrdinal("BaseUrl")),
            ManualMode = true, // Always true when loaded from database with specific API number
            ApiNumber = reader.GetInt32(reader.GetOrdinal("ApiNumber")),
            PrimaryEndpoint = reader.GetString(reader.GetOrdinal("Endpoint")),
            PrimaryHttpMethod = reader.GetString(reader.GetOrdinal("HttpMethod")),
            PrinterName = !reader.IsDBNull(reader.GetOrdinal("PrinterName")) ? reader.GetString(reader.GetOrdinal("PrinterName")) : null,
            LocalJsonFilePath = !reader.IsDBNull(reader.GetOrdinal("LocalJsonFilePath")) ? reader.GetString(reader.GetOrdinal("LocalJsonFilePath")) : null
        };

        // Parse headers JSON
        var headersJson = reader.GetString(reader.GetOrdinal("Headers"));
        var headersDoc = JsonDocument.Parse(headersJson);

        // Try to load cached token from ApiAuth table first (for better token reuse)
        var (_, _, cachedToken, tokenExpiresAt) = LoadAuthCredentials(config.BaseUrl);
        bool usedCachedToken = false;

        // Use cached token if it's still valid (not expired and has >5 minutes remaining)
        if (!string.IsNullOrEmpty(cachedToken) && tokenExpiresAt.HasValue && tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            config.BearerToken = cachedToken;
            usedCachedToken = true;
            _logger.LogDebug("API #{ApiNumber}: Using cached token from ApiAuth table, expires at {ExpiresAt}", apiNumber, tokenExpiresAt);
        }
        // Otherwise, fall back to token from Headers JSON (static/placeholder token)
        else if (headersDoc.RootElement.TryGetProperty("Authorization", out var authElement))
        {
            var authValue = authElement.GetString() ?? string.Empty;
            if (authValue.StartsWith("Bearer "))
            {
                config.BearerToken = authValue.Substring(7);
                _logger.LogDebug("API #{ApiNumber}: Using token from Headers JSON (cached token unavailable or expired)", apiNumber);
            }
        }

        // Parse cookies from Headers JSON
        if (headersDoc.RootElement.TryGetProperty("Cookie", out var cookieElement))
        {
            var cookieValue = cookieElement.GetString() ?? string.Empty;
            // Parse cookie string into dictionary: "token=...; userData=..."
            foreach (var part in cookieValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    config.Cookies[kv[0].Trim()] = kv[1].Trim();
                }
            }
        }

        // IMPORTANT: If we used cached token, update the cookies to use the cached token too
        // This ensures Puppeteer browser pages have the correct token
        if (usedCachedToken && !string.IsNullOrEmpty(config.BearerToken))
        {
            config.Cookies["token"] = config.BearerToken;
            _logger.LogDebug("API #{ApiNumber}: Updated cookies with cached token for Puppeteer browser", apiNumber);
        }

        // Load custom headers from ApiHeaders table (replaces hardcoded header logic)
        config.CustomHeaders = LoadCustomHeaders(config.BaseUrl);
        if (config.CustomHeaders.Count > 0)
        {
            _logger.LogDebug("API #{ApiNumber}: Loaded {Count} custom header(s) from database", apiNumber, config.CustomHeaders.Count);
        }

        if (headersDoc.RootElement.TryGetProperty("WarehouseId", out var warehouseElement))
        {
            if (warehouseElement.ValueKind == JsonValueKind.Number)
            {
                config.WarehouseId = warehouseElement.GetInt32();
            }
            else if (warehouseElement.ValueKind == JsonValueKind.String)
            {
                if (int.TryParse(warehouseElement.GetString(), out var warehouseId))
                {
                    config.WarehouseId = warehouseId;
                }
            }
        }

        // Parse params JSON into DefaultRequest (if provided)
        var paramsOrdinal = reader.GetOrdinal("Params");
        if (!reader.IsDBNull(paramsOrdinal))
        {
            var paramsJson = reader.GetString(paramsOrdinal);
            config.DefaultRequest = JsonSerializer.Deserialize<OrdersListRequest>(paramsJson) ?? new OrdersListRequest();
        }
        else
        {
            // No Params provided, use default empty request (will be ignored if Payload is provided)
            config.DefaultRequest = new OrdersListRequest();
        }

        // Optional raw payload for primary request (passthrough)
        if (!reader.IsDBNull(reader.GetOrdinal("Payload")))
        {
            var payloadJson = reader.GetString(reader.GetOrdinal("Payload"));
            if (!string.IsNullOrWhiteSpace(payloadJson))
            {
                config.PrimaryPayload = payloadJson;
            }
        }

        // Parse Configuration column for primary API filters
        var configOrdinal = reader.GetOrdinal("Configuration");
        if (!reader.IsDBNull(configOrdinal))
        {
            var configJson = reader.GetString(configOrdinal);
            if (!string.IsNullOrWhiteSpace(configJson))
            {
                try
                {
                    var configDoc = JsonDocument.Parse(configJson);
                    var root = configDoc.RootElement;

                    if (root.TryGetProperty("ChainedArrayJsonPath", out var arrayPathElement))
                    {
                        config.PrimaryFilterArrayJsonPath = arrayPathElement.GetString();
                    }

                    if (root.TryGetProperty("ChainedFilterArrayIndex", out var filterIndexElement))
                    {
                        if (filterIndexElement.ValueKind == JsonValueKind.Number)
                        {
                            config.PrimaryFilterArrayIndex = filterIndexElement.GetInt32();
                        }
                    }

                    if (root.TryGetProperty("ChainedFilterType", out var filterTypeElement))
                    {
                        config.PrimaryFilterType = filterTypeElement.GetString();
                    }

                    if (root.TryGetProperty("ChainedFilterValue", out var filterValueElement))
                    {
                        config.PrimaryFilterValue = filterValueElement.GetString();
                    }

                    if (root.TryGetProperty("ChainedFilterField", out var filterFieldElement))
                    {
                        config.PrimaryFilterField = filterFieldElement.GetString();
                    }

                    // Parse IdJsonPath if present in configuration
                    if (root.TryGetProperty("IdJsonPath", out var idJsonPathElement))
                    {
                        var idJsonPath = idJsonPathElement.GetString();
                        if (!string.IsNullOrWhiteSpace(idJsonPath))
                        {
                            config.IdJsonPath = idJsonPath;
                        }
                    }

                    // Parse PrimaryApiFieldMap for placeholder replacement (HTML injection feature)
                    if (root.TryGetProperty("PrimaryApiFieldMap", out var fieldMapElement) && fieldMapElement.ValueKind == JsonValueKind.Object)
                    {
                        config.PrimaryApiFieldMap = new Dictionary<string, string>();
                        foreach (var prop in fieldMapElement.EnumerateObject())
                        {
                            var value = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                config.PrimaryApiFieldMap[prop.Name] = value;
                            }
                        }
                        _logger.LogInformation("API #{ApiNumber}: Loaded {Count} field mappings for placeholder replacement", apiNumber, config.PrimaryApiFieldMap.Count);
                    }

                    _logger.LogInformation("API #{ApiNumber}: Loaded primary API filter configuration: Type={FilterType}, ArrayIndex={ArrayIndex}, Value={FilterValue}",
                        apiNumber, config.PrimaryFilterType, config.PrimaryFilterArrayIndex, config.PrimaryFilterValue);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Configuration JSON for primary API");
                }
            }
        }

        reader.Close();

        // Load sub-actions
        var subCmd = connection.CreateCommand();
        subCmd.CommandText = @"
            SELECT ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled
            FROM SubAction
            WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = @ApiNumber)
            ORDER BY ExecutionOrder";
        subCmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        using var subReader = subCmd.ExecuteReader();
        while (subReader.Read())
        {
            var configJson = subReader.GetString(subReader.GetOrdinal("Configuration"));
            var actionType = subReader.GetString(subReader.GetOrdinal("ActionType"));
            var isEnabled = subReader.GetBoolean(subReader.GetOrdinal("IsEnabled"));

            var subAction = JsonSerializer.Deserialize<SubAction>(configJson) ?? new SubAction();
            subAction.Type = actionType;
            subAction.Name = subReader.GetString(subReader.GetOrdinal("ActionName"));
            subAction.Enabled = isEnabled;

            // Normalize endpoint casing for known variants that may cause 404 due to case sensitivity.
            // Some scripts inserted '/api/Picklist/' (lowercase 'l') while other endpoints use '/api/PickList/'.
            // If the server treats path segments case-sensitively, this silent mismatch yields 404 responses.
            if (!string.IsNullOrWhiteSpace(subAction.Endpoint) && subAction.Endpoint.Contains("/api/Picklist/", StringComparison.Ordinal))
            {
                subAction.Endpoint = subAction.Endpoint.Replace("/api/Picklist/", "/api/PickList/");
            }

            config.SubActions.Add(subAction);
        }

        _logger.LogInformation("API #{ApiNumber}: Loaded API configuration: {Count} sub-actions ({Enabled} enabled)",
            apiNumber,
            config.SubActions.Count,
            config.SubActions.Count(a => a.Enabled));

        return config;
    }

    public List<Schedule> LoadEnabledSchedules()
    {
        _logger.LogInformation("Loading enabled schedules from database");

        var schedules = new List<Schedule>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // First ensure the columns exist (backwards compatibility)
        EnsureScheduleColumnsExist(connection);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, ScheduleName, CronExpression, IsEnabled, CreatedAt, UpdatedAt,
                   ProcessingMode, BatchSize, EnqueueNewOrders
            FROM Schedule
            WHERE IsEnabled = 1
            ORDER BY Id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var schedule = new Schedule
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ScheduleName = reader.GetString(reader.GetOrdinal("ScheduleName")),
                CronExpression = reader.GetString(reader.GetOrdinal("CronExpression")),
                IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt"))),
                ProcessingMode = !reader.IsDBNull(reader.GetOrdinal("ProcessingMode")) ? reader.GetString(reader.GetOrdinal("ProcessingMode")) : "immediate",
                BatchSize = !reader.IsDBNull(reader.GetOrdinal("BatchSize")) ? reader.GetInt32(reader.GetOrdinal("BatchSize")) : 10,
                EnqueueNewOrders = !reader.IsDBNull(reader.GetOrdinal("EnqueueNewOrders")) ? reader.GetBoolean(reader.GetOrdinal("EnqueueNewOrders")) : true
            };

            schedules.Add(schedule);
        }

        _logger.LogInformation("Loaded {Count} enabled schedule(s)", schedules.Count);
        return schedules;
    }

    private void EnsureScheduleColumnsExist(SqliteConnection connection)
    {
        // Check if ProcessingMode column exists
        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "PRAGMA table_info(Schedule)";
        var columns = new List<string>();

        using (var reader = checkCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                columns.Add(reader.GetString(1)); // Column name is in index 1
            }
        }

        // Add missing columns if needed
        if (!columns.Contains("ProcessingMode"))
        {
            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Schedule ADD COLUMN ProcessingMode TEXT DEFAULT 'immediate'";
            alterCmd.ExecuteNonQuery();
            _logger.LogInformation("Added ProcessingMode column to Schedule table");
        }

        if (!columns.Contains("BatchSize"))
        {
            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Schedule ADD COLUMN BatchSize INTEGER DEFAULT 10";
            alterCmd.ExecuteNonQuery();
            _logger.LogInformation("Added BatchSize column to Schedule table");
        }

        if (!columns.Contains("EnqueueNewOrders"))
        {
            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Schedule ADD COLUMN EnqueueNewOrders INTEGER DEFAULT 1";
            alterCmd.ExecuteNonQuery();
            _logger.LogInformation("Added EnqueueNewOrders column to Schedule table");
        }
    }

    private void EnsurePrimaryApiColumnsExist(SqliteConnection connection)
    {
        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "PRAGMA table_info(PrimaryApi)";
        var columns = new List<string>();

        using (var reader = checkCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                columns.Add(reader.GetString(1)); // Column name is in index 1
            }
        }

        // Add LocalJsonFilePath column if missing (for local JSON file as primary API source)
        if (!columns.Contains("LocalJsonFilePath"))
        {
            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE PrimaryApi ADD COLUMN LocalJsonFilePath TEXT";
            alterCmd.ExecuteNonQuery();
            _logger.LogInformation("Added LocalJsonFilePath column to PrimaryApi table");
        }
    }

    public List<int> LoadScheduleApiNumbers(int scheduleId)
    {
        _logger.LogDebug("Loading API numbers for Schedule #{ScheduleId}", scheduleId);

        var apiNumbers = new List<int>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT sa.ApiNumber
            FROM ScheduleApi sa
            JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber
            WHERE sa.ScheduleId = @ScheduleId AND pa.IsEnabled = 1
            ORDER BY sa.ExecutionOrder";
        cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            apiNumbers.Add(reader.GetInt32(0));
        }

        _logger.LogDebug("Schedule #{ScheduleId} has {Count} enabled API(s) assigned", scheduleId, apiNumbers.Count);
        return apiNumbers;
    }

    public (string? username, string? password, string? token, DateTime? tokenExpiresAt) LoadAuthCredentials(string baseUrl)
    {
        _logger.LogDebug("Loading auth credentials for BaseUrl={BaseUrl}", baseUrl);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Username, Password, BearerToken, TokenExpiresAt
            FROM ApiAuth
            WHERE BaseUrl = @BaseUrl";
        cmd.Parameters.AddWithValue("@BaseUrl", baseUrl);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            _logger.LogWarning("No auth credentials found for BaseUrl={BaseUrl}", baseUrl);
            return (null, null, null, null);
        }

        var username = reader.GetString(reader.GetOrdinal("Username"));
        var password = reader.GetString(reader.GetOrdinal("Password"));

        string? token = null;
        DateTime? tokenExpiresAt = null;

        if (!reader.IsDBNull(reader.GetOrdinal("BearerToken")))
        {
            token = reader.GetString(reader.GetOrdinal("BearerToken"));
        }

        if (!reader.IsDBNull(reader.GetOrdinal("TokenExpiresAt")))
        {
            tokenExpiresAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("TokenExpiresAt")));
        }

        return (username, password, token, tokenExpiresAt);
    }

    public void UpdateAuthToken(string baseUrl, string token, DateTime expiresAt)
    {
        _logger.LogDebug("Updating auth token for BaseUrl={BaseUrl}, expires at {ExpiresAt}", baseUrl, expiresAt);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE ApiAuth
            SET BearerToken = @Token,
                TokenExpiresAt = @ExpiresAt,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE BaseUrl = @BaseUrl";
        cmd.Parameters.AddWithValue("@Token", token);
        cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt.ToString("O"));
        cmd.Parameters.AddWithValue("@BaseUrl", baseUrl);

        var rowsAffected = cmd.ExecuteNonQuery();

        if (rowsAffected == 0)
        {
            _logger.LogWarning("No auth record found to update for BaseUrl={BaseUrl}", baseUrl);
        }
        else
        {
            _logger.LogInformation("Updated auth token for BaseUrl={BaseUrl}", baseUrl);
        }
    }

    public Dictionary<string, string> LoadCustomHeaders(string baseUrl)
    {
        var headers = new Dictionary<string, string>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT h.HeaderName, h.HeaderValue
            FROM ApiHeaders h
            INNER JOIN ApiAuth a ON h.ApiAuthId = a.Id
            WHERE a.BaseUrl = @BaseUrl AND h.IsEnabled = 1";
        cmd.Parameters.AddWithValue("@BaseUrl", baseUrl);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var headerName = reader.GetString(reader.GetOrdinal("HeaderName"));
            var headerValue = reader.GetString(reader.GetOrdinal("HeaderValue"));
            headers[headerName] = headerValue;
        }

        return headers;
    }
}
