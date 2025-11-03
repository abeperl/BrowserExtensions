# Silent Auto-Print with JSPrintManager - Specification

## Overview

This specification defines a new **Silent Auto-Print** feature that extends the existing `auto-print-buttons.js` functionality by adding configurable, silent printing capabilities using JSPrintManager. The system will automatically print both Packing Slip and Carton Label documents directly to configured printers without opening browser windows.

## Goals

1. **Silent Printing**: Print documents directly to printers without user interaction or window popups
2. **Configuration-Based**: Allow switching between window-based and silent printing modes via config
3. **Reliability**: Implement robust fallback mechanisms for when JSPrintManager is unavailable
4. **Consistency**: Maintain similar architecture and patterns as existing `auto-print-buttons.js`
5. **User Feedback**: Provide clear notifications using existing UI components (OverlayManager)

---

## Architecture

### Component Structure

```
silent-auto-print-buttons.js
├── Configuration Layer
│   ├── Print mode selection (jsprintmanager | windows)
│   ├── Printer configuration per job
│   └── Timing and behavior settings
├── JSPrintManager Integration
│   ├── Client detection and validation
│   ├── Printer enumeration and validation
│   └── Print job submission
├── Content Generation Layer
│   ├── Packing Slip HTML generation (via API)
│   ├── Carton Label HTML generation (via API)
│   └── CSS injection for print formatting
├── Fallback Layer
│   ├── localhost:8080 service fallback
│   └── Window-based printing fallback
└── UI Feedback Layer
    └── OverlayManager integration for notifications
```

### Integration Points

1. **Router Integration**: Similar to `auto-print-buttons.js`, the router (`router.js`) will call the handler when the shipment success modal appears
2. **Modal Detection**: Trigger on `#shipment-created` modal (NOT `#shipment-detail` modal)
3. **API Endpoints**:
   - Packing Slip: `OutbondShipment/GetPackingSlipDetailByShipmentId?ShipmentId={id}`
   - Carton Label: Existing `PrintCartonLabelPrint()` function logic
4. **UI Components**: Use existing `OverlayManager` for notifications

---

## Configuration

### Config Object

```javascript
const SILENT_AUTO_PRINT_CONFIG = {
    // Print mode selection
    printMode: 'jsprintmanager',  // Options: 'jsprintmanager' | 'windows'

    // Printer configuration
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',

    // Auto-click behavior
    autoClickEnabled: true,         // Enable/disable auto-clicking
    autoClickDelay: 500,           // Delay before auto-trigger (ms)

    // JSPrintManager settings
    jsprintmanagerTimeout: 5000,   // Timeout for client detection (ms)

    // Fallback settings
    fallbackToLocalhost: true,     // Enable localhost:8080 fallback
    fallbackToWindows: true,       // Enable window-based fallback

    // Debug and logging
    debugMode: true                // Enable detailed console logging
};
```

### Runtime API

```javascript
// Exposed on window.silentAutoPrint object
window.silentAutoPrint = {
    config: SILENT_AUTO_PRINT_CONFIG,

    // Main functions
    printAll: printAllSilent,
    handleModal: handleShipmentModalAppearance,

    // Utility functions
    setPrintMode: (mode) => { /* 'jsprintmanager' | 'windows' */ },
    setPrinter: (job, printerName) => { /* job: 'packingSlip' | 'cartonLabel' */ },
    setAutoClick: (enabled) => { /* true | false */ },

    // Testing/debugging
    testJSPrintManager: checkJSPrintManagerAvailable,
    listPrinters: getAvailablePrinters,
    validatePrinter: (printerName) => { /* returns true/false */ }
};
```

---

## Technical Requirements

### 1. JSPrintManager Integration

#### Library Loading
```html
<!-- Load from CDN -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
```

#### Client Detection
```javascript
async function checkJSPrintManagerAvailable() {
    try {
        await JSPM.JSPrintManager.start();
        console.log('✅ JSPrintManager client detected and connected');
        return true;
    } catch (error) {
        console.warn('⚠️ JSPrintManager client not available:', error);
        return false;
    }
}
```

