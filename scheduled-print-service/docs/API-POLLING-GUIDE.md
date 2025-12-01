# API Polling Guide - Action-Based Workflow

The Scheduled Print Service supports polling REST APIs and executing configurable sub-actions for each record returned.

## Overview

The service implements a flexible action-based workflow:

1. **Poll API**: Calls GetOrdersList endpoint at regular intervals
2. **Parse Response**: Extracts order IDs from JSON response
3. **Filter Duplicates**: Skips already-processed orders
4. **Execute Sub-Actions**: For each new order, runs a chain of configurable actions:
   - Call other APIs to perform operations
   - Fetch HTML content from APIs
   - Print HTML to PDF
   - Add delays between actions

## Architecture

```
┌─────────────────────────────────────────────────────┐
│ ApiPollSchedulerService (Background Service)       │
│                                                     │
│  ┌──────────────────────────────────────────────┐ │
│  │ 1. Poll GetOrdersList API                    │ │
│  │    ↓                                          │ │
│  │ 2. Extract Order IDs from JSON               │ │
│  │    ↓                                          │ │
│  │ 3. Filter out processed orders               │ │
│  │    ↓                                          │ │
│  │ 4. For each new order:                       │ │
│  │    ┌─────────────────────────────────────┐  │ │
│  │    │ SubActionExecutor                   │  │ │
│  │    │  ├─ CallApi: POST update status     │  │ │
│  │    │  ├─ Delay: 1000ms                   │  │ │
│  │    │  └─ GetHtmlAndPrint: fetch & print  │  │ │
│  │    └─────────────────────────────────────┘  │ │
│  │    ↓                                          │ │
│  │ 5. Mark order as processed                   │ │
│  └──────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## Configuration

### Full Example

```json
{
  "Api": {
    "Enabled": true,
    "BaseUrl": "https://mj.3plnext.com",
    "BearerToken": "your-jwt-token-here",
    "WarehouseId": 1,
    "Cookies": {
      "userData": "{...}",
      "token": "your-token-here"
    },
    "IdJsonPath": "[0]",
    "ProcessedIdsPath": "processed-orders.txt",
    "DefaultRequest": {
      "Draw": 1,
      "Start": 0,
      "Length": 25,
      "StatusName": "1,2,3,4,5,6,7,8,9,10"
    },
    "SubActions": [
      {
        "Type": "CallApi",
        "Name": "Update Order Status",
        "Endpoint": "/api/order/UpdateStatus",
        "Method": "POST",
        "RequestBody": "{\"orderId\":\"{id}\",\"status\":\"Processing\"}",
        "ContinueOnError": true
      },
      {
        "Type": "Delay",
        "Name": "Wait before fetching label",
        "DelayMilliseconds": 1000,
        "ContinueOnError": true
      },
      {
        "Type": "GetHtmlAndPrint",
        "Name": "Print Shipping Label",
        "Endpoint": "/api/order/GetShippingLabel/{id}",
        "Method": "GET",
        "HtmlJsonPath": "html",
        "ContinueOnError": false
      }
    ]
  },
  "Scheduler": {
    "Enabled": true,
    "IntervalSeconds": 300
  }
}
```

### Core Configuration

#### Api Section

- **Enabled** (bool): Enable/disable API polling
- **BaseUrl** (string): Base URL for all API calls
- **BearerToken** (string): JWT token for Authorization header
- **WarehouseId** (int): Warehouse ID sent in custom header
- **Cookies** (object): Key-value pairs of cookies
- **IdJsonPath** (string): JSON path to extract order ID from each record
  - `"[0]"` - First element in array
  - `"orderId"` - Property named "orderId"
- **ProcessedIdsPath** (string): File to track processed order IDs (prevents duplicates)
- **DefaultRequest** (object): Parameters for GetOrdersList endpoint
- **SubActions** (array): Ordered list of actions to execute for each order

### Sub-Actions

SubActions is an array of actions executed sequentially for each order. Actions are executed in the order defined.

#### Action Type: CallApi

Calls an API endpoint (useful for updating order status, triggering operations, etc.).

```json
{
  "Type": "CallApi",
  "Name": "Update Order Status",
  "Endpoint": "/api/order/UpdateStatus",
  "Method": "POST",
  "RequestBody": "{\"orderId\":\"{id}\",\"status\":\"Processing\"}",
  "Headers": {
    "X-Custom-Header": "value"
  },
  "ContinueOnError": true
}
```

**Fields:**
- **Type**: `"CallApi"`
- **Name**: Display name for logging
- **Endpoint**: API endpoint (supports `{id}` placeholder)
- **Method**: HTTP method (GET, POST, PUT, DELETE)
- **RequestBody**: JSON body template (supports `{id}` placeholder)
- **Headers**: Additional headers for this request
- **ContinueOnError**: If true, continues to next action even if this fails

#### Action Type: GetHtmlAndPrint

Fetches HTML from an API and prints it to PDF.

```json
{
  "Type": "GetHtmlAndPrint",
  "Name": "Print Shipping Label",
  "Endpoint": "/api/order/GetShippingLabel/{id}",
  "Method": "GET",
  "HtmlJsonPath": "html",
  "Headers": {},
  "ContinueOnError": false
}
```

**Fields:**
- **Type**: `"GetHtmlAndPrint"`
- **Name**: Display name (used in PDF filename)
- **Endpoint**: API endpoint (supports `{id}` placeholder)
- **Method**: HTTP method
- **HtmlJsonPath**: JSON path to extract HTML from response
  - If omitted, assumes entire response is HTML
  - Examples: `"html"`, `"data.content"`, `"result.htmlString"`
- **Headers**: Additional headers
- **ContinueOnError**: If false, stops action chain on failure

#### Action Type: Delay

Pauses between actions (useful for rate limiting or allowing time for server processing).

```json
{
  "Type": "Delay",
  "Name": "Wait for label generation",
  "DelayMilliseconds": 2000,
  "ContinueOnError": true
}
```

**Fields:**
- **Type**: `"Delay"`
- **Name**: Display name for logging
- **DelayMilliseconds**: Time to wait in milliseconds
- **ContinueOnError**: Always true for delays

### Placeholder Tokens

The following placeholders are automatically replaced in actions:
- **{id}**: Order ID extracted from the API response
- **{orderId}**: Alias for {id}

Use these in:
- Endpoint URLs
- RequestBody JSON
- Custom header values

**Example:**
```
Endpoint: "/api/shipping/create/{id}"
→ Replaced with: "/api/shipping/create/12345"

