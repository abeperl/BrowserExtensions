# CallApi Action Response Injection Fix

**Date**: 2025-12-15  
**Issue**: API 3 sub-actions were not working correctly - the second action (CallApi) was not saving its JSON response to memory for use by chained actions.

## Problem Summary

The first two sub-actions for API 3 are designed to work as a pair:

1. **Action 1 (NavigateOnly)**: Navigate to a URL and keep the browser page alive in memory
2. **Action 2 (CallApi)**: Fetch an API endpoint and save its JSON response to the browser's memory

**The Problem**: Action 2 was making the HTTP call but **NOT saving the JSON response to the browser's memory**. This meant:
- Chained actions couldn't access the data
- JavaScript injections had no data to work with
- The response was lost after logging

## Solution

Modified `ExecuteCallApiAsync` in [SubActionExecutor.cs](../ScheduledPrintService/Services/SubActionExecutor.cs) to inject the API response into the captured browser page's memory after successfully fetching it.

### How It Works

After the HTTP response is received, the code now:

1. **Checks if a page is captured** (from a previous NavigateOnly action)
   ```csharp
   if (_capturedPage != null && !string.IsNullOrWhiteSpace(responseBody))
   ```

2. **Determines the variable name** for storing the response
   - Uses `action.OutputVariableName` if configured (e.g., `"__orderDetailsResponse"`)
   - Otherwise defaults to `"__{ActionType}Response"` (e.g., `"__CallApiResponse"`)

3. **Injects the JSON into browser memory** using Puppeteer's `EvaluateFunctionAsync`
   ```javascript
   // Store in window global
   window[varName] = data;
   
   // Also store in sessionStorage for persistence
   sessionStorage.setItem(varName, jsonString);
   ```

### Key Features

✅ **No Navigation**: The HTTP call happens server-side; the browser page is NOT navigated away  
✅ **Data Persistence**: Response is stored in both `window` and `sessionStorage` for reliability  
✅ **Configurable Variable Name**: Use `OutputVariableName` in action configuration to customize where data is stored  
✅ **Error Handling**: Non-fatal error logging if injection fails - subsequent actions can continue  
✅ **Logging**: Detailed logs show what variable was injected and data size  

## Configuration Example

For API 3, configure Action 2 (CallApi) to save order details:

```json
{
  "Type": "CallApi",
  "Name": "Fetch Order Details",
  "Endpoint": "/api/Orders/{id}/Details",
  "Method": "GET",
  "OutputVariableName": "__orderDetailsResponse",
  "ContinueOnError": true
}
```

Then Action 3+ can access the data:
```javascript
// In subsequent JavaScript injections or HTML templates
if (window.__orderDetailsResponse?.data?.OrderItems) {
    // Process order items
    console.log(window.__orderDetailsResponse.data.OrderItems);
}
```

## Action Flow for API 3

```
┌─────────────────────────────────────────────────────────┐
│ Action 1: NavigateOnly                                  │
│ - Navigate to picklist page URL                         │
│ - KEEP browser page alive in _capturedPage              │
│ - HTML injections capture data from page (e.g., window) │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│ Action 2: CallApi                                       │
│ - Fetch /api/Orders/{id}/Details via HTTP              │
│ - Save response JSON to _capturedPage memory            │
│   → window.__orderDetailsResponse = {...}              │
│   → sessionStorage["__orderDetailsResponse"] = "{...}" │
│ - DO NOT navigate away (page still in _capturedPage)    │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│ Action 3+: Subsequent Actions                           │
│ - Can now access __orderDetailsResponse from memory     │
│ - HTML injections can use the data for rendering        │
│ - Can trigger another API call, print, etc.             │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│ PrintCapturedPage / SaveCapturedHtml                    │
│ - Render the kept-alive page with injected data         │
│ - Generate PDF and print                                │
└─────────────────────────────────────────────────────────┘
```

## Backward Compatibility

This change is **fully backward compatible**:
- Existing CallApi actions continue to work unchanged
- No database changes required
- No configuration changes required
- Injection only happens if a page is captured (safe to no-op otherwise)

## Testing Checklist

After deploying this fix, test API 3 with:

- [ ] Action 1 navigates to the correct URL
- [ ] Action 2 fetches the API and logs injection successful
- [ ] Browser console shows `✅ API response injected into page memory` message
- [ ] Subsequent actions can access the stored data
- [ ] HTML injections correctly reference `window.__orderDetailsResponse`
- [ ] Production Status column displays correctly
- [ ] PDFs print with all injected data visible

## Logs to Look For

**Success**: 
```
Injecting API response into page memory as '__orderDetailsResponse' (length: 12345)
✅ API response injected into page memory
```

**Warning (Non-Fatal)**:
```
Failed to inject API response into page memory (non-fatal - chaining may fail)
No captured page available - API response not injected to memory
```

## Related Files

- [SubActionExecutor.cs](../ScheduledPrintService/Services/SubActionExecutor.cs) - Core execution logic
- [ApiConfig.cs](../ScheduledPrintService/Models/ApiConfig.cs) - SubAction configuration model
- [CHAINING-GUIDE.md](./CHAINING-GUIDE.md) - Action chaining documentation
- [final-production-status-setup.sql](../scripts/final-production-status-setup.sql) - API 3 HTML injection configuration

## Questions?

If the fix doesn't work as expected, check:
1. Are logs showing the injection happened?
2. Does the browser console show any errors?
3. Is the captured page actually available when Action 2 runs?
4. Is the JSON response valid (parseable)?
