-- =====================================================
-- Quick Fix: Validate and Report JSON Issues
-- Date: 2025-12-02
-- Description: Check all SubAction configurations for valid JSON
-- =====================================================

-- Check for invalid JSON in SubAction configurations
SELECT
    p.ApiNumber,
    p.ApiName,
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    CASE
        WHEN json_valid(s.Configuration) = 1 THEN '✓ Valid'
        ELSE '✗ INVALID - NEEDS FIX'
    END as JSONStatus,
    CASE
        WHEN json_valid(s.Configuration) = 0
        THEN substr(s.Configuration, 1, 100)
        ELSE NULL
    END as InvalidSnippet
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
ORDER BY p.ApiNumber, s.ActionNumber;

-- Find specific common JSON issues
SELECT
    'Single Quotes Issue' as IssueType,
    p.ApiNumber,
    s.ActionNumber,
    s.ActionName
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE s.Configuration LIKE '%''%''%'  -- Contains single quotes around values
   OR s.Configuration LIKE '%''''%';   -- Contains single quotes in values

-- Count invalid configurations
SELECT
    COUNT(*) as TotalInvalid,
    'SubActions with invalid JSON' as Description
FROM SubAction
WHERE json_valid(Configuration) = 0;

-- Show example of fixing single quote issue
-- Uncomment and modify the ActionNumber to fix specific action:
/*
UPDATE SubAction
SET Configuration = replace(replace(Configuration, '''', '"'), '""', '"')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 3
  AND json_valid(Configuration) = 0;
*/

-- =====================================================
-- NOTES:
-- =====================================================
-- Common JSON issues:
-- 1. Single quotes (') instead of double quotes (")
--    Fix: Replace ' with " carefully
-- 2. Trailing commas in objects/arrays
--    Fix: Remove trailing commas before } or ]
-- 3. Unescaped quotes in string values
--    Fix: Escape with backslash (\")
-- 4. Missing quotes around property names
--    Fix: Add quotes around all property names
--
-- Always validate after fixing:
--   SELECT json_valid(Configuration) FROM SubAction WHERE Id = X;
-- =====================================================
