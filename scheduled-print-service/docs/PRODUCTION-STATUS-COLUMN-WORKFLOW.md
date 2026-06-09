# Production Status Column Workflow

## Overview

This workflow injects a "Production Status" column into the SS/SO Orders pick ticket table. The column displays the current production status for each item by matching data from two API responses and looking up status names from session storage.

## Data Flow

```
1. Primary API Call (Picklist)
   ↓
2. Store PickListItems in memory
   ↓
3. Navigate to Pick Ticket Page
   ↓
4. Fetch Order Details API
   ↓
5. Store OrderItems in memory
   ↓
6. Match items by OrderDetailsId
   ↓
7. Lookup status names from session
   ↓
8. Inject Production Status column
```

## Implementation Steps

### Step 1: Configure Fetch Order Details Sub-Action

The sub-action fetches order details and stores them in memory:

**SQL Script**: `add-order-detail-fetch-subaction.sql`

**Configuration**:
- **Endpoint**: `https://mj.3plnext.com/api/order/showOrderDetail?OrderId={orderId}`
- **Method**: GET
- **Execution Order**: 2 (between Navigate and Save PDF)
- **Dynamic Parameter**: Extracts `OrderID` from `data.PickListItems[0].OrderID`
- **Memory Storage**: Stores response with key `orderDetails`

**JavaScript Injection** (to be added at service level):
```javascript
window.__orderDetailsResponse = {responseData};
sessionStorage.setItem('__orderDetailsResponse', JSON.stringify({responseData}));
```

### Step 2: Store Picklist Response

The initial picklist API response needs to be stored in memory:

**JavaScript Injection** (at service level):
```javascript
window.__picklistResponse = {responseData};
sessionStorage.setItem('__picklistResponse', JSON.stringify({responseData}));
```

### Step 3: Inject Production Status Column Script

The Navigate action (Action #1) injects JavaScript that:
1. Retrieves order details from memory
2. Retrieves picklist items from memory
3. Matches items by `OrderDetailsId`
4. Looks up status names from `localStorage.tf__session._thirdpartyitemstatuses`
5. Injects a new column into the table

**Script File**: `production-status-column-injector.js`

## Data Structure Matching

### Picklist Response (`data.PickListItems`)
```json
{
  "PickListItemsId": 19149,
  "OrderDetailsId": 35040,
  "SKU": "TF196BS",
  "Quantity": 1,
  ...
}
```

### Order Details Response (`data.OrderItems`)
```json
{
  "orderDetailsId": 35040,
  "Sku": "TF196BS",
  "ItemStatusId": 67,
  "ItemStatus": "Pending",
  ...
}
```

### Status Lookup (from `localStorage.tf__session._thirdpartyitemstatuses`)
```json
[
  {
    "valueListId": 67,
    "valueListName": "Pending"
  },
  {
    "valueListId": 68,
    "valueListName": "In Progress"
  },
  ...
]
```

### Matching Logic

1. **Match by Index**: `pickListItems[index]` ↔ `table row[index]`
2. **Get OrderDetailsId**: `pickListItems[index].OrderDetailsId` → `35040`
3. **Lookup StatusId**: `orderItems.find(item => item.orderDetailsId === 35040).ItemStatusId` → `67`
4. **Lookup StatusName**: `statuses.find(s => s.valueListId === 67).valueListName` → `"Pending"`
5. **Inject Cell**: Insert `<td>Pending</td>` before existing Status column

## Files Created

### SQL Scripts
1. **`add-order-detail-fetch-subaction.sql`** - Adds the order details fetch sub-action
2. **`add-production-status-column-injection.sql`** - Adds HTML injection with inline script
3. **`implement-production-status-workflow.sql`** - Complete consolidated workflow
4. **`update-production-status-workflow.sql`** - Update existing configuration
5. **`inject-production-status-column.sql`** - Simple injection script

### JavaScript Files
1. **`production-status-column-injector.js`** - Standalone injection script

## Database State After Implementation

### SubActions for API 3 (Execution Order):
1. Navigate - SS/SO Custom Forms
2. **Fetch Order Details - SS/SO Orders** ← NEW
3. Save PDF - SS/SO Orders
4. Print Custom Form PDF - SS/SO Orders
5. Navigate - Non-SS/SO Custom Forms
6. Save PDF - Non-SS/SO Orders
7. Print Custom Form PDF - Non-SS/SO Orders

## Service-Level Requirements

The C# service needs to:

1. **Capture API Responses**: Store both picklist and order details responses
2. **Inject into Page**: Use Puppeteer to inject the responses into `window` object:

```csharp
await page.EvaluateExpressionAsync($@"
    window.__picklistResponse = {picklistResponseJson};
    sessionStorage.setItem('__picklistResponse', JSON.stringify({picklistResponseJson}));
");

await page.EvaluateExpressionAsync($@"
    window.__orderDetailsResponse = {orderDetailsResponseJson};
    sessionStorage.setItem('__orderDetailsResponse', JSON.stringify({orderDetailsResponseJson}));
");
```

3. **Execute in Order**: Ensure the order details are fetched BEFORE navigating to the page

## Testing

### Manual Testing Steps:

1. Run the SQL script to add the sub-action
2. Trigger the API workflow for an SS/SO order
3. Verify the pick ticket page shows the Production Status column
4. Check browser console for logs:
   - ✅ Loaded and cached X third-party statuses
   - 📋 Created mapping for X order items
   - ✅ Production Status column injection complete

### Console Testing:

```javascript
// Check if data is in memory
console.log(window.__picklistResponse);
console.log(window.__orderDetailsResponse);

// Check status cache
console.log(window.__productionStatusCache);

// Manually trigger column injection
window.addProductionStatusColumn();
```

## Troubleshooting

### Column Not Appearing
- Check if `#KortHyvdds` table exists
- Verify `window.__orderDetailsResponse` is populated
- Check browser console for errors

### Wrong Status Names
- Verify `localStorage.tf__session._thirdpartyitemstatuses` exists
- Check status ID mapping in order details response

### Items Not Matching
- Verify `OrderDetailsId` field exists in picklist items
- Check row index alignment between picklist and table

## Next Steps

After database configuration, update the C# service to:
1. Store API responses in variables
2. Inject responses into page context
3. Test complete workflow end-to-end