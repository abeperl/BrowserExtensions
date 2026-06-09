# Production Status Column Feature

## Overview

Adds a **"Production Status"** column to the order items table on the Order Details page (`#SO/orderdetails`). The column displays the production status name for each item by:
1. Intercepting API responses to capture `ItemStatusId` for each item
2. Looking up status names from the session storage status list
3. Dynamically inserting and updating the column in the table

## URL Pattern

- **Route**: `#SO/orderdetails?id={orderId}`
- **Example**: `https://mj.3plnext.com/#SO/orderdetails?id=10452`

## Features

### ✅ API Interception
- Intercepts `XMLHttpRequest` and `fetch` calls to `/api/SpecialOrder/GetSorderItemsDetails`
- Captures the full response data including `ItemStatusId` for each item
- Stores response data for lookup when building the column

### ✅ Status Lookup
- Reuses the status list loading logic from `status-dropdown.js`
- Loads third-party item statuses from `localStorage.tf__session._thirdpartyitemstatuses`
- Creates an in-memory lookup map (ID → Status Name)
- Caches the status list for performance

### ✅ Dynamic Column Insertion
- Finds the "Product Title" column in the table
- Inserts "Production Status" column immediately after it
- Adds header and data cells to all rows
- Updates cells with status names from the API response

### ✅ Auto-Update
- Uses `MutationObserver` to watch for table changes
- Re-adds column if table structure changes (pagination, sorting, etc.)
- Updates cells when new API data is available

### ✅ Visual Styling
- Header: Bold text with light gray background
- Data cells: Green bold text for valid statuses, gray for N/A
- Clean borders and padding matching table style

## File Structure

```
css-js-toinject/
├── production-status-column.js  # Main feature implementation
├── status-dropdown.js            # Shared status loading logic
├── router.js                     # Route configuration (updated)
└── docs/
    └── PRODUCTION-STATUS-COLUMN.md  # This documentation
```

## Implementation Details

### API Response Format

Expected response from `/api/SpecialOrder/GetSorderItemsDetails`:

```json
{
  "data": [
    {
      "ItemStatusId": 123,
      "ProductTitle": "Product Name",
      ...
    },
    ...
  ]
}
```

### Status List Format

From `localStorage.tf__session._thirdpartyitemstatuses`:

```json
[
  {
    "valueListId": 2,
    "valueListName": "Sent to machine",
    "isReady": false
  },
  {
    "valueListId": 3,
    "valueListName": "Embroidery-Ready",
    "isReady": true
  },
  ...
]
```

### Table Structure

**Before**:
```html
<table id="sorderItemsdet">
  <thead>
    <tr>
      <th>SKU</th>
      <th class="title_column">Product Title</th>
      <th>Qty</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>ABC123</td>
      <td class="title_column" data-repeat-item="ProductTitle">Product Name</td>
      <td>10</td>
    </tr>
  </tbody>
</table>
```

**After**:
```html
<table id="sorderItemsdet">
  <thead>
    <tr>
      <th>SKU</th>
      <th class="title_column">Product Title</th>
      <th class="production-status-header">Production Status</th>
      <th>Qty</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>ABC123</td>
      <td class="title_column" data-repeat-item="ProductTitle">Product Name</td>
      <td class="production-status-cell" data-row-index="0">In Production</td>
      <td>10</td>
    </tr>
  </tbody>
</table>
```

## Usage

### Automatic Activation

The feature activates automatically when navigating to any order details page:

1. Navigate to `#SO/orderdetails?id={orderId}`
2. Router detects the URL pattern
3. Initializes the production status column feature
4. API interceptor captures the response
5. Column is added and populated with status names

### Manual Control

Debug API available via browser console:

```javascript
// Refresh the column manually
window.productionStatusAPI.refreshColumn();

// View stored API response data
console.log(window.productionStatusAPI.store.data);

// Get status list
console.log(window.productionStatusAPI.getStatusList());

// Look up a status name by ID
console.log(window.productionStatusAPI.getStatusName(123));
```

## Router Configuration

In `router.js`:

