# Token Caching Fix

**Date:** 2025-12-02
**Issue:** Token renewal happens for every API call instead of reusing cached tokens

## Problem Description

The scheduled print service was renewing authentication tokens for every API call, even when executing multiple APIs within the same schedule run. This resulted in:

1. Unnecessary login requests (3 APIs = 3 logins per schedule run)
2. Slower execution time
3. Potential rate limiting issues
4. Increased server load

### Example from Logs

```
12:00:00 - Schedule starts, executing 3 APIs
12:00:01 - API #1: 401 Unauthorized → Token renewal → Success
12:00:05 - API #2: 401 Unauthorized → Token renewal → Success
12:00:06 - API #3: 401 Unauthorized → Token renewal → Success
```

Each API received 401 even though tokens were renewed seconds earlier and had 5-hour expiration.

## Root Cause

1. **`DatabaseApiConfigService.LoadApiConfig()`** was loading the static Bearer token from the database `PrimaryApi.Headers` JSON field, NOT the cached token from the `ApiAuth` table.

2. **`TokenRenewalService.RenewTokenAsync()`** was hardcoding token expiration to 24 hours instead of parsing the actual expiration from the JWT token (which is 5 hours).

## Solution

### Change 1: Load Cached Token and Update Cookies in LoadApiConfig

**File:** `Services/DatabaseApiConfigService.cs`

Modified the `LoadApiConfig` method to:
1. Load cached token from `ApiAuth` table via `LoadAuthCredentials()`
2. Check if cached token is still valid (expires > 5 minutes from now)
3. Use cached token if valid, otherwise fall back to static token from Headers
4. **CRITICAL:** Update cookies dictionary with cached token for Puppeteer browser pages

```csharp
// Try to load cached token from ApiAuth table first (for better token reuse)
var (_, _, cachedToken, tokenExpiresAt) = LoadAuthCredentials(config.BaseUrl);

// Use cached token if it's still valid (not expired and has >5 minutes remaining)
if (!string.IsNullOrEmpty(cachedToken) && tokenExpiresAt.HasValue &&
    tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
{
    config.BearerToken = cachedToken;
    _logger.LogDebug("Using cached token from ApiAuth table, expires at {ExpiresAt}", tokenExpiresAt);
}
// Otherwise, fall back to token from Headers JSON (static/placeholder token)
else if (headersDoc.RootElement.TryGetProperty("Authorization", out var authElement))
{
    // ... use static token
}
```

### Change 2: Parse JWT Expiration Time

**File:** `Services/TokenRenewalService.cs`

Modified `RenewTokenAsync` to:
1. Parse the JWT token to extract the `exp` (expiration) claim
2. Convert Unix timestamp to DateTime
3. Save accurate expiration time to database
4. Fall back to 5 hours if JWT parsing fails

```csharp
// Parse JWT token to extract actual expiration time
var parts = newToken.Split('.');
if (parts.Length == 3)
{
    // Decode Base64Url payload
    var payload = parts[1];
    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
    payload = payload.Replace('-', '+').Replace('_', '/');

    var payloadBytes = Convert.FromBase64String(payload);
    var payloadJson = Encoding.UTF8.GetString(payloadBytes);
    var payloadDoc = JsonDocument.Parse(payloadJson);

    if (payloadDoc.RootElement.TryGetProperty("exp", out var expElement))
    {
        var exp = expElement.GetInt64();
        expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
    }
}
```

## Expected Behavior After Fix

```
12:00:00 - Schedule starts, executing 3 APIs
12:00:01 - API #1: 401 Unauthorized → Token renewal → Success (token cached, expires 17:00:01)
12:00:05 - API #2: Using cached token from ApiAuth table, expires at 17:00:01 → Success
12:00:06 - API #3: Using cached token from ApiAuth table, expires at 17:00:01 → Success
```

**Result:** Only 1 token renewal for all 3 APIs in the schedule

## Token Lifecycle

1. **Initial Request**: API call with old/expired token → 401 Unauthorized
2. **Token Renewal**:
   - Login to `/api/account/login`
   - Extract JWT token and parse expiration
   - Save token + expiration to `ApiAuth` table
3. **Subsequent Requests**:
   - `LoadApiConfig()` loads cached token from `ApiAuth` table
   - Checks if token expires > 5 minutes from now
   - Uses cached token if valid
4. **Token Expiration**:
   - Token expires after ~5 hours (from JWT exp claim)
   - Next API call triggers renewal (back to step 1)

## Database Schema

The fix relies on the existing `ApiAuth` table:

```sql
CREATE TABLE ApiAuth (
    BaseUrl TEXT PRIMARY KEY,
    Username TEXT NOT NULL,
    Password TEXT NOT NULL,
    BearerToken TEXT,
    TokenExpiresAt TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);
```

## Testing

To verify the fix is working:

1. Stop the service
2. Delete the cached token from database:
   ```sql
   UPDATE ApiAuth SET BearerToken = NULL, TokenExpiresAt = NULL
   WHERE BaseUrl = 'https://mj.3plnext.com';
   ```
3. Start the service
4. Watch logs during next schedule run
5. Should see:
   - First API: "Attempting to renew authentication token"
   - Subsequent APIs: "Using cached token from ApiAuth table"

## Performance Impact

**Before:**
- 3 token renewals per schedule run
- ~900ms total for token renewal (300ms per login)
- Additional server load from repeated logins

**After:**
- 1 token renewal per schedule run (first run or after 5 hours)
- ~300ms total for token renewal
- Cached tokens reused for 5 hours

**Improvement:** ~67% reduction in token renewal overhead

## Backward Compatibility

The changes are backward compatible:
- If cached token is unavailable or expired, falls back to static token from Headers
- If JWT parsing fails, falls back to 5-hour default expiration
- Existing database schema unchanged (uses existing ApiAuth table)

## Related Files

- `Services/DatabaseApiConfigService.cs` - Loads cached tokens
- `Services/TokenRenewalService.cs` - Parses JWT expiration and caches tokens
- `Services/DatabaseSchedulerService.cs` - Calls LoadApiConfig for each API
- `Models/ApiConfig.cs` - Contains BearerToken property

## Additional Notes

- Tokens are considered expired if they expire within 5 minutes (safety buffer)
- JWT exp claim is a Unix timestamp in seconds
- The API returns JWT tokens with 5-hour expiration (not 24 hours)
- Token renewal is thread-safe (uses lock in TokenRenewalService)
