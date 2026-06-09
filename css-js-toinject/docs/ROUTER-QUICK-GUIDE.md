# Router Integration - Quick Guide

## 3-Step Integration

### Step 1: Find the Location

Open `css-js-toinject/router.js` and locate line **865** (approximately).

Look for this section:

```javascript
// ========== AUTO PRINT BUTTONS FEATURE ==========
if (typeof handleShipmentModalAppearance === 'function') {
    console.log('🖨️ Auto Print Buttons feature enabled');
    console.log('💡 Auto-click triggers ONLY when "Shipment Created Success" modal appears');

    // Set up MutationObserver to watch for shipment modal
    const modalObserver = new MutationObserver((mutations) => {
        // ... observer code ...
    });

    // ... rest of the code ...

    console.log('✅ Auto Print Buttons observer set up');
} else {
    console.warn('⚠️ Auto Print Buttons functions not loaded');
}
```

### Step 2: Replace with New Code

**DELETE** everything from `// ========== AUTO PRINT BUTTONS FEATURE ==========` to the closing `}` (around line 912).

**PASTE** the code from `router-integration-snippet.js` (or copy from below).

### Step 3: Save and Test

1. Save `router.js`
2. Reload the page `#outbound/packing`
3. Check browser console for:
   ```
   🖨️ Silent Auto Print feature enabled
   💡 Print mode: jsprintmanager
   ✅ Silent Auto Print observer set up
   ```

---

## Visual Guide

### BEFORE (Current code - Lines 865-912):

```javascript
{
    name: 'Outbound Packing Route',
    pattern: /^#outbound\/packing(\?.*)?$/i,
    action: () => {
        // ... other features ...

        // ========== AUTO PRINT BUTTONS FEATURE ==========   ← START: DELETE FROM HERE
        if (typeof handleShipmentModalAppearance === 'function') {
            console.log('🖨️ Auto Print Buttons feature enabled');

            const modalObserver = new MutationObserver((mutations) => {
                mutations.forEach(mutation => {
                    mutation.addedNodes.forEach(node => {
                        if (node.nodeType === 1) {
                            if (node.id === '_modal_block_ui' ||
                                node.id === 'shipment-created' ||
                                node.querySelector?.('#shipment-created')) {

                                console.log('🔄 Shipment modal detected by router');
                                setTimeout(handleShipmentModalAppearance, 100);
                            }
                        }
                    });

                    if (mutation.type === 'attributes' &&
                        mutation.target.id === '_modal_block_ui') {

                        setTimeout(handleShipmentModalAppearance, 100);
                    }
                });
            });

            modalObserver.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['style', 'class']
            });

            setTimeout(() => {
                handleShipmentModalAppearance();
            }, 500);

            console.log('✅ Auto Print Buttons observer set up');
        } else {
            console.warn('⚠️ Auto Print Buttons functions not loaded');
        }                                                      ← END: DELETE TO HERE
    },
    description: 'SKU and Qty item linker + Auto print buttons for outbound packing page'
},
```

### AFTER (New code):

