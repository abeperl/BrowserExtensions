# Filter Decision Tree - Visual Guide

## API 2 (Picklist Orders) - Decision Flow

```
Start: Fetch Picklist Datatable
         ↓
    [Get all rows from data array]
         ↓
         ├─────────────────────────────────────┬──────────────────────────────────────┐
         ↓                                     ↓                                      ↓
    BRANCH 1                              BRANCH 2                              (Other branches
  Action 1: Navigate                    Action 3: Navigate                      can be added)
         ↓                                     ↓
 Filter: data[x][17]                   Filter: data[x][17]
 StartsWithAny ["SS", "SO"]           NotStartsWithAny ["SS", "SO"]
         ↓                                     ↓
    Does OrderId                          Does OrderId
    start with                            NOT start with
    "SS" or "SO"?                        "SS" or "SO"?
         ↓                                     ↓
    ┌────┴────┐                          ┌────┴────┐
    ↓         ↓                          ↓         ↓
   YES       NO                         YES       NO
    ↓         ↓                          ↓         ↓
Navigate    SKIP                     Navigate    SKIP
to page                              to page
    ↓                                     ↓
Action 2:                            Action 4:
Print PDF                            Print PDF
    ↓                                     ↓
Print to                             Print to
Printer 1                            Printer 2
(NPI84BD10)                          (4301)
    ↓                                     ↓
   DONE                                 DONE

Examples:
Row 16: data[16][17] = "SS1234"
  → Branch 1: YES → Navigate → Print Printer 1

Row 17: data[17][17] = "SM5678"
  → Branch 1: NO → SKIP
  → Branch 2: YES → Navigate → Print Printer 2

Row 18: data[18][17] = "SO9999"
  → Branch 1: YES → Navigate → Print Printer 1
```

## API 3 (Personalized Orders) - Decision Flow

```
Start: Fetch Personalized Order Items
         ↓
    [Get all items from data array]
         ↓
         ├─────────────────────────────────────┬──────────────────────────────────────┐
         ↓                                     ↓                                      ↓
    BRANCH 1                              BRANCH 2                              (Other branches
  Action 1: Navigate                    Action 4: Navigate                      can be added)
         ↓                                     ↓
┌────────────────────────┐            ┌────────────────────────┐
│ PRIMARY FILTER         │            │ PRIMARY FILTER         │
│ IsFilePath on          │            │ IsFilePath on          │
│ itemNotes              │            │ itemNotes              │
└────────────────────────┘            └────────────────────────┘
         ↓                                     ↓
    Does itemNotes                        Does itemNotes
    contain .html and                     contain .html and
    NOT start with {?                     NOT start with {?
         ↓                                     ↓
    ┌────┴────┐                          ┌────┴────┐
    ↓         ↓                          ↓         ↓
   YES       NO                         YES       NO
    ↓         ↓                          ↓         ↓
Continue   SKIP                      Continue   SKIP
    ↓                                     ↓
┌────────────────────────┐            ┌────────────────────────┐
│ ADDITIONAL FILTER      │            │ ADDITIONAL FILTER      │
│ StartsWithAny on       │            │ NotStartsWithAny on    │
│ orderId                │            │ orderId                │
└────────────────────────┘            └────────────────────────┘
         ↓                                     ↓
    Does orderId                          Does orderId
    start with                            NOT start with
    "SS" or "SO"?                        "SS" or "SO"?
         ↓                                     ↓
    ┌────┴────┐                          ┌────┴────┐
    ↓         ↓                          ↓         ↓
   YES       NO                         YES       NO
    ↓         ↓                          ↓         ↓
Navigate    SKIP                     Navigate    SKIP
to {itemNotes}                       to {itemNotes}
    ↓                                     ↓
Action 2:                            Action 5:
Save PDF                             Save PDF
    ↓                                     ↓
Save as                              Save as
personalized-ss-so-{id}.pdf         personalized-other-{id}.pdf
    ↓                                     ↓
Action 3:                            Action 6:
Print PDF                            Print PDF
    ↓                                     ↓
Print to                             Print to
Printer 1                            Printer 2
(NPI84BD10)                          (4301)
    ↓                                     ↓
   DONE                                 DONE

Examples:
Item 1: orderId="SS1234", itemNotes="/custom/form.html"
  → Branch 1: IsFilePath=YES, StartsWithAny=YES
  → Navigate → Save personalized-ss-so-1001.pdf → Print Printer 1

Item 2: orderId="SM5678", itemNotes="/custom/form2.html"
  → Branch 1: IsFilePath=YES, StartsWithAny=NO → SKIP
  → Branch 2: IsFilePath=YES, NotStartsWithAny=YES
  → Navigate → Save personalized-other-1002.pdf → Print Printer 2

Item 3: orderId="SS9999", itemNotes='{"json":"data"}'
  → Branch 1: IsFilePath=NO → SKIP
  → Branch 2: IsFilePath=NO → SKIP
  → Item completely skipped (not a file path)
```

