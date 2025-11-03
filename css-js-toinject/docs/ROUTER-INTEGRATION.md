# Router.js Integration for Silent Auto Print

This document shows how to integrate `silent-auto-print-buttons.js` into your existing `router.js`.

## Integration Options

You have two options for integration:

### Option 1: Replace Existing Auto Print (Recommended)

Replace the entire auto print section with the new silent version. The silent version will handle both modes (silent and windows) based on configuration.

### Option 2: Keep Both (Fallback Support)

Keep both the original and silent versions, with the silent version as primary and original as fallback.

---

## Option 1: Replace Existing (Recommended)

Replace the **AUTO PRINT BUTTONS FEATURE** section (lines 865-912) in the "Outbound Packing Route" with this:

```javascript
// ========== SILENT AUTO PRINT FEATURE ==========
if (typeof window.silentAutoPrint !== 'undefined') {
    console.log('🖨️ Silent Auto Print feature enabled');
    console.log('💡 Print mode:', window.silentAutoPrint.config.printMode);
    console.log('💡 Configured printers:');
    console.log('   Packing Slip:', window.silentAutoPrint.config.printerNamePackingSlip);
    console.log('   Carton Label:', window.silentAutoPrint.config.printerNameCartonLabel);

    // Set up MutationObserver to watch for shipment modal
    const modalObserver = new MutationObserver((mutations) => {
        mutations.forEach(mutation => {
            // Check for added nodes
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === 1) {
                    // Check if the modal or its container was added
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

            // Check for attribute changes (like style changes that show the modal)
            if (mutation.type === 'attributes' &&
                mutation.target.id === '_modal_block_ui') {

                setTimeout(() => {
                    window.silentAutoPrint.handleModal();
                }, 100);
            }
        });
    });

    // Start observing
    modalObserver.observe(document.body, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['style', 'class']
    });

    // Try immediately in case modal already exists
    setTimeout(() => {
        window.silentAutoPrint.handleModal();
    }, 500);

    console.log('✅ Silent Auto Print observer set up');
} else {
    console.warn('⚠️ Silent Auto Print not loaded');
}
```

---

## Option 2: Keep Both with Fallback

This option keeps the original auto-print-buttons.js as a fallback if silent-auto-print-buttons.js is not loaded.

Replace the **AUTO PRINT BUTTONS FEATURE** section (lines 865-912) with this:

```javascript
// ========== AUTO PRINT FEATURE (SILENT + FALLBACK) ==========
// Try silent auto print first, fall back to original if not available
const autoPrintAvailable = typeof window.silentAutoPrint !== 'undefined';
const legacyAutoPrintAvailable = typeof handleShipmentModalAppearance === 'function';

if (autoPrintAvailable) {
    console.log('🖨️ Silent Auto Print feature enabled (PRIMARY)');
    console.log('💡 Print mode:', window.silentAutoPrint.config.printMode);
    console.log('💡 Configured printers:');
    console.log('   Packing Slip:', window.silentAutoPrint.config.printerNamePackingSlip);
    console.log('   Carton Label:', window.silentAutoPrint.config.printerNameCartonLabel);

    // Set up MutationObserver to watch for shipment modal
    const modalObserver = new MutationObserver((mutations) => {
        mutations.forEach(mutation => {
            // Check for added nodes
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === 1) {
                    // Check if the modal or its container was added
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

            // Check for attribute changes
            if (mutation.type === 'attributes' &&
                mutation.target.id === '_modal_block_ui') {

                setTimeout(() => {
                    window.silentAutoPrint.handleModal();
                }, 100);
            }
        });
    });

    // Start observing
    modalObserver.observe(document.body, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['style', 'class']
    });

    // Try immediately
    setTimeout(() => {
        window.silentAutoPrint.handleModal();
    }, 500);

    console.log('✅ Silent Auto Print observer set up');

} else if (legacyAutoPrintAvailable) {
    console.log('🖨️ Auto Print Buttons feature enabled (LEGACY FALLBACK)');
    console.log('💡 Auto-click triggers ONLY when "Shipment Created Success" modal appears');

    // Use original auto-print-buttons.js implementation
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

    console.log('✅ Auto Print Buttons observer set up (legacy mode)');
} else {
    console.warn('⚠️ No auto print functions loaded');
}
```