#### Printer Validation
```javascript
async function validatePrinterExists(printerName) {
    try {
        const printers = await JSPM.JSPrintManager.getPrinters();
        const exists = printers.some(p => p.name === printerName);

        if (!exists) {
            console.warn(`⚠️ Printer "${printerName}" not found`);
            console.log('Available printers:', printers.map(p => p.name));
        }

        return exists;
    } catch (error) {
        console.error('❌ Failed to enumerate printers:', error);
        return false;
    }
}
```

#### Print Job Submission
```javascript
async function printHTMLDocument(printerName, htmlContent, cssContent) {
    try {
        // Create print job
        const cpj = new JSPM.ClientPrintJob();

        // Validate and set printer
        const printerValid = await validatePrinterExists(printerName);
        if (printerValid) {
            cpj.clientPrinter = new JSPM.InstalledPrinter(printerName);
        } else {
            // Fallback to default printer
            console.log('⚠️ Using default printer as fallback');
            cpj.clientPrinter = new JSPM.DefaultPrinter();
        }

        // Combine CSS and HTML
        const fullHTML = `
            <!DOCTYPE html>
            <html>
            <head>
                <style>${cssContent}</style>
            </head>
            <body>
                ${htmlContent}
            </body>
            </html>
        `;

        // Create print file from HTML string
        const printFile = new JSPM.PrintFile(
            fullHTML,
            JSPM.FileSourceType.BLOB,
            'document.html',
            1  // Number of copies
        );

        cpj.files.push(printFile);

        // Send to client
        await cpj.sendToClient();

        console.log(`✅ Print job sent to printer: ${printerName}`);
        return true;

    } catch (error) {
        console.error('❌ Print job failed:', error);
        throw error;
    }
}
```

### 2. Content Generation

#### Packing Slip HTML Generation
```javascript
async function generatePackingSlipHTML(shipmentId) {
    try {
        // Fetch data from API
        const response = await fetch(
            `OutbondShipment/GetPackingSlipDetailByShipmentId?ShipmentId=${shipmentId}`
        );
        const data = await response.json();

        if (data.responseCode !== 0 && data.responseCode !== 200) {
            throw new Error('Failed to fetch packing slip data');
        }

        // Calculate total quantity
        let TotalQty = 0;
        for (let i = 0; i < data.data.ShipmentDetails.length; i++) {
            TotalQty += parseInt(data.data.ShipmentDetails[i].Quantity);
        }
        Object.assign(data.data.Shipment, { TotalQty });

        // Group by box number
        const boxesArray = groupArrayBy(data.data.ShipmentDetails, 'BoxNo');
        const boxesArrayResult = Object.entries(boxesArray);

        // Generate HTML (use existing template logic)
        let html = generatePackingSlipTemplate(data.data, boxesArrayResult);

        // Fetch CSS
        const cssResponse = await fetch('pages/Outbound/packing-slip.css');
        const css = await cssResponse.text();

        return { html, css };

    } catch (error) {
        console.error('❌ Failed to generate Packing Slip HTML:', error);
        throw error;
    }
}
```

#### Carton Label HTML Generation
```javascript
async function generateCartonLabelHTML(shipmentId) {
    try {
        // Get the carton label wrapper element
        // (assumes it's already generated and available in the DOM)
        const wrp = document.querySelector('.carton-label-content');

        if (!wrp) {
            throw new Error('Carton label content not found');
        }

        const html = wrp.innerHTML;

        // Fetch CSS
        const cssResponse = await fetch('pages/Outbound/placard.css');
        let css = await cssResponse.text();

        // Add required CSS rules
        css += `
            * { font-family: sans-serif !important; }
            body { -webkit-print-color-adjust: exact !important; }
            .box-label-wrp {
                page-break-inside: auto;
                page-break-inside: avoid;
                page-break-after: auto;
            }
            @page { size: letter; }
        `;

        return { html, css };

    } catch (error) {
        console.error('❌ Failed to generate Carton Label HTML:', error);
        throw error;
    }
}
```

### 3. Main Print Workflow

