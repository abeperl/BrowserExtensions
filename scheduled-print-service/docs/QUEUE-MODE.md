# Queue-Based Processing Mode

## Overview

The Scheduled Print Service now supports two processing modes:

1. **Immediate Mode** (default): Fetches primary API and processes all orders immediately in one run
2. **Queued Mode** (new): Fetches primary API once, stores all IDs in database, then processes them in controlled batches

## Why Use Queued Mode?

Queued mode solves several problems:

- **Avoid Reprocessing**: Each ID is stored in the database and marked as processed, ensuring it's never processed more than once
- **Controlled Throughput**: Process a fixed number of orders per run to avoid overwhelming the system
- **Resume from Failures**: If processing stops mid-batch, remaining orders stay queued for next run
- **Separate Fetch from Process**: Fetch all new IDs once (fast), then process them gradually over time
- **Better Monitoring**: Database tracks pending, processing, failed, and processed orders with timestamps

## Configuration

### Database Schema

The queue mode uses a new `PendingOrders` table and extends the `Schedule` table:

```sql
-- Schedule table (new columns)
ALTER TABLE Schedule ADD COLUMN ProcessingMode TEXT DEFAULT 'immediate';
ALTER TABLE Schedule ADD COLUMN BatchSize INTEGER DEFAULT 10;
ALTER TABLE Schedule ADD COLUMN EnqueueNewOrders INTEGER DEFAULT 1;

-- PendingOrders table
CREATE TABLE PendingOrders (
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
```

### Schedule Configuration

Configure queue mode in the `Schedule` table:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `ProcessingMode` | TEXT | `'immediate'` | Processing mode: `'immediate'` or `'queued'` |
| `BatchSize` | INTEGER | `10` | Number of orders to process per run (queued mode only) |
| `EnqueueNewOrders` | BOOLEAN | `1` (true) | Whether to fetch and enqueue new orders on each run |

### Example: Enable Queued Mode

```sql
-- Update schedule #1 to use queued mode with batch size of 5
UPDATE Schedule
SET ProcessingMode = 'queued',
    BatchSize = 5,
    EnqueueNewOrders = 1
WHERE Id = 1;
```

## How It Works

### Immediate Mode Flow (Default)

```
Schedule triggers → Fetch primary API → Process all orders → Done
```

Each run:
1. Fetches primary API (e.g., 100 orders)
2. Checks `ProcessedOrders` table to skip already-processed IDs
3. Processes all new orders immediately
4. Marks processed IDs in `ProcessedOrders`

### Queued Mode Flow

```
Schedule triggers → [Optional: Fetch & Enqueue] → Get batch → Process batch → Done
```

**First run (EnqueueNewOrders = true):**
1. Fetch primary API (e.g., 100 orders)
2. Insert all order IDs into `PendingOrders` table with status `'pending'`
3. Skip IDs that already exist (unique constraint)
4. Get next batch (e.g., 5 orders) where status = `'pending'`
5. Mark batch as `'processing'`
6. Process each order in batch
7. Mark successful orders as `'processed'`, failed orders as `'failed'`

**Subsequent runs:**
- If `EnqueueNewOrders = true`: Repeat step 1-7 (fetch new + process batch)
- If `EnqueueNewOrders = false`: Skip to step 4 (only process existing queue)

## Order Statuses

| Status | Description |
|--------|-------------|
| `pending` | Order is queued and waiting to be processed |
| `processing` | Order has been pulled into current batch and is being processed |
| `processed` | Order completed successfully |
| `failed` | Order failed during processing (can be retried) |

## Workflow Examples

### Example 1: Initial Queue Setup

**Goal**: Fetch all pending orders once, then process 10 per run.

```sql
-- Configure schedule for queued mode
UPDATE Schedule
SET ProcessingMode = 'queued',
    BatchSize = 10,
    EnqueueNewOrders = 1  -- Fetch new orders each run
WHERE Id = 1;
```

**What happens:**
- **Run 1**: Fetch API (100 orders) → Enqueue all → Process 10 → 90 pending
- **Run 2**: Fetch API (105 orders) → Enqueue 5 new → Process 10 → 85 pending
- **Run 3**: Fetch API (105 orders) → No new → Process 10 → 75 pending
- ... continues until queue is empty

### Example 2: One-Time Bulk Enqueue

**Goal**: Fetch all orders once, then process gradually without fetching more.

