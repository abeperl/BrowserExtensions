-- =====================================================
-- Migration: Fix Filter Location - Move from SubAction to PrimaryApi
-- Date: 2025-11-27
-- Description: Adds Configuration column to PrimaryApi and moves
--              filtering logic from sub-actions to primary API responses
-- =====================================================

-- Step 1: Add Configuration column to PrimaryApi if it doesn't exist
ALTER TABLE PrimaryApi ADD COLUMN Configuration TEXT;

-- Step 2: Update API #2 - Add filter configuration to Primary API
-- Filter for records where first 2 chars of index 17 are "SS" OR "SO"
UPDATE PrimaryApi
SET Configuration = '{"ChainedArrayJsonPath":"data","ChainedFilterArrayIndex":17,"ChainedFilterType":"StartsWithAny","ChainedFilterValue":"SS,SO"}'
WHERE ApiNumber = 2;

-- Step 3: Update API #4 - Add filter configuration to Primary API
-- Filter for records where first 2 chars of index 17 are NEITHER "SS" NOR "SO"
UPDATE PrimaryApi
SET Configuration = '{"ChainedArrayJsonPath":"data","ChainedFilterArrayIndex":17,"ChainedFilterType":"NotStartsWithAny","ChainedFilterValue":"SS,SO"}'
WHERE ApiNumber = 4;

-- Step 4: Remove filter from API #2 Sub-Action #1 (keep other settings)
UPDATE SubAction
SET Configuration = '{"ChainedArrayJsonPath":"data","UseChainedInput":true,"ChainedItemFieldPath":"[0]","Endpoint":"https://mj.3plnext.com/#Outbound/ManualPicking?id={id}","Method":"GET","WaitForNetworkIdleMs":3000,"MakeHiddenVisible":true,"ContinueOnError":true}'
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 1;

-- Step 5: Remove filter from API #4 Sub-Action #1 (keep other settings)
UPDATE SubAction
SET Configuration = '{"ChainedArrayJsonPath":"data","UseChainedInput":true,"ChainedItemFieldPath":"[0]","Endpoint":"https://mj.3plnext.com/#Outbound/ManualPicking?id={id}","Method":"GET","WaitForNetworkIdleMs":3000,"MakeHiddenVisible":true,"ContinueOnError":true}'
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 4)
  AND ActionNumber = 1;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Check PrimaryApi configurations
SELECT ApiNumber, ApiName, Configuration
FROM PrimaryApi
WHERE ApiNumber IN (2, 4);

-- Check SubAction configurations (should have no filter fields)
SELECT p.ApiNumber, s.ActionNumber, s.Configuration
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber IN (2, 4)
ORDER BY p.ApiNumber, s.ExecutionOrder;

-- =====================================================
-- NOTES:
-- =====================================================
-- After this migration:
--
-- API #2 (Picklist Datatable API - SS/SO items):
--   Primary API filters: data[i][17] StartsWithAny "SS,SO"
--   Matches: Records where first 2 chars are "SS" OR "SO"
--   Sub-Action #1: Processes only filtered records (no filter)
--   Sub-Action #2: Processes only filtered records (no filter)
--
-- API #4 (Picklist Datatable API - Non-SS/SO items):
--   Primary API filters: data[i][17] NotStartsWithAny "SS,SO"
--   Matches: Records where first 2 chars are NEITHER "SS" NOR "SO"
--   Sub-Action #1: Processes only filtered records (no filter)
--   Sub-Action #2: Processes only filtered records (no filter)
--
-- The application code must be updated to:
-- 1. Read Configuration from PrimaryApi table
-- 2. Apply filters to primary API response BEFORE calling sub-actions
-- 3. Support StartsWithAny and NotStartsWithAny filter types (comma-separated values)
-- 4. Pass only filtered records to sub-actions
-- =====================================================
