using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ScheduledPrintService.Services;

/// <summary>
/// Manages a pending orders queue in the database.
/// Supports fetching primary API once, storing all IDs, and processing them in controlled batches.
/// </summary>
public interface IPendingOrdersService
{
    /// <summary>
    /// Fetch primary API and enqueue all new order IDs that haven't been processed
    /// </summary>
    Task EnqueueNewOrdersAsync(int apiNumber, Func<Task<List<(string orderId, string rawData)>>> fetchOrdersFunc, CancellationToken ct = default);

    /// <summary>
    /// Get the next batch of pending order IDs to process
    /// </summary>
    List<PendingOrder> GetNextBatch(int apiNumber, int batchSize);

    /// <summary>
    /// Mark an order as successfully processed
    /// </summary>
    void MarkProcessed(int apiNumber, string orderId);

    /// <summary>
    /// Mark an order as failed (increments retry count)
    /// </summary>
    void MarkFailed(int apiNumber, string orderId, string? errorMessage = null);

    /// <summary>
    /// Get statistics about pending orders for an API
    /// </summary>
    (int pending, int processing, int failed, int processed) GetStats(int apiNumber);

    /// <summary>
    /// Check if any records exist for an API (regardless of status)
    /// </summary>
    bool HasAnyRecords(int apiNumber);

    /// <summary>
    /// Clear all pending/failed orders for an API (use with caution)
    /// </summary>
    void ClearQueue(int apiNumber);
}

public class PendingOrder
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string RawData { get; set; } = string.Empty;
    public DateTime EnqueuedAt { get; set; }
    public int RetryCount { get; set; }
}

public class PendingOrdersService : IPendingOrdersService
{
    private readonly ILogger<PendingOrdersService> _logger;
    private readonly string _dbPath;

    public PendingOrdersService(ILogger<PendingOrdersService> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(AppContext.BaseDirectory, "api_config.db");

        EnsureTableExists();
    }

    private void EnsureTableExists()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS PendingOrders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiNumber INTEGER NOT NULL,
                OrderId TEXT NOT NULL,
                RawData TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'pending',
                EnqueuedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                ProcessingStartedAt TEXT,
                ProcessedAt TEXT,
                FailedAt TEXT,
                RetryCount INTEGER DEFAULT 0,
                ErrorMessage TEXT,
                UNIQUE(ApiNumber, OrderId)
            );
            CREATE INDEX IF NOT EXISTS idx_pendingorders_status ON PendingOrders(ApiNumber, Status);
            CREATE INDEX IF NOT EXISTS idx_pendingorders_enqueueat ON PendingOrders(ApiNumber, EnqueuedAt);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task EnqueueNewOrdersAsync(int apiNumber, Func<Task<List<(string orderId, string rawData)>>> fetchOrdersFunc, CancellationToken ct = default)
    {
        _logger.LogInformation("API #{ApiNumber}: Fetching orders from primary API to enqueue", apiNumber);

        var orders = await fetchOrdersFunc();

        if (orders.Count == 0)
        {
            _logger.LogInformation("API #{ApiNumber}: No orders returned from primary API", apiNumber);
            return;
        }

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var enqueuedCount = 0;
        var skippedCount = 0;

        foreach (var (orderId, rawData) in orders)
        {
            ct.ThrowIfCancellationRequested();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO PendingOrders (ApiNumber, OrderId, RawData, Status)
                VALUES (@ApiNumber, @OrderId, @RawData, 'pending')
            ";
            cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@RawData", rawData);

            var rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                enqueuedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        _logger.LogInformation("API #{ApiNumber}: Enqueued {Enqueued} new orders, {Skipped} already queued/processed",
            apiNumber, enqueuedCount, skippedCount);
    }

    public List<PendingOrder> GetNextBatch(int apiNumber, int batchSize)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // Get pending orders (oldest first)
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, OrderId, RawData, EnqueuedAt, RetryCount
            FROM PendingOrders
            WHERE ApiNumber = @ApiNumber
              AND Status = 'pending'
            ORDER BY EnqueuedAt ASC
            LIMIT @BatchSize
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);
        cmd.Parameters.AddWithValue("@BatchSize", batchSize);

        var orders = new List<PendingOrder>();

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(new PendingOrder
            {
                Id = reader.GetInt32(0),
                OrderId = reader.GetString(1),
                RawData = reader.GetString(2),
                EnqueuedAt = DateTime.Parse(reader.GetString(3)),
                RetryCount = reader.GetInt32(4)
            });
        }

        reader.Close();

        // Mark orders as 'processing'
        if (orders.Count > 0)
        {
            var ids = string.Join(",", orders.Select(o => o.Id));
            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = $@"
                UPDATE PendingOrders
                SET Status = 'processing',
                    ProcessingStartedAt = CURRENT_TIMESTAMP
                WHERE Id IN ({ids})
            ";
            updateCmd.ExecuteNonQuery();
        }

        return orders;
    }

    public void MarkProcessed(int apiNumber, string orderId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE PendingOrders
            SET Status = 'processed',
                ProcessedAt = CURRENT_TIMESTAMP
            WHERE ApiNumber = @ApiNumber AND OrderId = @OrderId
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.ExecuteNonQuery();

        // Also add to ProcessedOrders for backward compatibility
        var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT OR IGNORE INTO ProcessedOrders (ApiNumber, OrderId)
            VALUES (@ApiNumber, @OrderId)
        ";
        insertCmd.Parameters.AddWithValue("@ApiNumber", apiNumber);
        insertCmd.Parameters.AddWithValue("@OrderId", orderId);
        insertCmd.ExecuteNonQuery();

        _logger.LogDebug("API #{ApiNumber}: Marked order {OrderId} as processed", apiNumber, orderId);
    }

    public void MarkFailed(int apiNumber, string orderId, string? errorMessage = null)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE PendingOrders
            SET Status = 'failed',
                FailedAt = CURRENT_TIMESTAMP,
                RetryCount = RetryCount + 1,
                ErrorMessage = @ErrorMessage
            WHERE ApiNumber = @ApiNumber AND OrderId = @OrderId
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? string.Empty);
        cmd.ExecuteNonQuery();

        _logger.LogWarning("API #{ApiNumber}: Marked order {OrderId} as failed. Error: {Error}",
            apiNumber, orderId, errorMessage ?? "Unknown");
    }

    public (int pending, int processing, int failed, int processed) GetStats(int apiNumber)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT
                SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) as pending,
                SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) as processing,
                SUM(CASE WHEN Status = 'failed' THEN 1 ELSE 0 END) as failed,
                SUM(CASE WHEN Status = 'processed' THEN 1 ELSE 0 END) as processed
            FROM PendingOrders
            WHERE ApiNumber = @ApiNumber
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (
                pending: reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetInt64(0)),
                processing: reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetInt64(1)),
                failed: reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetInt64(2)),
                processed: reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetInt64(3))
            );
        }

        return (0, 0, 0, 0);
    }

    public bool HasAnyRecords(int apiNumber)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM PendingOrders
            WHERE ApiNumber = @ApiNumber
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        var count = (long)(cmd.ExecuteScalar() ?? 0L);
        return count > 0;
    }

    public void ClearQueue(int apiNumber)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM PendingOrders
            WHERE ApiNumber = @ApiNumber
              AND Status IN ('pending', 'failed', 'processing')
        ";
        cmd.Parameters.AddWithValue("@ApiNumber", apiNumber);

        var rowsAffected = cmd.ExecuteNonQuery();

        _logger.LogWarning("API #{ApiNumber}: Cleared {Count} pending/failed orders from queue", apiNumber, rowsAffected);
    }
}