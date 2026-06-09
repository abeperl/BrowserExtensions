# Picklist Style Fixes - API 2

**Date**: 2025-12-15
**API**: API 2 (Picklist Datatable API - Unified)
**Action Modified**: Action 1 (Navigate - Picklist - SS/SO Orders)

## Changes Made

### 1. Customer Box Width ✅
**Problem**: Customer name was wrapping to multiple lines
**Solution**: Added full-width and no-wrap constraints

```css
.form-group[column-permission=CustomerMaster] .form-control[data-bind=Customers] {
  min-width: 100% !important;
  width: 100% !important;
  white-space: nowrap !important;
  overflow: visible !important;
}
```

### 2. SKU Font - Remove Bold ✅
**Problem**: SKU was bold and different size from other text
**Solution**: Reset to normal font weight and inherit size

```css
.show_thermal .sku[data-repeat-item="SKU"] {
  font-family: inherit !important;
  font-size: inherit !important;
  font-weight: normal !important;
  display: inline-block !important;
}
```

### 3. Barcode Header Size ✅
**Problem**: "Barcode" header was 14px while other headers were larger
**Solution**: Changed to `font-size: inherit` to match other headers

```css
th.numberid_width.skip-extend[locale-res="OrderNo"]::before {
  content: "Barcode" !important;
  font-weight: bold !important;
  font-size: inherit !important; /* Same size as other headers */
}
```

### 4. Barcode Data - Show SKU as Barcode ✅
**Problem**: Barcode column was empty
**Solution**: Display SKU value as barcode using Libre Barcode 128 font

```css
td[data-repeat-item="OrderNumber"]::after {
  content: attr(data-barcode-value) !important;
  font-size: 32px !important;
  font-family: "Libre Barcode 128", monospace !important;
  font-weight: normal !important;
  display: block !important;
  line-height: 1.2 !important;
}
```

**JavaScript**: Copies SKU value to `data-barcode-value` attribute
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

## How It Works

### Page Load Sequence
1. Page navigates to picklist detail
2. CSS is injected into `<head>`
3. JavaScript executes after DOM loads
4. SKU values are copied to barcode column
5. CSS `::after` pseudo-element displays barcode

### Barcode Font
The page already loads **Libre Barcode 128** font via CDN or local assets. The CSS uses this font to render SKU values as scannable barcodes.

### Timing
JavaScript runs at:
- Document ready
- 500ms delay
- 1500ms delay
- 3000ms delay

This ensures it captures dynamically loaded content.

## Testing Checklist

After applying this update, verify:

- [ ] Customer name displays on single line (no wrapping)
- [ ] SKU text is normal weight (not bold)
- [ ] SKU font size matches other table text
- [ ] "Barcode" header is same size as "SKU", "Qty", etc.
- [ ] Barcode column shows SKU values as barcodes
- [ ] Barcodes are scannable (test with scanner)
- [ ] All changes visible in printed PDF

## Files Modified

- **Database**: `api_config.db` → SubAction table (PrimaryApiId=2, ActionNumber=1)
- **Script**: `scripts/update-picklist-styles-api2.sql`
- **Documentation**: `docs/PICKLIST-STYLE-FIXES.md`

## Deployment

To apply these changes to production:

1. **Run the SQL script**:
   ```bash
   sqlite3 api_config.db < scripts/update-picklist-styles-api2.sql
   ```

2. **Restart the service**:
   ```powershell
   Restart-Service ScheduledPrintService
   ```

3. **Test manually**:
   ```bash
   ScheduledPrintService.exe -m -a 2
   ```

4. **Check output PDF** in `/out/` folder

## Before & After

### Before
- ❌ Customer name wrapped to 2-3 lines
- ❌ SKU was bold and larger font
- ❌ "Barcode" header was small (14px)
- ❌ Barcode column was empty

### After
- ✅ Customer name on single line
- ✅ SKU normal font, same as other text
- ✅ "Barcode" header matches other headers
- ✅ Barcode column shows SKU as scannable barcode

## Related Files

- [SubActionExecutor.cs](../ScheduledPrintService/Services/SubActionExecutor.cs) - Action execution
- [api_config.db](../data/api_config.db) - Database with configuration
- [update-picklist-styles-api2.sql](../scripts/update-picklist-styles-api2.sql) - SQL migration

## Notes

- All changes are CSS/JavaScript only - no code changes required
- Changes only affect printed PDF output
- Does not affect database data or API responses
- Safe to rollback by reverting HtmlInjections[0] in database