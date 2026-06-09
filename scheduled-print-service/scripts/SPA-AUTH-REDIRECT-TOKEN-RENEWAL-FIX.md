# SPA Authentication Redirect Token Renewal Fix

**Date:** 2025-12-02
**Issue:** Token not renewing when Puppeteer navigation fails with 401 error
**Root Cause:** Token renewal logic only triggered on HTTP API failures, not on in-page JavaScript 401 errors

## Problem Analysis

### Symptom
API #2 failed with 401 error during Puppeteer page navigation, but the service did not attempt to renew the token:

```
2025-12-02 17:00:19.203 -05:00 [WRN] [CONSOLE ERROR] Failed to load resource: the server responded with a status of 401 (Unauthorized)
2025-12-02 17:00:19.793 -05:00 [WRN] Effective window.location.href (https://mj.3plnext.com/#account?returnurl=Outbound%2FManualPicking%3Fid%3D7167) does not match expected URL (https://mj.3plnext.com/#Outbound/ManualPicking?id=7167) - SPA may have redirected to home or token substitution failed
```

### Root Cause Analysis

1. **HTTP API Call Succeeded:** The initial HTTP call to fetch orders returned 200 OK
2. **In-Page JavaScript Failed:** After navigating to the page, the page's JavaScript tried to load resources and got 401
3. **SPA Redirect to Login:** The page redirected to `#account?returnurl=...` (login page)
4. **No Token Renewal:** The code detected the redirect but did NOT attempt token renewal

### Why Existing Token Renewal Didn't Work

**HTTP API Token Renewal** (`OrderApiService.cs:121-149`):
- Only triggers when `HttpClient.SendAsync()` returns 401
- Does NOT trigger for 401 errors inside Puppeteer pages
- Works perfectly for HTTP API calls

**Puppeteer Navigation** (`SubActionExecutor.cs:1434`):
- Detected the SPA redirect to login page
- Logged a warning about URL mismatch
- Retried navigation with same invalid token → Failed again

## Solution

Enhanced SPA redirect detection logic to recognize login page redirects as authentication failures and trigger token renewal.

### Code Changes

**File:** `Services/SubActionExecutor.cs`
**Lines:** 1428-1513 (85 lines modified)

**Key Changes:**

1. **Detect Login Page Redirect** (lines 1437-1438):
```csharp
if (effectiveHref.Contains("#account?returnurl=", StringComparison.OrdinalIgnoreCase) ||
    effectiveHref.Contains("#account/", StringComparison.OrdinalIgnoreCase))
```

2. **Renew Token on Detection** (line 1443):
```csharp
bool tokenRenewed = await _tokenRenewal.RenewTokenAsync(ct, forceRefresh: true);
```

3. **Update Page Credentials** (lines 1449-1475):
   - Update `localStorage.auth_token` with new token
   - Update page cookies with new credentials
   - Update HTTP client auth headers

4. **Retry Navigation with Fresh Token** (line 1481):
```csharp
await page.EvaluateExpressionAsync($"window.location.href='{url.Replace("'","%27")}'");
```

### How It Works Now

#### Before Fix:
```
API HTTP Call → 200 OK → Navigate to page
  ↓
Page JavaScript loads resources → 401 Unauthorized
  ↓
SPA redirects to login: #account?returnurl=...
  ↓
Code detects redirect → Logs warning → Retries with SAME invalid token → FAILS
```

#### After Fix:
```
API HTTP Call → 200 OK → Navigate to page
  ↓
Page JavaScript loads resources → 401 Unauthorized
  ↓
SPA redirects to login: #account?returnurl=...
  ↓
Code detects redirect → RENEWS TOKEN → Updates page credentials → Retries → SUCCESS ✓
```

## Implementation Details

### Login Page Detection Patterns

The code checks for two patterns:
1. `#account?returnurl=...` - Redirect to login with return URL
2. `#account/` - Direct navigation to account/login page

Both indicate authentication failure.

### Token and Cookie Updates

**1. localStorage Token:**
```csharp
await page.EvaluateExpressionAsync($"localStorage.setItem('auth_token', '{newToken.Replace("'", "\\'")}')");
```

