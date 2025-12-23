-- Schema Migration: Add ApiHeaders table for site-specific HTTP headers
-- This replaces hardcoded header logic with database-driven configuration
-- Created: 2025-12-22

-- Step 1: Create ApiHeaders table to store custom headers per API
CREATE TABLE IF NOT EXISTS ApiHeaders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiAuthId INTEGER NOT NULL,
    HeaderName TEXT NOT NULL,
    HeaderValue TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ApiAuthId) REFERENCES ApiAuth(Id) ON DELETE CASCADE,
    UNIQUE(ApiAuthId, HeaderName)
);

-- Step 2: Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_apiheaders_apiauthid ON ApiHeaders(ApiAuthId);

-- Step 3: Populate headers for existing APIs

-- Get the ApiAuth ID for mj.3plnext.com
INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'WarehouseId', '4', 1
FROM ApiAuth
WHERE BaseUrl = 'https://mj.3plnext.com';

-- Get the ApiAuth ID for malchus.3plnext.com
INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'ClientId', '1', 1
FROM ApiAuth
WHERE BaseUrl = 'https://malchus.3plnext.com';

INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'StoreId', '1', 1
FROM ApiAuth
WHERE BaseUrl = 'https://malchus.3plnext.com';

-- Step 4: Verification query
SELECT
    a.BaseUrl,
    a.Username,
    h.HeaderName,
    h.HeaderValue,
    h.IsEnabled
FROM ApiAuth a
LEFT JOIN ApiHeaders h ON a.Id = h.ApiAuthId
ORDER BY a.BaseUrl, h.HeaderName;