```javascript
async function printAllSilent(shipmentId) {
    console.log('🖨️🖨️ Silent Print All - Starting...');

    try {
        // Step 1: Check JSPrintManager availability
        const jsprintAvailable = await checkJSPrintManagerAvailable();

        if (!jsprintAvailable) {
            // Show user notification with options
            await handleJSPrintManagerUnavailable(shipmentId);
            return false;
        }

        // Step 2: Generate content for both documents
        console.log('📄 Generating Packing Slip content...');
        const packingSlipContent = await generatePackingSlipHTML(shipmentId);

        console.log('📦 Generating Carton Label content...');
        const cartonLabelContent = await generateCartonLabelHTML(shipmentId);

        // Step 3: Print both documents in parallel
        console.log('🖨️ Submitting print jobs...');

        const printPromises = [
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNamePackingSlip,
                packingSlipContent.html,
                packingSlipContent.css
            ),
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNameCartonLabel,
                cartonLabelContent.html,
                cartonLabelContent.css
            )
        ];

        await Promise.all(printPromises);

        // Step 4: Show success notification
        console.log('✅ Silent print completed successfully');
        OverlayManager.success({
            message: 'Documents sent to printer successfully',
            duration: 2000
        });

        return true;

    } catch (error) {
        console.error('❌ Silent print failed:', error);
        await handlePrintFailure(error, shipmentId);
        return false;
    }
}
```

### 4. Fallback Mechanisms

#### JSPrintManager Unavailable Handler
```javascript
async function handleJSPrintManagerUnavailable(shipmentId) {
    console.warn('⚠️ JSPrintManager not available - showing user options');

    // Use OverlayManager to show notification with extended duration
    OverlayManager.warning({
        title: 'Silent Print Unavailable',
        message: 'JSPrintManager client is not running. Choose fallback option:',
        duration: 10000  // 10 seconds to allow user to read
    });

    // After notification, ask user for choice
    const choice = await showFallbackChoice();

    if (choice === 'localhost') {
        await fallbackToLocalhost(shipmentId);
    } else if (choice === 'windows') {
        await fallbackToWindows(shipmentId);
    }
}

async function showFallbackChoice() {
    // Show modal or use browser confirm
    // For simplicity, using confirm dialogs
    const useLocalhost = confirm(
        'JSPrintManager is not available.\n\n' +
        'Click OK to use localhost:8080 print service\n' +
        'Click Cancel to open print windows instead'
    );

    return useLocalhost ? 'localhost' : 'windows';
}
```

#### Localhost:8080 Fallback
```javascript
async function fallbackToLocalhost(shipmentId) {
    console.log('🔄 Falling back to localhost:8080 print service');

    try {
        // Generate content
        const packingSlipContent = await generatePackingSlipHTML(shipmentId);
        const cartonLabelContent = await generateCartonLabelHTML(shipmentId);

        // Send to localhost service (existing logic)
        const packingSlipHTML = `<style>${packingSlipContent.css}</style>${packingSlipContent.html}`;
        const cartonLabelHTML = `<style>${cartonLabelContent.css}</style>${cartonLabelContent.html}`;

        // Send both print jobs
        await Promise.all([
            $.post('http://localhost:8080/printinvoice', packingSlipHTML),
            $.post('http://localhost:8080/printinvoice', cartonLabelHTML)
        ]);

        OverlayManager.info({
            message: 'Documents sent to localhost print service',
            duration: 2000
        });

        return true;

    } catch (error) {
        console.error('❌ Localhost fallback failed:', error);
        OverlayManager.error({
            message: 'Print service failed. Please print manually.',
            duration: 3000
        });

        // Final fallback to windows
        await fallbackToWindows(shipmentId);
    }
}
```

#### Window-Based Fallback
```javascript
async function fallbackToWindows(shipmentId) {
    console.log('🔄 Falling back to window-based printing');

    // Use existing auto-print-buttons.js logic
    if (window.autoPrintButtons && window.autoPrintButtons.printAll) {
        OverlayManager.info({
            message: 'Opening print windows...',
            duration: 2000
        });

        await window.autoPrintButtons.printAll();
    } else {
        console.error('❌ Window-based printing not available');
        OverlayManager.error({
            message: 'All print methods failed. Please print manually.',
            duration: 4000
        });
    }
}
```

