# Automatic Token Renewal

## Overview

The scheduled-print-service now includes automatic token renewal to handle authentication token expiration. The API tokens expire after 5 hours, and the service will automatically detect 401 Unauthorized responses and renew the token by logging in again.

## Configuration

Add your login credentials to the `Api` section in `appsettings.json`:

```json
{
  "Api": {
    "Enabled": true,
    "BaseUrl": "https://mj.3plnext.com",
    "BearerToken": "your-initial-token",
    "WarehouseId": 1,
    "UserEmail": "your-email@example.com",
    "Password": "your-password",
    "Cookies": {
      "userData": "{...}",
      "isRefreshedToken": "false",
      "token": "your-initial-token"
    }
  }
}
```

### Required Configuration

| Property | Description | Required |
|----------|-------------|----------|
| `UserEmail` | Login email address | Yes (for auto-renewal) |
| `Password` | Login password | Yes (for auto-renewal) |
| `BearerToken` | Initial bearer token | Optional (will be renewed) |
| `Cookies` | Initial cookies | Optional (will be updated) |

**Note**: If `UserEmail` or `Password` are not configured, the service will log a warning when it encounters a 401 error and will not attempt renewal.

## How It Works

### 1. Token Storage

The `TokenRenewalService` maintains the current authentication state:
- Current bearer token
- Current cookies (token, userData, isRefreshedToken)

Both `OrderApiService` and `SubActionExecutor` use this shared state for all HTTP requests.

### 2. 401 Detection

When any API call receives a `401 Unauthorized` response:

1. The service logs: `"Received 401 Unauthorized - token may have expired"`
2. Triggers token renewal automatically
3. Does not count the failed request as a retry attempt

### 3. Token Renewal Process

The renewal process:

1. **Login Request**: POST to `/api/account/login` with credentials
2. **Parse Response**: Extracts `token` and `userData` from JSON response
3. **Update State**: Stores new token and cookies in `TokenRenewalService`
4. **Update HTTP Clients**: Refreshes authorization headers on all HTTP clients
5. **Retry Request**: Immediately retries the failed request with new token

### Login Request Details

The login request mimics browser behavior:

```http
POST /api/account/login HTTP/1.1
Host: mj.3plnext.com
Authorization: Bearer null
Content-Type: application/json
WarehouseId: 1
Cookie: isRefreshedToken=false

{
  "userEmail": "your-email@example.com",
  "Password": "your-password"
}
```

### Expected Login Response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userData": {
    "userID": 108,
    "warehouseId": 4,
    "firstName": "John",
    "lastName": "Doe",
    ...
  }
}
```

## Retry Logic

The token renewal integrates with the existing retry mechanism:

1. **First 401**: Attempt token renewal (doesn't count as retry)
2. **Renewal Success**: Retry request immediately with new token
3. **Renewal Failure**: Continue with normal retry logic
4. **Subsequent 401s**: Will not attempt renewal again in same retry cycle

This prevents infinite renewal loops while ensuring tokens are refreshed when needed.

## Logging

### Successful Renewal

```
[WRN] Received 401 Unauthorized - token may have expired
[INF] Attempting to renew authentication token
[DBG] Sending login request to /api/account/login
[DBG] Login response: {"token":"...","userData":{...}}
[INF] Successfully renewed authentication token
[INF] Token renewed successfully, updating HTTP client
```

### Failed Renewal

```
[WRN] Received 401 Unauthorized - token may have expired
[INF] Attempting to renew authentication token
[ERR] Login failed with status 401: {"error":"Invalid credentials"}
[ERR] Failed to renew token, request will fail
```

### Configuration Missing

```
[WRN] Received 401 Unauthorized - token may have expired
[WRN] Cannot renew token: UserEmail or Password not configured
```

## Security Considerations

1. **Password Storage**: Credentials are stored in plain text in `appsettings.json`. Consider using:
   - Environment variables
   - Azure Key Vault
   - Windows Credential Manager
   - Encrypted configuration sections

2. **Token Lifetime**: Tokens expire after 5 hours. The service will automatically renew them.

3. **Credentials Protection**: Ensure `appsettings.json` has appropriate file permissions and is not committed to version control with real credentials.

## Example: Environment Variables (Recommended)

Instead of storing credentials in `appsettings.json`, use environment variables:

```json
{
  "Api": {
    "UserEmail": "",
    "Password": ""
  }
}
```

Then set environment variables:

```powershell
# Windows
$env:Api__UserEmail = "your-email@example.com"
$env:Api__Password = "your-password"

# Or in the service configuration
[Environment]::SetEnvironmentVariable("Api__UserEmail", "your-email@example.com", "Machine")
[Environment]::SetEnvironmentVariable("Api__Password", "your-password", "Machine")
```

The double underscore (`__`) in environment variable names maps to nested configuration sections.

## Troubleshooting

### Tokens Not Renewing

1. **Check Credentials**: Verify `UserEmail` and `Password` are correct
2. **Check Logs**: Look for "Cannot renew token" warnings
3. **Test Login**: Try the credentials in a browser
4. **Network Access**: Ensure service can reach `https://mj.3plnext.com`

### Still Getting 401 Errors

1. **Check Response Format**: Login response may have changed
2. **Review Logs**: Look for JSON parsing errors
3. **Manual Test**: Use the curl command to test login:

```bash
curl 'https://mj.3plnext.com/api/account/login' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer null' \
  --data-raw '{"userEmail":"your-email","Password":"your-password"}'
```

### Renewal Fails Silently

1. **Enable Debug Logging**: Set `Serilog.MinimumLevel.Default` to `"Debug"`
2. **Check File Logs**: Review logs in `logs/scheduled-print-service-YYYYMMDD.log`
3. **Network Issues**: Check for connectivity problems or firewall blocks

## Architecture

### Components

```
┌─────────────────────────────────────┐
│   TokenRenewalService (Singleton)   │
│   - Stores current token & cookies  │
│   - Handles login API calls         │
└─────────────────┬───────────────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
┌───────▼────────┐  ┌───────▼────────┐
│ OrderApiService│  │SubActionExecutor│
│ - Gets orders  │  │ - Sub-actions   │
│ - Detects 401  │  │ - Detects 401   │
│ - Calls renewal│  │ - Calls renewal │
└────────────────┘  └─────────────────┘
```

### Service Lifetime

- **TokenRenewalService**: Singleton (shared state across all requests)
- **OrderApiService**: Scoped (per HTTP client)
- **SubActionExecutor**: Scoped (per HTTP client)

## Testing

### Manual Test

1. **Set ManualMode**: `"ManualMode": true` in appsettings.json
2. **Use Expired Token**: Set `BearerToken` to an expired token
3. **Run Service**: Token should auto-renew on first 401

### Expected Behavior

```
[INF] API polling starting
[INF] Polling orders API...
[WRN] Received 401 Unauthorized - token may have expired
[INF] Attempting to renew authentication token
[INF] Successfully renewed authentication token
[INF] Processing 10 orders
```

## Migration from Previous Version

No breaking changes. Existing configurations work without modification:

**Before** (still works):
```json
{
  "Api": {
    "BearerToken": "your-token",
    "Cookies": {...}
  }
}
```

**After** (with auto-renewal):
```json
{
  "Api": {
    "BearerToken": "your-token",
    "UserEmail": "your-email",
    "Password": "your-password",
    "Cookies": {...}
  }
}
```

If credentials are not provided, the service will continue to use the static token (no auto-renewal).