RequestBody: "{\"orderId\":\"{id}\",\"warehouse\":{warehouseId}}"
→ Replaced with: "{\"orderId\":\"12345\",\"warehouse\":1}"
```

## How It Works

### Workflow

1. **Service starts**: ApiPollSchedulerService begins background polling
2. **Poll interval**: Waits `Scheduler.IntervalSeconds` (e.g., 300s = 5 minutes)
3. **API call**: POSTs to `/api/order/GetOrdersList` with authentication
4. **Parse response**: Extracts `data` array from JSON
5. **Extract IDs**: For each record, extracts ID using `IdJsonPath`
6. **Check processed**: Loads `processed-orders.txt` to skip duplicates
7. **Execute actions**: For each new order:
   - Runs each SubAction sequentially
   - Replaces `{id}` placeholders
   - Logs progress: `[1/3] Update Order Status for order 12345`
   - Continues or stops based on `ContinueOnError`
8. **Mark processed**: Appends order ID to `processed-orders.txt`
9. **Repeat**: Returns to step 2

### Tracking Processed Orders

The service maintains a file (`processed-orders.txt` by default) containing all processed order IDs:

```
12345
12346
12347
```

This file:
- Prevents duplicate processing if orders appear in multiple API polls
- Persists across service restarts
- Grows indefinitely (consider periodic cleanup)
- Located in `%ProgramData%\ScheduledPrintService\` by default

### Error Handling

**ContinueOnError = true**:
- Logs error but continues to next action
- Example: Order status update fails, but still prints label

**ContinueOnError = false**:
- Stops action chain immediately
- Order is **not** marked as processed
- Will retry on next poll cycle
- Example: Label printing fails, should retry later

## Real-World Examples

### Example 1: Process and Print Orders

```json
{
  "SubActions": [
    {
      "Type": "CallApi",
      "Name": "Mark as Processing",
      "Endpoint": "/api/order/{id}/status",
      "Method": "PUT",
      "RequestBody": "{\"status\":\"processing\"}",
      "ContinueOnError": true
    },
    {
      "Type": "GetHtmlAndPrint",
      "Name": "Print Packing Slip",
      "Endpoint": "/api/order/{id}/packingslip",
      "Method": "GET",
      "ContinueOnError": false
    },
    {
      "Type": "CallApi",
      "Name": "Mark as Printed",
      "Endpoint": "/api/order/{id}/status",
      "Method": "PUT",
      "RequestBody": "{\"status\":\"printed\"}",
      "ContinueOnError": true
    }
  ]
}
```

**Flow:**
1. Update order status to "processing"
2. Fetch and print packing slip (fails if can't print)
3. Update order status to "printed"

### Example 2: Multi-Document Printing

```json
{
  "SubActions": [
    {
      "Type": "GetHtmlAndPrint",
      "Name": "Print Shipping Label",
      "Endpoint": "/api/shipment/label/{id}",
      "Method": "GET",
      "HtmlJsonPath": "labelHtml",
      "ContinueOnError": false
    },
    {
      "Type": "Delay",
      "Name": "Wait between prints",
      "DelayMilliseconds": 500
    },
    {
      "Type": "GetHtmlAndPrint",
      "Name": "Print Packing Slip",
      "Endpoint": "/api/order/packingslip/{id}",
      "Method": "GET",
      "HtmlJsonPath": "html",
      "ContinueOnError": false
    },
    {
      "Type": "Delay",
      "Name": "Wait between prints",
      "DelayMilliseconds": 500
    },
    {
      "Type": "GetHtmlAndPrint",
      "Name": "Print Invoice",
      "Endpoint": "/api/billing/invoice/{id}",
      "Method": "GET",
      "ContinueOnError": true
    }
  ]
}
```

**Flow:**
1. Print shipping label (must succeed)
2. Wait 500ms
3. Print packing slip (must succeed)
4. Wait 500ms
5. Print invoice (optional, continue if fails)

### Example 3: API Workflow with Validation

```json
{
  "SubActions": [
    {
      "Type": "CallApi",
      "Name": "Validate Order",
      "Endpoint": "/api/order/{id}/validate",
      "Method": "POST",
      "ContinueOnError": false
    },
    {
      "Type": "CallApi",
      "Name": "Reserve Inventory",
      "Endpoint": "/api/inventory/reserve",
      "Method": "POST",
      "RequestBody": "{\"orderId\":\"{id}\"}",
      "ContinueOnError": false
    },
    {
      "Type": "CallApi",
      "Name": "Create Shipment",
      "Endpoint": "/api/shipment/create",
      "Method": "POST",
      "RequestBody": "{\"orderId\":\"{id}\"}",
      "ContinueOnError": false
    },
    {
      "Type": "GetHtmlAndPrint",
      "Name": "Print Label",
      "Endpoint": "/api/shipment/label/order/{id}",
      "Method": "GET",
      "HtmlJsonPath": "html",
      "ContinueOnError": false
    }
  ]
}
```

**Flow:**
1. Validate order (stops if invalid)
2. Reserve inventory (stops if out of stock)
3. Create shipment record (stops if fails)
4. Print label (stops if fails)

All steps must succeed or order will retry on next poll.

## Deployment

See [INSTALL.md](INSTALL.md) for full installation instructions.

### Quick Deploy Steps

1. **Update appsettings.json** with your API configuration and sub-actions
2. **Build and publish**:
   ```powershell
   cd scheduled-print-service
   .\publish.ps1 -Configuration Release -Runtime win-x64 -SelfContained
   ```
3. **Copy to server**: `publish/Release/win-x64/` → Server location
4. **Install service** (as Administrator):
   ```powershell
   .\install-service.ps1 -ExePath "C:\path\to\ScheduledPrintService.exe"
   ```

## Monitoring

### Check Logs

```powershell
cd "$env:ProgramData\ScheduledPrintService\logs"
Get-Content (Get-ChildItem -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 100 -Wait
```

**Key Log Messages:**

```
[INFO] API polling starting. Interval: 300s
[INFO] Sub-actions configured: 3
[INFO] Polling orders API...
[INFO] Extracted 15 order records
[INFO] Processing 15 orders
[INFO] Processing order: 12345
[INFO] Executing 3 sub-actions for order 12345
[INFO] [1/3] Update Order Status for order 12345
[INFO] [1/3] Update Order Status completed successfully
[INFO] [2/3] Wait before fetching label for order 12345
[INFO] [2/3] Wait before fetching label completed successfully
[INFO] [3/3] Print Shipping Label for order 12345
[INFO] Printing HTML for order 12345 (length: 4523)
[INFO] Successfully printed Print Shipping Label-12345
[INFO] [3/3] Print Shipping Label completed successfully
[INFO] All sub-actions completed for order 12345
[INFO] Successfully processed order: 12345
[INFO] Batch complete: 15 processed, 0 skipped, 0 failed
```

### Check Processed Orders

```powershell
Get-Content "$env:ProgramData\ScheduledPrintService\processed-orders.txt" | Select-Object -Last 20
```

### Check PDF Output

```powershell
cd "$env:ProgramData\ScheduledPrintService\out"
Get-ChildItem -Filter "*.pdf" | Sort-Object LastWriteTime -Descending | Select-Object -First 10
```

## Troubleshooting

### No Orders Being Processed

1. **Check API is enabled**:
   ```json
   { "Api": { "Enabled": true } }
   ```

2. **Check API returns data**:
   - Look for "Extracted X order records" in logs
   - If 0 records, check `DefaultRequest` filters

3. **Check orders aren't already processed**:
   - View `processed-orders.txt`
   - Delete file to reprocess all orders (for testing)

### Sub-Action Fails

1. **Check logs** for specific error:
   ```
   [ERROR] [2/3] Print Label failed: HTTP 404 Not Found
   ```

2. **Verify endpoint** is correct:
   - Check `{id}` placeholder is replaced
   - Verify base URL + endpoint is valid

3. **Check authentication**:
   - Bearer token may be expired
   - Cookies may be stale

4. **Test endpoint manually**:
   ```powershell
   curl "https://mj.3plnext.com/api/order/12345" `
     -H "Authorization: Bearer your-token"
   ```

### Token Expired (401 Unauthorized)

1. Login to website in browser
2. Open DevTools → Network tab
3. Find GetOrdersList request
4. Copy Authorization header (Bearer token)
5. Copy Cookie header values
6. Update `appsettings.json`
7. Restart service: `Restart-Service -Name ScheduledPrintService`

### Duplicate Processing

If orders are being processed multiple times:

1. **Check ProcessedIdsPath** is writable
2. **Verify IdJsonPath** extracts unique IDs
3. **Check logs** for "Loaded X processed order IDs"

### PDF Not Generated

1. **Check HTML response** is valid
2. **Verify HtmlJsonPath** extracts HTML correctly
3. **Check Chromium** downloaded successfully
4. **Test with simple HTML** first

## Advanced Configuration

### Custom ID Extraction

If order ID is nested in JSON:

**Response:**
```json
{
  "data": [
    {"order": {"id": 12345, "status": "pending"}},
    {"order": {"id": 12346, "status": "pending"}}
  ]
}
```

**Config:**
```json
{
  "IdJsonPath": "order.id"
}
```

Note: Current implementation supports simple paths. For complex nested structures, extend `ExtractIdFromJsonPath()` in `OrderApiService.cs`.

### Dynamic Request Bodies

Use placeholders in request bodies:

```json
{
  "RequestBody": "{\"orderId\":\"{id}\",\"warehouse\":{warehouseId},\"user\":\"service\"}"
}
```

Note: Only `{id}` is currently supported. To use other values (like warehouseId from config), extend `ReplaceTokens()` in `SubActionExecutor.cs`.

### Conditional Actions

To skip actions based on order data, extend the workflow:

1. Add condition field to `SubAction` model
2. Evaluate condition in `SubActionExecutor.ExecuteActionAsync()`
3. Skip action if condition not met

Example future feature:
```json
{
  "Type": "GetHtmlAndPrint",
  "Name": "Print International Label",
  "Condition": "order.country != 'US'",
  "Endpoint": "/api/label/international/{id}"
}
```

## Security Considerations

1. **Protect appsettings.json**: Contains Bearer tokens and cookies
2. **File permissions**: Restrict access to installation directory
3. **Token rotation**: Update tokens before expiration
4. **Audit processed-orders.txt**: Contains order IDs (may be sensitive)
5. **Log redaction**: Consider redacting sensitive data from logs

## Performance Tuning

### Poll Interval

```json
{
  "Scheduler": {
    "IntervalSeconds": 60  // Poll every minute (more frequent)
  }
}
```

**Recommendations:**
- **High volume**: 30-60 seconds
- **Medium volume**: 120-300 seconds (2-5 minutes)
- **Low volume**: 600-3600 seconds (10-60 minutes)

### Batch Size

```json
{
  "DefaultRequest": {
    "Length": 100  // Fetch up to 100 orders per poll
  }
}
```

**Recommendations:**
- Start with 25
- Increase if processing is fast
- Decrease if actions are slow or error-prone

### Delays

Add delays between actions to:
- Avoid overwhelming APIs
- Allow server processing time
- Prevent rate limiting

```json
{
  "Type": "Delay",
  "DelayMilliseconds": 500  // Wait 500ms between prints
}
```

## Support

For issues or questions:
- Check logs: `%ProgramData%\ScheduledPrintService\logs\`
- Review configuration in `appsettings.json`
- Test in console mode for debugging
- Refer to README.md for core service features