---

## Complete Modified Section

Here's the complete "Outbound Packing Route" with Option 1 (recommended):

```javascript
{
    name: 'Outbound Packing Route',
    pattern: /^#outbound\/packing(\?.*)?$/i,
    action: () => {
        console.log('🚀 Matched #outbound/packing route');

        // ========== TAB MANAGER FOR WINDOW.OPEN ==========
        if (typeof TabManager !== 'undefined') {
            console.log('🪟 Tab Manager feature enabled');

            // Ensure TabManager is installed
            if (!window.open._tabManagerInstalled) {
                TabManager.install();
                console.log('✅ Tab Manager installed for this route');
            } else {
                console.log('ℹ️ Tab Manager already installed');
            }

            // Enable debug mode for visibility
            TabManager.setDebug(true);

            console.log('💡 All window.open() calls will now use reusable tabs');
            console.log('💡 Debug with: window.TabManager.printStatus()');
        } else {
            console.warn('⚠️ TabManager not loaded');
        }

        // ========== SKU AND QTY CLICKABLE FEATURE ==========
        if (typeof makeSkuItemsClickable === 'function' || typeof makeQtyItemsClickable === 'function') {
            // Set up MutationObserver to watch for table data changes
            const observer = new MutationObserver((mutations) => {
                // Check if any mutations added SKU elements
                const hasSkuChanges = mutations.some(mutation => {
                    return Array.from(mutation.addedNodes).some(node => {
                        return node.nodeType === 1 && (
                            node.matches?.('p.sku[data-repeat-item="Sku"]') ||
                            node.querySelector?.('p.sku[data-repeat-item="Sku"]')
                        );
                    });
                });

                if (hasSkuChanges) {
                    console.log('🔄 Table data changed, updating clickable SKUs and quantities');
                    if (typeof makeSkuItemsClickable === 'function') {
                        makeSkuItemsClickable();
                    }
                    if (typeof makeQtyItemsClickable === 'function') {
                        makeQtyItemsClickable();
                    }
                }
            });

            // Start observing the document body for changes
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });

            // Also try immediately in case elements already exist
            setTimeout(() => {
                console.log('🔄 Initial attempt to make SKUs and quantities clickable');
                if (typeof makeSkuItemsClickable === 'function') {
                    makeSkuItemsClickable();
                }
                if (typeof makeQtyItemsClickable === 'function') {
                    makeQtyItemsClickable();
                }
            }, 500);

            console.log('✅ MutationObserver set up for SKU and Qty table monitoring');
        } else {
            console.warn('⚠️ makeSkuItemsClickable and makeQtyItemsClickable not loaded');
        }

        // ========== SILENT AUTO PRINT FEATURE ==========
        if (typeof window.silentAutoPrint !== 'undefined') {
            console.log('🖨️ Silent Auto Print feature enabled');
            console.log('💡 Print mode:', window.silentAutoPrint.config.printMode);
            console.log('💡 Auto-click:', window.silentAutoPrint.config.autoClickEnabled ? 'ENABLED' : 'DISABLED');
            console.log('💡 Configured printers:');
            console.log('   Packing Slip:', window.silentAutoPrint.config.printerNamePackingSlip);
            console.log('   Carton Label:', window.silentAutoPrint.config.printerNameCartonLabel);

            // Set up MutationObserver to watch for shipment modal
            const modalObserver = new MutationObserver((mutations) => {
                mutations.forEach(mutation => {
                    // Check for added nodes
                    mutation.addedNodes.forEach(node => {
                        if (node.nodeType === 1) {
                            // Check if the modal or its container was added
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

                    // Check for attribute changes (like style changes that show the modal)
                    if (mutation.type === 'attributes' &&
                        mutation.target.id === '_modal_block_ui') {

                        setTimeout(() => {
                            window.silentAutoPrint.handleModal();
                        }, 100);
                    }
                });
            });

            // Start observing
            modalObserver.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['style', 'class']
            });

            // Try immediately in case modal already exists
            setTimeout(() => {
                window.silentAutoPrint.handleModal();
            }, 500);

            console.log('✅ Silent Auto Print observer set up');
        } else {
            console.warn('⚠️ Silent Auto Print not loaded - falling back to legacy auto-print if available');

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
            }
        }
    },
    description: 'SKU and Qty item linker + Silent auto print for outbound packing page'
},
```