```sql
-- Step 1: Configure for initial fetch
UPDATE Schedule
SET ProcessingMode = 'queued',
    BatchSize = 10,
    EnqueueNewOrders = 1
WHERE Id = 1;

-- Wait for one run to complete (orders are now enqueued)

-- Step 2: Disable fetching, only process queue
UPDATE Schedule
SET EnqueueNewOrders = 0
WHERE Id = 1;
```

**What happens:**
- **Run 1**: Fetch API (100 orders) → Enqueue all → Process 10 → 90 pending
- **Run 2**: Skip fetch → Process 10 → 80 pending
- **Run 3**: Skip fetch → Process 10 → 70 pending
- ... continues until queue is empty

### Example 3: Failed Orders Retry

Failed orders remain in the queue with status `'failed'`. To retry them:

```sql
-- Option 1: Reset failed orders to pending (will be retried)
UPDATE PendingOrders
SET Status = 'pending',
    RetryCount = 0,
    ErrorMessage = NULL
WHERE ApiNumber = 1 AND Status = 'failed';

-- Option 2: Delete failed orders (won't be retried)
DELETE FROM PendingOrders
WHERE ApiNumber = 1 AND Status = 'failed';
```

## Monitoring & Troubleshooting

### View Queue Statistics

```sql
SELECT
    Status,
    COUNT(*) as Count,
    MIN(EnqueuedAt) as OldestEnqueuedAt,
    MAX(EnqueuedAt) as NewestEnqueuedAt
FROM PendingOrders
WHERE ApiNumber = 1
GROUP BY Status;
```

### View Pending Orders

```sql
SELECT OrderId, EnqueuedAt, RetryCount
FROM PendingOrders
WHERE ApiNumber = 1 AND Status = 'pending'
ORDER BY EnqueuedAt ASC
LIMIT 20;
```

### View Failed Orders

```sql
SELECT OrderId, FailedAt, RetryCount, ErrorMessage
FROM PendingOrders
WHERE ApiNumber = 1 AND Status = 'failed'
ORDER BY FailedAt DESC
LIMIT 20;
```

### Clear Entire Queue (Use with Caution)

```sql
-- Delete all pending/failed orders for API #1
DELETE FROM PendingOrders
WHERE ApiNumber = 1 AND Status IN ('pending', 'failed', 'processing');
```

## Logs

Look for these log messages to monitor queue mode:

### Enqueue Phase
```
API #1 [QUEUED MODE]: Batch size: 5, Enqueue new: True
API #1: Fetching primary API to enqueue new orders
API #1: Enqueued 47 new orders, 0 already queued/processed
```

### Queue Stats
```
API #1 queue stats: 42 pending, 0 processing, 5 failed, 1248 total processed
```

### Processing Phase
```
API #1: Processing batch of 5 orders
Successfully processed order: ORD-12345
API #1 batch complete: 5 processed, 0 failed. Queue: 37 pending, 5 failed
```

## Migration from Immediate Mode

To migrate an existing schedule from immediate to queued mode:

1. **Update schedule configuration:**
   ```sql
   UPDATE Schedule
   SET ProcessingMode = 'queued',
       BatchSize = 10,
       EnqueueNewOrders = 1
   WHERE Id = 1;
   ```

2. **Monitor first run logs** to ensure orders are being enqueued

3. **Check queue statistics** to verify pending orders:
   ```sql
   SELECT Status, COUNT(*) FROM PendingOrders WHERE ApiNumber = 1 GROUP BY Status;
   ```

4. **Adjust batch size** if needed based on processing time and system load

## Best Practices

1. **Start with small batch sizes** (5-10) and increase gradually based on processing time
2. **Monitor failed orders** regularly and investigate error patterns
3. **Use `EnqueueNewOrders = false`** if you only want to process a backlog without fetching new orders
4. **Set appropriate cron schedules** - e.g., fetch every hour but process every 5 minutes
5. **Clean up old processed records** periodically to keep database size manageable

## Performance Considerations

- **Database size**: The `PendingOrders` table grows with each unique order ID. Consider archiving or deleting old processed records.
- **Batch size**: Larger batches = faster throughput but longer processing time per run
- **Fetch frequency**: Fetching too frequently when `EnqueueNewOrders = true` may cause duplicate API calls
- **Indexes**: The table includes indexes on `(ApiNumber, Status)` and `(ApiNumber, EnqueuedAt)` for fast queries

## Backwards Compatibility

- **Default mode is `'immediate'`** - existing schedules continue working without changes
- **Automatic schema migration** - new columns are added automatically on first run
- **ProcessedOrders table** - still used for tracking in both modes (backwards compatible)