using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ScheduledPrintService.Models;
using System.Text.Json;

namespace ScheduledPrintService.Services;

public interface IDatabaseApiConfigService
{
    ApiConfig LoadApiConfig(int apiNumber);
    List<Schedule> LoadEnabledSchedules();
    List<int> LoadScheduleApiNumbers(int scheduleId);
    (string? username, string? password, string? token, DateTime? tokenExpiresAt) LoadAuthCredentials(string baseUrl);
    void UpdateAuthToken(string baseUrl, string token, DateTime expiresAt);
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

    public ApiConfig LoadApiConfig(int apiNumber)
    {
        _logger.LogInformation("Loading API configuration for ApiNumber={ApiNumber} from database", apiNumber);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // Load primary API
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, Headers, Params, Payload, IsEnabled, PrinterName
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
            PrinterName = !reader.IsDBNull(reader.GetOrdinal("PrinterName")) ? reader.GetString(reader.GetOrdinal("PrinterName")) : null
        };

        // Parse headers JSON
        var headersJson = reader.GetString(reader.GetOrdinal("Headers"));
        var headersDoc = JsonDocument.Parse(headersJson);
        
        if (headersDoc.RootElement.TryGetProperty("Authorization", out var authElement))
        {
            var authValue = authElement.GetString() ?? string.Empty;
            if (authValue.StartsWith("Bearer "))
            {
                config.BearerToken = authValue.Substring(7);
            }
        }

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

        // Parse params JSON into DefaultRequest
        var paramsJson = reader.GetString(reader.GetOrdinal("Params"));
        var paramsDoc = JsonDocument.Parse(paramsJson);
        config.DefaultRequest = JsonSerializer.Deserialize<OrdersListRequest>(paramsJson) ?? new OrdersListRequest();

        // Optional raw payload for primary request (passthrough)
        if (!reader.IsDBNull(reader.GetOrdinal("Payload")))
        {
            var payloadJson = reader.GetString(reader.GetOrdinal("Payload"));
            if (!string.IsNullOrWhiteSpace(payloadJson))
            {
                config.PrimaryPayload = payloadJson;
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

        _logger.LogInformation("Loaded API configuration: {Count} sub-actions ({Enabled} enabled)",
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

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, ScheduleName, CronExpression, IsEnabled, CreatedAt, UpdatedAt
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
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
            };

            schedules.Add(schedule);
        }

        _logger.LogInformation("Loaded {Count} enabled schedule(s)", schedules.Count);
        return schedules;
    }

    public List<int> LoadScheduleApiNumbers(int scheduleId)
    {
        _logger.LogDebug("Loading API numbers for Schedule #{ScheduleId}", scheduleId);

        var apiNumbers = new List<int>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT ApiNumber
            FROM ScheduleApi
            WHERE ScheduleId = @ScheduleId
            ORDER BY ExecutionOrder";
        cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            apiNumbers.Add(reader.GetInt32(0));
        }

        _logger.LogDebug("Schedule #{ScheduleId} has {Count} API(s) assigned", scheduleId, apiNumbers.Count);
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
}
