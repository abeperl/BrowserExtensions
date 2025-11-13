# Print Service URL Redirect

## Overview

Both `simple-auto-print.js` and `silent-auto-print-buttons.js` now intercept and redirect print service URLs automatically.

## What Changed

### URL Redirection
- **From:** `http://localhost:8080/printinvoice`
- **To:** `https://server:5555/print` (HTTPS to avoid mixed content errors)

### How It Works

The scripts intercept jQuery's `$.post()` method and check if the URL contains `localhost:8080/printinvoice`. If it does, the URL is automatically redirected to `https://server:5555/print` while preserving all other POST parameters.

## Implementation Details

### 1. Simple Auto Print (`simple-auto-print.js`)

The URL interceptor is automatically installed on page load along with the shipment ID interceptor:

```javascript
function setupPrintServiceUrlInterceptor() {
    const originalPost = $.post;

    $.post = function(url, data, success, dataType) {
        // Redirect localhost:8080/printinvoice → https://server:5555/print
        if (url && url.includes('localhost:8080/printinvoice')) {
            const newUrl = 'https://server:5555/print';
            console.log(`🔀 Redirecting: ${url} → ${newUrl}`);
            return originalPost.call(this, newUrl, data, success, dataType);
        }

        // Pass through all other POST requests
        return originalPost.apply(this, arguments);
    };
}
```

### 2. Silent Auto Print (`silent-auto-print-buttons.js`)

The same interceptor is also installed in the silent print version, ensuring consistent behavior across both printing modes.

## Usage

### Automatic Setup
The URL interceptor is automatically installed when either script loads. No manual configuration needed!

### Console Output
When a URL is redirected, you'll see console messages:
```
🔀 Redirecting print service URL:
   From: http://localhost:8080/printinvoice
   To: https://server:5555/print
```

### Manual Setup (if needed)
If you need to manually reinstall the interceptor:

```javascript
// For simple auto print
window.simpleAutoPrint.setupUrlInterceptor();

// For silent auto print
window.silentAutoPrint.setupUrlInterceptor();
```

## Testing

### Test the Interception
1. Load the page with either script active
2. Trigger a carton label print
3. Check console for redirection messages
4. Verify the POST request goes to `server:5555/print`

### Verify Original Functionality
The interceptor only affects URLs containing `localhost:8080/printinvoice`. All other `$.post()` calls work normally:

```javascript
// This will be redirected
$.post('http://localhost:8080/printinvoice', htmlContent);

// These will NOT be affected
$.post('/api/some-endpoint', data);
$.post('http://example.com/endpoint', data);
```

## Browser Network Tab

You can verify the redirect in your browser's developer tools:
1. Open DevTools → Network tab
2. Trigger a print operation
3. Look for POST request to `https://server:5555/print` instead of `localhost:8080/printinvoice`

## Compatibility

- **jQuery Version:** Works with any jQuery version that has `$.post()`
- **Browser Support:** All modern browsers
- **Conflicts:** No known conflicts with other scripts

## Technical Notes

### Preserving Original Behavior
- All POST data is passed through unchanged
- Success/error callbacks work as expected
- Data types and other parameters are preserved
- Only the URL is modified

### Server Requirements
The new print service at `https://server:5555/print` must:
1. Accept HTTPS connections with a valid SSL certificate
2. Accept the same POST data format as the old service
3. Handle HTML content sent in the request body

**Note:** Using HTTPS is required because the main site loads over HTTPS. Browsers block mixed HTTP/HTTPS content for security.

## Related Files

- `simple-auto-print.js` - Basic auto-print with URL redirect
- `silent-auto-print-buttons.js` - Silent print with URL redirect
- Both files have identical URL interception logic

## Troubleshooting

### Redirect Not Working
1. Check console for `✅ Print service URL interceptor installed` message
2. Verify jQuery (`$`) is loaded before the script
3. Try manually calling: `window.simpleAutoPrint.setupUrlInterceptor()`

### Wrong URL Still Being Used
1. Clear browser cache
2. Reload the page
3. Check for other scripts that might override `$.post()`

### Print Service Not Responding
The new service at `https://server:5555/print` must:
- Accept HTTPS connections with valid SSL certificate
- Accept POST requests
- Handle the HTML content in the request body
- Return appropriate success/error responses

### SSL Certificate Errors
If you see SSL certificate errors:
1. Ensure the server has a valid SSL certificate
2. For testing, you may need to accept self-signed certificates in your browser
3. Check that the hostname matches the certificate
