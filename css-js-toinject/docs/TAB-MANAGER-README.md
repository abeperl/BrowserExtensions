# Tab Manager - Smart Tab Reuse System

## Overview

The Tab Manager automatically intercepts `window.open()` calls and converts popup windows into **reusable browser tabs**. Each unique URL (ignoring query parameters) gets its own persistent tab that can be reused for subsequent requests.

## Purpose

On pages like `#outbound/packing`, the application code frequently opens new popup windows with code like:

```javascript
window.open("#outbound/packingSlipdetail?id=" + shipmentId);
window.open("productimages/" + imageFile);
```

**Without Tab Manager**: Each call creates a new popup or tab, leading to tab clutter.

**With Tab Manager**: Tabs are intelligently reused based on the base URL, keeping your browser organized.

## How It Works

### URL Normalization

The Tab Manager normalizes URLs by removing query parameters and hash fragments to determine tab identity:

```javascript
// These all map to the same tab:
"#outbound/packingSlipdetail?id=123"  → "#outbound/packingSlipdetail"
"#outbound/packingSlipdetail?id=456"  → "#outbound/packingSlipdetail"
"#outbound/packingSlipdetail?id=789"  → "#outbound/packingSlipdetail"
```

### Example Behavior

```javascript
// First call - creates NEW tab for packing slip
window.open("#outbound/packingSlipdetail?id=123");

// Second call - REUSES existing packing slip tab, updates to id=456
window.open("#outbound/packingSlipdetail?id=456");

// Different base URL - creates NEW tab for carton label
window.open("#outbound/cartonLabeldetail?id=123");
```

## Features

✅ **Smart Tab Reuse**: Same base URL → reuse tab
✅ **Parameter Independence**: Query params ignored for tab identity
✅ **No Waiting Required**: Multiple tabs can open in parallel
✅ **Automatic Cleanup**: Closed tabs removed from registry
✅ **Debug Mode**: Detailed logging of all tab operations
✅ **Global API**: Full programmatic control

## Integration

### Automatic Installation

Tab Manager is automatically installed on the `#outbound/packing` route by the router:

```javascript
// In router.js
if (typeof TabManager !== 'undefined') {
    TabManager.install();
    console.log('✅ Tab Manager installed');
}
```

### Manual Installation

To install on other routes:

```javascript
// Install the interceptor
TabManager.install();

// Now all window.open() calls use smart tabs
window.open("#outbound/packingSlip?id=123");  // New tab
window.open("#outbound/packingSlip?id=456");  // Reuses tab
```

## Usage with Auto Print Buttons

The Tab Manager works seamlessly with the auto-print-buttons feature:

```javascript
// Both tabs open in parallel (no waiting needed)
window.open("#outbound/packingSlipdetail?id=123");
window.open("#outbound/cartonLabeldetail?id=123");

// Each gets its own reusable tab
// Future calls to same URLs will reuse these tabs
```

### Print All Button Behavior

When clicking "Print All":

1. **Carton Label button** → Opens/reuses carton label tab
2. **Packing Slip button** → Opens/reuses packing slip tab
3. **No waiting** between clicks (parallel opening)
4. **Same tabs** reused for future shipments

## API Reference

### Core Methods

```javascript
// Install the interceptor (auto-installs on load)
TabManager.install();

// View current tab status
TabManager.printStatus();
// Output:
// 📊 Tab Manager Status:
//    Total tabs: 2
//    Open tabs: 2
//    Closed tabs: 0
//    Tabs:
//      - #outbound/packingSlipdetail (OPEN)
//      - #outbound/cartonLabeldetail (OPEN)

// Get status object
const status = TabManager.getStatus();
// Returns:
// {
//   totalTabs: 2,
//   openTabs: 2,
//   closedTabs: 0,
//   tabs: [
//     { url: "#outbound/packingSlipdetail", isOpen: true, reference: Window },
//     { url: "#outbound/cartonLabeldetail", isOpen: true, reference: Window }
//   ]
// }
```

### Control Methods

```javascript
// Close all managed tabs
TabManager.closeAllTabs();
// Output: 🗑️ Closed 2 tab(s)

// Clear registry without closing tabs
TabManager.clearRegistry();
// Output: 🗑️ Cleared 2 tab reference(s) from registry

// Temporarily disable (pass-through mode)
TabManager.disable();
// Output: ⚠️ Tab Manager disabled

// Re-enable
TabManager.enable();
// Output: ✅ Tab Manager enabled

// Toggle debug mode
TabManager.setDebug(true);   // Enable detailed logging
TabManager.setDebug(false);  // Disable logging
```

### Configuration

```javascript
// Access configuration object
TabManager.config
// {
//   enabled: true,
//   debugMode: true,
//   reuseDelay: 100
// }

// Modify configuration
TabManager.config.debugMode = false;
TabManager.config.reuseDelay = 200;
```

## Debug Console Output

With debug mode enabled, you'll see detailed logs:

```
🪟 Tab Manager intercepting window.open:
   URL: #outbound/packingSlipdetail?id=123
   Target: undefined
   Features: undefined
   Normalized URL: #outbound/packingSlipdetail
🆕 Creating new tab: #outbound/packingSlipdetail
✅ Tab opened/reused
   Total tabs managed: 1

🪟 Tab Manager intercepting window.open:
   URL: #outbound/packingSlipdetail?id=456
   Target: undefined
   Features: undefined
   Normalized URL: #outbound/packingSlipdetail
♻️ Reusing existing tab: #outbound/packingSlipdetail
✅ Tab opened/reused
   Total tabs managed: 1
```

## Technical Details

### Tab Registry

The Tab Manager maintains a `Map` of normalized URLs to window references:

```javascript
Map {
  "#outbound/packingSlipdetail" => Window,
  "#outbound/cartonLabeldetail" => Window,
  "productimages/label.pdf" => Window
}
```

### Tab Validation

Before reusing a tab, the manager checks:

1. **Tab exists**: Reference is not null
2. **Tab is open**: `!tab.closed`
3. **Tab is accessible**: Can navigate to new URL

If any check fails, a new tab is created.

### Navigation Strategy

When reusing a tab:

```javascript
// Attempt to navigate existing tab
existingTab.location.href = newUrl;
existingTab.focus();

// If cross-origin error, create new tab
```

## Common Use Cases

### Use Case 1: Packing Workflow

```javascript
// User processes multiple shipments
// Each shipment reuses the same packing slip tab

window.open("#outbound/packingSlip?id=100");  // Tab 1 created
window.open("#outbound/packingSlip?id=101");  // Tab 1 reused
window.open("#outbound/packingSlip?id=102");  // Tab 1 reused

// Result: Only 1 tab for all packing slips
```

### Use Case 2: Mixed Documents

```javascript
// User opens different document types
// Each type gets its own reusable tab

window.open("#outbound/packingSlip?id=100");   // Packing slip tab
window.open("#outbound/cartonLabel?id=100");   // Carton label tab
window.open("#outbound/packingSlip?id=101");   // Reuses packing slip tab
window.open("#outbound/cartonLabel?id=101");   // Reuses carton label tab

// Result: 2 tabs total (1 per document type)
```

### Use Case 3: Parallel Printing

```javascript
// Auto Print Buttons clicks multiple buttons at once
// No waiting needed - tabs open in parallel

async function printAll() {
    // Both tabs open immediately
    window.open("#outbound/packingSlip?id=123");
    window.open("#outbound/cartonLabel?id=123");

    // No await needed - TabManager handles both instantly
}
```

## Troubleshooting

### Tab Not Reusing

**Problem**: New tab created instead of reusing existing

**Solution**: Check if tabs have identical base URLs:
```javascript
TabManager.printStatus();  // View all normalized URLs
```

### Tab Manager Not Working

**Problem**: window.open() still creates popups

**Solution 1**: Check if TabManager is loaded:
```javascript
console.log(typeof TabManager);  // Should be 'object'
```

**Solution 2**: Check if interceptor is installed:
```javascript
console.log(window.open._tabManagerInstalled);  // Should be true
```

**Solution 3**: Reinstall manually:
```javascript
TabManager.install();
```

### Cross-Origin Errors

**Problem**: Console shows "Cannot navigate existing tab"

**Explanation**: Browser security prevents navigation across different origins

**Behavior**: TabManager automatically creates a new tab instead

## Browser Compatibility

✅ Chrome/Edge (Chromium)
✅ Firefox
✅ Safari
✅ Opera

**Requirements**:
- ES6+ support (Map, arrow functions)
- window.open() API
- Tab navigation support

## Best Practices

### 1. Let TabManager Handle Everything

```javascript
// ❌ Don't manually track tabs
let myTab = null;
if (!myTab || myTab.closed) {
    myTab = window.open(url);
}

// ✅ Just call window.open()
window.open(url);  // TabManager handles reuse
```

### 2. Use Base URLs for Tab Identity

```javascript
// ✅ Good - same base URL reuses tab
window.open("#outbound/packingSlip?id=123");
window.open("#outbound/packingSlip?id=456");

// ❌ Bad - different base URLs create multiple tabs
window.open("#outbound/packingSlip123");
window.open("#outbound/packingSlip456");
```

### 3. No Waiting Between Opens

```javascript
// ✅ Good - parallel opening
window.open(url1);
window.open(url2);
window.open(url3);

// ❌ Unnecessary - don't wait
window.open(url1);
await new Promise(resolve => setTimeout(resolve, 1000));
window.open(url2);  // No need to wait!
```

## Integration with Existing Code

### Replacing Manual Tab Management

**Before** (manual tracking):
```javascript
let packingSlipWindow = null;

function openPackingSlip(id) {
    if (!packingSlipWindow || packingSlipWindow.closed) {
        packingSlipWindow = window.open("#outbound/packingSlip?id=" + id);
    } else {
        packingSlipWindow.location.href = "#outbound/packingSlip?id=" + id;
        packingSlipWindow.focus();
    }
}
```

**After** (with TabManager):
```javascript
function openPackingSlip(id) {
    window.open("#outbound/packingSlip?id=" + id);
    // TabManager handles everything automatically!
}
```

## Performance

- **Minimal overhead**: URL normalization is fast (regex-based)
- **No polling**: Event-driven tab management
- **Memory efficient**: Only stores window references
- **Auto cleanup**: Closed tabs automatically removed

## Security

- **Same-origin policy**: Respects browser security
- **No data exposure**: Only manages window references
- **No credentials**: Doesn't access tab content
- **Cross-origin safe**: Falls back to new tab if needed

## Related Features

- **Auto Print Buttons**: Seamlessly works with parallel printing
- **Overlay Manager**: Provides feedback when tabs open
- **Router**: Auto-enables on `#outbound/packing` route

## Quick Reference

```javascript
// Installation
TabManager.install()

// Status
TabManager.printStatus()

// Control
TabManager.enable()
TabManager.disable()
TabManager.closeAllTabs()
TabManager.clearRegistry()

// Configuration
TabManager.setDebug(true)
TabManager.config

// Access
window.TabManager
```

## Support

For issues or questions:
1. Check console for debug output: `TabManager.setDebug(true)`
2. View status: `TabManager.printStatus()`
3. Check integration in router.js

---

**Last Updated**: 2025-10-29
**Version**: 1.0.0
**Author**: Browser Extensions Team
