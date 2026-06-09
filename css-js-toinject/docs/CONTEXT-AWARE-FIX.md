# Context-Aware PrintWithUtility Override

## Problem

The `PrintWithUtility` override was affecting **BOTH** buttons:
- ❌ Packing Slip: `PrintWithUtility` forced to FALSE (wrong!)
- ✅ Carton Label: `PrintWithUtility` forced to FALSE (correct)

## Solution

Added a **context flag** that tracks whether we're printing Carton Labels or not:

```javascript
let isCartonLabelContext = false;

common.getConfigByName = function(configName, defaultValue) {
    if (configName === 'PrintWithUtility') {
        if (isCartonLabelContext) {
            return false;  // Force FALSE for carton labels
        } else {
            return originalGetConfig(...);  // Use ORIGINAL for packing slips
        }
    }
    return originalGetConfig(...);
};
```

## How It Works

### Step-by-Step Flow

```
STEP 0: Install context-aware override
        ↓
        Set context flag = FALSE

STEP 1: Click Packing Slip button
        ↓
        Context = FALSE
        ↓
        PrintWithUtility check → Returns ORIGINAL value
        ↓
        Packing slip uses correct setting ✅

STEP 2: Enable carton label context
        ↓
        Set context flag = TRUE

STEP 3: Click Carton Label button
        ↓
        Context = TRUE
        ↓
        PrintWithUtility check → Returns FALSE
        ↓
        Carton label skips utility, uses window.print() ✅

STEP 4: Cleanup
        ↓
        Set context flag = FALSE
```

## Console Output (Fixed)

### Packing Slip
```
📄 STEP 1: Clicking Packing Slip button...
   Context: DISABLED (will use original PrintWithUtility value)
   Button found: {visible: true, disabled: false, hasOnclick: false}
🔧 PrintWithUtility intercepted - using ORIGINAL for PACKING SLIP
✅ Packing Slip button clicked
```

### Carton Label
```
📦 STEP 2: Enabling Carton Label context...
🏷️ Carton label context ENABLED (PrintWithUtility will be FALSE)

📦 STEP 3: Clicking Carton Label button...
   Context: ENABLED (will force PrintWithUtility to FALSE)
   Button found: {visible: true, disabled: false, hasOnclick: false}
🔧 PrintWithUtility intercepted - forcing FALSE for CARTON LABEL
✅ Carton Label button clicked
```

## Key Changes in Code

### 1. Added Context Flag
```javascript
let isCartonLabelContext = false;
```

### 2. Context Control Functions
```javascript
function enableCartonLabelContext() {
    isCartonLabelContext = true;
}

function disableCartonLabelContext() {
    isCartonLabelContext = false;
}
```

### 3. Smart Override
```javascript
if (configName === 'PrintWithUtility') {
    if (isCartonLabelContext) {
        // Carton label context
        return false;
    } else {
        // Packing slip context (or anything else)
        return originalGetConfig.call(this, configName, defaultValue);
    }
}
```

### 4. Updated Button Click Sequence
```javascript
// STEP 0: Install override (but context disabled)
overridePrintWithUtility();
disableCartonLabelContext();

// STEP 1: Click packing slip (context disabled)
packingSlipBtn.click();

// STEP 2: Enable context
enableCartonLabelContext();

// STEP 3: Click carton label (context enabled)
cartonLabelBtn.click();

// STEP 4: Cleanup
disableCartonLabelContext();
```

## Benefits

1. ✅ **Packing Slip**: Uses its original `PrintWithUtility` setting (whatever it was configured to be)
2. ✅ **Carton Label**: Always uses `PrintWithUtility = false` (skips utility, direct to window.print())
3. ✅ **Clean Separation**: Each button gets the right behavior
4. ✅ **Cleanup**: Context is disabled after use

## Debugging

### Check Current Context
```javascript
// Should be false when idle
window.simpleAutoPrint.isCartonContext()
```

### Manually Control Context
```javascript
// Enable context
window.simpleAutoPrint.enableCartonContext()

// Check it
window.simpleAutoPrint.isCartonContext()  // Should be true

// Disable context
window.simpleAutoPrint.disableCartonContext()
```

### Test Override
```javascript
// Install override
window.simpleAutoPrint.overridePrintUtility()

// Test packing slip context
window.simpleAutoPrint.disableCartonContext()
common.getConfigByName('PrintWithUtility')  // Should return original value

// Test carton label context
window.simpleAutoPrint.enableCartonContext()
common.getConfigByName('PrintWithUtility')  // Should return false
```

## What Each Button Gets Now

| Button | Context Flag | PrintWithUtility Result |
|--------|-------------|------------------------|
| **Packing Slip** | `false` | ✅ Original value (from config) |
| **Carton Label** | `true` | ✅ Always `false` (skips utility) |

## Why This Matters

The **Carton Label** function checks `PrintWithUtility`:

```javascript
if(common.getConfigByName("PrintWithUtility", true)){
  common.PrintCartonLabelPrint(labelMasterDiv, function(){
    // Utility path (may not work or may have issues)
  });
} else {
  // Direct window.open() path (what we want!)
  var mywindow = window.open("", "", "height=700,width=1100");
  mywindow.document.write(...);
  mywindow.print();
}
```

By forcing `PrintWithUtility = false` **only for carton labels**, we ensure they always take the direct path!

## Result

✅ Packing slips work as originally configured
✅ Carton labels always use direct window printing
✅ No unwanted side effects
✅ Clean, context-aware override
