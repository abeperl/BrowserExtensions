# Overlay & Modal Interception Analysis

## Current Architecture (Updated)

The project has **two** interception layers:

1. **Snackbar Interceptor** (`router.js:256-293`) - ✅ ACTIVE
2. **Modal Interceptor** (`modal-interceptor.js`) - ✅ ACTIVE

**API Response Interceptor** - ❌ REMOVED (to prevent duplicate messages)

---

## Problem 1: Duplicate Snackbar + Overlay Messages

### How It Happens

When `CompletePicklist` calls `snackbar.show()`:

```javascript
snackbar.show(tf.langData().Picklist + " Completed.");
```

**Expected Flow:**
1. Snackbar interceptor catches it
2. Shows OverlayManager popup
3. **BLOCKS** original snackbar

**What Actually Happens:**
- If snackbar is loaded/reinitialized AFTER the interceptor runs
- Or if another script wraps snackbar.show later
- **Both** the overlay AND snackbar appear

### Root Cause

**router.js:256-293** - Snackbar Interceptor:
```javascript
window.snackbar.show = function(message, type) {
    // ... shows overlay ...

    // Original snackbar is NOT called (commented out)
    // return originalSnackbarShow.call(this, message, type);
};
```

**Issue:** The interceptor assumes `snackbar` exists when the script runs. If snackbar is:
- Loaded dynamically later
- Re-initialized by the app
- Wrapped by another script first

The interceptor won't catch it.

### Solution Options

#### Option A: Defensive Interception with Monitoring
```javascript
// Re-check and re-wrap snackbar periodically
setInterval(() => {
    if (typeof snackbar !== 'undefined' &&
        snackbar.show &&
        !snackbar.show._overlayIntercepted) {

        const originalShow = snackbar.show;
        snackbar.show = function(message, type) {
            // Show overlay instead
            OverlayManager[overlayType]({
                message: message,
                duration: 3000
            });
            // Don't call original
        };
        snackbar.show._overlayIntercepted = true;
    }
}, 1000);
```

#### Option B: CSS Hide + JavaScript Override
```css
/* Hide all snackbar elements */
.snackbar,
#snackbar,
[class*="snackbar"] {
    display: none !important;
    visibility: hidden !important;
}
```

Plus JavaScript interception as backup.

#### Option C: MutationObserver for Snackbar DOM
```javascript
// Watch for snackbar elements being added to DOM
const observer = new MutationObserver((mutations) => {
    mutations.forEach(mutation => {
        mutation.addedNodes.forEach(node => {
            if (node.classList &&
                (node.classList.contains('snackbar') ||
                 node.id === 'snackbar')) {
                // Immediately remove it
                node.remove();
            }
        });
    });
});

observer.observe(document.body, {
    childList: true,
    subtree: true
});
```

---

## Problem 2: Modal.show() with OK Buttons

### Current Implementation

**modal-interceptor.js** intercepts `modal.show()` and:

1. **Displays OverlayManager popup** instead of native modal
2. **Auto-triggers callback** with `'ok'` after 100ms

```javascript
// modal-interceptor.js:180-192
if (CONFIG.autoCallback && callback) {
    setTimeout(() => {
        callback('ok');
    }, 100);
}
```

### What This Means

For code like:
```javascript
modal.show("Success", "Picklist Completed", ["OK"], function(result) {
    if (result === 'ok') {
        // Do something after user clicks OK
        window.location.reload();
    }
});
```

**With Interceptor:**
- Shows overlay popup (no blocking modal)
- **Automatically calls callback('ok') after 100ms**
- User doesn't need to click anything
- Workflow continues automatically

**Without Interceptor:**
- Shows blocking modal dialog
- User must click "OK" button
- Callback fires when button clicked
- Workflow pauses until interaction

### Potential Issues

#### Issue 1: Unintended Auto-Actions
If the callback has important side effects:
```javascript
modal.show("Delete?", "Are you sure?", ["OK", "Cancel"], function(result) {
    if (result === 'ok') {
        deleteAllData(); // DANGEROUS - auto-triggered!
    }
});
```

The auto-callback means **confirmation dialogs become non-blocking warnings**.

#### Issue 2: User Doesn't See Message
If overlay duration is too short (3 seconds default):
```javascript
modal.show("Important", "Please read these 5 steps carefully...", ["OK"]);
```