### 5. Modal Detection & Auto-Trigger

```javascript
async function handleShipmentModalAppearance(shipmentId) {
    console.log('🎯 handleShipmentModalAppearance called with shipmentId:', shipmentId);

    // Check if modal container is visible
    const modalBlockUI = document.getElementById('_modal_block_ui');
    const isVisible = modalBlockUI &&
                     modalBlockUI.classList.contains('loader_block_ui') &&
                     modalBlockUI.style.display !== 'none';

    if (!isVisible) {
        console.log('⚠️ Modal container not visible, exiting');
        return;
    }

    // Verify correct modal (success modal, not create modal)
    const successModal = document.getElementById('shipment-created');
    const successModalVisible = successModal && successModal.offsetParent !== null;

    if (!successModalVisible) {
        console.log('⚠️ Success modal not visible, exiting');
        return;
    }

    console.log('✅ Success modal detected');

    // Check print mode configuration
    if (SILENT_AUTO_PRINT_CONFIG.printMode === 'jsprintmanager') {
        console.log('🔧 Print mode: JSPrintManager (silent)');

        if (SILENT_AUTO_PRINT_CONFIG.autoClickEnabled) {
            console.log(`⏳ Auto-triggering silent print in ${SILENT_AUTO_PRINT_CONFIG.autoClickDelay}ms`);

            setTimeout(() => {
                printAllSilent(shipmentId);
            }, SILENT_AUTO_PRINT_CONFIG.autoClickDelay);
        }
    } else if (SILENT_AUTO_PRINT_CONFIG.printMode === 'windows') {
        console.log('🔧 Print mode: Windows (existing behavior)');

        // Use existing auto-print-buttons.js logic
        if (window.autoPrintButtons && window.autoPrintButtons.handleModal) {
            window.autoPrintButtons.handleModal();
        }
    }
}
```

### 6. Shipment ID Extraction

```javascript
function extractShipmentIdFromModal() {
    // Strategy 1: Check if modal has data attribute
    const modal = document.getElementById('shipment-created');
    if (modal && modal.dataset.shipmentId) {
        return modal.dataset.shipmentId;
    }

    // Strategy 2: Check button data attributes
    const packingSlipBtn = document.getElementById('btnPrintPackSlip');
    if (packingSlipBtn && packingSlipBtn.dataset.shipmentId) {
        return packingSlipBtn.dataset.shipmentId;
    }

    // Strategy 3: Parse from button onclick handler
    if (packingSlipBtn && packingSlipBtn.onclick) {
        const onclickStr = packingSlipBtn.onclick.toString();
        const match = onclickStr.match(/id=(\d+)/);
        if (match) {
            return match[1];
        }
    }

    // Strategy 4: Intercept API response
    // (Implemented via MutationObserver or API interception)
    if (window._lastShipmentId) {
        return window._lastShipmentId;
    }

    console.error('❌ Could not extract shipment ID from modal');
    return null;
}

// API Response Interceptor (add to router.js or main script)
function setupShipmentIdInterceptor() {
    // Intercept fetch API
    const originalFetch = window.fetch;
    window.fetch = async function(...args) {
        const response = await originalFetch.apply(this, args);

        // Check if this is the ProcessOutboundShipment endpoint
        if (args[0] && args[0].includes('ProcessOutboundShipment')) {
            const clonedResponse = response.clone();
            const data = await clonedResponse.json();

            if (data.data && data.data.shipmentId) {
                window._lastShipmentId = data.data.shipmentId;
                console.log('📌 Captured shipment ID:', window._lastShipmentId);

                // Store on modal when it appears
                setTimeout(() => {
                    const modal = document.getElementById('shipment-created');
                    if (modal) {
                        modal.dataset.shipmentId = window._lastShipmentId;
                    }
                }, 100);
            }
        }

        return response;
    };
}
```

---

## Error Handling

### Error Categories

1. **JSPrintManager Unavailable**
   - Client app not running
   - Connection timeout
   - **Action**: Show warning, offer fallback options

2. **Printer Not Found**
   - Configured printer name doesn't exist
   - **Action**: Use default printer, show warning