```javascript
{
    name: 'Order Details Production Status Route',
    pattern: /^#SO\/orderdetails(\?.*)?$/i,
    action: () => {
        if (typeof window.initProductionStatusColumn === 'function') {
            window.initProductionStatusColumn();
        }
    },
    description: 'Adds Production Status column to order items table with API data'
}
```

## Dependencies

### Required
- `status-dropdown.js` - Provides `getThirdPartyStatuses()` pattern (logic is duplicated in production-status-column.js for independence)
- Session storage with `tf__session._thirdpartyitemstatuses`
- Table element: `table#sorderItemsdet`
- API endpoint: `/api/SpecialOrder/GetSorderItemsDetails`

### Optional
- Browser console for debugging with `window.productionStatusAPI`

## Troubleshooting

### Column Not Appearing

**Check console for errors**:
```javascript
// Verify functions are loaded
typeof window.initProductionStatusColumn === 'function'

// Verify table exists
document.querySelector('table#sorderItemsdet')

// Verify API data was captured
window.productionStatusAPI.store.data
```

### Status Showing "N/A"

**Check status list**:
```javascript
// View all statuses
window.productionStatusAPI.getStatusList()

// Check specific ID
window.productionStatusAPI.getStatusName(123)
```

**Verify API response**:
```javascript
// View captured response
window.productionStatusAPI.store.data

// Check ItemStatusId values
window.productionStatusAPI.store.data.data.map(item => item.ItemStatusId)
```

### Column Disappears After Table Update

**Normal behavior** - MutationObserver should re-add it automatically.

**Manual fix**:
```javascript
window.productionStatusAPI.refreshColumn()
```

## Performance Notes

- ✅ **Status list cached** - Only loaded once from localStorage
- ✅ **Efficient lookup** - Uses `Map` for O(1) status name retrieval
- ✅ **Minimal DOM manipulation** - Only adds column once, updates cells as needed
- ✅ **Non-blocking** - API interception doesn't delay original requests

## Future Enhancements

Potential improvements:
1. Add color coding by status type (pending, in progress, complete)
2. Make status clickable to filter/sort table
3. Add status change dropdown in each cell
4. Show status timestamp/history on hover
5. Add export functionality including status column

## Testing

### Test Steps

1. **Navigate to order details**:
   ```
   https://mj.3plnext.com/#SO/orderdetails?id=10452
   ```

2. **Verify column appears**:
   - Check for "Production Status" header
   - Verify it's after "Product Title"
   - Check that all rows have status cells

3. **Verify status names display**:
   - Should show status names (e.g., "In Production")
   - Should show "N/A" for items without status
   - Green bold text for valid statuses

4. **Test table interactions**:
   - Pagination: Navigate to next page, verify column persists
   - Sorting: Click column headers, verify column stays
   - Refresh: Reload page, verify column re-appears

5. **Check console**:
   - No errors
   - See success messages for API interception and column addition

### Expected Console Output

```
📊 Production Status Column Functions Loading...
🚀 Browser Extension JS Router - Starting...
🔍 Current URL hash: #SO/orderdetails?id=10452
🎯 Matched route: Order Details Production Status Route
🚀 Matched #SO/orderdetails route
📊 Initializing Production Status Column feature
🔌 Setting up API interceptor for order items
✅ API interceptor installed
✅ Production Status Column Functions Loaded
🎯 Intercepted order items API call: /api/SpecialOrder/GetSorderItemsDetails
📦 Captured order items response: {data: Array(5), ...}
💾 Saved API response data: {data: Array(5), ...}
🔄 Adding Production Status column to order items table
✅ Found Product Title header at index 2
✅ Added Production Status header
✅ Added Production Status cells to 5 rows
✅ Loaded and cached 15 third-party statuses
📋 Created status lookup map with 15 entries
✅ Updated 5 production status cells
✅ Production Status Column feature initialized
```

## Version History

- **v1.0** (2025-12-04)
  - Initial implementation
  - API interception for order items
  - Status lookup from session
  - Dynamic column insertion
  - Auto-update with MutationObserver
