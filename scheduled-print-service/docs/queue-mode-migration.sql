-- Migration script to add queue-based processing support to existing databases
-- This script is idempotent and can be run multiple times safely

-- ============================================================================
-- IMPORTANT: Schema Auto-Migration
-- ============================================================================
-- The application automatically handles schema migration when it starts.
-- DatabaseApiConfigService.EnsureScheduleColumnsExist() checks for missing
-- columns and adds them automatically.
--
-- You DO NOT need to run this script manually unless:
-- 1. You want to apply migrations before starting the service
-- 2. You're troubleshooting schema issues
-- 3. You're setting up a database outside of the service
-- ============================================================================

-- Manual migration instructions (OPTIONAL - only if needed):
-- SQLite doesn't support "IF NOT EXISTS" for ALTER TABLE, so check first:

-- Step 1: Check if columns exist
-- Run this query to see current Schedule table structure:
-- PRAGMA table_info(Schedule);

-- Step 2: Add missing columns (only run the ones that don't exist)
-- Uncomment and run ONLY if the column doesn't exist:

-- ALTER TABLE Schedule ADD COLUMN ProcessingMode TEXT DEFAULT 'immediate';
-- ALTER TABLE Schedule ADD COLUMN BatchSize INTEGER DEFAULT 10;
-- ALTER TABLE Schedule ADD COLUMN EnqueueNewOrders INTEGER DEFAULT 1;

-- Create PendingOrders table (for queue-based processing)
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

-- Example: Update existing schedule to use queued mode
-- UPDATE Schedule SET ProcessingMode = 'queued', BatchSize = 5, EnqueueNewOrders = 1 WHERE Id = 1;

-- Example: View pending orders for an API
-- SELECT * FROM PendingOrders WHERE ApiNumber = 1 AND Status = 'pending';

-- Example: View queue statistics for an API
-- SELECT
--     Status,
--     COUNT(*) as Count
-- FROM PendingOrders
-- WHERE ApiNumber = 1
-- GROUP BY Status;

-- Example: Clear failed orders for retry
-- UPDATE PendingOrders SET Status = 'pending', RetryCount = 0, ErrorMessage = NULL
-- WHERE ApiNumber = 1 AND Status = 'failed';

-- Example: Delete all pending/failed orders for an API (reset queue)
-- DELETE FROM PendingOrders WHERE ApiNumber = 1 AND Status IN ('pending', 'failed', 'processing');