/**
 * Simple Auto Print - Just Click Buttons
 *
 * Simplified approach:
 * 1. Wait for shipment modal to appear
 * 2. Click "Packing Slip" button
 * 3. Click "Print Carton Label" button
 * 4. Force PrintWithUtility = false for Carton Label
 */

(function() {
    'use strict';

    console.log('🖨️ Simple Auto Print - Loading...');

    // =============================================================================
    // CONFIGURATION
    // =============================================================================

    const CONFIG = {
        autoClickEnabled: true,
        autoClickDelay: 2000,  // Wait 2s after modal appears

        // Delay between clicking buttons (ms)
        delayBetweenClicks: 500,

        // Wait time after clicking carton label before disabling context (ms)
        // This ensures the button's async operations complete while context is still enabled
        contextCleanupDelay: 2000,

        // Force PrintWithUtility to false for carton labels
        forcePrintWithUtilityFalse: true,

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
    // PRINTWITHTILITY OVERRIDE
    // =============================================================================

    /**
     * Flag to track if we're currently printing carton label
     * This allows the override to only apply to carton labels, not packing slips
     */
    let isCartonLabelContext = false;

    /**
     * Context-aware override for PrintWithUtility config
     * Only returns false when we're in carton label context
     */
    function overridePrintWithUtility() {
        if (CONFIG.forcePrintWithUtilityFalse && typeof common !== 'undefined' && common.getConfigByName) {
            const originalGetConfig = common.getConfigByName;

            common.getConfigByName = function(configName, defaultValue) {
                // Intercept PrintWithUtility and check context
                if (configName === 'PrintWithUtility') {
                    if (isCartonLabelContext) {
                        console.log('🔧 PrintWithUtility intercepted - forcing FALSE for CARTON LABEL');
                        return false;  // Force false for carton label
                    } else {
                        console.log('🔧 PrintWithUtility intercepted - using ORIGINAL for PACKING SLIP');
                        return originalGetConfig.call(this, configName, defaultValue);
                    }
                }

                // All other configs pass through normally
                return originalGetConfig.call(this, configName, defaultValue);
            };

            console.log('✅ PrintWithUtility context-aware override installed');
            return true;
        }
        return false;
    }

    /**
     * Enable carton label context (makes override return false)
     */
    function enableCartonLabelContext() {
        isCartonLabelContext = true;
        console.log('🏷️ Carton label context ENABLED (PrintWithUtility will be FALSE)');
    }

    /**
     * Disable carton label context (makes override use original value)
     */
    function disableCartonLabelContext() {
        isCartonLabelContext = false;
        console.log('📄 Carton label context DISABLED (PrintWithUtility will use original)');
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
            // STEP 0: Install context-aware override (but keep context disabled for now)
            console.log('\n🔧 STEP 0: Installing context-aware override...');
            overridePrintWithUtility();
            disableCartonLabelContext();  // Ensure disabled for packing slip

            // STEP 1: Click Packing Slip button (with context disabled)
            console.log('\n📄 STEP 1: Clicking Packing Slip button...');
            console.log('   Context: DISABLED (will use original PrintWithUtility value)');

            const packingSlipBtn = document.getElementById('btnPrintPackSlip');

            if (!packingSlipBtn) {
                throw new Error('Packing Slip button not found (id: btnPrintPackSlip)');
            }

            console.log('   Button found:', {
                visible: packingSlipBtn.offsetParent !== null,
                disabled: packingSlipBtn.disabled,
                hasOnclick: !!packingSlipBtn.onclick
            });

            packingSlipBtn.click();
            console.log('✅ Packing Slip button clicked');

            // Wait before next button
            console.log(`⏳ Waiting ${CONFIG.delayBetweenClicks}ms before next button...`);
            await new Promise(resolve => setTimeout(resolve, CONFIG.delayBetweenClicks));

            // STEP 2: Enable carton label context
            console.log('\n📦 STEP 2: Enabling Carton Label context...');
            enableCartonLabelContext();

            // STEP 3: Click Carton Label button (with context enabled)
            console.log('📦 STEP 3: Clicking Carton Label button...');
            console.log('   Context: ENABLED (will force PrintWithUtility to FALSE)');

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

            // STEP 4: Wait for button's async operations to complete before cleanup
            console.log('\n🔧 STEP 4: Waiting for carton label operations to complete...');
            console.log(`   (Keeping context ENABLED for ${CONFIG.contextCleanupDelay}ms)`);

            // Wait for the button's async operations to complete
            await new Promise(resolve => setTimeout(resolve, CONFIG.contextCleanupDelay));

            // Now it's safe to disable context
            console.log('🔧 STEP 5: Cleanup...');
            disableCartonLabelContext();

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

            // Make sure to disable context on error
            disableCartonLabelContext();

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

            // Context control (for debugging)
            enableCartonContext: enableCartonLabelContext,
            disableCartonContext: disableCartonLabelContext,
            isCartonContext: () => isCartonLabelContext,

            // Utilities
            setAutoClick: (enabled) => {
                CONFIG.autoClickEnabled = enabled;
                console.log(`🔧 Auto-click ${enabled ? 'enabled' : 'disabled'}`);
            },

            overridePrintUtility: overridePrintWithUtility,

            // Get current shipment ID
            getShipmentId: () => window._lastShipmentId
        };

        console.log('✅ Simple Auto Print loaded');
        console.log('🔧 Debug API: window.simpleAutoPrint');
        console.log('💡 Manual trigger: window.simpleAutoPrint.clickButtons()');
        console.log('💡 Disable auto: window.simpleAutoPrint.setAutoClick(false)');
        console.log('💡 Check context: window.simpleAutoPrint.isCartonContext()');
        console.log('');
    }

    // =============================================================================
    // INITIALIZATION
    // =============================================================================

    // Setup interceptor
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupShipmentIdInterceptor);
    } else {
        setupShipmentIdInterceptor();
    }

    console.log('📌 Shipment ID interceptor ready');
})();
