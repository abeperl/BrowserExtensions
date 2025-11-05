# Sentry Error Tracking - Status Dropdown

## Overview

The `status-dropdown.js` script now includes comprehensive Sentry error tracking to monitor submit failures and other errors during the auto-fill and auto-submit workflow.

## Sentry Configuration

**DSN:** `https://104595fd1de31dc644958f35b4f325ec@o4510313441853440.ingest.us.sentry.io/4510313480454144`

**SDK:** Dynamically loaded from CDN by the script itself

## Initialization

Sentry SDK is automatically loaded and initialized by `status-dropdown.js`:

1. **Dynamic CDN Loading:** The script dynamically loads the Sentry SDK if not already present:
```javascript
function loadAndInitSentry() {
    // Check if Sentry is already loaded
    if (typeof Sentry !== 'undefined') {
        initializeSentry();
        return;
    }

    // Load Sentry SDK from CDN
    const script = document.createElement('script');
    script.src = 'https://js.sentry-cdn.com/104595fd1de31dc644958f35b4f325ec.min.js';
    script.crossOrigin = 'anonymous';
    script.onload = function() {
        console.log('✅ Sentry SDK loaded from CDN');
        initializeSentry();
    };
    script.onerror = function() {
        console.warn('⚠️ Failed to load Sentry SDK from CDN - error tracking disabled');
    };
    document.head.appendChild(script);
}
```

2. **Initialization:** Once loaded, Sentry is initialized with configuration:
```javascript
function initializeSentry() {
    if (typeof Sentry !== 'undefined') {
        Sentry.init({
            dsn: "https://104595fd1de31dc644958f35b4f325ec@o4510313441853440.ingest.us.sentry.io/4510313480454144",
            integrations: [
                Sentry.consoleLoggingIntegration({ levels: ["warn", "error"] }),
            ],
            beforeSend(event) {
                event.tags = event.tags || {};
                event.tags.script = 'status-dropdown.js';
                event.tags.feature = 'status-auto-fill';
                return event;
            }
        });
    }
}
```

**Note:** Console logging integration only captures `warn` and `error` levels to reduce noise.

## Tracked Events

### 1. Submit Button Missing
**Trigger:** When submit button cannot be found in modal
**Level:** Warning
**Tags:**
- `action: auto-submit-status`
- `reason: submit-button-missing`

**Context Data:**
- `statusValue` - Selected status value
- `productValue` - Product scan value
- `modalVisible` - Whether modal is visible
- `submitButtonExists` - Submit button existence check
- `timestamp` - ISO timestamp

### 2. Modal Still Visible After Submit
**Trigger:** Modal remains visible 500ms after submit attempt
**Level:** Warning
**Tags:**
- `action: auto-submit-status`
- `reason: modal-still-visible`

**Context Data:**
- Same as above, plus `verificationTime`

### 3. Exception During Submit
**Trigger:** JavaScript exception during submit process
**Level:** Error
**Tags:**
- `action: auto-submit-status`
- `reason: exception-during-submit`

**Context Data:**
- `statusValue` - Selected status value
- `productValue` - Product scan value
- `timestamp` - ISO timestamp
- Stack trace from exception

### 4. Exception During Auto-Fill
**Trigger:** JavaScript exception during status auto-fill
**Level:** Error
**Tags:**
- `action: auto-fill-status`
- `reason: exception-during-fill`

**Context Data:**
- `statusValue` - Selected status value
- `timestamp` - ISO timestamp
- Stack trace from exception

## Usage

### Ensuring Sentry is Available

The script checks for Sentry availability before attempting to log:

```javascript
if (typeof Sentry !== 'undefined') {
    // Log error
}
```

### Console Logging

All Sentry errors are also logged to console with `console.error()` for local debugging.

## Verification

### Success Indicators
- ✅ Console log: "Sentry initialized for status-dropdown.js"
- ✅ Console log: "Submit verification passed - modal closed"

### Failure Indicators
- ❌ Console error: "Submit button not found in modal"
- ❌ Console error: "Status submit may have failed - modal still visible"
- ❌ Console error: "Error during auto-submit"
- ❌ Console error: "Error during status auto-fill"

## Integration with Router

No special setup required! The `status-dropdown.js` script automatically loads the Sentry SDK from CDN when it runs. Simply include the script via router:

```javascript
// In router.js
loadScript('status-dropdown.js').then(() => {
    // Sentry will be automatically loaded and initialized
    setupStatusAutoFill();
    setupProductScanAutoSubmit();
});
```

## Monitoring Dashboard

View errors in Sentry dashboard:
- **Organization:** o4510313441853440
- **Project:** 4510313480454144
- **Filter by tags:**
  - `script:status-dropdown.js`
  - `feature:status-auto-fill`
  - `action:auto-submit-status`

## Logging Optimization

To reduce console noise, the following optimizations have been made:

1. **Console logging integration:** Only captures `warn` and `error` levels (not `log`)
2. **Character-by-character logging removed:** The verbose "Product input received" and "Status input received" logs that showed each character have been removed
3. **Focused error logging:** Only errors and warnings are sent to Sentry

## Future Enhancements

Potential additions:
- Performance monitoring with `Sentry.startTransaction()`
- User feedback on submission errors
- Custom breadcrumbs for workflow steps
- Network request tracking
- Session replay for debugging complex issues
