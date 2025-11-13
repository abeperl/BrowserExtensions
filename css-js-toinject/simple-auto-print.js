/**
 * Simple Auto Print - Just Click Buttons
 *
 * Simplified approach:
 * 1. Wait for shipment modal to appear
 * 2. Click "View Invoice" button
 * 3. Click "Print Carton Label" button
 */

(function() {
    'use strict';

    console.log('🖨️ Simple Auto Print - Loading...');

    // =============================================================================
    // CONFIGURATION
    // =============================================================================

    const CONFIG = {
        autoClickEnabled: true,
        autoClickDelay: 500,  // Wait 500ms after modal appears (reduced for faster response)

        // Delay between clicking buttons (ms)
        delayBetweenClicks: 500,

        debugMode: true
    };

    // =============================================================================
    // SHIPMENT ID CAPTURE
    // =============================================================================

    window._lastShipmentId = null;

    function setupShipmentIdInterceptor() {
        if (typeof $ !== 'undefined' && $.ajax) {
            const originalAjax = $.ajax;

            $.ajax = function(url, settings) {
                let actualUrl = url;
                let actualSettings = settings;

                if (typeof url === 'object') {
                    actualSettings = url;
                    actualUrl = actualSettings.url;
                }

                const promise = originalAjax.call(this, url, settings);

                promise.then(function(data) {
                    if (actualUrl && actualUrl.includes('ProcessOutboundShipment')) {
                        if (data && data.data && data.data.shipmentId) {
                            window._lastShipmentId = data.data.shipmentId;
                            console.log('📌 Captured shipment ID:', window._lastShipmentId);

                            setTimeout(() => {
                                const modal = document.getElementById('shipment-created');
                                if (modal) {
                                    modal.dataset.shipmentId = window._lastShipmentId;
                                }
                            }, 100);
                        }
                    }
                });

                return promise;
            };

            console.log('✅ Shipment ID interceptor setup');
        }
    }

    // =============================================================================
    // URL INTERCEPTION FOR PRINT SERVICE
    // =============================================================================

    /**
     * Override $.post to redirect print service URLs
     * Only intercepts when called from PrintShipmentLabelSlipSoftPrint or PrintCartonLabelPrint
     * Changes: http://localhost:8080/printinvoice -> https://server:5555/print
     */
    function setupPrintServiceUrlInterceptor() {
        if (typeof $ !== 'undefined' && $.post) {
            const originalPost = $.post;

            $.post = function(url, data, success, dataType) {
                // Only intercept if this is the exact print service call
                // Check: URL is localhost:8080/printinvoice AND data contains HTML content (CSS styles)
                if (url && url.includes('localhost:8080/printinvoice') &&
                    typeof data === 'string' && data.includes('<style')) {

                    const newUrl = 'https://server:5555/print';
                    console.log(`🔀 Redirecting print service URL (HTML print job):`);
                    console.log(`   From: ${url}`);
                    console.log(`   To: ${newUrl}`);
                    console.log(`   Data size: ${data.length} bytes`);

                    // Call original $.post with new URL
                    return originalPost.call(this, newUrl, data, success, dataType);
                }

                // Pass through all other POST requests unchanged
                return originalPost.apply(this, arguments);
            };

            console.log('✅ Print service URL interceptor installed (HTML print jobs only)');
            console.log('   Redirecting: localhost:8080/printinvoice → https://server:5555/print');
            return true;
        }
        return false;
    }

    // =============================================================================
    // BUTTON CLICKING
    // =============================================================================

    /**
     * Click the print buttons in sequence
     */
    async function clickPrintButtons() {
        console.log('═══════════════════════════════════════════');
        console.log('🖱️ Starting button click sequence...');
        console.log('═══════════════════════════════════════════');

        try {
            // STEP 1: Click View Invoice button
            console.log('\n📄 STEP 1: Clicking View Invoice button...');

            const viewInvoiceBtn = document.querySelector('button[data-value="pinvoice"]');

            if (!viewInvoiceBtn) {
                throw new Error('View Invoice button not found (selector: button[data-value="pinvoice"])');
            }

            console.log('   Button found:', {
                visible: viewInvoiceBtn.offsetParent !== null,
                disabled: viewInvoiceBtn.disabled,
                hasOnclick: !!viewInvoiceBtn.onclick
            });

            viewInvoiceBtn.click();
            console.log('✅ View Invoice button clicked');

            // Wait before next button
            console.log(`⏳ Waiting ${CONFIG.delayBetweenClicks}ms before next button...`);
            await new Promise(resolve => setTimeout(resolve, CONFIG.delayBetweenClicks));

            // STEP 2: Click Carton Label button
            console.log('\n📦 STEP 2: Clicking Carton Label button...');

            const cartonLabelBtn = document.getElementById('box-label');

            if (!cartonLabelBtn) {
                throw new Error('Carton Label button not found (id: box-label)');
            }

            console.log('   Button found:', {
                visible: cartonLabelBtn.offsetParent !== null,
                disabled: cartonLabelBtn.disabled,
                hasOnclick: !!cartonLabelBtn.onclick
            });

            cartonLabelBtn.click();
            console.log('✅ Carton Label button clicked');

            console.log('\n✅ All buttons clicked successfully!');
            console.log('═══════════════════════════════════════════\n');

            // Show success notification if OverlayManager available
            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.success({
                    message: 'Print windows opened',
                    duration: 2000
                });
            }

            return true;

        } catch (error) {
            console.error('❌ Button click failed:', error);

            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.error({
                    message: 'Auto-print failed: ' + error.message,
                    duration: 3000
                });
            }

            return false;
        }
    }

    // =============================================================================
    // MODAL DETECTION
    // =============================================================================

    /**
     * Handle modal appearance
     */
    function handleModalAppearance() {
        console.log('🎯 Modal appearance detected');

        // Check if correct modal is visible
        const modalBlockUI = document.getElementById('_modal_block_ui');
        const successModal = document.getElementById('shipment-created');
        const createModal = document.getElementById('shipment-detail');

        const isVisible = modalBlockUI &&
                         modalBlockUI.classList.contains('loader_block_ui') &&
                         modalBlockUI.style.display !== 'none';

        if (!isVisible) {
            console.log('⚠️ Modal container not visible, exiting');
            return;
        }

        const successModalVisible = successModal && successModal.offsetParent !== null;
        const createModalVisible = createModal && createModal.offsetParent !== null;

        console.log('📊 Modal check:');
        console.log('   Success modal:', successModalVisible ? 'VISIBLE' : 'hidden');
        console.log('   Create modal:', createModalVisible ? 'VISIBLE' : 'hidden');

        // Must be success modal (not create modal)
        if (createModalVisible) {
            console.log('❌ Wrong modal (create modal), exiting');
            return;
        }

        if (!successModalVisible) {
            console.log('⚠️ Success modal not visible, exiting');
            return;
        }

        console.log('✅ Success modal confirmed');

        // Check shipment ID
        const shipmentId = window._lastShipmentId;
        if (shipmentId) {
            console.log('📌 Shipment ID:', shipmentId);
        } else {
            console.warn('⚠️ Shipment ID not found (but continuing anyway)');
        }

        // Auto-click if enabled
        if (CONFIG.autoClickEnabled) {
            console.log(`⏳ Auto-clicking buttons in ${CONFIG.autoClickDelay}ms...`);

            setTimeout(() => {
                clickPrintButtons();
            }, CONFIG.autoClickDelay);
        } else {
            console.log('ℹ️ Auto-click disabled');
        }
    }

    // =============================================================================
    // GLOBAL API
    // =============================================================================

    if (typeof window !== 'undefined') {
        window.simpleAutoPrint = {
            // Configuration
            config: CONFIG,

            // Main functions
            clickButtons: clickPrintButtons,
            handleModal: handleModalAppearance,

            // Interceptors
            setupUrlInterceptor: setupPrintServiceUrlInterceptor,

            // Utilities
            setAutoClick: (enabled) => {
                CONFIG.autoClickEnabled = enabled;
                console.log(`🔧 Auto-click ${enabled ? 'enabled' : 'disabled'}`);
            },

            // Get current shipment ID
            getShipmentId: () => window._lastShipmentId
        };

        console.log('✅ Simple Auto Print loaded');
        console.log('🔧 Debug API: window.simpleAutoPrint');
        console.log('💡 Manual trigger: window.simpleAutoPrint.clickButtons()');
        console.log('💡 Disable auto: window.simpleAutoPrint.setAutoClick(false)');
        console.log('💡 Print service URL: localhost:8080/printinvoice → https://server:5555/print');
        console.log('');
    }

    // =============================================================================
    // INITIALIZATION
    // =============================================================================

    // Setup interceptors
    function initializeInterceptors() {
        setupShipmentIdInterceptor();
        setupPrintServiceUrlInterceptor();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeInterceptors);
    } else {
        initializeInterceptors();
    }

    console.log('📌 All interceptors ready');
})();
