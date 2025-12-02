# Box Label Format Fix - Quick Start

## Problem
The print button opens a window with box labels that need formatting adjustments, but TabManager is intercepting `window.open()` and interfering with CSS injection.

## Solution
Use `global-box-label-monitor.js` - a persistent monitor that aggressively tries to inject CSS into all opened windows.

## Quick Fix

### Option 1: Use Global Monitor (Recommended)
Load `global-box-label-monitor.js` **before** other scripts:

```html
<script src="global-box-label-monitor.js"></script>
<script src="tab-manager.js"></script>
<script src="router.js"></script>
```

This ensures it intercepts `window.open` first and tries injection multiple times.

### Option 2: Test the CSS First
Open `test-box-label-format.html` in a browser to verify the CSS works correctly. You should see:
- ❌ "Box No : 1" hidden
- ❌ "To:" hidden  
- ✅ Phone numbers on same line
- ✅ Reduced padding

### Option 3: Manual Console Injection
If scripts aren't loading, manually inject in the print window console:

```javascript
const style = document.createElement('style');
style.id = 'box-label-format-override';
style.textContent = `
    .top-info-section .text:first-child { display: none !important; }
    .ship-info-section > .text:nth-child(1) { display: none !important; }
    .top-info-section .text[style*="font-size: 30px"] { display: inline-block !important; margin-right: 20px !important; }
    .top-info-section { padding-top: 10px !important; padding-bottom: 20px !important; margin-bottom: 20px !important; text-align: center !important; }
    .ship-info-section { padding: 0 20px !important; }
`;
document.head.appendChild(style);
```

## Debugging

**📖 Full debugging guide available at: [docs/BOX-LABEL-DEBUGGING.md](docs/BOX-LABEL-DEBUGGING.md)**

### Quick Checks

#### Check if Injector is Loaded
```javascript
// Check if window.open is wrapped
console.log(window.open.toString().includes('Box Label')); // Should be true
```

#### Check if CSS is Injected in Print Window
In the opened print window console:
```javascript
document.getElementById('box-label-format-override'); // Should exist
```

#### View TabManager Tabs
```javascript
window.TabManager.printStatus();
```

## Log Messages to Look For

### Success (box-label-format-injector.js):
```
🪟 window.open intercepted: ["", "", "height=700,width=1100"]
✅ New window opened, preparing early CSS injection
👀 MutationObserver installed on print window
🔍 Checking window: {readyState: "loading", hasBoxLabel: false, bodyChildren: 0}
👀 MutationObserver detected .box-label-wrp added to DOM
✅ BOX LABEL FORMAT CSS INJECTED! {boxLabelCount: 3, readyState: "complete", attemptNumber: "MutationObserver"}
```

### Issues:
- No "✅ BOX LABEL FORMAT CSS INJECTED!" - CSS injection failed, see debugging guide
- "readyState: loading, hasBoxLabel: false" - Document not ready yet (normal)
- No MutationObserver message - Injection succeeded via polling instead (also fine)

## Files

- `global-box-label-monitor.js` - Main solution (load first)
- `box-label-format.css` - Standalone CSS (for reference)
- `box-label-format-injector.js` - Alternative injector
- `test-box-label-format.html` - Test page to verify CSS
- `router.js` - Contains integrated version (may conflict with TabManager)

## Why Multiple Injection Attempts?

The site uses `document.write()` after opening the window, which means:
1. Window opens immediately (empty)
2. JavaScript writes HTML content (takes time)
3. CSS must be injected **after** content is written
4. Application calls `print()` at 1000ms

### box-label-format-injector.js Strategy

The injector uses multiple approaches:

1. **MutationObserver** - Watches for `.box-label-wrp` being added to DOM (most reliable)
2. **19 timed attempts** - Polls at 50ms, 100ms, 150ms... up to 950ms
3. **Event listeners** - DOMContentLoaded and readystatechange
4. **Print override** - Gates the print() call until CSS is present

This aggressive multi-pronged approach ensures CSS injection **before** the 1000ms print() call.
