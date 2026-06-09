-- Migration: Add LocalJsonFilePath column to PrimaryApi table
-- This allows using a local JSON file as the primary data source instead of an API call
-- Run this script on existing databases to add the new column

-- Check if column exists before adding (SQLite doesn't support IF NOT EXISTS for columns)
-- This script is idempotent - running it multiple times is safe

-- Add LocalJsonFilePath column to PrimaryApi table
-- The column will be NULL by default, meaning the API endpoint will be used
ALTER TABLE PrimaryApi ADD COLUMN LocalJsonFilePath TEXT;

-- Usage example:
-- UPDATE PrimaryApi SET LocalJsonFilePath = 'data/orders.json' WHERE ApiNumber = 1;
--
-- The path can be:
--   - Relative: 'data/orders.json' (resolved against app's base directory)
--   - Absolute: 'C:\Data\orders.json'
--
-- When LocalJsonFilePath is set, the service reads from the file instead of calling the API endpoint.
-- All existing filtering options (PrimaryFilterArrayJsonPath, IdJsonPath, etc.) work with local files.