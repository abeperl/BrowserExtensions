using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ScheduledPrintService.Services;

/// <summary>
/// Tracks processed orders for a specific API using the database
/// </summary>
public class DatabaseOrderTracker
{
    private readonly int _apiNumber;
    private readonly ILogger _logger;
    private readonly string _dbPath;
    private readonly HashSet<string> _processedIds = new();

    public DatabaseOrderTracker(int apiNumber, ILogger logger)
    {
        _apiNumber = apiNumber;
        _logger = logger;
        _dbPath = Path.Combine(AppContext.BaseDirectory, "api_config.db");
        
        EnsureTableExists();
        LoadProcessedIds();
    }

    private void EnsureTableExists()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ProcessedOrders (
                ApiNumber INTEGER NOT NULL,
                OrderId TEXT NOT NULL,
                ProcessedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (ApiNumber, OrderId)
            );
            CREATE INDEX IF NOT EXISTS idx_processedorders_apinumber ON ProcessedOrders(ApiNumber);";
        cmd.ExecuteNonQuery();
    }

    private void LoadProcessedIds()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT OrderId FROM ProcessedOrders WHERE ApiNumber = @ApiNumber";
        cmd.Parameters.AddWithValue("@ApiNumber", _apiNumber);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            _processedIds.Add(reader.GetString(0));
        }

        _logger.LogInformation("Loaded {Count} processed IDs for API #{ApiNumber} from database", _processedIds.Count, _apiNumber);
    }

    public bool HasProcessed(string orderId)
    {
        return _processedIds.Contains(orderId);
    }

    public void MarkProcessed(string orderId)
    {
        if (_processedIds.Add(orderId))
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO ProcessedOrders (ApiNumber, OrderId)
                VALUES (@ApiNumber, @OrderId)";
            cmd.Parameters.AddWithValue("@ApiNumber", _apiNumber);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.ExecuteNonQuery();

            _logger.LogDebug("Marked order {OrderId} as processed for API #{ApiNumber}", orderId, _apiNumber);
        }
    }
}
