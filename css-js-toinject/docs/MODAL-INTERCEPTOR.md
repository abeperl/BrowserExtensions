# Modal Interceptor Documentation

## Overview

The Modal Interceptor replaces native blocking modal dialogs with the cleaner, non-blocking OverlayManager display system.

**Route:** `#outbound/ProcessPersonalizedOrderItems`

## Features

- **Automatic Detection**: Intercepts all `modal.show()` calls
- **Smart Type Detection**: Analyzes title and content to determine overlay type (success/error/warning/info)
- **Non-Blocking**: Uses OverlayManager for better UX (no blocking modal overlay)
- **Auto-Callback**: Optionally triggers callbacks automatically
- **Fallback Support**: Falls back to original modal if OverlayManager unavailable

## How It Works

### 1. Modal Interception

The interceptor wraps the native `modal.show()` function:

```javascript
// Original modal call
modal.show("Success", "Operation completed successfully", buttons, callback);

// Intercepted and converted to
OverlayManager.success({
    title: "Success",
    message: "Operation completed successfully",
    duration: 3000
});
```

### 2. Type Detection

The interceptor automatically determines the overlay type based on keywords:

| Keywords in Title/Content | Overlay Type |
|---------------------------|--------------|
| error, failed, invalid, not found | Error (red) |
| warning, caution, are you sure | Warning (yellow) |
| success, completed, saved | Success (green) |
| (default) | Info (blue) |

### 3. Content Extraction

Handles various input types:
- **HTML strings**: Strips tags, extracts text
- **jQuery objects**: Extracts text content
- **DOM elements**: Extracts text content
- **Existing modal objects**: Finds `.modal-box-title` and extracts

## Configuration

### Enable/Disable

```javascript
// Disable interception (use original modals)
window.modalInterceptor.disable();

// Re-enable interception
window.modalInterceptor.enable();
```

### Auto-Callback

Controls whether callbacks are triggered automatically:

```javascript
// Enable auto-callback (default: true)
window.modalInterceptor.setAutoCallback(true);

// Disable auto-callback
window.modalInterceptor.setAutoCallback(false);
```

### Display Duration

Change how long overlays are displayed:

```javascript
// Set duration to 5 seconds
window.modalInterceptor.setDuration(5000);

// Default is 3000ms (3 seconds)
```

### Debug Mode

```javascript
// Enable debug logging
window.modalInterceptor.config.debugMode = true;

// Disable debug logging
window.modalInterceptor.config.debugMode = false;
```

## Restore Original Behavior

To completely restore the original modal system:

```javascript
window.modalInterceptor.restore();
```

This removes the interceptor and restores `modal.show()` to its original implementation.

## API Reference

### Configuration Object

```javascript
window.modalInterceptor.config = {
    enabled: true,              // Enable/disable interception
    defaultDuration: 3000,      // Default overlay duration (ms)
    autoCallback: true,         // Auto-trigger callbacks
    debugMode: true            // Enable debug logging
}
```

### Methods

#### `enable()`
Enables modal interception.

#### `disable()`
Disables modal interception (uses original modal.show).

#### `restore()`
Completely restores original modal.show function.

#### `isIntercepted()`
Returns `true` if modal is currently intercepted.

#### `setDuration(ms)`
Sets default overlay duration in milliseconds.

#### `setAutoCallback(enabled)`
Enables or disables automatic callback triggering.

## Testing

### Manual Test

```javascript
// Test with success message
modal.show("Success", "This is a success message");

// Test with error message
modal.show("Error", "This is an error message");

// Test with warning
modal.show("Warning", "Please review this carefully");

// Test with custom content
modal.show("Info", "Just some information");
```

### Debug Output

When debug mode is enabled, you'll see:

```
🔔 Modal intercepted: { title: "Success", content: "...", buttons: [...] }
📊 Modal details: { type: "success", title: "Success", message: "..." }
✅ Displayed as success overlay
🔄 Auto-triggering callback with "ok"
```

## Integration Notes

### Load Order

The modal interceptor should be loaded:
1. **After** OverlayManager (dependency)
2. **After** the page's modal object is defined
3. **Before** any code that calls `modal.show()`

### Browser Extension Integration

For browser extension injection, add to your content script manifest:

```json
{
  "content_scripts": [{
    "matches": ["https://mj.3plnext.com/*"],
    "js": [
      "ui-feedback.js",          // Load first (provides OverlayManager)
      "modal-interceptor.js",    // Load second
      "router.js"                // Load last
    ]
  }]
}
```

## Troubleshooting

### Modal not being intercepted

**Check 1**: Is OverlayManager loaded?
```javascript
typeof OverlayManager !== 'undefined'  // Should be true
```

**Check 2**: Is the interceptor loaded?
```javascript
typeof window.modalInterceptor !== 'undefined'  // Should be true
```

**Check 3**: Is interception active?
```javascript
window.modalInterceptor.isIntercepted()  // Should be true
```

**Check 4**: Is it enabled?
```javascript
window.modalInterceptor.config.enabled  // Should be true
```

### Callbacks not firing

If callbacks aren't being triggered:
```javascript
// Check auto-callback setting
window.modalInterceptor.config.autoCallback  // Should be true

// Or manually trigger
window.modalInterceptor.setAutoCallback(true);
```

### Wrong overlay type

If overlays are showing the wrong type:
```javascript
// Check debug output
window.modalInterceptor.config.debugMode = true;

// Manually test type detection
modal.show("Error Test", "This should be red");
```

## Comparison: Before vs After

### Before (Native Modal)

```javascript
modal.show(
    "Success",
    "Your order has been processed",
    [
        { label: "OK", value: "ok" }
    ],
    (result) => {
        console.log("User clicked:", result);
    }
);
```

**Result**: Blocking modal dialog, requires user to click OK button.

### After (OverlayManager)

Same code automatically becomes:

```javascript
OverlayManager.success({
    title: "Success",
    message: "Your order has been processed",
    duration: 3000
});
// Callback automatically triggered with "ok"
```

**Result**: Non-blocking overlay, auto-dismisses after 3 seconds, callback triggered automatically.

## Benefits

1. **Non-Blocking**: Users can continue working while seeing feedback
2. **Auto-Dismiss**: No need to click OK for every message
3. **Consistent UX**: All feedback uses the same overlay system
4. **Better Accessibility**: Overlays support ARIA live regions
5. **Faster Workflow**: Eliminates click fatigue from constant OK button clicks
6. **Visual Consistency**: Matches other overlay feedback (snackbar, API responses)

## Future Enhancements

Potential improvements:
- Support for custom buttons (Yes/No/Cancel)
- Configurable type mapping
- Per-modal duration override
- Animation customization
- Sound feedback integration