## Filter Type Quick Reference

### Simple Filters (Single Value)

```
StartsWith
    Input: "SS1234"
    Pattern: "SS"
    Result: ✓ MATCH

NotStartsWith
    Input: "SS1234"
    Pattern: "SS"
    Result: ✗ NO MATCH

Contains
    Input: "ORDER-SS-123"
    Pattern: "SS"
    Result: ✓ MATCH (substring found)

Equals
    Input: "SS1234"
    Pattern: "SS1234"
    Result: ✓ MATCH (exact)
```

### Multi-Value Filters

```
StartsWithAny
    Input: "SS1234"
    Patterns: ["SS", "SO", "SR"]
    Logic: Match if starts with ANY pattern
    Result: ✓ MATCH (starts with "SS")

    Input: "SM5678"
    Patterns: ["SS", "SO", "SR"]
    Result: ✗ NO MATCH (doesn't start with any)

NotStartsWithAny
    Input: "SM5678"
    Patterns: ["SS", "SO"]
    Logic: Match if does NOT start with ANY pattern
    Result: ✓ MATCH (doesn't start with "SS" or "SO")

    Input: "SS1234"
    Patterns: ["SS", "SO"]
    Result: ✗ NO MATCH (starts with "SS")
```

### Special Filter: IsFilePath

```
IsFilePath
    Input: "/custom/forms/order.html"
    Check 1: Contains ".html"? ✓
    Check 2: Starts with "{"? ✗
    Check 3: Starts with "["? ✗
    Result: ✓ MATCH (valid file path)

    Input: '{"orderId": "123"}'
    Check 1: Contains ".html"? ✗
    Result: ✗ NO MATCH (JSON object)

    Input: "regular text note"
    Check 1: Contains ".html"? ✗
    Result: ✗ NO MATCH (not a file path)
```

## Combined Filter Logic (API 3)

### AND Logic - Both Must Pass

```
Item Must Pass:
    PRIMARY Filter (IsFilePath)
         AND
    ADDITIONAL Filter (StartsWithAny/NotStartsWithAny)

Example 1: Both Pass
    itemNotes: "/custom/form.html"
        → IsFilePath: ✓ (has .html)
    orderId: "SS1234"
        → StartsWithAny ["SS", "SO"]: ✓ (starts with "SS")
    RESULT: ✓ PROCESS THIS ITEM

Example 2: Primary Fails
    itemNotes: '{"json": "data"}'
        → IsFilePath: ✗ (JSON, not file path)
    orderId: "SS1234"
        → StartsWithAny ["SS", "SO"]: ✓ (would pass)
    RESULT: ✗ SKIP (primary filter failed)

Example 3: Additional Fails
    itemNotes: "/custom/form.html"
        → IsFilePath: ✓ (has .html)
    orderId: "SM9999"
        → StartsWithAny ["SS", "SO"]: ✗ (doesn't start)
    RESULT: ✗ SKIP (additional filter failed)

Example 4: Both Fail
    itemNotes: "regular note"
        → IsFilePath: ✗ (no .html)
    orderId: "AB1111"
        → StartsWithAny ["SS", "SO"]: ✗ (doesn't start)
    RESULT: ✗ SKIP (both failed)
```

## Printer Routing Matrix

### API 2 (Picklist Orders)

| Order ID | Filter Match | Printer | Example |
|----------|--------------|---------|---------|
| SS* | Branch 1 ✓ | Printer 1 (NPI84BD10) | SS1234 → P1 |
| SO* | Branch 1 ✓ | Printer 1 (NPI84BD10) | SO5678 → P1 |
| SM* | Branch 2 ✓ | Printer 2 (4301) | SM9999 → P2 |
| AB* | Branch 2 ✓ | Printer 2 (4301) | AB1111 → P2 |
| Any other | Branch 2 ✓ | Printer 2 (4301) | XY7777 → P2 |

### API 3 (Personalized Orders)

| Order ID | Item Notes | IsFilePath | Filter Match | Printer | Example |
|----------|------------|------------|--------------|---------|---------|
| SS* | /form.html | ✓ | Branch 1 ✓ | Printer 1 | SS1234 + .html → P1 |
| SO* | /form.html | ✓ | Branch 1 ✓ | Printer 1 | SO5678 + .html → P1 |
| SM* | /form.html | ✓ | Branch 2 ✓ | Printer 2 | SM9999 + .html → P2 |
| SS* | {"json"} | ✗ | Skipped | None | SS1234 + JSON → Skip |
| Any | /form.html | ✓ | Branch 2 ✓ | Printer 2 | AB1111 + .html → P2 |

