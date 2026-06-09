# Sub-Action Chaining Guide

## Overview

The scheduled-print-service now supports chaining sub-actions, allowing the response from one action to trigger subsequent actions. This is particularly useful for workflows where you need to:

1. Create a batch of records (e.g., picklists)
2. Extract IDs from the response
3. Perform additional actions for each ID (e.g., fetch and print pages)

## How It Works

### Basic Concepts

**Chaining Configuration:**
- `ChainedArrayJsonPath`: JSON path to the array in the response (e.g., `"data"`)
- `ChainedItemFieldPath`: Field name to extract from each array item (e.g., `"pickListId"`)
- `UseChainedInput`: Set to `true` on actions that should use chained data
- Token replacement: Use `{fieldName}` in URLs/bodies to substitute values from chained context

### Action Types

1. **CreatePicklistBatch** - Creates picklists and can trigger chained actions
2. **GetUrlAndPrint** - NEW! Fetches a URL with Puppeteer, waits for SPA data to load, optionally makes hidden fields visible, then prints
3. **CallApi** - Makes API calls with context substitution
4. **Delay** - Waits specified milliseconds
5. **GetHtmlAndPrint** - Fetches HTML from API and prints

## Example Configuration

### Scenario: Create Picklists and Print Picking Pages

This example creates picklists for orders, then for each picklist it:
1. Navigates to the manual picking page
2. Waits for the SPA to load data via JSON
3. Makes all hidden fields visible
4. Generates and prints a PDF

```json
{
  "Api": {
    "SubActions": [
      {
        "Type": "CreatePicklistBatch",
        "Name": "Create Pending Order Picklist Batch",
        "Enabled": true,
        "Endpoint": "/api/PickList/CreatePendingOrderPicklist",
        "Method": "POST",
        "BatchSize": 25,
        "QuickShip": false,
        "ContinueOnError": true,
        
        // Chaining configuration
        "ChainedArrayJsonPath": "data",
        "ChainedItemFieldPath": "pickListId"
      },
      {
        "Type": "GetUrlAndPrint",
        "Name": "Print Manual Picking Page",
        "Enabled": true,
        
        // Use {pickListId} from chained context
        "Endpoint": "https://mj.3plnext.com/#Outbound/ManualPicking?id={pickListId}",
        "Method": "GET",
        
        // Enable chaining
        "UseChainedInput": true,
        
        // Wait 3 seconds after network idle for SPA data loading
        "WaitForNetworkIdleMs": 3000,
        
        // Make hidden inputs visible before printing
        "MakeHiddenVisible": true,
        
        "ContinueOnError": true
      }
    ]
  }
}
```

### Response Format Expected

The `CreatePicklistBatch` action expects a response like:

```json
{
  "responseCode": 0,
  "responseType": "Success",
  "responseMessage": null,
  "data": [
    {
      "pickListId": 3722,
      "pickListNumber": "2202500003722",
      "orderid": 3485,
      "orderNumber": "2102500003485"
    },
    {
      "pickListId": 3721,
      "pickListNumber": "2202500003721",
      "orderid": 3486,
      "orderNumber": "2102500003486"
    }
  ]
}
```

With `ChainedArrayJsonPath: "data"`, the system will:
1. Extract the `data` array
2. For each item, create a context dictionary with all fields (`pickListId`, `pickListNumber`, `orderid`, `orderNumber`)
3. Execute chained actions (those with `UseChainedInput: true`) for each item
4. Replace tokens like `{pickListId}` with actual values

## GetUrlAndPrint Action Type

### Purpose
Designed for Single Page Applications (SPAs) where:
- Initial HTML is minimal
- Data loads via subsequent JSON requests
- Hidden fields need to be visible for printing

### Key Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Type` | string | - | Must be `"GetUrlAndPrint"` |
| `Endpoint` | string | - | Full URL or relative path. Supports token replacement. |
| `WaitForNetworkIdleMs` | int | 3000 | Additional wait time after network idle for SPA data loading |
| `MakeHiddenVisible` | bool | false | Convert all `<input type="hidden">` to `type="text"` and make visible |
| `UseChainedInput` | bool | false | If true, uses context from previous action |
| `ContinueOnError` | bool | true | Continue to next action if this fails |

### How It Works

1. **Navigate**: Opens URL in Puppeteer browser
2. **Wait for Network Idle**: Waits until network is idle (no pending requests)
3. **Additional Wait**: Waits `WaitForNetworkIdleMs` for SPA data loading
4. **Modify DOM**: If `MakeHiddenVisible` is true, makes hidden inputs visible using JavaScript
5. **Generate PDF**: Creates PDF from the rendered page
6. **Print**: Sends PDF to configured printer

### JavaScript Injection

When `MakeHiddenVisible: true`, the following JavaScript is executed:

```javascript
Array.from(document.querySelectorAll('input[type="hidden"]')).forEach(el => {
    el.type = 'text';
    el.style.display = 'block';
});
```

## Token Replacement

Tokens in `Endpoint`, `RequestBody`, and `Headers` are replaced with context values:

### Available Tokens

- `{id}` or `{orderId}` - Order ID (when processing individual orders)
- `{pickListId}` - Picklist ID from chained context
- `{pickListNumber}` - Picklist number from chained context
- `{orderNumber}` - Order number from chained context
- Any field from the chained response array items

### Example

If context contains `{"pickListId": 3722, "orderNumber": "210250003485"}`:

```
URL: https://example.com/pick?id={pickListId}&order={orderNumber}
Result: https://example.com/pick?id=3722&order=210250003485
```

