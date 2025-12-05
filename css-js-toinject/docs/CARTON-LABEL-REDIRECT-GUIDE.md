# Carton Label Redirect - Targeted Solution

## Problem Statement

**Requirement:** Only redirect **carton label** prints to new server (`https://server:5555/print`)

**Must NOT redirect:**
- ✗ View Invoice (button: `data-value="pinvoice"`)
- ✗ Packing Slip (button: `data-value="pslip"`)
- ✗ Print Label (button: `data-value="plabel"`)
- ✗ Any other print operations

**Must redirect:**
- ✓ Print Carton Label (button: `data-value="pboxlabel"`)

## Solution: Targeted Detection

### File: [carton-label-redirect.js](carton-label-redirect.js)

This script uses **content-based detection** to identify carton label requests:

```javascript
function isCartonLabelRequest(data) {
    // Carton labels have unique HTML structure
    const hasBoxLabelClass = data.includes('box-label-wrp');

    // Only redirect if HTML contains this class
    return hasBoxLabelClass;
}
```

## How It Works

### 1. Button Click Detection
When you click any print button, the script logs what type:

```
🖱️ CARTON LABEL BUTTON CLICKED  ← Will be redirected
📄 View Invoice button clicked   ← Will NOT be redirected
📋 Packing Slip button clicked   ← Will NOT be redirected
```

### 2. Request Interception
When a POST request is made to `localhost:8080/printinvoice`:

```javascript
// Step 1: Intercept the request
$.post('localhost:8080/printinvoice', htmlContent, ...)

// Step 2: Check the HTML content
if (htmlContent.includes('box-label-wrp')) {
    // This is a CARTON LABEL → redirect
    url = 'https://server:5555/print';
} else {
    // This is something else → pass through unchanged
}
```

### 3. Selective Redirect
```
Request Type          | Contains box-label-wrp? | Action
--------------------- | ----------------------- | ------------------
Carton Label          | ✅ Yes                  | 🔀 REDIRECT
Invoice               | ❌ No                   | ✓ Pass through
Packing Slip          | ❌ No                   | ✓ Pass through
Other Labels          | ❌ No                   | ✓ Pass through
```

## Comparison: Global vs Targeted

### ❌ Global Redirect (You Rejected This)
```javascript
// BAD: Redirects EVERYTHING
if (url.includes('localhost:8080/printinvoice')) {
    url = 'https://server:5555/print';  // Breaks invoices, packing slips, etc!
}
```

### ✅ Targeted Redirect (New Solution)
```javascript
// GOOD: Only redirects carton labels
if (url.includes('localhost:8080/printinvoice') &&
    data.includes('box-label-wrp')) {  // Carton label specific!
    url = 'https://server:5555/print';
}
```

## Why This Is Safe

### Unique HTML Markers

**Carton Label HTML:**
```html
<div class="box-label-wrp">           ← Only carton labels have this
    <div class="top-info-section">
    <div class="ship-info-section">
    <div class="carton-count">
</div>
```

**Invoice HTML:**
```html
<div class="invoice-container">       ← Different structure
    <h1>Invoice #12345</h1>
</div>
```

**Packing Slip HTML:**
```html
<div class="packing-slip">            ← Different structure
    <table class="items">
</div>
```

## Server Name vs IP Address

You mentioned SSL issues with IP address. The script uses:

```javascript
const NEW_URL = 'https://server:5555/print';  // Server name (no SSL issues)
```

If you need to change it:
1. Edit line 13 in [carton-label-redirect.js](carton-label-redirect.js)
2. Change to your server name or IP

## Installation

### Option 1: Add to Your HTML
```html
<script src="carton-label-redirect.js"></script>
```

### Option 2: Browser Extension
Load via your browser extension's content script injection.

### Option 3: Browser Console (Testing)
Copy/paste the entire script into console to test immediately.

## Testing

### Step 1: Check Installation
```javascript
// Should show stats
window.cartonLabelRedirect.stats();
```

Expected output:
```
📊 CARTON LABEL REDIRECT STATS
   Status: ✅ ENABLED
   Total redirects: 0
   From: localhost:8080/printinvoice
   To: https://server:5555/print
```

### Step 2: Test Detection Logic
```javascript
window.cartonLabelRedirect.test();
```

Expected output:
```
🧪 Testing carton label detection...
1. Testing carton label HTML: ✅ DETECTED
2. Testing invoice HTML: ✅ CORRECTLY IGNORED
```

### Step 3: Click Buttons and Watch Console

**Click View Invoice:**
```
📄 View Invoice button clicked (will NOT be redirected)
```

**Click Carton Label:**
```
🖱️ CARTON LABEL BUTTON CLICKED
   Interceptor ready: YES

... (when request is made) ...

📦 CARTON LABEL DETECTED - REDIRECTING
   From: localhost:8080/printinvoice
   To: https://server:5555/print
   Contains: box-label-wrp class ✓
```

### Step 4: Verify in Network Tab
1. Open DevTools → Network
2. Click "Print Carton Label" button
3. Find POST request
4. Verify URL is `https://server:5555/print`

### Step 5: Try Other Buttons
Click Invoice, Packing Slip, etc. and verify they still use original URL.

## Troubleshooting

### Issue: All Prints Are Redirected
**Cause:** You're using global redirect without detection
**Fix:** Use [carton-label-redirect.js](carton-label-redirect.js) which has content detection

### Issue: Carton Labels NOT Redirected
**Cause:** Detection not finding `box-label-wrp` class
**Fix:** Check console for detection logs. The HTML might have changed.

### Issue: SSL Certificate Error
**Cause:** Server name doesn't match certificate
**Fix:**
1. Navigate to `https://server:5555/` in browser
2. Accept the certificate
3. Try printing again

Or change to IP if that works better:
```javascript
const NEW_URL = 'https://192.168.1.254:5555/print';
```

### Issue: No Console Output
**Cause:** Script not loaded
**Fix:**
1. Check if `window.cartonLabelRedirect` exists
2. Reload page
3. Check for JavaScript errors in console

## What Makes This Different

### vs simple-auto-print.js
- **simple-auto-print.js**: Installs/uninstalls interceptor (timing issues)
- **carton-label-redirect.js**: Always active, no timing issues

### vs print-url-redirect-global.js
- **global**: Redirects ALL prints to new server
- **carton-label**: Only redirects carton labels (content detection)

### vs Modifying simple-auto-print.js
- **Modifying**: Risk breaking other features
- **New file**: Standalone, no conflicts

## Summary

| Requirement | Solution |
|-------------|----------|
| Only redirect carton labels | ✅ Content detection (`box-label-wrp`) |
| Don't break other prints | ✅ Passes through non-carton requests |
| Use server name (SSL) | ✅ `https://server:5555/print` |
| Easy to test | ✅ Debug API included |
| No timing issues | ✅ Always-on interceptor |

## Quick Reference

```javascript
// Check status
window.cartonLabelRedirect.stats();

// Test detection
window.cartonLabelRedirect.test();

// See last redirect time
window.cartonLabelRedirect.lastRedirect();

// Get redirect count
window.cartonLabelRedirect.redirectCount();
```

## Next Steps

1. ✅ Load [carton-label-redirect.js](carton-label-redirect.js)
2. ✅ Run `window.cartonLabelRedirect.test()` to verify detection
3. ✅ Click "Print Carton Label" button
4. ✅ Watch console for redirect message
5. ✅ Check Network tab to verify URL
6. ✅ Verify other buttons still work normally

This solution is **safe, targeted, and won't affect other print operations**. 🎯