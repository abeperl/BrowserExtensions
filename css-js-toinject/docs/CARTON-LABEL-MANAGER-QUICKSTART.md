# Carton Label Manager - Quick Start

## What It Does

**One file, three features:**
1. 🎨 **CSS Styling** - Formats carton labels (hides Box No, To:, etc.)
2. 🔀 **URL Redirect** - Redirects carton labels to `https://server:5555/print` (NOT other prints)
3. 🖱️ **Auto-Print** - Auto-clicks View Invoice when shipment is created

## Installation

```html
<!-- Include in your HTML -->
<script src="carton-label-manager.js"></script>
```

## Quick Test

```javascript
// Check if loaded
window.cartonLabelManager.stats();

// Expected output:
// 📊 CARTON LABEL MANAGER STATS
//    Auto-click: ENABLED
//    Total redirects: 0
//    Redirect from: localhost:8080/printinvoice
//    Redirect to: https://server:5555/print
```

## Common Commands

```javascript
// View stats
window.cartonLabelManager.stats();

// Disable auto-click
window.cartonLabelManager.setAutoClick(false);

// Enable auto-click
window.cartonLabelManager.setAutoClick(true);

// Count redirects
window.cartonLabelManager.redirectCount();

// Manual trigger
window.cartonLabelManager.handleModal();
```

## Configuration

```javascript
// Change redirect URL
window.cartonLabelManager.config.newUrl = 'https://192.168.1.254:5555/print';

// Change auto-click delay
window.cartonLabelManager.config.autoClickDelay = 1000; // 1 second

// Disable debug logs
window.cartonLabelManager.config.debugMode = false;
```

## What Gets Redirected

| Button | Redirected? | Why? |
|--------|-------------|------|
| Print Carton Label | ✅ YES | Contains `box-label-wrp` class |
| View Invoice | ❌ NO | Different HTML structure |
| Packing Slip | ❌ NO | Different HTML structure |
| Print Label | ❌ NO | Different HTML structure |

## Expected Console Output

### When Creating Shipment:
```
📌 Captured shipment ID: 12345
🎯 Modal appearance detected
✅ Success modal confirmed
🖱️ Auto-clicking View Invoice button...
✅ View Invoice button clicked
```

### When Clicking "Print Carton Label":
```
🖱️ CARTON LABEL BUTTON CLICKED
🔍 Carton label content detected
═══════════════════════════════════════════
📦 CARTON LABEL DETECTED - REDIRECTING
   Redirect #1
   From: localhost:8080/printinvoice
   To: https://server:5555/print
═══════════════════════════════════════════
🪟 window.open intercepted
✅ CARTON LABEL CSS INJECTED!
```

## Troubleshooting

### CSS Not Applied
```javascript
// Check window.open is intercepted (should show logs)
// Look for: "🪟 window.open intercepted"
```

### Not Redirecting
```javascript
// Check redirect count
window.cartonLabelManager.redirectCount(); // Should increment

// Check console for detection
// Look for: "📦 CARTON LABEL DETECTED"
```

### Auto-Click Not Working
```javascript
// Check if enabled
window.cartonLabelManager.config.autoClickEnabled; // Should be true

// Enable it
window.cartonLabelManager.setAutoClick(true);
```

## Files Replaced

This ONE file replaces:
- ❌ box-label-format-injector.js
- ❌ carton-label-redirect.js
- ❌ simple-auto-print.js
- ❌ print-url-redirect-global.js
- ❌ global-box-label-monitor.js

## Router Integration

The router automatically calls `window.cartonLabelManager.handleModal()` when shipment modal appears.

**No additional setup needed!**

## Quick Reference

```javascript
// API
window.cartonLabelManager = {
    config: { autoClickEnabled, oldUrl, newUrl, debugMode },
    handleModal(),           // Trigger modal handler
    clickViewInvoice(),      // Click View Invoice button
    setAutoClick(boolean),   // Enable/disable auto-click
    stats(),                 // Show statistics
    redirectCount(),         // Get redirect count
    lastRedirect()           // Get last redirect time
}
```

## What's Different from Old Files?

| Old | New |
|-----|-----|
| `window.simpleAutoPrint` | `window.cartonLabelManager` |
| Multiple files to include | One file |
| Confusing which does what | Clear, organized features |

## Migration

**Change this:**
```javascript
window.simpleAutoPrint.handleModal();
```

**To this:**
```javascript
window.cartonLabelManager.handleModal();
```

Done! ✅