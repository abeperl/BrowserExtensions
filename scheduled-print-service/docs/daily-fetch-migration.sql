-- Migration: Add LastFetchedAt column to PrimaryApi table
-- Purpose: Track when primary API was last fetched to enable daily refresh logic
--
-- New Behavior:
--   - Primary API will be fetched if: no records exist in PendingOrders OR it's a new day
--   - This allows picking up new orders that may have been added since last fetch
--   - The service auto-migrates this column on startup, so manual execution is optional

-- Add LastFetchedAt column (nullable, since existing APIs haven't been fetched yet)
ALTER TABLE PrimaryApi ADD COLUMN LastFetchedAt TEXT;

-- Optional: Set existing APIs to have been "fetched" yesterday so they fetch on next run
-- UPDATE PrimaryApi SET LastFetchedAt = date('now', '-1 day');

-- Optional: View current state
-- SELECT ApiNumber, ApiName, LastFetchedAt FROM PrimaryApi;