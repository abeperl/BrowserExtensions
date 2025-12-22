# API #7: Sales Order Export

## Overview

API #7 fetches sales orders from the 3PL system (malchus.3plnext.com) and exports order details to JSON files for archival, analysis, or integration purposes.

## Configuration

- **Base URL**: `https://malchus.3plnext.com`
- **Primary Endpoint**: `/api/SaleOrder/GetSaleOrderList` (POST)
- **ID Extraction**: Index `[22]` from response data array
- **Status**: Disabled by default (enable when ready to use)

## Sub-Actions

### 1. Get Order Details (CallApi)
- **Endpoint**: `/api/SaleOrder/GetSaleOrderById?saleOrderId={id}`
- **Method**: GET
- **Purpose**: Fetches complete order details for each order ID
- **Output**: Stores response in `orderDetails` variable for next action

### 2. Save Order Details to JSON File (SaveJsonToFile)
- **Output Directory**: `sales-orders/` (relative to app directory)
- **Filename Template**: `order-{id}.json`
- **Purpose**: Saves the API response from step 1 as a JSON file

## How It Works

1. **Primary API Call**: Fetches list of sales orders using POST request with DataTables-style pagination
2. **ID Extraction**: Extracts order ID from index 22 of each data array item
3. **Get Details**: For each order, calls `/api/SaleOrder/GetSaleOrderById` to get full order data
4. **Save to File**: Saves the detailed order JSON to a file named `order-{id}.json` in the `sales-orders` directory

## File Output

Files are saved to:
```
{AppDirectory}/sales-orders/order-{orderId}.json
```

For example:
- `sales-orders/order-14721.json`
- `sales-orders/order-14722.json`

## Authentication

- Uses long-lived JWT Bearer token (expires 2066-12-22)
- Token stored in ApiAuth table for malchus.3plnext.com
- Cookie authentication also configured

## Enabling the API

To enable API #7:

```sql
UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 7;
```

Or run the service with manual mode:
```bash
ScheduledPrintService.exe --api 7
```

## New Action Type: SaveJsonToFile

This API introduces a new sub-action type `SaveJsonToFile` that can be used in any API configuration.

### Configuration Fields

- **Endpoint**: Directory path (relative to app directory) where files will be saved
- **RequestBody**: Filename template with `{id}` placeholder
- **OutputVariableName**: (Optional) Alternative directory name if Endpoint not specified

### Example Configuration

```json
{
  "Type": "SaveJsonToFile",
  "Name": "Save Order Details",
  "Endpoint": "sales-orders",
  "RequestBody": "order-{id}.json"
}
```

### Behavior

1. Reads the last API response from previous `CallApi` action
2. Validates JSON format
3. Creates output directory if it doesn't exist
4. Generates filename by replacing `{id}` placeholder with order ID
5. Saves JSON to file
6. Logs file path and size

## Primary API Request Payload

The API uses a complex DataTables-style request body:

```json
{
  "draw": 1,
  "columns": [...],
  "order": [{"column": 5, "dir": "asc"}],
  "start": 0,
  "length": 50,
  "search": {"value": "", "regex": false},
  "dateFrom": null,
  "dateTo": null,
  "status": 1,
  "OrderStatusId": -1,
  "orderType": -1,
  "CustomStatusId": "0",
  "StoreId": "1"
}
```

This fetches the first 50 orders with status=1 (active orders).

## Response Format

The GetSaleOrderList response contains:
- `data`: Array of order arrays (each with ~30 fields)
- Order ID is at index 22 in each data array

The GetSaleOrderById response contains:
- Complete order object with all details
- Customer information
- Order items
- Shipping details
- Payment information
- Order status and history

## Migration Script

Location: [scripts/create-api-7-sales-orders.sql](../scripts/create-api-7-sales-orders.sql)

To apply the migration:
```bash
cd scheduled-print-service
sqlite3 data/api_config.db < scripts/create-api-7-sales-orders.sql
```

## Code Changes

### Modified Files

1. **SubActionExecutor.cs**
   - Added `_lastApiResponse` field to store API responses
   - Updated `ExecuteCallApiAsync` to store responses
   - Added `savejsontofile` case in switch statement
   - Implemented `ExecuteSaveJsonToFileAsync` method

### New Action Type Implementation

The `SaveJsonToFile` action type:
- Reuses existing SubAction fields (Endpoint, RequestBody)
- No new database schema changes required
- Compatible with existing action chaining
- Validates JSON before saving
- Creates directories automatically
- Supports {id} placeholder in filenames

## Testing

To test API #7:

1. Enable the API:
   ```sql
   UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 7;
   ```

2. Run in manual mode:
   ```bash
   cd scheduled-print-service/publish
   ScheduledPrintService.exe --api 7
   ```

3. Check output:
   ```bash
   ls sales-orders/
   ```

Expected output:
- Multiple JSON files named `order-{id}.json`
- Each file contains complete order details

## Use Cases

1. **Order Archival**: Backup all sales orders to JSON files
2. **Data Integration**: Export orders for external systems
3. **Analytics**: Extract order data for analysis
4. **Audit Trail**: Maintain historical order records
5. **Debugging**: Capture API responses for troubleshooting

## Notes

- API is disabled by default to prevent accidental mass exports
- Bearer token is long-lived (300 years) so manual token renewal not needed
- Files are not deduplicated - running the API multiple times will create new files
- Order tracking uses the standard `processed-orders.txt` file to avoid re-processing
- JSON files are stored in plain text - consider encryption for sensitive data

## Future Enhancements

Potential improvements:
- Add compression (gzip) for JSON files
- Support for date-based subdirectories
- Configurable file retention/cleanup
- Batch export to single file option
- Incremental export (only new orders)
- Export filtering by date range or status
