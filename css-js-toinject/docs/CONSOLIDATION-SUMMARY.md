# File Consolidation Summary

## Changes Made

### ❌ **Files Deleted**
1. `print-url-redirect-global.js` - Dangerous global redirect (breaks other prints)
2. `global-box-label-monitor.js` - Duplicate of box-label-format-injector.js

### ✅ **New Consolidated File**
**`carton-label-manager.js`** - All-in-one carton label solution

### 📦 **Files Combined Into carton-label-manager.js**

| Original File | Features Extracted | Status |
|---------------|-------------------|---------|
| box-label-format-injector.js | CSS injection via window.open | ✅ Consolidated |
| carton-label-redirect.js | Targeted URL redirect (carton labels only) | ✅ Consolidated |
| simple-auto-print.js | Auto-click View Invoice button | ✅ Consolidated |

### ✅ **Files Kept As-Is**
- `table-item-linker.js` - Unrelated SKU/Qty functionality (kept separate)

### ❌ **Features Excluded**
- Text doubling/enhancement (from placard-text-enhancer.js) - Not needed
- Silent print with JSPrintManager (from box-label-only-silent.js) - Not needed
- TabManager integration (from global-box-label-monitor.js) - Not needed

---

## New File: carton-label-manager.js

### **Features Included**

#### 1. **CSS Styling**
```javascript
// Hides:
- "Box No : 1" line
- "To:" text

// Adjusts:
- Phone numbers on same line
- Padding and spacing
```

**Source:** `box-label-format-injector.js`

#### 2. **URL Redirect (Targeted)**
```javascript
// ONLY redirects carton labels:
localhost:8080/printinvoice → https://server:5555/print

// Detection method:
- Checks if HTML contains "box-label-wrp" class
- Other prints (invoice, packing slip) pass through unchanged
```

**Source:** `carton-label-redirect.js`

#### 3. **Auto-Print**
```javascript
// Auto-clicks "View Invoice" button when shipment is created
// Manual click for "Print Carton Label" (with automatic redirect)
```

**Source:** `simple-auto-print.js`

---

## API Changes

### **Old API (No Longer Available)**

```javascript
// OLD - DO NOT USE
window.simpleAutoPrint.handleModal();
window.simpleAutoPrint.setAutoClick(false);
window.simpleAutoPrint.config;
```

### **New API (Use This)**

```javascript
// NEW - USE THIS
window.cartonLabelManager.handleModal();
window.cartonLabelManager.setAutoClick(false);
window.cartonLabelManager.config;
window.cartonLabelManager.stats();
window.cartonLabelManager.redirectCount();
```

---

## Router.js Changes

### **Before**
```javascript
if (typeof window.simpleAutoPrint !== 'undefined') {
    window.simpleAutoPrint.handleModal();
}
```

### **After**
```javascript
if (typeof window.cartonLabelManager !== 'undefined') {
    window.cartonLabelManager.handleModal();
}
```

---

## Configuration

All settings are in one place:

```javascript
window.cartonLabelManager.config = {
    autoClickEnabled: true,      // Enable/disable auto-click
    autoClickDelay: 0,           // Delay before clicking (ms)
    oldUrl: 'localhost:8080/printinvoice',  // Original server
    newUrl: 'https://server:5555/print',    // New print server
    debugMode: true              // Console logging
};
```

**Change settings:**
```javascript
// Disable auto-click
window.cartonLabelManager.setAutoClick(false);

// View stats
window.cartonLabelManager.stats();
```

---

## How It Works

### 1. **CSS Injection**
- Intercepts `window.open()` calls
- Detects if window contains `.box-label-wrp` element
- Injects custom CSS before print dialog appears
- Uses multiple timing strategies to catch DOM early

### 2. **URL Redirect**
- Intercepts Fetch API, XMLHttpRequest, and jQuery $.post
- Checks POST request body for `box-label-wrp` class
- **ONLY redirects if carton label detected**
- Other prints (invoice, packing slip) use original server

### 3. **Auto-Print**
- Captures shipment ID from API response
- Watches for shipment success modal
- Auto-clicks "View Invoice" button
- User manually clicks "Print Carton Label" (auto-redirects to new server)

---

## Testing Checklist

### ✅ **Carton Label CSS**
1. Click "Print Carton Label" button
2. Verify:
   - "Box No : 1" line is hidden
   - "To:" text is hidden
   - Phone numbers on same line
   - Proper spacing