## Chaining Workflow

1. **Batch Action Executes** (e.g., `CreatePicklistBatch`)
   - Makes API call
   - Receives response

2. **Response Parsing**
   - Extracts array using `ChainedArrayJsonPath`
   - For each item, extracts all fields into context

3. **Chained Actions Execute**
   - Finds actions with `UseChainedInput: true` that follow the batch action
   - For each context item (e.g., each picklist):
     - Executes all chained actions in sequence
     - Replaces tokens with context values
     - Handles errors per `ContinueOnError` setting

## Error Handling

- `ContinueOnError: true` - Log error and continue to next action
- `ContinueOnError: false` - Stop processing and raise error

Errors in chained actions are logged with full context for debugging.

## Logging

The service provides detailed logging:

```
[INF] Found 2 chained action(s) to execute
[INF] Extracted 9 item(s) for chained execution
[INF] Executing chained action 'Print Manual Picking Page' with context data
[DBG] Navigating to https://mj.3plnext.com/#Outbound/ManualPicking?id=3722
[DBG] Waiting 3000ms for network idle
[DBG] Making hidden fields visible
[INF] Generating PDF from page
[INF] Printing PDF: Print Manual Picking Page-3722 (125843 bytes)
[INF] Chained action 'Print Manual Picking Page' completed successfully
```

## Performance Considerations

1. **Batch Size**: Keep `BatchSize` reasonable (10-50) to avoid overwhelming the API
2. **Wait Times**: Adjust `WaitForNetworkIdleMs` based on your SPA load time
3. **Parallel Processing**: Currently sequential - each picklist is processed one at a time
4. **Network Timeout**: Default Puppeteer navigation timeout is 60 seconds

## Troubleshooting

### Chained Actions Not Executing

1. Verify `UseChainedInput: true` is set on chained actions
2. Check that `ChainedArrayJsonPath` correctly points to response array
3. Ensure chained action appears AFTER the source action in SubActions array

### Hidden Fields Still Hidden

1. Confirm `MakeHiddenVisible: true` is set
2. Check browser console logs (enable Puppeteer headless: false for debugging)
3. Verify JavaScript is not being blocked by CSP

### PDF Not Printing

1. Check printer configuration in `Printer` section of appsettings.json
2. Verify `Mode: "Windows"` and printer name is correct
3. Review logs for printer spooling errors

### SPA Data Not Loading

1. Increase `WaitForNetworkIdleMs` (try 5000-10000ms)
2. Check if authentication cookies are properly configured
3. Verify URL is correct and accessible

## Advanced Usage

### Multiple Chained Actions

You can chain multiple actions after a batch:

```json
{
  "SubActions": [
    {
      "Type": "CreatePicklistBatch",
      "ChainedArrayJsonPath": "data"
    },
    {
      "Type": "GetUrlAndPrint",
      "UseChainedInput": true,
      "Endpoint": "https://example.com/pick/{pickListId}"
    },
    {
      "Type": "CallApi",
      "UseChainedInput": true,
      "Endpoint": "/api/picklist/{pickListId}/complete",
      "Method": "POST"
    },
    {
      "Type": "Delay",
      "DelayMilliseconds": 500
    }
  ]
}
```

### Custom Field Extraction

All fields from response items are available for token replacement:

```json
{
  "data": [
    {
      "pickListId": 123,
      "customField": "ABC",
      "anotherField": 456
    }
  ]
}
```

Use: `{pickListId}`, `{customField}`, `{anotherField}` in your chained actions.

## API Reference

### SubAction Properties (Complete List)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Type | string | - | Action type: CallApi, GetHtmlAndPrint, GetUrlAndPrint, Delay, CreatePicklistBatch |
| Name | string | - | Display name for logging |
| Enabled | bool | true | Enable/disable action |
| Endpoint | string | - | API endpoint or full URL (supports tokens) |
| Method | string | GET | HTTP method |
| RequestBody | string | null | JSON body (supports tokens) |
| Headers | Dictionary | {} | Custom HTTP headers |
| HtmlJsonPath | string | null | For GetHtmlAndPrint: JSON path to HTML content |
| DelayMilliseconds | int | 0 | For Delay: wait time |
| ContinueOnError | bool | true | Continue on error |
| BatchSize | int | 10 | For CreatePicklistBatch: IDs per request |
| QuickShip | bool | false | For CreatePicklistBatch: QuickShip flag |
| ChainedArrayJsonPath | string | null | JSON path to response array for chaining |
| ChainedItemFieldPath | string | null | (Deprecated) Use all fields from items |
| UseChainedInput | bool | false | Use context from previous action |
| WaitForNetworkIdleMs | int | 3000 | For GetUrlAndPrint: extra wait after network idle |
| MakeHiddenVisible | bool | false | For GetUrlAndPrint: show hidden inputs |

## Migration from Previous Version

Previous configuration (without chaining):

```json
{
  "SubActions": [
    {
      "Type": "CreatePicklistBatch",
      "Enabled": true
    }
  ]
}
```

New configuration (with chaining):

```json
{
  "SubActions": [
    {
      "Type": "CreatePicklistBatch",
      "Enabled": true,
      "ChainedArrayJsonPath": "data"
    },
    {
      "Type": "GetUrlAndPrint",
      "Enabled": true,
      "UseChainedInput": true,
      "Endpoint": "https://example.com/page/{pickListId}"
    }
  ]
}
```

No breaking changes - existing configurations continue to work without modification.
