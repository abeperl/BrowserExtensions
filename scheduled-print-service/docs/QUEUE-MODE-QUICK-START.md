# Queue Mode - Quick Start Guide

## What is Queue Mode?

Queue mode lets you:
- ✅ Fetch primary API **once** and save all IDs to database
- ✅ Process IDs in **controlled batches** (e.g., 10 at a time)
- ✅ **Never reprocess** the same ID twice
- ✅ Resume from where you left off if processing stops
- ✅ Monitor queue status in real-time

## Enable Queue Mode (3 Steps)

### Step 1: Update Your Schedule

```sql
-- Enable queue mode for schedule #1
UPDATE Schedule
SET ProcessingMode = 'queued',    -- Switch to queue mode
    BatchSize = 10,               -- Process 10 IDs per run
    EnqueueNewOrders = 1          -- Fetch new IDs each run (1 = true)
WHERE Id = 1;
```

### Step 2: Restart the Service

The service will automatically:
- Create the `PendingOrders` table
- Add new columns to `Schedule` table
- Start using queue mode on next scheduled run

### Step 3: Monitor the Queue

```sql
-- View queue statistics
SELECT Status, COUNT(*) as Count
FROM PendingOrders
WHERE ApiNumber = 1
GROUP BY Status;
```

**Example output:**
```
Status      | Count
------------|------
pending     | 45
processing  | 0
processed   | 1205
failed      | 3
```

## Configuration Options

| Setting | Values | Description |
|---------|--------|-------------|
| `ProcessingMode` | `'immediate'` or `'queued'` | Processing strategy |
| `BatchSize` | Integer (e.g., `10`) | Orders to process per run |
| `EnqueueNewOrders` | `0` (false) or `1` (true) | Whether to fetch new orders |

## Common Scenarios

### Scenario 1: Process Backlog Only (No New Fetches)

**Use case**: You have 500 pending orders and want to process them 10 at a time without fetching more.

```sql
UPDATE Schedule
SET ProcessingMode = 'queued',
    BatchSize = 10,
    EnqueueNewOrders = 0  -- Don't fetch new orders
WHERE Id = 1;
```

### Scenario 2: Continuous Processing with Fetching

**Use case**: Fetch new orders every hour, process 20 at a time.

```sql
UPDATE Schedule
SET ProcessingMode = 'queued',
    BatchSize = 20,
    EnqueueNewOrders = 1  -- Fetch new orders each run
WHERE Id = 1;
```

### Scenario 3: Retry Failed Orders

```sql
-- Reset failed orders to pending (they'll be retried)
UPDATE PendingOrders
SET Status = 'pending',
    RetryCount = 0,
    ErrorMessage = NULL
WHERE ApiNumber = 1 AND Status = 'failed';
```

## Monitoring & Troubleshooting

### Check Pending Orders

```sql
SELECT COUNT(*) as PendingCount
FROM PendingOrders
WHERE ApiNumber = 1 AND Status = 'pending';
```

### View Failed Orders

```sql
SELECT OrderId, ErrorMessage, RetryCount
FROM PendingOrders
WHERE ApiNumber = 1 AND Status = 'failed'
ORDER BY FailedAt DESC;
```

### Clear Queue (Dangerous!)

```sql
-- Delete all pending/failed orders
DELETE FROM PendingOrders
WHERE ApiNumber = 1 AND Status IN ('pending', 'failed');
```

## Logs

Look for these messages in the service logs:

```
API #1 [QUEUED MODE]: Batch size: 10, Enqueue new: True
API #1: Fetching primary API to enqueue new orders
API #1: Enqueued 47 new orders, 0 already queued/processed
API #1 queue stats: 42 pending, 0 processing, 5 failed, 1248 total processed
API #1: Processing batch of 10 orders
API #1 batch complete: 10 processed, 0 failed. Queue: 32 pending, 5 failed
```

## Switch Back to Immediate Mode

```sql
UPDATE Schedule
SET ProcessingMode = 'immediate'
WHERE Id = 1;
```

**Note**: Switching back doesn't delete the queue. Pending orders remain in `PendingOrders` table but won't be processed.

## Need More Help?

See [QUEUE-MODE.md](./QUEUE-MODE.md) for detailed documentation including:
- Complete workflow explanations
- Performance considerations
- Migration guide
- Advanced SQL queries