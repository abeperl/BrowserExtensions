# Simple Auto Print - Just Click Buttons

## Overview

**New simplified approach** that:
1. ✅ Just clicks the existing modal buttons
2. ✅ Forces `PrintWithUtility = false` for Carton Labels
3. ✅ No complex HTML generation or JSPrintManager needed
4. ✅ Uses the existing button logic (tested and working)

## What It Does

When the shipment success modal appears:

```
1. Wait 2 seconds (configurable)
2. Click "Packing Slip" button (id: btnPrintPackSlip)
3. Wait 500ms
4. Override PrintWithUtility config → force FALSE
5. Click "Print Carton Label" button (id: box-label)
6. Done! Both print windows open with browser print dialog
```

## Key Feature: PrintWithUtility Override

**Problem**: The original `PrintBoxLabelByPackingSlip` function checks:
```javascript
if(common.getConfigByName("PrintWithUtility", true)){
  common.PrintCartonLabelPrint(labelMasterDiv, function(){
    // opens window
  });
} else {
  // opens window directly
}
```

**Solution**: We intercept `common.getConfigByName()` and force it to return `false` for `"PrintWithUtility"`:

```javascript
common.getConfigByName = function(configName, defaultValue) {
    if (configName === 'PrintWithUtility') {
        return false;  // Always false for carton labels
    }
    return originalGetConfig.call(this, configName, defaultValue);
};
```

This makes the carton label **always** skip the utility and go straight to `window.open()` → `window.print()`.

## Configuration

Edit these settings in `simple-auto-print.js`:

```javascript
const CONFIG = {
    autoClickEnabled: true,        // Enable/disable auto-clicking
    autoClickDelay: 2000,          // Wait 2s after modal appears
    delayBetweenClicks: 500,       // Wait 500ms between buttons
    forcePrintWithUtilityFalse: true,  // Force PrintWithUtility=false
    debugMode: true                // Detailed console logging
};
```

## Console Output

When modal appears:

```
🎯 Modal appearance detected
📊 Modal check:
   Success modal: VISIBLE
   Create modal: hidden
✅ Success modal confirmed
📌 Shipment ID: 12345
⏳ Auto-clicking buttons in 2000ms...

═══════════════════════════════════════════
🖱️ Starting button click sequence...
═══════════════════════════════════════════

📄 STEP 1: Clicking Packing Slip button...
   Button found: {visible: true, disabled: false, hasOnclick: true}
✅ Packing Slip button clicked
⏳ Waiting 500ms before next button...

📦 STEP 2: Preparing Carton Label print...
🔧 PrintWithUtility intercepted - forcing FALSE for carton label
✅ PrintWithUtility override installed (always returns FALSE)

📦 STEP 3: Clicking Carton Label button...
   Button found: {visible: true, disabled: false, hasOnclick: true}
✅ Carton Label button clicked

✅ All buttons clicked successfully!
═══════════════════════════════════════════
```

## Router Integration

Updated router (lines 865-924) now uses `window.simpleAutoPrint`:

```javascript
// ========== SIMPLE AUTO PRINT - JUST CLICK BUTTONS ==========
if (typeof window.simpleAutoPrint !== 'undefined') {
    console.log('🖨️ Simple Auto Print feature enabled');
    // Sets up modal observer
    // Calls window.simpleAutoPrint.handleModal() when modal appears
}
```

## Files

1. ✅ **`simple-auto-print.js`** - Main script (NEW)
2. ✅ **`router.js`** - Updated to use simpleAutoPrint (UPDATED)
3. ✅ **`SIMPLE-AUTO-PRINT-README.md`** - This file (NEW)

## Testing

### Load the Script

```html
<!-- Load before router.js -->
<script src="simple-auto-print.js"></script>
<script src="router.js"></script>
```

### Check If Loaded

```javascript
// Should show configuration
window.simpleAutoPrint.config
```

### Manual Test

```javascript
// Disable auto-click for testing
window.simpleAutoPrint.setAutoClick(false);

// Manually trigger button clicks
window.simpleAutoPrint.clickButtons();
```

### Check Shipment ID

```javascript
// View captured shipment ID
window.simpleAutoPrint.getShipmentId();
```

## Advantages Over Previous Approaches

| Approach | Pros | Cons |
|----------|------|------|
| **Silent Print (JSPrintManager)** | No print dialog, truly silent | Complex setup, requires client app, printer config |
| **Box Label Only** | Focused on one document | Still complex, regenerates HTML |
| **Simple Auto Print (NEW)** | ✅ Just clicks buttons<br>✅ Uses existing code<br>✅ No regeneration<br>✅ Forces PrintWithUtility=false<br>✅ Easy to test | Shows print dialog (but that's acceptable) |

## How It Works

```
┌─────────────────────────────────────────────┐
│ 1. User clicks "Create Shipment"           │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 2. API: ProcessOutboundShipment             │
│    → Interceptor captures shipmentId       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 3. Success modal appears                    │
│    → Router detects modal                  │
│    → Calls simpleAutoPrint.handleModal()   │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 4. Wait 2 seconds                           │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 5. Click "Packing Slip" button             │
│    → Opens packing slip in new window      │
│    → Browser print dialog appears          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 6. Wait 500ms                               │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 7. Override PrintWithUtility → FALSE       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 8. Click "Print Carton Label" button       │
│    → Checks PrintWithUtility (gets FALSE)  │
│    → Skips utility, goes direct to window  │
│    → Opens carton label in new window      │
│    → Browser print dialog appears          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 9. Done! Both documents ready to print     │
└─────────────────────────────────────────────┘
```

## Troubleshooting

### ❌ "Simple Auto Print NOT LOADED"

**Solution**: Ensure `simple-auto-print.js` is loaded before `router.js`

### ❌ "Packing Slip button not found"

**Solution**: Check button exists with: `document.getElementById('btnPrintPackSlip')`

### ❌ "Carton Label button not found"

**Solution**: Check button exists with: `document.getElementById('box-label')`

### ⚠️ "Shipment ID not found (but continuing anyway)"

**Info**: This is OK - buttons don't need shipment ID, they get it from modal context

### ❌ PrintWithUtility override not working

**Debug**:
```javascript
// Check if common exists
typeof common !== 'undefined'

// Check if getConfigByName exists
typeof common.getConfigByName === 'function'

// Test override manually
window.simpleAutoPrint.overridePrintUtility()
```

## Manual Control

```javascript
// Disable auto-click (for manual testing)
window.simpleAutoPrint.setAutoClick(false)

// Enable auto-click
window.simpleAutoPrint.setAutoClick(true)

// Manually click buttons (when modal is open)
window.simpleAutoPrint.clickButtons()

// Get current config
window.simpleAutoPrint.config

// Check if override is active
typeof common.getConfigByName  // Should be function
```

## Next Steps

1. ✅ Load `simple-auto-print.js`
2. ✅ Load `router.js` (updated)
3. ✅ Navigate to `#outbound/packing`
4. ✅ Create a shipment
5. ✅ Watch console - should see button clicks
6. ✅ Both print windows should open

This is now the **simplest possible approach** - just clicking the existing buttons! 🎉
