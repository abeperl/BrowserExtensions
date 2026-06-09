# Quick Start: API #7 Sales Order Export

## What Was Added

✅ **API #7**: Sales Order Export API
- Fetches sales orders from malchus.3plnext.com
- Gets detailed order information for each order
- Saves order details as JSON files

✅ **New Action Type**: `SaveJsonToFile`
- Saves API responses to JSON files
- Supports directory creation and filename templates
- Available for use in any API configuration

## Files Modified

1. **Database**: `data/api_config.db`
   - Added API #7 configuration
   - Added 2 sub-actions (CallApi + SaveJsonToFile)
   - Added ApiAuth for malchus.3plnext.com

2. **Code**: `Services/SubActionExecutor.cs`
   - Added `_lastApiResponse` field
   - Added `SaveJsonToFile` action handler
   - Updated `CallApi` to store responses

3. **Published**: `publish/` folder
   - All changes compiled and ready to deploy

## Quick Test

### Enable API #7
```bash
cd scheduled-print-service
sqlite3 data/api_config.db "UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 7"
```

### Run in Manual Mode
```bash
cd publish
./ScheduledPrintService.exe --api 7
```

### Check Output
```bash
ls sales-orders/
# Expected: order-14721.json, order-14722.json, etc.
```

## What Happens When You Run It

1. **Primary API Call**: POST to `/api/SaleOrder/GetSaleOrderList`
   - Fetches up to 50 sales orders
   - Extracts order IDs from index 22 of data array

2. **For Each Order**:
   - **Action 1**: GET `/api/SaleOrder/GetSaleOrderById?saleOrderId={id}`
     - Fetches complete order details
     - Stores in `_lastApiResponse` variable

   - **Action 2**: SaveJsonToFile
     - Reads from `_lastApiResponse`
     - Saves to `sales-orders/order-{id}.json`

## Output Location

Files saved to:
```
publish/sales-orders/order-{orderId}.json
```

Example:
```
publish/sales-orders/order-14721.json
publish/sales-orders/order-14722.json
```

## Configuration Details

### API #7 Settings
- **BaseUrl**: `https://malchus.3plnext.com`
- **Authentication**: Long-lived JWT Bearer token
- **ID Extraction**: `[22]` from data array
- **Status**: Disabled by default

### Sub-Action 1: Get Order Details
```json
{
  "Type": "CallApi",
  "Endpoint": "/api/SaleOrder/GetSaleOrderById?saleOrderId={id}",
  "Method": "GET",
  "OutputVariableName": "orderDetails"
}
```

### Sub-Action 2: Save to File
```json
{
  "Type": "SaveJsonToFile",
  "Endpoint": "sales-orders",
  "RequestBody": "order-{id}.json"
}
```

## Troubleshooting

### No files created?
- Check if API is enabled: `SELECT IsEnabled FROM PrimaryApi WHERE ApiNumber = 7`
- Check logs in `logs/` folder
- Verify authentication token is valid

### Files empty or invalid JSON?
- Check that CallApi action ran before SaveJsonToFile
- Verify API response is valid JSON
- Check logs for parsing errors

### Permission denied?
- Ensure write permissions on output directory
- Run as administrator if needed

## Advanced Usage

### Change Output Directory
```sql
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.Endpoint', 'my-custom-folder')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 7)
  AND ActionType = 'SaveJsonToFile';
```

### Change Filename Pattern
```sql
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.RequestBody', 'sales-order-{id}-backup.json')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 7)
  AND ActionType = 'SaveJsonToFile';
```

### Fetch More Orders
```sql
UPDATE PrimaryApi
SET Payload = json_set(Payload, '$.length', 100)
WHERE ApiNumber = 7;
```

## Integration with Schedules

To run API #7 on a schedule:

```sql
-- Create a schedule (runs daily at 2 AM)
INSERT INTO Schedule (ScheduleName, CronExpression, IsEnabled)
VALUES ('Daily Order Export', '0 0 2 * * *', 1);

-- Link API #7 to the schedule
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES ((SELECT Id FROM Schedule WHERE ScheduleName = 'Daily Order Export'), 7, 1);
```

## See Also

- [API-7-SALES-ORDER-EXPORT.md](API-7-SALES-ORDER-EXPORT.md) - Full documentation
- [create-api-7-sales-orders.sql](../scripts/create-api-7-sales-orders.sql) - Migration script