3. **Content Generation Failed**
   - API call failed
   - Invalid response data
   - **Action**: Show error, don't attempt print

4. **Print Job Failed**
   - JSPrintManager rejected job
   - Print queue error
   - **Action**: Try localhost fallback

5. **All Methods Failed**
   - JSPrintManager failed
   - Localhost service failed
   - **Action**: Show error, ask user to print manually

### Error Notification Matrix

| Error Type | Notification Type | Message | Duration | Action |
|------------|------------------|---------|----------|--------|
| JSPrintManager unavailable | Warning | "Silent print unavailable. Choose fallback option." | 10s | Show choice dialog |
| Printer not found | Warning | "Printer '{name}' not found. Using default printer." | 3s | Continue with default |
| Content generation failed | Error | "Failed to prepare documents. Please try again." | 4s | Abort print |
| Print job failed | Warning | "Print failed. Trying alternate method..." | 2s | Try localhost |
| Localhost failed | Error | "Print service unavailable. Opening windows..." | 3s | Open windows |
| All methods failed | Error | "All print methods failed. Please print manually." | 5s | Show manual links |
| Success | Success | "Documents sent to printer successfully" | 2s | None |
| Fallback success | Info | "Documents sent via alternate method" | 2s | None |

---

## Implementation Steps

### Phase 1: Core Infrastructure
1. Create `silent-auto-print-buttons.js` file
2. Implement configuration object
3. Add JSPrintManager library loading
4. Implement client detection logic
5. Create global API object (`window.silentAutoPrint`)

### Phase 2: Content Generation
1. Implement Packing Slip HTML generation
2. Implement Carton Label HTML generation
3. Add CSS fetching and injection
4. Test HTML output matches current print windows

### Phase 3: Printing Logic
1. Implement printer validation
2. Implement print job submission
3. Add parallel printing logic
4. Test with actual Brother printer

### Phase 4: Fallback Mechanisms
1. Implement JSPrintManager unavailable handler
2. Implement localhost:8080 fallback
3. Implement window-based fallback
4. Add user choice dialog

### Phase 5: Integration
1. Add shipment ID extraction logic
2. Integrate with router.js
3. Add modal detection
4. Implement auto-trigger logic

### Phase 6: UI Feedback
1. Integrate OverlayManager notifications
2. Add success/error/warning messages
3. Implement progress indicators (optional)
4. Test notification timing and visibility

### Phase 7: Testing & Documentation
1. Test all print modes
2. Test all fallback scenarios
3. Test printer validation
4. Create setup guide
5. Document troubleshooting steps

---

## Testing Checklist

### Functional Testing

- [ ] **Silent Print - Happy Path**
  - [ ] JSPrintManager client running
  - [ ] Both printers configured and available
  - [ ] Both documents print successfully
  - [ ] Success notification shows

- [ ] **Printer Validation**
  - [ ] Configured printer not found → uses default printer
  - [ ] Warning notification shows
  - [ ] Print job still succeeds

- [ ] **JSPrintManager Unavailable**
  - [ ] Client app not running
  - [ ] Warning notification shows
  - [ ] User choice dialog appears
  - [ ] Fallback to localhost works
  - [ ] Fallback to windows works

- [ ] **Content Generation**
  - [ ] Packing Slip HTML matches current output
  - [ ] Carton Label HTML matches current output
  - [ ] CSS properly applied
  - [ ] All data fields populated correctly

- [ ] **Configuration Switching**
  - [ ] `printMode: 'jsprintmanager'` → silent print
  - [ ] `printMode: 'windows'` → window print
  - [ ] Runtime API changes take effect

- [ ] **Error Scenarios**
  - [ ] API fails to fetch data → error shown
  - [ ] Print job rejected → fallback triggered
  - [ ] Localhost service down → windows fallback
  - [ ] All methods fail → manual print message

### Integration Testing

- [ ] Modal detection works correctly
- [ ] Shipment ID extraction works
- [ ] Auto-trigger timing correct
- [ ] Doesn't trigger on wrong modal
- [ ] Works alongside existing auto-print-buttons.js

### Browser Compatibility

