# Print Files Analysis & Fix

## Files Comparison

### `simple-auto-print.js` ✅ **ACTIVE** (loaded by router.js)
**Purpose:** Simple button-clicking approach
- Clicks "View Invoice" button (opens window via `window.open`)
- Clicks "Carton Label" button (sends POST to localhost:8080)
- **NOW FIXED:** URL redirection only active during Carton Label operation

### `silent-auto-print-buttons.js` ❌ **NOT LOADED** (advanced, not used)
**Purpose:** JSPrintManager silent printing
- Intercepts window.open() and $.post() to capture HTML
- Sends captured HTML to JSPrintManager for silent printing
- More complex, requires JSPrintManager client
- **Not currently used** - would need to be loaded by router to be active

## The Problem (FIXED)

**Issue:** The URL interceptor was being installed at page load and remained active permanently.

**Impact:** 
- ❌ View Invoice button was redirected (incorrect)
- ❌ Carton Label button was redirected (correct, but always on)
- ❌ ANY $.post to `localhost:8080/printinvoice` was redirected globally

**Root Cause:** `setupPrintServiceUrlInterceptor()` was called in `initializeInterceptors()` which runs on page load.

## The Fix

**Changes made to `simple-auto-print.js`:**

1. **Renamed function:** `setupPrintServiceUrlInterceptor()` → `installPrintServiceUrlInterceptor()`
2. **Added state tracking:**
   - `_originalPost` - stores original $.post function
   - `_interceptorActive` - boolean flag
3. **Added restore function:** `restorePrintServiceUrl()` - restores original $.post
4. **Modified button click sequence:**
   - Step 1: Click View Invoice (NO interception)
   - Step 2: **Install interceptor** (just before Carton Label)
   - Step 3: Click Carton Label (WITH interception)
   - Interceptor auto-restores via `.always()` callback
5. **Removed from initialization:** URL interceptor no longer installed at page load
6. **Added error handling:** Restores interceptor if error occurs

## How It Works Now

```javascript
// Page Load
setupShipmentIdInterceptor();  // ✅ Always active (tracks shipment ID)
// URL interceptor NOT installed

// User clicks auto-print or modal triggers
clickPrintButtons() {
  // Step 1: Click View Invoice
  viewInvoiceBtn.click();  // Uses native window.open (no redirection)
  
  // Step 2: Install interceptor
  installPrintServiceUrlInterceptor();  // NOW active
  
  // Step 3: Click Carton Label
  cartonLabelBtn.click();  // $.post redirected to https://server:5555/print
  
  // Auto-restore after completion
  result.always(() => restorePrintServiceUrl());  // Interceptor removed
}
```

## Testing Commands

```javascript
// Check if interceptor is active
window.simpleAutoPrint.isInterceptorActive()  // Should be false normally

// Manual test
window.simpleAutoPrint.clickButtons()  // Watch console logs

// Install interceptor manually (for testing)
window.simpleAutoPrint.installInterceptor()

// Restore interceptor manually
window.simpleAutoPrint.restoreInterceptor()
```

## Expected Console Output

```
📌 Shipment ID interceptor ready
📌 URL interceptor: ON-DEMAND (Carton Label only)

[User triggers auto-print]

═══════════════════════════════════════════
🖱️ Starting button click sequence...
═══════════════════════════════════════════

📄 STEP 1: Clicking View Invoice button...
   (No URL redirection for this button)
✅ View Invoice button clicked

⏳ Waiting 500ms before next button...

📦 STEP 2: Installing URL interceptor for Carton Label...
✅ Print service URL interceptor installed (temporary)
   Will auto-restore after Carton Label completes

📦 STEP 3: Clicking Carton Label button...
   (URL redirection ACTIVE for this button only)
✅ Carton Label button clicked
   Interceptor will auto-restore after request completes

🔀 Redirecting print service URL (Carton Label):
   From: http://localhost:8080/printinvoice
   To: https://server:5555/print
   Data size: 12345 bytes

🔄 Carton Label request complete, restoring original $.post
✅ Original $.post restored - interceptor removed

✅ All buttons clicked successfully!
═══════════════════════════════════════════
```

## Recommendation

**Keep only `simple-auto-print.js`** - it's the active file and now properly scoped.

**Archive `silent-auto-print-buttons.js`** - it's not loaded and serves a different purpose (JSPrintManager silent printing). If silent printing is needed in the future, it would require:
1. Loading JSPrintManager library
2. Installing JSPrintManager client
3. Updating router.js to load this file instead
4. Similar fix for scoped URL interception
