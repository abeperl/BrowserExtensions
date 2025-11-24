# Database Schema Diagram

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Schedule                            │
├─────────────────────────────────────────────────────────────┤
│ • Id (PK)                                                   │
│ • ScheduleName                                              │
│ • CronExpression                                            │
│ • IsEnabled                                                 │
│ • CreatedAt                                                 │
│ • UpdatedAt                                                 │
└───────────────┬─────────────────────────────────────────────┘
                │
                │ (1:N)
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│                      ScheduleApi                            │
│                    (Junction Table)                         │
├─────────────────────────────────────────────────────────────┤
│ • Id (PK)                                                   │
│ • ScheduleId (FK -> Schedule.Id)                           │
│ • ApiNumber (FK -> PrimaryApi.ApiNumber)                   │
│ • ExecutionOrder                                            │
└───────────────┬─────────────────────────────────────────────┘
                │
                │ (N:1)
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│                       PrimaryApi                            │
├─────────────────────────────────────────────────────────────┤
│ • Id (PK)                                                   │
│ • ApiNumber (UNIQUE)  ← Main identifier                    │
│ • ApiName                                                   │
│ • BaseUrl                                                   │
│ • Endpoint                                                  │
│ • HttpMethod                                                │
│ • Headers (JSON)                                            │
│ • Params (JSON)                                             │
│ • Payload (JSON)                                            │
│ • IsEnabled                                                 │
│ • CreatedAt                                                 │
│ • UpdatedAt                                                 │
└───────────────┬─────────────────────────────────────────────┘
                │
                │ (1:N)
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│                       SubAction                             │
├─────────────────────────────────────────────────────────────┤
│ • Id (PK)                                                   │
│ • PrimaryApiId (FK -> PrimaryApi.Id)                       │
│ • ActionNumber (UNIQUE per PrimaryApiId)                   │
│ • ActionName                                                │
│ • ActionType                                                │
│ • Configuration (JSON)                                      │
│ • ExecutionOrder                                            │
│ • IsEnabled                                                 │
│ • CreatedAt                                                 │
│ • UpdatedAt                                                 │
└─────────────────────────────────────────────────────────────┘
```

## Workflow Diagram

```
┌──────────────┐
│   Schedule   │  Cron: "0 */15 * * * *" (Every 15 min)
│    Trigger   │
└──────┬───────┘
       │
       │ Looks up in ScheduleApi table
       ▼
┌──────────────────────────────────────┐
│  Which APIs to execute?              │
│  - API #1 (ExecutionOrder: 1)       │
│  - API #2 (ExecutionOrder: 2)       │
│  - API #3 (ExecutionOrder: 3)       │
└──────┬───────────────────────────────┘
       │
       │ For each API (in order)...
       ▼
┌─────────────────────────────────────────────────────┐
│  Execute Primary API #1                             │
│  POST https://mj.3plnext.com/api/order/GetOrdersList│
│  - Headers: Bearer token, Cookies                   │
│  - Params: Status filters, date range               │
└──────┬──────────────────────────────────────────────┘
       │
       │ API Response (e.g., list of orders)
       ▼
┌─────────────────────────────────────────────────────┐
│  Get Sub-Actions for API #1 (where IsEnabled=1)    │
│  Order by ExecutionOrder                            │
└──────┬──────────────────────────────────────────────┘
       │
       │ Execute each sub-action in order...
       ▼
┌─────────────────────────────────────────────────────┐
│  Sub-Action #1: Create Picklist Batch              │
│  - Type: CreatePicklistBatch                        │
│  - Process 25 orders at a time                      │
│  - Output: picklistIds[]                            │
└──────┬──────────────────────────────────────────────┘
       │
       │ If chaining enabled, pass output to next...
       ▼
┌─────────────────────────────────────────────────────┐
│  Sub-Action #2: Print Manual Picking Page          │
│  - Type: GetUrlAndPrint                             │
│  - Input: picklistIds from previous action          │
│  - For each picklistId, navigate and print          │
└──────┬──────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│  Sub-Action #3: Update Order Status                │
│  - Type: CallApi                                    │
│  - Call API to update status to "Processing"        │
└──────┬──────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│  Sub-Action #4: Delay                               │
│  - Type: Delay                                      │
│  - Wait 1000ms                                      │
└──────┬──────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│  Sub-Action #5: Print Shipping Label                │
│  - Type: GetHtmlAndPrint                            │
│  - Fetch HTML from API                              │
│  - Print to PDF                                     │
└──────┬──────────────────────────────────────────────┘
       │
       ▼
   [Complete]