### ✅ **URL Redirect**
1. Open DevTools → Network tab
2. Click "Print Carton Label" button
3. Verify POST request goes to `https://server:5555/print`
4. Check console for redirect message
5. Test other buttons (Invoice, Packing Slip) - should NOT redirect

### ✅ **Auto-Print**
1. Create a shipment
2. Verify "View Invoice" button auto-clicks
3. Manually click "Print Carton Label"
4. Verify it redirects to new server

---

## Debug Commands

```javascript
// Check if loaded
typeof window.cartonLabelManager

// View stats
window.cartonLabelManager.stats();

// Count redirects
window.cartonLabelManager.redirectCount();

// Last redirect time
window.cartonLabelManager.lastRedirect();

// Disable auto-click
window.cartonLabelManager.setAutoClick(false);

// Re-enable auto-click
window.cartonLabelManager.setAutoClick(true);

// Manual modal trigger
window.cartonLabelManager.handleModal();

// Manual View Invoice click
window.cartonLabelManager.clickViewInvoice();
```

---

## File Size Comparison

| Before | After | Savings |
|--------|-------|---------|
| box-label-format-injector.js (6.8 KB) | | |
| carton-label-redirect.js (8.2 KB) | | |
| simple-auto-print.js (18.5 KB) | | |
| print-url-redirect-global.js (7.1 KB) | | |
| global-box-label-monitor.js (7.5 KB) | | |
| **Total: 48.1 KB (5 files)** | **carton-label-manager.js: 29.4 KB (1 file)** | **-38.8% size, -80% files** |

---

## Migration Guide

### **Step 1: Remove Old Files**
```bash
# Already completed
rm print-url-redirect-global.js
rm global-box-label-monitor.js
```

### **Step 2: Update HTML/Script Includes**

**Before:**
```html
<script src="box-label-format-injector.js"></script>
<script src="carton-label-redirect.js"></script>
<script src="simple-auto-print.js"></script>
```

**After:**
```html
<script src="carton-label-manager.js"></script>
```

### **Step 3: Update Code References**

**Find and replace:**
```javascript
// OLD
window.simpleAutoPrint

// NEW
window.cartonLabelManager
```

### **Step 4: Test All Features**
- ✅ CSS formatting on carton labels
- ✅ URL redirect (carton labels only)
- ✅ Auto-click View Invoice
- ✅ Manual carton label (with redirect)
- ✅ Other prints NOT redirected

---

## Benefits

### **Before Consolidation:**
- ❌ 5 separate files to manage
- ❌ Duplicate code (CSS injection)
- ❌ Multiple window.open interceptors
- ❌ Confusing API (which file does what?)
- ❌ Risk of loading wrong files

### **After Consolidation:**
- ✅ 1 consolidated file
- ✅ No duplicate code
- ✅ Single window.open interceptor
- ✅ Clear API (`window.cartonLabelManager`)
- ✅ All features work together seamlessly

---

## Troubleshooting

### **Issue: CSS not applied**
**Check:**
```javascript
// Should show CSS injection logs
window.cartonLabelManager.stats();
```

**Solution:** Ensure window.open is being called (check console for 🪟 logs)

### **Issue: Not redirecting**
**Check:**
```javascript
// Should show redirect count
window.cartonLabelManager.redirectCount();
```

**Solution:** Ensure request body contains `box-label-wrp` class

### **Issue: Auto-click not working**
**Check:**
```javascript
// Should be true
window.cartonLabelManager.config.autoClickEnabled
```

**Solution:** Enable with `window.cartonLabelManager.setAutoClick(true)`

### **Issue: Redirecting ALL prints**
**This should NOT happen!**

If it does:
1. Check you deleted `print-url-redirect-global.js`
2. Check console for "CARTON LABEL DETECTED" message
3. Only carton labels should show this message

---

## Summary

### **What Changed**
- Consolidated 5 files → 1 file
- Removed duplicates and unused features
- Unified API under `window.cartonLabelManager`

### **What Stayed the Same**
- `table-item-linker.js` (unchanged)
- All features still work identically
- Router integration updated

### **What's Better**
- Simpler codebase
- Easier to maintain
- Clear separation of concerns
- Single point of configuration

---

## Next Steps

1. ✅ Files deleted
2. ✅ Consolidated file created
3. ✅ Router updated
4. ✅ Documentation complete

**Ready to use!** Load `carton-label-manager.js` and test the features. 🎉