- [ ] Chrome
- [ ] Edge
- [ ] Firefox
- [ ] Safari (limited - JSPrintManager support may vary)

---

## Setup & Installation Guide

### Prerequisites

1. **JSPrintManager Client App**
   - Download from: https://www.neodynamic.com/downloads/jsprintmanager/
   - Install on user's machine
   - Ensure client app is running (system tray icon visible)
   - Default runs on: `ws://localhost:22443/` or `wss://localhost:22443/`

2. **Printer Configuration**
   - Brother HL-L6200DW series (or configured printer) must be installed in Windows
   - Printer name must exactly match the config (case-sensitive)
   - Printer must be set as "Ready" status

3. **Optional: localhost:8080 Service**
   - For fallback functionality
   - Must support POST to `/printinvoice` endpoint with HTML payload

### Installation Steps

1. **Add JSPrintManager Library**
   ```html
   <!-- Add to page head or before closing body tag -->
   <script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
   ```

2. **Add Script File**
   ```html
   <script src="css-js-toinject/silent-auto-print-buttons.js"></script>
   ```

3. **Configure Printers**
   ```javascript
   // Update config in silent-auto-print-buttons.js
   SILENT_AUTO_PRINT_CONFIG.printerNamePackingSlip = 'Your Printer Name';
   SILENT_AUTO_PRINT_CONFIG.printerNameCartonLabel = 'Your Printer Name';
   ```

4. **Router Integration**
   ```javascript
   // In router.js, add to modal observer:
   if (successModal && successModal.offsetParent !== null) {
       const shipmentId = extractShipmentIdFromModal();
       if (shipmentId) {
           window.silentAutoPrint.handleModal(shipmentId);
       }
   }
   ```

5. **Verify Installation**
   ```javascript
   // Open browser console and run:
   await window.silentAutoPrint.testJSPrintManager();
   // Should return true if client is running

   await window.silentAutoPrint.listPrinters();
   // Should list all available printers
   ```

---

## Configuration Examples

### Example 1: Silent Print Only
```javascript
SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',
    autoClickEnabled: true,
    autoClickDelay: 500,
    fallbackToLocalhost: false,
    fallbackToWindows: false,
    debugMode: false
};
```

### Example 2: Silent Print with Fallback
```javascript
SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',
    autoClickEnabled: true,
    autoClickDelay: 1000,
    fallbackToLocalhost: true,
    fallbackToWindows: true,
    debugMode: true
};
```

### Example 3: Manual Trigger Only
```javascript
SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',
    autoClickEnabled: false,  // Must click button manually
    autoClickDelay: 0,
    fallbackToLocalhost: true,
    fallbackToWindows: true,
    debugMode: true
};
```

### Example 4: Different Printers Per Job
```javascript
SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',
    printerNamePackingSlip: 'HP LaserJet P3015',
    printerNameCartonLabel: 'Zebra ZP450',
    autoClickEnabled: true,
    autoClickDelay: 500,
    fallbackToLocalhost: true,
    fallbackToWindows: true,
    debugMode: false
};
```

---

## Troubleshooting

### Issue: "JSPrintManager client not available"

**Causes:**
- Client app not installed
- Client app not running
- Firewall blocking connection
- Wrong port configuration

**Solutions:**
1. Check JSPrintManager client is running (system tray)
2. Restart client app
3. Check firewall allows localhost:22443
4. Verify browser console for WebSocket errors

### Issue: "Printer not found"

**Causes:**
- Printer name mismatch (case-sensitive)
- Printer not installed in Windows
- Printer offline

**Solutions:**
1. Run `window.silentAutoPrint.listPrinters()` in console
2. Copy exact printer name (case-sensitive)
3. Update config with correct name
4. Verify printer is online in Windows

### Issue: "Print jobs not appearing"

