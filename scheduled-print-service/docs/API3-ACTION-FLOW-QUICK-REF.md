# API 3 Quick Reference - Action Flow Fix

## What Was Fixed

**Before**: Action 2 (CallApi) was fetching the API but discarding the response
```
❌ API call made → Response logged → Data lost
```

**After**: Action 2 (CallApi) fetches the API AND saves response to browser memory
```
✅ API call made → Response logged → Data saved to window + sessionStorage → Available for next actions
```

## Your API 3 Setup

| Step | Action Type | Purpose | Key Behavior |
|------|-----------|---------|--------------|
| 1 | **NavigateOnly** | Navigate to picklist page | ✅ Keeps browser page ALIVE in memory |
| 2 | **CallApi** | Fetch order details API | ✅ NOW SAVES response to browser memory (NEW FIX) |
| 3+ | Chained actions | Access saved data & print | ✅ Can now read from `window.__orderDetailsResponse` |

## How to Verify It's Working

### 1. Check the logs after Action 2 runs:

```
[API #3] Injecting API response into page memory as '__CallApiResponse' (length: 5432)
[API #3] ✅ API response injected into page memory
```

### 2. Check browser console:

Press F12 in the browser window showing the picklist, then in console:

```javascript
// Should show your fetched data
console.log(window.__CallApiResponse)
console.log(sessionStorage.getItem('__CallApiResponse'))
```

### 3. Verify HTML injection can access it:

If your HTML injection scripts reference the data, they should now work:

```javascript
// Example: Production Status injection
const orderDetails = window.__orderDetailsResponse;
if (orderDetails?.data?.OrderItems) {
    // This now works because data is available!
}
```

## Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| "No captured page available" | Action 2 runs before Action 1 completes | Verify Action 1 is NavigateOnly, not GetUrlAndPrint |
| "Failed to inject" (non-fatal) | JSON parsing error in response | Check API returns valid JSON |
| Data not in sessionStorage | JavaScript error | Check browser console for errors |
| Next action can't find data | Wrong variable name | Use `OutputVariableName` in config to set explicit name |

## Configuration Tips

### To customize the variable name:

```json
{
  "Type": "CallApi",
  "Name": "Fetch Order Details",
  "Endpoint": "/api/Orders/{id}/Details",
  "Method": "GET",
  "OutputVariableName": "__myCustomName",
  "ContinueOnError": true
}
```

### Default behavior (if OutputVariableName not set):

- Variable name = `__CallApiResponse`
- Available as: `window.__CallApiResponse` and `sessionStorage['__CallApiResponse']`

## Performance Notes

✅ **No impact**: The injection happens asynchronously on the captured page  
✅ **Fast**: JSON serialization/injection is ~1-5ms for typical responses  
✅ **Reliable**: Both `window` and `sessionStorage` storage ensures data persists  

## When This Doesn't Apply

This fix is for **CallApi actions only**. Other action types work differently:

- **NavigateOnly**: Keeps page alive (no API response to inject)
- **GetUrlAndPrint**: Makes HTTP call AND prints inline (not chaining-focused)
- **GetHtmlAndPrint**: Makes HTTP call AND prints inline (not chaining-focused)

---

**Questions?** See [CALLAPI-RESPONSE-INJECTION.md](./CALLAPI-RESPONSE-INJECTION.md) for detailed documentation.
