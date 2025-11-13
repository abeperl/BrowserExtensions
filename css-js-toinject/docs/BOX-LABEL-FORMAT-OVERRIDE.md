# Box Label Format Override

## Overview

This feature intercepts dynamically generated box label print windows and applies custom CSS formatting to modify the layout before printing.

## Problem

The site's `common.PrintCartonLabelPrint()` function generates HTML content and opens it in a new window for printing. The default format had several issues:
- Phone numbers appeared on separate lines
- "Box No : 1" line was visible
- "To:" label was shown
- Extra padding in the top info section

## Solution

The router intercepts `window.open()` calls on the `#Outbound/shipmentdetails` page and injects custom CSS into print windows that contain `.box-label-wrp` elements.

## Files

### 1. `box-label-format.css`
Standalone CSS file with the formatting rules (can be used independently).

### 2. `box-label-format-injector.js`
Standalone JavaScript module that intercepts `window.open()` and injects CSS (can be used independently).

### 3. `router.js` (modified)
The main router now includes the box label format injector in the "Shipment Details Route" section.

## Changes Applied

### 1. Hide "Box No : 1" Line
```css
.top-info-section .text:first-child {
    display: none !important;
}
```

### 2. Hide "To:" Label
```css
.ship-info-section > .text:nth-child(1) {
    display: none !important;
}
```

### 3. Put Phone Numbers on Same Line
```css
.top-info-section .text[style*="font-size: 30px"] {
    display: inline-block !important;
    margin-right: 20px !important;
}
```

### 4. Remove Extra Padding
```css
.top-info-section {
    padding-top: 10px !important;
    padding-bottom: 20px !important;
    margin-bottom: 20px !important;
}
```

## How It Works

1. **Route Detection**: When user navigates to `#Outbound/shipmentdetails`, the router activates
2. **Interceptor Installation**: `window.open()` is wrapped with a custom function
3. **Window Monitoring**: Each opened window is checked for `.box-label-wrp` elements
4. **CSS Injection**: If detected, custom CSS is injected into the `<head>` with ID `box-label-format-override`
5. **Timing**: CSS injection happens 1.2 seconds after window open to ensure content is written

## Usage

### Automatic (via Router)
1. Load the router on the page: `router.js`
2. Navigate to `#Outbound/shipmentdetails`
3. Click the print button
4. Formatting is applied automatically

### Manual (Standalone)
```html
<!-- Include the injector script -->
<script src="box-label-format-injector.js"></script>
```

## Testing

1. Navigate to shipment details page
2. Open browser console
3. Look for these log messages:
   ```
   📋 Installing box label format injector...
   ✅ Box label format injector installed
   ```
4. Click the carton label print button
5. Check for:
   ```
   🪟 window.open intercepted for box label formatting
   ✅ New window opened, scheduling CSS injection
   🎯 Detected box label print window, injecting CSS
   ✅ Injected box label format CSS
   ```

## Debugging

### Check if Interceptor is Installed
```javascript
console.log(window.open._boxLabelFormatInstalled); // Should be true
```

### Inspect Print Window CSS
In the print window console:
```javascript
document.getElementById('box-label-format-override');
```

### View Original HTML Structure
The site's print function creates HTML like:
```html
<div class="box-label-wrp">
  <div class="top-info-section">
    <div class="text">Box No : 1</div>  <!-- Hidden -->
    <div class="text">Malchut Fine Judaica</div>
    <div class="text">3902 14 Ave</div>
    <div class="text" style="font-size: 30px">Brooklyn, NY 11218</div>
    <div class="text" style="font-size: 30px">(718) 854-7700</div>  <!-- Inline -->
    <div class="text" style="font-size: 30px">(718) 853-2722</div>  <!-- Inline -->
  </div>
  <div class="ship-info-section">
    <div class="text">To:</div>  <!-- Hidden -->
    ...
  </div>
</div>
```

## Compatibility

- Works with existing placard text enhancer feature
- Does not interfere with other window.open calls
- Only applies to windows containing `.box-label-wrp` elements
- Safe to run multiple times (checks for existing injection)

## TabManager Conflict Issue

### Problem
When TabManager is also intercepting `window.open()`, the interception chain can cause issues:
1. Box label formatter intercepts first
2. TabManager intercepts second
3. The window reference may be lost or timing becomes problematic

**Solution: Use `global-box-label-monitor.js`**

### `global-box-label-monitor.js`
A standalone, aggressive monitor that:
- Installs at the earliest possible point (before any routes)
- Tries injection at multiple intervals (100ms, 300ms, 500ms, 800ms, 1200ms, 1500ms, 2000ms, 2500ms, 3000ms)
- Monitors TabManager tabs if available
- Uses WeakSet to track processed windows
- Listens for DOMContentLoaded and readystatechange events

### Load Order (Recommended)
```html
<script src="global-box-label-monitor.js"></script>  <!-- Load FIRST -->
<script src="tab-manager.js"></script>
<script src="router.js"></script>
```

This ensures the monitor intercepts `window.open` before TabManager or any routes.

## Notes

- The multiple delays are required because the site writes content to the window using `document.write()` after opening
- The interceptor is installed once per page load and persists across multiple print operations
- CSS uses `!important` to override inline styles from the site's JavaScript
- Using `global-box-label-monitor.js` is the most reliable approach when TabManager is active