---

## Installation Steps

### 1. Locate the Section to Modify

Open `router.js` and find the "Outbound Packing Route" section (around line 791-913).

### 2. Replace AUTO PRINT BUTTONS FEATURE

Find this section:
```javascript
// ========== AUTO PRINT BUTTONS FEATURE ==========
if (typeof handleShipmentModalAppearance === 'function') {
    // ... existing code ...
}
```

Replace it with the new code from **Option 1** or **Option 2** above.

### 3. Update Description (Optional)

Update the route description at the end:
```javascript
description: 'SKU and Qty item linker + Silent auto print for outbound packing page'
```

---

## Verification

After making the changes, verify the integration:

### 1. Check Console Logs

Open browser console on `#outbound/packing` page. You should see:

```
🚀 Matched #outbound/packing route
🪟 Tab Manager feature enabled
✅ MutationObserver set up for SKU and Qty table monitoring
🖨️ Silent Auto Print feature enabled
💡 Print mode: jsprintmanager
💡 Auto-click: ENABLED
💡 Configured printers:
   Packing Slip: Brother HL-L6200DW series
   Carton Label: Brother HL-L6200DW series
✅ Silent Auto Print observer set up
```

### 2. Test Modal Detection

Create a shipment and watch the console:

```
🔄 Shipment modal detected by router
🎯 handleShipmentModalAppearance called
✅ Success modal detected
📌 Captured shipment ID: 950
🖨️🖨️ Silent Print All - Starting...
```

### 3. Test Print Modes

```javascript
// Switch to windows mode
window.silentAutoPrint.setPrintMode('windows');

// Create shipment - should open windows
// Console will show: 🔧 Print mode: Windows (using existing auto-print-buttons.js)

// Switch back to silent mode
window.silentAutoPrint.setPrintMode('jsprintmanager');
```

---

## Troubleshooting

### Issue: "Silent Auto Print not loaded"

**Check:**
1. Is `silent-auto-print-buttons.js` loaded before `router.js`?
2. Check browser console for script loading errors
3. Verify script path is correct

**Fix:**
```html
<!-- Correct load order -->
<script src="css-js-toinject/overlay-manager.js"></script>
<script src="css-js-toinject/auto-print-buttons.js"></script>
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
<script src="css-js-toinject/silent-auto-print-buttons.js"></script>
<script src="css-js-toinject/router.js"></script>
```

### Issue: Modal not detected

**Check:**
1. Console logs show modal observer is set up
2. Modal ID is `shipment-created` (not `shipment-detail`)
3. No JavaScript errors preventing execution

**Debug:**
```javascript
// Manually trigger
window.silentAutoPrint.handleModal();

// Check if shipment ID is captured
console.log(window._lastShipmentId);
```

### Issue: Using wrong print mode

**Check:**
```javascript
// Check current mode
console.log(window.silentAutoPrint.config.printMode);

// Change mode
window.silentAutoPrint.setPrintMode('jsprintmanager');
```

---

## Rollback

If you need to rollback to the original auto-print-buttons.js:

1. Remove or comment out the Silent Auto Print section
2. Restore the original AUTO PRINT BUTTONS FEATURE code
3. Remove `silent-auto-print-buttons.js` script tag from HTML

---

## Next Steps

After integration:
1. Test with real shipment creation
2. Verify both print modes work (jsprintmanager and windows)
3. Test all fallback scenarios
4. Configure printer names for your environment
5. Disable debug mode in production

---

## Support

- **Full Spec**: `css-js-toinject/docs/SILENT-AUTO-PRINT-SPEC.md`
- **Setup Guide**: `css-js-toinject/docs/SILENT-AUTO-PRINT-SETUP.md`
- **Source Code**: `css-js-toinject/silent-auto-print-buttons.js`