```

## Data Flow Example

### Scenario: Schedule triggers API #1

```
Step 1: Schedule Check
┌────────────────────────────────────────┐
│ Cron: 0 */15 * * * * (hits at 15:15)  │
│ Query: Get APIs from ScheduleApi       │
│ Result: [API #1]                       │
└────────────────────────────────────────┘

Step 2: Load API #1 Configuration
┌────────────────────────────────────────┐
│ ApiNumber: 1                           │
│ ApiName: Orders List API               │
│ BaseUrl: https://mj.3plnext.com        │
│ Endpoint: /api/order/GetOrdersList     │
│ Method: POST                           │
│ Headers: {                             │
│   "Authorization": "Bearer xxx...",    │
│   "Cookie": "userData=...; token=..."  │
│ }                                      │
│ Params: {                              │
│   "StatusName": "7",                   │
│   "Length": 100,                       │
│   ...                                  │
│ }                                      │
└────────────────────────────────────────┘

Step 3: Execute API Call
┌────────────────────────────────────────┐
│ POST https://mj.3plnext.com/api/order/ │
│      GetOrdersList                     │
│                                        │
│ Response: {                            │
│   "data": [                            │
│     [123, "Order #1", ...],            │
│     [124, "Order #2", ...],            │
│     ...                                │
│   ],                                   │
│   "recordsTotal": 15                   │
│ }                                      │
└────────────────────────────────────────┘

Step 4: Load Sub-Actions (IsEnabled=1)
┌────────────────────────────────────────┐
│ Query: SELECT * FROM SubAction         │
│        WHERE PrimaryApiId = 1          │
│        AND IsEnabled = 1               │
│        ORDER BY ExecutionOrder         │
│                                        │
│ Result: [                              │
│   {                                    │
│     ActionNumber: 1,                   │
│     ActionType: "CreatePicklistBatch", │
│     Configuration: {...}               │
│   }                                    │
│ ]                                      │
└────────────────────────────────────────┘

Step 5: Execute Sub-Action #1
┌────────────────────────────────────────┐
│ Type: CreatePicklistBatch              │
│ Extract order IDs: [123, 124, ...]    │
│ Batch into groups of 25                │
│ Call: POST /api/PickList/Create...    │
│                                        │
│ Response: {                            │
│   "data": [                            │
│     {"pickListId": 501},               │
│     {"pickListId": 502}                │
│   ]                                    │
│ }                                      │
│                                        │
│ Store in context: {                    │
│   "picklistIds": [501, 502]            │
│ }                                      │
└────────────────────────────────────────┘

Step 6: Check for Next Sub-Action
┌────────────────────────────────────────┐
│ Next enabled sub-action?               │
│ Result: None (others are disabled)     │
└────────────────────────────────────────┘

[Complete - Ready for next schedule]
```

## Table Relationships Summary

### One-to-Many Relationships
- **Schedule → ScheduleApi** (1:N)
  - One schedule can trigger multiple APIs

- **PrimaryApi → ScheduleApi** (1:N)
  - One API can be in multiple schedules

- **PrimaryApi → SubAction** (1:N)
  - One API can have multiple sub-actions

### Many-to-Many Relationship
- **Schedule ↔ PrimaryApi** (through ScheduleApi)
  - Schedules can have multiple APIs
  - APIs can be in multiple schedules

## Indexes for Performance

```sql
-- API lookups by ApiNumber
CREATE INDEX idx_primaryapi_apinumber ON PrimaryApi(ApiNumber);

-- Filter enabled APIs
CREATE INDEX idx_primaryapi_isenabled ON PrimaryApi(IsEnabled);

-- Get sub-actions for API
CREATE INDEX idx_subaction_primaryapiid ON SubAction(PrimaryApiId);

-- Order sub-actions
CREATE INDEX idx_subaction_executionorder ON SubAction(ExecutionOrder);

-- Filter enabled schedules
CREATE INDEX idx_schedule_isenabled ON Schedule(IsEnabled);

-- Get APIs for schedule
CREATE INDEX idx_scheduleapi_scheduleid ON ScheduleApi(ScheduleId);

-- Get schedules for API
CREATE INDEX idx_scheduleapi_apinumber ON ScheduleApi(ApiNumber);
```

## Query Examples

### Get all APIs for a schedule
```sql
SELECT pa.*
FROM ScheduleApi sa
JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber
WHERE sa.ScheduleId = 1 
  AND pa.IsEnabled = 1
ORDER BY sa.ExecutionOrder;
```

### Get all sub-actions for an API
```sql
SELECT *
FROM SubAction
WHERE PrimaryApiId = 1
  AND IsEnabled = 1
ORDER BY ExecutionOrder;
```

### Get all schedules that trigger an API
```sql
SELECT s.*
FROM Schedule s
JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
WHERE sa.ApiNumber = 1
  AND s.IsEnabled = 1;
```

### Check if API is scheduled
```sql
SELECT COUNT(*) as ScheduleCount
FROM ScheduleApi
WHERE ApiNumber = 1;
```

## Future Enhancements

### ExecutionLog Table (Audit Trail)
```sql
CREATE TABLE ExecutionLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER,
    SubActionId INTEGER,
    ExecutedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    Success INTEGER,
    ErrorMessage TEXT,
    ResponseTime INTEGER,
    FOREIGN KEY (ApiNumber) REFERENCES PrimaryApi(ApiNumber),
    FOREIGN KEY (SubActionId) REFERENCES SubAction(Id)
);
```

### ApiHealth Table (Monitoring)
```sql
CREATE TABLE ApiHealth (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER,
    CheckedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    IsHealthy INTEGER,
    ResponseTime INTEGER,
    StatusCode INTEGER,
    ErrorMessage TEXT,
    FOREIGN KEY (ApiNumber) REFERENCES PrimaryApi(ApiNumber)
);
```
