# Bug Fix: Infinite Recursion in TabManager

## Issue

**Error**: `Uncaught RangeError: Maximum call stack size exceeded`

**Location**: `tab-manager.js` - `getOrCreateTab()` function

## Root Cause

The `getOrCreateTab()` function was calling `window.open()` to create new tabs. However, since TabManager intercepts `window.open()`, this created an infinite recursion:

```javascript
// BAD CODE (before fix):
function getOrCreateTab(url, normalizedUrl) {
    // ... check for existing tab ...

    // This calls the INTERCEPTED window.open, not the original!
    const newTab = window.open(url, '_blank');  // ❌ Causes infinite loop

    return newTab;
}

// Interceptor
window.open = function(url, target, features) {
    // This calls getOrCreateTab...
    const tab = getOrCreateTab(url, normalizedUrl);
    // ... which calls window.open...
    // ... which calls getOrCreateTab...
    // ... INFINITE LOOP! 💥
};
```

## Call Stack

```
window.open (intercepted)
  → getOrCreateTab
    → window.open (intercepted)
      → getOrCreateTab
        → window.open (intercepted)
          → getOrCreateTab
            → ... (repeats until stack overflow)
```

## Solution

Store the original `window.open` function **before** intercepting it, then use the original directly in `getOrCreateTab()`:

```javascript
// GOOD CODE (after fix):
const TabManager = (() => {
    // Store original window.open for direct calls
    let originalWindowOpen = null;  // ✅ Module-level variable

    function getOrCreateTab(url, normalizedUrl) {
        // ... check for existing tab ...

        // Use ORIGINAL window.open to avoid recursion
        const newTab = originalWindowOpen.call(window, url, '_blank');  // ✅ Direct call

        return newTab;
    }

    function installInterceptor() {
        // Store the original function
        originalWindowOpen = window.open;  // ✅ Capture before interception

        // Now intercept
        window.open = function(url, target, features) {
            // This calls getOrCreateTab...
            const tab = getOrCreateTab(url, normalizedUrl);
            // ... which calls originalWindowOpen (NOT intercepted)
            // ... no recursion! ✅
        };
    }
})();
```

## Key Changes

### Before (Broken)
```javascript
const TabManager = (() => {
    const tabRegistry = new Map();

    function getOrCreateTab(url, normalizedUrl) {
        const newTab = window.open(url, '_blank');  // ❌ Calls intercepted version
        return newTab;
    }

    function installInterceptor() {
        const originalWindowOpen = window.open;  // ❌ Local variable, not accessible
        window.open = function(url, target, features) {
            return getOrCreateTab(url, normalizedUrl);
        };
    }
})();
```

### After (Fixed)
```javascript
const TabManager = (() => {
    const tabRegistry = new Map();
    let originalWindowOpen = null;  // ✅ Module-level, accessible everywhere

    function getOrCreateTab(url, normalizedUrl) {
        const newTab = originalWindowOpen.call(window, url, '_blank');  // ✅ Direct call
        return newTab;
    }

    function installInterceptor() {
        originalWindowOpen = window.open;  // ✅ Store at module level
        window.open = function(url, target, features) {
            return getOrCreateTab(url, normalizedUrl);
        };
    }
})();
```

## Testing

### Before Fix
```javascript
window.open("#outbound/packingSlip?id=123");
// Result: RangeError: Maximum call stack size exceeded 💥
```

### After Fix
```javascript
window.open("#outbound/packingSlip?id=123");
// Result: New tab opened successfully ✅
```

## Lessons Learned

1. **Always store the original function** before intercepting global APIs
2. **Use module-level variables** for shared state across functions
3. **Test interception code** thoroughly to catch recursion issues
4. **Use `.call()` when invoking stored functions** to preserve context

## Related Files

- `tab-manager.js` (lines 19, 97, 116) - Fixed infinite recursion
- `router.js` (lines 715-734) - TabManager integration
- `auto-print-buttons.js` - Depends on TabManager

## Impact

- **Before**: TabManager caused stack overflow on first use
- **After**: TabManager works correctly, reuses tabs as designed

---

**Fixed**: 2025-10-29
**Bug Severity**: Critical (prevented all tab management)
**Lines Changed**: 3
**Files Modified**: 1 (`tab-manager.js`)