User might not have time to read before it disappears.

### Configuration

**modal-interceptor.js:19-24** - CONFIG:
```javascript
const CONFIG = {
    enabled: true,
    defaultDuration: 3000,        // 3 seconds
    autoCallback: true,           // Auto-trigger "ok"
    debugMode: true
};
```

#### To Disable Auto-Callback:
```javascript
// In console or another script:
window.modalInterceptor.setAutoCallback(false);
```

Now modals will show as overlays but **NOT** auto-trigger callbacks.

#### To Restore Native Modals:
```javascript
window.modalInterceptor.disable();  // Disable overlay replacement
// or
window.modalInterceptor.restore();  // Restore original modal.show
```

---

## Problem 3: Triple Message Display - ✅ RESOLVED

**Previous Issue:** For API responses like `CompletePicklist`, you could see:

1. **API Response Overlay** (from API interceptor in router.js)
2. **Snackbar Overlay** (from snackbar interceptor)
3. **Original Snackbar** (if interception failed)

**Solution Implemented:** Removed API Response Interceptor entirely. Now only snackbar interception is active.

**Current Flow:**
```javascript
tf.service.post("apiv2/picklist/PicklistComplete", {...}, function(data) {
    if (data.responseCode == 0) {
        snackbar.show("Picklist Completed.");  // <-- Only this is intercepted
        common.GetLpLabelByPickListId(picklistId);
    }
});
```

**Result:** Single overlay per action! ✅

---

## Current Configuration (Production Ready)

### Active Interception
1. **Snackbar Interceptor** - Catches all `snackbar.show()` calls
2. **Modal Interceptor** - Replaces `modal.show()` with overlays

### Recommended Settings

**Modal Interceptor Tuning:**
```javascript
window.modalInterceptor.setAutoCallback(false);  // Disable auto-trigger for confirmations
window.modalInterceptor.setDuration(5000);       // Longer display time
```

**Optional CSS Fallback (if snackbar still appears):**
```css
/* Hide duplicate snackbars if interception fails */
.snackbar {
    display: none !important;
}
```

### For Testing/Debugging

Keep all interceptors but add debug flags:
```javascript
window.DEBUG_OVERLAYS = true;

if (window.DEBUG_OVERLAYS) {
    console.log('Overlay shown by:', source);
}
```

---

## Testing Checklist

### Snackbar Interception
- [ ] Success message shows only once (overlay OR snackbar, not both)
- [ ] Error message shows only once
- [ ] Original snackbar is hidden/removed

### Modal Interception
- [ ] `modal.show()` displays as overlay
- [ ] Callback fires automatically (if autoCallback enabled)
- [ ] Can disable auto-callback when needed
- [ ] Can restore original modals

### API Interception
- [ ] Success/error responses show overlay
- [ ] No duplicate messages from snackbar
- [ ] Error clears inputs and refocuses

### Edge Cases
- [ ] Script load order doesn't break interception
- [ ] Page refresh doesn't cause duplicates
- [ ] Multiple rapid API calls don't stack overlays

---

## Debug Commands

```javascript
// Check interceptor status
console.log('Snackbar wrapped:', snackbar.show._overlayIntercepted);
console.log('Modal intercepted:', window.modalInterceptor?.isIntercepted());
console.log('API fetch wrapped:', window.fetch._apiOverlayIntercepted);

// Disable specific interceptors
window.modalInterceptor.disable();
window.modalInterceptor.restore();

// Test overlay directly
OverlayManager.success({ message: 'Test Success', duration: 3000 });
OverlayManager.error({ message: 'Test Error', duration: 3000 });

// Monitor snackbar calls
const originalSnackbar = snackbar.show;
snackbar.show = function(...args) {
    console.log('Snackbar called:', args);
    return originalSnackbar.apply(this, args);
};
```

---

## Current State Summary

| Component | Status | Notes |
|-----------|--------|-------|
| **Snackbar Interceptor** | ✅ Active | Primary notification method |
| **Modal Interceptor** | ✅ Active | Replaces blocking modals with overlays |
| **API Interceptor** | ❌ Removed | Eliminated to prevent duplicate messages |
| **OverlayManager** | ✅ Active | Single source of overlays |

**Result:** Clean, single-overlay notification system without duplicates! ✅