## Adding New Branches - Decision Points

### When to Add a New Branch?

```
Do you need to:
├─ Route specific order types to dedicated printer?
│  └─ YES → Add new branch with filter
│
├─ Process only certain SKUs differently?
│  └─ YES → Add new branch with SKU filter
│
├─ Handle priority orders faster?
│  └─ YES → Add new branch for RUSH orders
│
└─ Split by customer or location?
   └─ YES → Add new branch with appropriate filter

Example Decision:
"We need RUSH orders to print immediately on express printer"
    ↓
Add Branch 3:
    Filter: orderId StartsWith "RUSH"
    Printer: Express-Printer-Name
```

### Branch Addition Checklist

```
For API 2 (Picklist):
☐ Navigate action with filter
☐ Print action chained from Navigate
☐ Update existing filters (if needed)
☐ Test with sample data

For API 3 (Personalized):
☐ Navigate action with IsFilePath + additional filter
☐ Save PDF action chained from Navigate
☐ Print PDF action chained from Save
☐ Update existing filters (if needed)
☐ Set unique OutputFilePrefix
☐ Test with sample data
```

## Troubleshooting Decision Tree

```
Problem: Items not printing
    ↓
Are items being filtered out?
    ├─ YES → Check filter configuration
    │         ├─ Filter values correct?
    │         ├─ Filter type correct?
    │         └─ Check logs for "filtered out"
    │
    └─ NO → Items passing filter but not printing
              ├─ Check printer name spelling
              ├─ Check printer exists (Get-Printer)
              ├─ Check ChainedFromActionNumber
              └─ Check action IsEnabled=1

Problem: Items going to wrong printer
    ↓
Check which filter they match
    ├─ Review filter logic
    ├─ Check filter values
    └─ Verify ChainedFromActionNumber points to correct Navigate action

Problem: PDFs not saving (API 3)
    ↓
Check Save action configuration
    ├─ OutputFilePrefix set?
    ├─ ChainedFromActionNumber correct?
    ├─ IsEnabled = 1?
    └─ Check disk permissions

Problem: All items skipped (API 3)
    ↓
Check primary IsFilePath filter
    ├─ Is itemNotes actually a file path?
    ├─ Does it contain .html?
    └─ Is it JSON data instead?
```

## Performance Decision Tree

```
Is processing slow?
    ↓
How many items per API call?
    ├─ < 10 items → Individual item processing time
    │               ├─ Reduce WaitForNetworkIdleMs
    │               └─ Check network latency
    │
    ├─ 10-50 items → Filter efficiency
    │                ├─ Are filters too broad?
    │                └─ Can you split into more branches?
    │
    └─ > 50 items → System resources
                    ├─ Printer queue depth
                    ├─ Disk I/O (API 3 PDFs)
                    └─ Consider batching limits
```

## Quick Decision Matrix

### "Where should this feature go?"

| Requirement | API | Filter Type | Example |
|-------------|-----|-------------|---------|
| Filter picklists by order prefix | API 2 | StartsWithAny on index 17 | SS, SO, SR |
| Route personalized orders | API 3 | IsFilePath + StartsWithAny | Must be .html + SS/SO |
| Different printer per order type | Either | Add new branch | RUSH → Express printer |
| Save PDFs for audit | API 3 | Add Save action | SaveCapturedHtml |
| Skip invalid data | API 3 | IsFilePath filter | Reject JSON in itemNotes |

### "What filter should I use?"

| Goal | Filter Type | Multi-Value? | Example |
|------|-------------|--------------|---------|
| Match exact prefix | StartsWith | No | "SS" |
| Match multiple prefixes | StartsWithAny | Yes | ["SS", "SO", "SR"] |
| Exclude specific prefixes | NotStartsWithAny | Yes | ["SS", "SO"] |
| Find substring | Contains | No | "RUSH" |
| Exact match | Equals | No | "SS1234" |
| Validate file path | IsFilePath | N/A | .html check |

## Visual Summary

```
┌─────────────────────────────────────────────────────────────┐
│                    MIGRATION DECISION                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Do you have duplicate API calls?                           │
│    YES → Consolidate into single API with branches          │
│    NO  → Consider filter-based routing for flexibility      │
│                                                              │
│  Do different order types need different printers?          │
│    YES → Use filter branches to route accordingly           │
│    NO  → Single branch sufficient                           │
│                                                              │
│  Do you need to validate data format (API 3)?              │
│    YES → Use IsFilePath as primary filter                   │
│    NO  → Use order ID / SKU filters directly                │
│                                                              │
│  Do you need to save PDFs for records?                      │
│    YES → API 3 pattern: Navigate → Save → Print            │
│    NO  → API 2 pattern: Navigate → Print                    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```