```javascript
{
    name: 'Outbound Packing Route',
    pattern: /^#outbound\/packing(\?.*)?$/i,
    action: () => {
        // ... other features ...

        // ========== SILENT AUTO PRINT FEATURE ==========   ← PASTE NEW CODE HERE
        if (typeof window.silentAutoPrint !== 'undefined') {
            console.log('🖨️ Silent Auto Print feature enabled');
            console.log('💡 Print mode:', window.silentAutoPrint.config.printMode);
            console.log('💡 Auto-click:', window.silentAutoPrint.config.autoClickEnabled ? 'ENABLED' : 'DISABLED');
            console.log('💡 Configured printers:');
            console.log('   Packing Slip:', window.silentAutoPrint.config.printerNamePackingSlip);
            console.log('   Carton Label:', window.silentAutoPrint.config.printerNameCartonLabel);

            const modalObserver = new MutationObserver((mutations) => {
                mutations.forEach(mutation => {
                    mutation.addedNodes.forEach(node => {
                        if (node.nodeType === 1) {
                            if (node.id === '_modal_block_ui' ||
                                node.id === 'shipment-created' ||
                                node.querySelector?.('#shipment-created')) {

                                console.log('🔄 Shipment modal detected by router');
                                setTimeout(() => {
                                    window.silentAutoPrint.handleModal();
                                }, 100);
                            }
                        }
                    });

                    if (mutation.type === 'attributes' &&
                        mutation.target.id === '_modal_block_ui') {

                        setTimeout(() => {
                            window.silentAutoPrint.handleModal();
                        }, 100);
                    }
                });
            });

            modalObserver.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['style', 'class']
            });

            setTimeout(() => {
                window.silentAutoPrint.handleModal();
            }, 500);

            console.log('✅ Silent Auto Print observer set up');

        } else {
            console.warn('⚠️ Silent Auto Print not loaded - falling back to legacy auto-print');

            // Fallback to legacy auto-print-buttons.js
            if (typeof handleShipmentModalAppearance === 'function') {
                console.log('🖨️ Using legacy Auto Print Buttons');

                const modalObserver = new MutationObserver((mutations) => {
                    mutations.forEach(mutation => {
                        mutation.addedNodes.forEach(node => {
                            if (node.nodeType === 1) {
                                if (node.id === '_modal_block_ui' ||
                                    node.id === 'shipment-created' ||
                                    node.querySelector?.('#shipment-created')) {

                                    setTimeout(handleShipmentModalAppearance, 100);
                                }
                            }
                        });

                        if (mutation.type === 'attributes' &&
                            mutation.target.id === '_modal_block_ui') {
                            setTimeout(handleShipmentModalAppearance, 100);
                        }
                    });
                });

                modalObserver.observe(document.body, {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['style', 'class']
                });

                setTimeout(() => {
                    handleShipmentModalAppearance();
                }, 500);

                console.log('✅ Legacy Auto Print Buttons observer set up');
            } else {
                console.warn('⚠️ No auto print functions loaded');
            }
        }
    },
    description: 'SKU and Qty item linker + Silent auto print for outbound packing page'  ← OPTIONAL: Update description
},
```

---

## What Changed?

### Key Differences:

1. **Function Call**:
   - OLD: `handleShipmentModalAppearance()`
   - NEW: `window.silentAutoPrint.handleModal()`

2. **Availability Check**:
   - OLD: `typeof handleShipmentModalAppearance === 'function'`
   - NEW: `typeof window.silentAutoPrint !== 'undefined'`

3. **Fallback Logic**:
   - NEW: Falls back to legacy auto-print if silent version not loaded

4. **Additional Logging**:
   - NEW: Shows print mode and configured printers

---

## Testing Checklist

After making changes:

- [ ] Save router.js
- [ ] Clear browser cache (Ctrl+Shift+R)
- [ ] Navigate to `#outbound/packing`
- [ ] Open browser console
- [ ] Verify you see: `🖨️ Silent Auto Print feature enabled`
- [ ] Verify you see: `💡 Print mode: jsprintmanager`
- [ ] Create a test shipment
- [ ] Verify modal is detected: `🔄 Shipment modal detected by router`
- [ ] Verify print workflow starts: `🖨️🖨️ Silent Print All - Starting...`

---

## Troubleshooting

### Console shows "Silent Auto Print not loaded"

**Problem**: Script not loaded or loaded in wrong order

**Solution**:
```html
<!-- Correct order -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
<script src="css-js-toinject/silent-auto-print-buttons.js"></script>
<script src="css-js-toinject/router.js"></script>
```

### Console shows "Using legacy Auto Print Buttons"

**This is OK!** It means silent-auto-print isn't loaded, but the fallback to the original auto-print-buttons.js is working.

### Modal not detected

**Check**:
```javascript
// Manually trigger
window.silentAutoPrint.handleModal();

// Check configuration
console.log(window.silentAutoPrint.config);
```

---

## Quick Commands

```javascript
// Check if silent print is loaded
typeof window.silentAutoPrint !== 'undefined'

// Check current mode
window.silentAutoPrint.config.printMode

// Switch to windows mode for testing
window.silentAutoPrint.setPrintMode('windows')

// Switch back to silent mode
window.silentAutoPrint.setPrintMode('jsprintmanager')

// Disable auto-click for manual testing
window.silentAutoPrint.setAutoClick(false)

// Check JSPrintManager availability
await window.silentAutoPrint.checkJSPrintManager()

// List printers
await window.silentAutoPrint.listPrinters()
```

---

## Done!

After completing these steps, the Silent Auto Print feature will be fully integrated and ready to use.

Proceed to:
1. Configure your printer names (if not Brother HL-L6200DW)
2. Install JSPrintManager client on workstations
3. Test with real shipment creation

**See also**:
- Full integration guide: `ROUTER-INTEGRATION.md`
- Setup guide: `SILENT-AUTO-PRINT-SETUP.md`
- Full specification: `SILENT-AUTO-PRINT-SPEC.md`
