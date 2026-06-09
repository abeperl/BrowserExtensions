-- Script to enable/disable Save and Print sub-actions for API 3 independently
-- This allows you to control whether PDFs are saved, printed, or both

-- View current status
SELECT
    sa.Id,
    sa.ActionNumber,
    sa.ActionName,
    sa.ActionType,
    sa.ExecutionOrder,
    CASE WHEN sa.IsEnabled = 1 THEN 'ENABLED' ELSE 'DISABLED' END as Status
FROM SubAction sa
JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 3
ORDER BY sa.ExecutionOrder;

-- ============================================
-- ENABLE/DISABLE COMMANDS
-- ============================================

-- Enable Save PDF (Action 2)
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15;

-- Disable Save PDF (Action 2)
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15;

-- Enable Print PDF (Action 3)
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16;

-- Disable Print PDF (Action 3)
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16;

-- ============================================
-- COMMON CONFIGURATIONS
-- ============================================

-- Save ONLY (no printing)
-- BEGIN TRANSACTION;
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; -- Enable Save
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; -- Disable Print
-- COMMIT;

-- Print ONLY (no saving)
-- BEGIN TRANSACTION;
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; -- Disable Save
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; -- Enable Print
-- COMMIT;

-- Both Save AND Print (default)
-- BEGIN TRANSACTION;
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; -- Enable Save
-- UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; -- Enable Print
-- COMMIT;

-- Neither (only navigate)
-- BEGIN TRANSACTION;
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; -- Disable Save
-- UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; -- Disable Print
-- COMMIT;
