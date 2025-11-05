# Timing Fix for Context-Aware Override

## Problem Discovered

The context was being disabled **too early**:

```
✅ Carton Label button clicked
🔧 STEP 4: Cleanup...
📄 Carton label context DISABLED    ← Disabled immediately!
...
🔧 PrintWithUtility intercepted - using ORIGINAL for PACKING SLIP  ← Wrong!
```

The button's `click()` is **synchronous**, but the button's **function execution is async**:

```javascript
cartonLabelBtn.click();               // Returns immediately
console.log('✅ Clicked');            // Runs right away
disableCartonLabelContext();          // Disables context
                                      // ... button function executes later
                                      // ... checks PrintWithUtility (context already disabled!)
```

## Root Cause

When you click a button:
1. `button.click()` returns immediately ✅
2. But the button's `onclick` handler runs **asynchronously** ⏰
3. The handler makes API calls, generates HTML, etc. ⏰
4. Eventually it checks `common.getConfigByName('PrintWithUtility')` ⏰

By the time step 4 happens, we've already disabled the context!

## Solution: Delayed Cleanup

Keep the context **enabled** for a few seconds after clicking:

```javascript
// Click button
cartonLabelBtn.click();

// Keep context enabled
console.log('Keeping context ENABLED for 2000ms');

// Wait for button's async operations
await new Promise(resolve => setTimeout(resolve, 2000));

// Now it's safe to cleanup
disableCartonLabelContext();
```

## New Flow

```
STEP 3: Click Carton Label button
        ↓
        Click returns immediately
        ↓
STEP 4: Wait 2000ms (context stays ENABLED)
        ↓
        Meanwhile, button's async function executes:
        - Makes API call
        - Generates HTML
        - Checks PrintWithUtility (context is TRUE ✅)
        - Opens print window
        ↓
STEP 5: Cleanup (after 2000ms)
        ↓
        Disable context
```

## Configuration

The delay is now configurable:

```javascript
const CONFIG = {
    contextCleanupDelay: 2000,  // Wait 2s before cleanup
};
```

Adjust if needed:
- **Too short** (< 1000ms): Context disabled before check happens
- **Too long** (> 3000ms): Unnecessary delay, but harmless
- **Recommended**: 2000ms (2 seconds)

## Expected Console Output Now

### Carton Label with Correct Context

```
📦 STEP 2: Enabling Carton Label context...
🏷️ Carton label context ENABLED (PrintWithUtility will be FALSE)

📦 STEP 3: Clicking Carton Label button...
   Context: ENABLED (will force PrintWithUtility to FALSE)
✅ Carton Label button clicked

🔧 STEP 4: Waiting for carton label operations to complete...
   (Keeping context ENABLED for 2000ms)

... (button's async function runs here) ...
🔧 PrintWithUtility intercepted - forcing FALSE for CARTON LABEL ✅

... (after 2000ms) ...
🔧 STEP 5: Cleanup...
📄 Carton label context DISABLED
```

## Why 2000ms?

The button's function does:
1. API call to get shipment data (100-500ms)
2. Generate HTML with barcodes (100-300ms)
3. Check `PrintWithUtility` config ← **This is where context matters**
4. Open print window (immediate)

Total: Usually completes in < 1000ms, so 2000ms is safe with margin.

## Alternative: No Cleanup at All

We could also just **never disable** the context:

```javascript
// Enable context once
enableCartonLabelContext();

// Click carton label
cartonLabelBtn.click();

// Don't disable - just leave it enabled!
// (It only affects carton labels anyway)
```

This would work because:
- Packing slips check the flag and see it's enabled
- But they use `originalGetConfig()` anyway (not affected)
- Only carton labels are affected by the enabled context

But the delayed cleanup is cleaner for debugging.

## Debugging

If you see wrong behavior, check the timing:

```javascript
// Test with longer delay
window.simpleAutoPrint.config.contextCleanupDelay = 5000;
```

Or keep it enabled permanently:

```javascript
// Enable and never disable (for testing)
window.simpleAutoPrint.enableCartonContext();
// Don't call disableCartonContext()
```

## Summary

| Before Fix | After Fix |
|------------|-----------|
| Context disabled immediately | Context kept enabled for 2s |
| PrintWithUtility check happens after disable | PrintWithUtility check happens while enabled |
| ❌ Uses ORIGINAL value | ✅ Uses FALSE value |
| Both documents affected wrong | ✅ Each document correct |