**Causes:**
- Wrong printer selected
- Printer queue paused
- Print spooler service stopped
- Browser blocked popup (shouldn't apply but check)

**Solutions:**
1. Check printer queue in Windows
2. Restart print spooler service
3. Verify printer is set as default
4. Check JSPrintManager client logs

### Issue: "Content looks wrong when printed"

**Causes:**
- CSS not loading
- HTML structure changed
- Page size settings incorrect

**Solutions:**
1. Verify CSS files accessible
2. Check browser console for 404 errors
3. Update CSS paths in config
4. Test HTML generation separately

### Issue: "Fallback not working"

**Causes:**
- localhost:8080 service not running
- Network request blocked
- CORS issues

**Solutions:**
1. Verify localhost:8080 service running
2. Test endpoint manually: `curl -X POST http://localhost:8080/printinvoice -d "<html>test</html>"`
3. Check browser console for CORS errors
4. Update fallback URLs if needed

---

## Future Enhancements

### Phase 2 Features
1. **Print Preview** - Show preview before printing
2. **Print History** - Track successful prints with timestamps
3. **Retry Logic** - Auto-retry failed prints with exponential backoff
4. **Batch Printing** - Queue multiple shipments for printing
5. **Print Templates** - Customizable print layouts per client

### Phase 3 Features
1. **Cloud Printing** - Support for Google Cloud Print alternative
2. **Email Fallback** - Email documents if all print methods fail
3. **Multi-language** - Support for internationalization
4. **Analytics** - Track print success rates and errors
5. **Mobile Support** - Handle mobile devices gracefully

---

## Security Considerations

1. **Local Services**
   - localhost:8080 service should validate input
   - Sanitize HTML before printing to prevent injection
   - Use HTTPS for CDN resources (JSPrintManager)

2. **API Endpoints**
   - Verify shipment ID belongs to current user/client
   - Validate authentication before fetching print data
   - Rate limit print requests to prevent abuse

3. **Client-side Storage**
   - Don't store sensitive shipment data
   - Clear print history on logout
   - Encrypt any stored configuration

4. **Network**
   - Use HTTPS for all API calls
   - Validate SSL certificates
   - Timeout long-running requests

---

## Appendices

### A. JSPrintManager Documentation Links

- Official Website: https://www.neodynamic.com/products/printing/js-print-manager/
- Documentation: https://www.neodynamic.com/Products/Help/JSPrintManager8.0/
- Downloads: https://www.neodynamic.com/downloads/jsprintmanager/
- NPM Package: https://www.npmjs.com/package/jsprintmanager
- GitHub: https://github.com/neodynamic/JSPrintManager

### B. Browser Printer Name Format

Printer names must match exactly as they appear in Windows:
- Correct: `Brother HL-L6200DW series`
- Wrong: `brother hl-l6200dw series` (case doesn't match)
- Wrong: `Brother HL-L6200DW` (missing "series")

To find exact name:
1. Open Windows Settings > Devices > Printers & scanners
2. Copy printer name exactly as shown
3. Or use: `window.silentAutoPrint.listPrinters()`

### C. API Response Structures

**ProcessOutboundShipment Response:**
```json
{
    "responseCode": 0,
    "data": {
        "shipmentId": 950,
        "labelUrl": "...",
        "errorMessage": null
    }
}
```

**GetPackingSlipDetailByShipmentId Response:**
```json
{
    "responseCode": 0,
    "data": {
        "Shipment": {
            "ShipmentNumber": "SH-12345",
            "ShipmentDate": "2024-01-15",
            ...
        },
        "ShipmentDetails": [
            {
                "Sku": "SKU-001",
                "Quantity": 5,
                "BoxNo": 1,
                ...
            }
        ]
    }
}
```

### D. Related Files

- `auto-print-buttons.js` - Original window-based auto-print
- `router.js` - Main router with modal detection
- `overlay-manager.js` - UI notification system
- `popup-controller.js` - Popup control interface
- `tab-manager.js` - Tab management utilities

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-03 | Claude Code | Initial specification |

---

## Approval & Sign-off

This specification has been developed through comprehensive requirements gathering covering:
- ✅ Print mode configuration
- ✅ JSPrintManager integration
- ✅ Content generation strategies
- ✅ Printer validation and fallback logic
- ✅ Error handling and user notifications
- ✅ Shipment ID extraction
- ✅ Testing requirements
- ✅ Installation and setup procedures

**Ready for Implementation**: YES

**Estimated Effort**: 3-5 days for full implementation and testing
