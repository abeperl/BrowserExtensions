# Print Redirect Quick Start

## Problem
Carton label printing needs to go to the new server at `https://192.168.1.254:5555/print` instead of `localhost:8080/printinvoice`.

## Solution Summary

Your code **already has** URL redirection implemented in [simple-auto-print.js](simple-auto-print.js), but there are two issues:

1. **Wrong URL**: Code uses `https://server:5555/print` instead of `https://192.168.1.254:5555/print`
2. **Reliability**: jQuery-only interception may have timing issues

## Quick Fix (Option 1) - Fix Existing Code

Edit [simple-auto-print.js](simple-auto-print.js) line 128:

```javascript
// CHANGE THIS LINE:
const newUrl = 'https://server:5555/print';

// TO THIS:
const newUrl = 'https://192.168.1.254:5555/print';
```

Then reload the page and test.

## Recommended Fix (Option 2) - Use Global Interceptor

Use the new [print-url-redirect-global.js](print-url-redirect-global.js) file:

### Step 1: Load the Script
Add to your page (before other scripts):

```html
<script src="print-url-redirect-global.js"></script>
```

Or inject via browser extension.

### Step 2: Test It
Open browser console and run:

```javascript
// Check if loaded
window.printUrlRedirect.stats();

// Test the redirect (safe - won't actually print)
window.printUrlRedirect.test();
```

### Step 3: Print Something
Click the Carton Label button and watch console output:

```
🔀 FETCH REQUEST REDIRECTED #1
   From: http://localhost:8080/printinvoice
   To: https://192.168.1.254:5555/print
```

## How It Works

### Current Implementation (simple-auto-print.js)
- Installs jQuery `$.post()` interceptor when button is clicked
- Auto-restores after request completes
- Works but has timing/reliability issues

### New Implementation (print-url-redirect-global.js)
- Intercepts at browser level (Fetch, XHR, jQuery)
- Always active - no timing issues
- More reliable and comprehensive

## Comparison

| Feature | simple-auto-print.js | print-url-redirect-global.js |
|---------|---------------------|------------------------------|
| Intercepts jQuery $.post | ✅ Yes | ✅ Yes |
| Intercepts Fetch API | ❌ No | ✅ Yes |
| Intercepts XMLHttpRequest | ❌ No | ✅ Yes |
| Always active | ❌ No (install/uninstall) | ✅ Yes |
| Timing issues | ⚠️ Possible | ✅ None |
| Debug API | ⚠️ Limited | ✅ Comprehensive |
| Can test without printing | ❌ No | ✅ Yes |

## Testing Checklist

### 1. Check if Interceptor is Active
```javascript
// For simple-auto-print.js
console.log(window.simpleAutoPrint?.installInterceptor);

// For print-url-redirect-global.js
window.printUrlRedirect.stats();
```

### 2. Watch Console
When you click Carton Label button, you should see:
```
🔀 [METHOD] REQUEST REDIRECTED #1
   From: localhost:8080/printinvoice
   To: https://192.168.1.254:5555/print
```

### 3. Check Network Tab
1. Open DevTools → Network
2. Click Carton Label
3. Find POST request
4. Verify URL is `https://192.168.1.254:5555/print`

### 4. Check Server Logs
From your earlier question, logs are at:
```
E:\Share\server\servern\Software\Logs\html-printer-service-YYYYMMDD.txt
```

Check for incoming print requests.

## Troubleshooting

### No Redirect Message in Console
**Problem:** Interceptor not installed
**Fix:**
- For simple-auto-print.js: Run `window.simpleAutoPrint.installInterceptor()`
- For print-url-redirect-global.js: Check if script is loaded

### Redirect Message but Wrong URL
**Problem:** Still going to `localhost:8080`
**Fix:** Browser may have cached the request. Hard reload (Ctrl+Shift+R)

### SSL Certificate Error
**Problem:** Server has invalid/self-signed certificate
**Fix:**
1. Navigate to `https://192.168.1.254:5555/` in browser
2. Accept the certificate warning
3. Try printing again

### CORS Error
**Problem:** Server blocking cross-origin requests
**Fix:** Check server's CORS configuration (AllowedOrigins in appsettings.json)

## Server Configuration

Your HTML printer service ([html-printer-service](../html-printer-service/)) is configured to listen on:
- **Port:** 5555
- **Protocol:** HTTPS
- **Host:** 0.0.0.0 (all interfaces)
- **Certificate:** From Windows certificate store

Make sure the service is running:
```powershell
Get-Service HTMLZebraPrinterService
```

## Debugging Commands

```javascript
// Simple Auto Print (existing code)
window.simpleAutoPrint.config                    // Show configuration
window.simpleAutoPrint.installInterceptor()      // Manually install
window.simpleAutoPrint.restoreInterceptor()      // Restore original

// Global Redirect (new code)
window.printUrlRedirect.stats()                  // Show statistics
window.printUrlRedirect.test()                   // Test (safe)
window.printUrlRedirect.disable()                // Disable temporarily
window.printUrlRedirect.enable()                 // Re-enable
```

## Recommendation

**For immediate fix:** Use Option 1 (change one line in simple-auto-print.js)

**For reliability:** Use Option 2 (add print-url-redirect-global.js)

**For best results:** Use BOTH - the global interceptor as a safety net, plus the existing simple-auto-print.js for other features (auto-clicking, modal detection, etc.)

## File Locations

- Current implementation: [simple-auto-print.js](simple-auto-print.js) (lines 100-169)
- New global interceptor: [print-url-redirect-global.js](print-url-redirect-global.js)
- Detailed analysis: [PRINT-REDIRECT-ANALYSIS.md](PRINT-REDIRECT-ANALYSIS.md)
- Router integration: [router.js](router.js) (lines 660-719)
- Server configuration: [../html-printer-service/HTMLZebraPrinterService/appsettings.json](../html-printer-service/HTMLZebraPrinterService/appsettings.json)

## Next Steps

1. Choose Option 1 (quick) or Option 2 (reliable)
2. Make the changes
3. Reload the page
4. Test by clicking Carton Label button
5. Check console, Network tab, and server logs
6. Report back what you see!