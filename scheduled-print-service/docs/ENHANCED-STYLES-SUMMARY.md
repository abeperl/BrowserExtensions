# Enhanced Styles Applied to API #2 - Summary

**Date**: 2025-12-04
**Migration File**: `scripts/update-api2-enhanced-styles.sql`
**Applied To**: API #2 (Picklist Datatable API) - Actions 1 & 4

---

## Changes Applied ✅

### 1. **Make "Pick List Detail" Title Bigger**
```css
.page-title[locale-res="PickListDetail"] {
  font-size: 3em !important;
  font-weight: bold !important;
  padding: 15px 0 !important;
}
```
**Effect**: Title is now 3x larger with bold font and extra padding

---

### 2. **Hide Form Group Elements**
Hidden the following information fields:
- ✅ **Seller** (`column-permission="Seller"`)
- ✅ **Total Qty** (`column-permission="TotalQty"`)
- ✅ **Total Cases** (`column-permission="TotalCases"`)
- ✅ **Pallet Count** (`column-permission="PalletCount"`)
- ✅ **Assigned Cart** (`column-permission="AssignedCart"`)
- ✅ **Total Amount** (`column-permission="TotalAmount"`)

```css
.form-group[column-permission="Seller"],
.form-group[column-permission="TotalQty"],
.form-group[column-permission="TotalCases"],
.form-group[column-permission="PalletCount"],
.form-group[column-permission="AssignedCart"],
.form-group[column-permission="TotalAmount"] {
  display: none !important;
}
```

---

### 3. **Change "Order#" Header to "Barcode"**
```css
th.numberid_width.skip-extend[locale-res="OrderNo"]::before {
  content: "Barcode" !important;
  font-weight: bold !important;
}
th.numberid_width.skip-extend[locale-res="OrderNo"] {
  font-size: 0 !important;
}
th.numberid_width.skip-extend[locale-res="OrderNo"]::before {
  font-size: 14px !important;
}
```
**Effect**: Column header now displays "Barcode" instead of "Order#"

---

### 4. **Replace Order Number with SKU Barcode**
```css
td[data-repeat-item="OrderNumber"] {
  font-size: 0 !important;
}
td[data-repeat-item="OrderNumber"]::after {
  content: attr(data-barcode-value) !important;
  font-size: 14px !important;
  font-family: "Libre Barcode 128", monospace !important;
  font-weight: normal !important;
}
```

**JavaScript Logic**:
```javascript
function updateBarcodes() {
  const rows = document.querySelectorAll("tbody tr");
  rows.forEach(row => {
    const skuCell = row.querySelector(".sku[data-repeat-item=\"SKU\"]");
    const orderCell = row.querySelector("td[data-repeat-item=\"OrderNumber\"]");
    if (skuCell && orderCell) {
      const skuValue = skuCell.textContent.trim();
      orderCell.setAttribute("data-barcode-value", skuValue);
    }
  });
}
```

**Effect**:
- Order number column now displays the SKU value in barcode font
- JavaScript copies SKU from SKU column to Order# column
- Runs on page load + delayed retries (500ms, 1500ms) for dynamic content

---

### 5. **Enhanced SKU Column Barcode Display**
```css
.show_thermal .sku[data-repeat-item="SKU"] {
  font-family: "Libre Barcode 128", monospace !important;
  font-size: 2em !important;
  font-weight: bold !important;
  display: block !important;
}
```
**Effect**: SKU column displays barcode at 2x size with bold font

---

## Actions Updated

### API #2 - Action 1: Navigate - SS/SO Orders
✅ All styles applied

### API #2 - Action 4: Navigate - Non SS/SO Orders
✅ All styles applied

---

## Technical Details

### Injection Configuration
- **TargetSelector**: `head`
- **InsertPosition**: `append`
- **Total Size**: 2,754 characters (includes CSS + JavaScript)

### CSS Techniques Used
- `::before` pseudo-element for changing header text
- `::after` pseudo-element for displaying barcode value
- `display: none !important` for hiding elements
- `font-size: 0` trick to hide original text while keeping layout

### JavaScript Features
- DOM manipulation to copy SKU values
- Multiple timing strategies (DOMContentLoaded + setTimeout delays)
- Handles dynamically loaded content
- Runs in IIFE to avoid global scope pollution

---

## Database Verification

```sql
SELECT
    p.ApiNumber,
    s.ActionNumber,
    s.ActionName,
    length(json_extract(s.Configuration, '$.HtmlInjections[0].HtmlTemplate')) as InjectionSize
FROM PrimaryApi p
JOIN SubAction s ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2 AND s.ActionNumber IN (1, 4);
```

**Results**:
| ApiNumber | ActionNumber | ActionName | InjectionSize |
|-----------|--------------|------------|---------------|
| 2 | 1 | Navigate - SS/SO Orders | 2754 |
| 2 | 4 | Navigate - Non SS/SO Orders | 2754 |

---

## Next Steps

The styles have been successfully applied to the database at:
```
c:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\publish\api_config.db
```

**To test**:
1. Ensure the service is stopped
2. The database already contains the updated styles
3. Restart the service
4. Trigger API #2 manually or wait for scheduled run
5. Verify the PDF output shows:
   - Larger "Pick List Detail" title
   - Hidden form fields (Seller, Total Qty, etc.)
   - "Barcode" column header instead of "Order#"
   - SKU values displayed in barcode font in both columns

---

## Barcode Font Note

The styles use `"Libre Barcode 128"` font family. This is a free Google Font.

**Options**:
1. ✅ **Current approach**: Relies on browser fallback if font not available
2. **Alternative**: Add Google Font import to guarantee availability:
   ```html
   <link href="https://fonts.googleapis.com/css2?family=Libre+Barcode+128&display=swap" rel="stylesheet">
   ```

If barcodes don't render properly, we can add the font import in a follow-up update.

---

## Files Modified

1. ✅ `scripts/update-api2-enhanced-styles.sql` - Created migration script
2. ✅ `publish/api_config.db` - Updated SubAction configurations for Actions 1 & 4
3. ✅ `ENHANCED-STYLES-SUMMARY.md` - This documentation file

---

## Rollback Instructions

If you need to revert these changes:

```sql
-- Restore original styles (customer name only)
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.HtmlInjections',
    json_array(
        json_object(
            'TargetSelector', 'head',
            'InsertPosition', 'append',
            'HtmlTemplate', '<style>.form-group[column-permission=CustomerMaster] .form-control[data-bind=Customers] { background-color: #000 !important; color: #fff !important; font-size: 2.55em !important; font-weight: bold !important; padding: 20px !important; text-align: center !important; display: block !important; margin-top: 10px !important; }</style>'
        )
    )
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber IN (1, 4);
```