**2. Page Cookies:**
```csharp
foreach (var cookie in newCookies)
{
    await page.SetCookieAsync(new PuppeteerSharp.CookieParam
    {
        Name = cookie.Key,
        Value = cookie.Value,
        Domain = new Uri(activeConfig.BaseUrl).Host,
        Path = "/",
        Secure = true,
        HttpOnly = false
    });
}
```

**3. HTTP Client Auth:**
```csharp
UpdateHttpClientAuth(); // Updates Bearer token and Cookie headers
```

### Error Handling

If token renewal fails after detecting login redirect:
```csharp
_logger.LogCritical("Failed to renew token after detecting SPA auth redirect - authentication is broken");
throw new TokenRenewalException("Unable to renew authentication token after SPA redirected to login");
```

This stops the service immediately to prevent repeated failures.

## Expected Behavior After Fix

### Successful Token Renewal:
```
[WRN] Effective window.location.href (...#account?returnurl=...) does not match expected URL
[WRN] Detected redirect to login page - attempting token renewal
[INF] Attempting to renew authentication token (forcing fresh token from server)
[INF] Token renewed successfully - updating token in database
[INF] Token renewed successfully after SPA auth redirect - updating page localStorage and retrying navigation
[DBG] Updated page localStorage with renewed token
[DBG] Updated page cookies with renewed credentials (2 cookies)
[INF] Navigation succeeded after token renewal
[DBG] Processing order: 7167
[INF] [1/6] Navigate - SS/SO Orders for order 7167
```

### Failed Token Renewal (Critical Error):
```
[WRN] Detected redirect to login page - attempting token renewal
[CRI] Failed to renew token after detecting SPA auth redirect - authentication is broken
System.Exception: Unable to renew authentication token after SPA redirected to login
```

## Testing

### Verify Fix Working:

1. **Check for token renewal logs:**
   - "Detected redirect to login page - attempting token renewal"
   - "Token renewed successfully after SPA auth redirect"
   - "Navigation succeeded after token renewal"

2. **Verify NO MORE redirect loops:**
   - Should NOT see repeated "window.location.href does not match expected URL" warnings
   - Should NOT see multiple failed navigation attempts with same order

3. **Check PDF generation:**
   - PDFs should generate successfully after token renewal
   - No missing orders due to auth failures

## Related Files

- `Services/SubActionExecutor.cs:1428-1513` - SPA redirect detection and token renewal
- `Services/TokenRenewalService.cs` - Token renewal service interface and implementation
- `Services/OrderApiService.cs:121-149` - HTTP API token renewal (existing, still used)
- `scripts/fix-api3-id-json-path.sql` - Previous API #3 fix
- `scripts/ARRAY-INDEX-FILTER-FIX.md` - Previous API #2 array handling fix

## When This Fix Applies

### Triggers Token Renewal When:
- Puppeteer navigation results in page redirecting to `#account?returnurl=...`
- Indicates SPA detected authentication failure (401/403)
- Token may be expired or invalidated server-side

### Does NOT Trigger When:
- HTTP API calls get 401 (handled by `OrderApiService` existing logic)
- Navigation URL mismatch for other reasons (original retry logic still applies)
- Page redirects to other non-login pages

## Token Expiration Notes

Database token expiration time may show future date even when server rejects token:
```sql
-- Token showed valid until tomorrow, but server rejected it today:
SELECT BaseUrl, datetime(TokenExpiresAt) as ExpiresAt, datetime('now') as CurrentTime
FROM ApiAuth;
-- ExpiresAt: 2025-12-03 16:45:04
-- CurrentTime: 2025-12-02 17:00:19
```

**Why?** The JWT expiration claim may differ from server-side token invalidation:
- Server may invalidate tokens earlier than JWT expiration
- Database stores JWT claim, but server has different expiration logic
- `forceRefresh: true` bypasses cached token and gets fresh one from server

This is why the fix uses `forceRefresh: true` when renewing tokens after SPA redirect.

## Published

**Date:** 2025-12-02 17:25
**File:** `C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\publish\ScheduledPrintService.exe`
**Published Using:** `scripts/publish.ps1 -SelfContained`
