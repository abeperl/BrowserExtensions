# C# Integration Guide for Production Status Column

## Current State Analysis

✅ **The C# service already has most of the infrastructure in place!**

### What's Already Working

1. **Picklist Response Storage** (`SubActionExecutor.cs:36`)
   - Variable: `_lastInterceptedPicklistJson`
   - Stores the full picklist API response
   - Used for DOM population via `tf.binder.scatter()`

2. **HTML Injection System** (`SubActionExecutor.cs:2674`)
   - Method: `PerformHtmlInjectionsAsync()`
   - Supports placeholder replacement
   - Injects HTML/JavaScript into page after navigation

3. **Page Evaluation** (`SubActionExecutor.cs:1699`)
   - Already uses `page.EvaluateFunctionAsync()` to inject data
   - Already uses `window.tf.binder.scatter()` to populate DOM

## Required Changes

### 1. Store Picklist Response in Window Object

**Location**: `SubActionExecutor.cs` around line 1736 (after `tf.binder.scatter()`)

**Add this code**:
```csharp
// Store picklist response in window for Production Status column
window.__picklistResponse = data;
sessionStorage.setItem('__picklistResponse', jsonString);
console.log('✅ Picklist response stored in memory');
```

**Full context**:
```csharp
// Scatter data to DOM using tf.binder
if (typeof window.tf !== 'undefined' && window.tf.binder && window.tf.binder.scatter) {
    window.tf.binder.scatter(data.data, '.data-scatter');
    window.tf.binder.scatter(data.data.PickList, '.data-data-scatter');

    // ⭐ ADD THIS: Store picklist response in window for Production Status column
    window.__picklistResponse = data;
    sessionStorage.setItem('__picklistResponse', jsonString);
    console.log('✅ Picklist response stored in memory');

    // Generate barcodes if function exists
    if (typeof GenerateOrderNoBarcode === 'function') {
        GenerateOrderNoBarcode();
    }

    return 'Success: Data scattered to DOM';
}
```

### 2. Handle NavigateOnly Actions (Order Details Fetch)

The new sub-action (ActionNumber 7) fetches order details. We need to:

1. **Check if it's a NavigateOnly action that makes an API call**
2. **Store the response in window object**

**Location**: Check `ExecuteNavigateOnlyAsync()` method

**Required Implementation**:
```csharp
private async Task ExecuteNavigateOnlyAsync(SubAction action, string orderId, Dictionary<string, object>? context, CancellationToken ct)
{
    // ... existing code ...

    // If this is an API call (not a page navigation)
    if (!string.IsNullOrWhiteSpace(action.Endpoint) && action.Endpoint.Contains("/api/"))
    {
        _logger.LogInformation("NavigateOnly: Making API call to {Endpoint}", action.Endpoint);

        // Make the API call
        var response = await MakeApiCallAsync(action, orderId, context, ct);

        // Store response in window if page is available
        if (_capturedPage != null && !string.IsNullOrWhiteSpace(response))
        {
            var memoryKey = action.Configuration?.MemoryKey ?? "apiResponse";

            await _capturedPage.EvaluateFunctionAsync(@"(responseJson, key) => {
                try {
                    const data = JSON.parse(responseJson);
                    window['__' + key + 'Response'] = data;
                    sessionStorage.setItem('__' + key + 'Response', responseJson);
                    console.log('✅ ' + key + ' response stored in memory');
                } catch(e) {
                    console.error('❌ Failed to store ' + key + ' response:', e);
                }
            }", response, memoryKey);

            _logger.LogInformation("Stored {Key} response in window object", memoryKey);
        }
    }
}
```

### 3. Alternative: Simpler Approach Using HTML Injection

Instead of modifying C# code extensively, we can add JavaScript injection in the HTML injection itself:

**Update the Navigate action's HTML injection** to include:

```javascript
<script>
// Intercept the picklist API response
(function() {
    // Wait for window.tf to be available
    const waitForData = setInterval(function() {
        if (window.tf && window.tf.page && window.tf.page.data) {
            const picklistData = window.tf.page.data;

            // Store in window for Production Status column
            window.__picklistResponse = {
                responseCode: 0,
                responseType: 'Success',
                data: picklistData
            };

            sessionStorage.setItem('__picklistResponse', JSON.stringify(window.__picklistResponse));
            console.log('✅ Picklist response captured from tf.page.data');
            clearInterval(waitForData);
        }
    }, 100);

    // Timeout after 10 seconds
    setTimeout(function() {
        clearInterval(waitForData);
    }, 10000);
})();
</script>
```

## Recommended Approach

**Use the HTML Injection method** (Alternative 3) because:
1. ✅ No C# code changes required
2. ✅ Works with existing infrastructure
3. ✅ Can be updated via SQL scripts
4. ✅ Easier to test and debug

### Implementation Steps

1. **Add picklist capture script** to Navigate action HTML injection
2. **Order details are already handled** by the new sub-action (Action 7)
3. **Production Status column script** uses the captured data

## SQL Script Update

Update the Navigate action to include both scripts:

```sql
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.HtmlInjections',
    json_array(
        -- Existing customer name injection
        json_object(
            'HtmlTemplate', '<h4 class="panel-box-title text-center">{customerName}</h4>',
            'InsertPosition', 'append',
            'TargetSelector', '.modal-head'
        ),
        -- NEW: Capture picklist data from window.tf.page.data
        json_object(
            'HtmlTemplate', '<script>
(function() {
    const waitForData = setInterval(function() {
        if (window.tf && window.tf.page && window.tf.page.data) {
            const picklistData = window.tf.page.data;
            window.__picklistResponse = {
                responseCode: 0,
                responseType: "Success",
                data: picklistData
            };
            sessionStorage.setItem("__picklistResponse", JSON.stringify(window.__picklistResponse));
            console.log("✅ Picklist response captured");
            clearInterval(waitForData);
        }
    }, 100);
    setTimeout(function() { clearInterval(waitForData); }, 10000);
})();
</script>',
            'InsertPosition', 'append',
            'TargetSelector', 'head'
        ),
        -- Production Status column injection script
        json_object(
            'HtmlTemplate', '<script src="/path/to/production-status-column-injector.js"></script>',
            'InsertPosition', 'append',
            'TargetSelector', 'head'
        )
    )
)
WHERE PrimaryApiId = 7 AND ActionNumber = 1;
```

## Testing

1. **Check browser console** for:
   - ✅ Picklist response captured
   - ✅ Order details stored in memory
   - 📋 Created status lookup map with X entries
   - ✅ Production Status column injection complete

2. **Check window objects**:
   ```javascript
   console.log(window.__picklistResponse);
   console.log(window.__orderDetailsResponse);
   console.log(window.__productionStatusCache);
   ```

3. **Manual trigger**:
   ```javascript
   window.addProductionStatusColumn();
   ```

## Next Steps

1. ✅ Database configuration is complete (sub-action added)
2. ⏳ Update Navigate action HTML injections to capture picklist data
3. ⏳ Verify order details sub-action stores response correctly
4. ⏳ Test end-to-end workflow
5. ⏳ Verify Production Status column appears with correct data