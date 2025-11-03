/**
 * READY-TO-USE ROUTER.JS INTEGRATION CODE
 *
 * Replace the "AUTO PRINT BUTTONS FEATURE" section in router.js (lines 865-912)
 * with this code block.
 *
 * Location: router.js -> "Outbound Packing Route" -> AUTO PRINT BUTTONS section
 */

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

        console.log('✅ Legacy Auto Print Buttons observer set up');
    } else {
        console.warn('⚠️ No auto print functions loaded');
    }
}
