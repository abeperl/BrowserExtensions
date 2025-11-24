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
        autoClickEnabled: true,  // Set to true to enable auto-clicking buttons
        autoClickDelay: 0,  // No delay - click immediately

        // Delay between clicking buttons (ms)
        delayBetweenClicks: 500,

        debugMode: true
    };

    // =============================================================================
    // PRINT STATE GUARDS (prevent endless loops / duplicate window spawns)
    // =============================================================================
    // Tracks whether a print sequence is currently executing
    let _printInProgress = false;
    // Last shipment ID successfully processed (used for cooldown duplicate suppression)
    let _lastPrintedShipmentId = null;
    // Timestamp of last completed print (ms since epoch)
    let _lastPrintTimestamp = 0;
    // Cooldown window to ignore duplicate modal triggers for same shipment
    const PRINT_COOLDOWN_MS = 4000; // 4s – adjust if backend timing changes

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
     * Store original $.post for restoration
     */
    let _originalPost = null;
    let _interceptorActive = false;

    /**
     * Install URL interceptor temporarily for Carton Label printing
     * ONLY redirects during Carton Label operation, then auto-restores
     * Changes: http://localhost:8080/printinvoice -> https://server:5555/print
     */
    function installPrintServiceUrlInterceptor() {
        if (typeof $ === 'undefined' || !$.post) {
            console.warn('⚠️ jQuery not available, cannot install interceptor');
            return false;
        }

        if (_interceptorActive) {
            console.log('ℹ️ Print service URL interceptor already active');
            return true;
        }

        // Store original $.post
        _originalPost = $.post;

        $.post = function(url, data, success, dataType) {
            console.log('🔍 $.post intercepted:', {
                url: url,
                dataType: typeof data,
                dataLength: typeof data === 'string' ? data.length : 'N/A',
                hasStyle: typeof data === 'string' ? data.includes('<style') : false,
                hasHtml: typeof data === 'string' ? data.includes('<html') : false
            });

            // Only intercept if this is the exact print service call
            // Check: URL is localhost:8080/printinvoice AND data contains HTML content (CSS styles)
            if (url && url.includes('localhost:8080/printinvoice') &&
                typeof data === 'string' && data.includes('<style')) {

                const newUrl = 'https://server:5555/print';
                console.log(`🔀 Redirecting print service URL (Carton Label):`);
                console.log(`   From: ${url}`);
                console.log(`   To: ${newUrl}`);
                console.log(`   Data size: ${data.length} bytes`);

                // Call original $.post with new URL
                const result = _originalPost.call(this, newUrl, data, success, dataType);

                // Auto-restore after request completes
                result.always(() => {
                    console.log('🔄 Carton Label request complete, restoring original $.post');
                    restorePrintServiceUrl();
                });

                return result;
            }

            // Pass through all other POST requests unchanged
            return _originalPost.apply(this, arguments);
        };

        _interceptorActive = true;
        console.log('✅ Print service URL interceptor installed (temporary)');
        console.log('   Will auto-restore after Carton Label completes');
        return true;
    }

    /**
     * Restore original $.post function
     */
    function restorePrintServiceUrl() {
        if (!_interceptorActive) {
            return;
        }

        if (_originalPost && typeof $ !== 'undefined') {
            $.post = _originalPost;
            _interceptorActive = false;
            console.log('✅ Original $.post restored - interceptor removed');
        }
    }

    // =============================================================================
    // BUTTON CLICKING
    // =============================================================================

    /**
     * Click the print buttons in sequence
     * Now only clicks View Invoice (auto-click)
     */
    async function clickPrintButtons() {
        console.log('═══════════════════════════════════════════');
        console.log('🖱️ Starting button click sequence...');
        console.log('═══════════════════════════════════════════');
        // Note: _printInProgress is already set to true in handleModalAppearance
        // This prevents duplicate modal triggers from scheduling multiple timeouts

        try {
            // STEP 1: Click View Invoice button ONLY
            console.log('\n📄 STEP 1: Clicking View Invoice button...');
            console.log('   (Box Label removed from auto-click - use manual click for redirection)');

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

            console.log('\n✅ Auto-click complete (View Invoice only)!');
            console.log('💡 To print Box Label with redirection: click button manually');
            console.log('═══════════════════════════════════════════\n');

            // Show success notification if OverlayManager available
            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.success({
                    message: 'Invoice opened',
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
        } finally {
            // Record last processed shipment & release in-progress flag
            _lastPrintedShipmentId = window._lastShipmentId || _lastPrintedShipmentId;
            _lastPrintTimestamp = Date.now();
            _printInProgress = false;
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

        // Duplicate / cooldown suppression - CHECK BEFORE SCHEDULING TIMEOUT
        if (_printInProgress) {
            console.log('🚫 Print already in progress – modal trigger ignored');
            return;
        }
        if (shipmentId && _lastPrintedShipmentId === shipmentId && (Date.now() - _lastPrintTimestamp) < PRINT_COOLDOWN_MS) {
            console.log(`🛑 Duplicate shipment ${shipmentId} detected within ${PRINT_COOLDOWN_MS}ms cooldown – skipping auto print`);
            return;
        }

        // Auto-click if enabled
        if (CONFIG.autoClickEnabled) {
            // Set in-progress flag IMMEDIATELY to prevent duplicate scheduling
            _printInProgress = true;
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

            // Interceptors (manual control)
            installInterceptor: installPrintServiceUrlInterceptor,
            restoreInterceptor: restorePrintServiceUrl,
            isInterceptorActive: () => _interceptorActive,
            // State inspection helpers
            isPrinting: () => _printInProgress,
            lastPrintedShipmentId: () => _lastPrintedShipmentId,
            cooldownMs: PRINT_COOLDOWN_MS,
            // Manual reset (debugging)
            resetState: () => {
                _printInProgress = false;
                _lastPrintedShipmentId = null;
                _lastPrintTimestamp = 0;
                console.log('🔧 Print state reset (in-progress=false, lastPrintedShipmentId cleared)');
            },

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
        console.log('💡 URL Redirection: ONLY active during Carton Label operation');
        console.log('💡 View Invoice: NO redirection (uses original window.open)');
        console.log('💡 Duplicate protection: cooldown', PRINT_COOLDOWN_MS + 'ms, re-entry guarded');
        console.log('');
    }

    // =============================================================================
    // MANUAL BUTTON CLICK INTERCEPTOR
    // =============================================================================

    /**
     * Intercept manual clicks on Box Label button to enable redirection
     */
    function setupManualBoxLabelInterceptor() {
        // Find the Box Label button
        const checkForButton = () => {
            const cartonLabelBtn = document.getElementById('box-label');
            if (cartonLabelBtn) {
                console.log('📦 Box Label button found, installing click interceptor');
                console.log('   Button details:', {
                    id: cartonLabelBtn.id,
                    tagName: cartonLabelBtn.tagName,
                    hasOnclick: !!cartonLabelBtn.onclick,
                    dataValue: cartonLabelBtn.getAttribute('data-value')
                });
                
                // Store original onclick handler if exists
                const originalOnclick = cartonLabelBtn.onclick;
                
                // Replace with our interceptor - use addEventListener for better reliability
                cartonLabelBtn.addEventListener('click', function(e) {
                    console.log('🖱️ Manual Box Label click detected (via addEventListener)');
                    console.log('   Event details:', {
                        target: e.target.id,
                        currentTarget: e.currentTarget.id,
                        interceptorActive: _interceptorActive
                    });
                    
                    // Install URL interceptor BEFORE original handler executes
                    console.log('🔀 Installing URL interceptor for manual Box Label click');
                    const installed = installPrintServiceUrlInterceptor();
                    console.log('   Interceptor installed:', installed);
                }, true); // Use capture phase to run before other handlers
                
                // Also replace onclick if it exists
                if (originalOnclick) {
                    cartonLabelBtn.onclick = function(e) {
                        console.log('🖱️ Manual Box Label click detected (via onclick)');
                        
                        // Install URL interceptor BEFORE original handler executes
                        console.log('🔀 Installing URL interceptor for manual Box Label click');
                        installPrintServiceUrlInterceptor();
                        
                        // Call original handler
                        originalOnclick.call(this, e);
                    };
                }
                
                console.log('✅ Manual Box Label interceptor installed (both addEventListener + onclick)');
                return true;
            }
            return false;
        };
        
        // Try immediately
        if (!checkForButton()) {
            // If not found, watch for modal appearance
            const observer = new MutationObserver(() => {
                if (checkForButton()) {
                    observer.disconnect();
                }
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        }
    }

    // =============================================================================
    // INITIALIZATION
    // =============================================================================

    // Setup shipment ID interceptor only (NOT URL interceptor - that's installed on-demand)
    function initializeInterceptors() {
        setupShipmentIdInterceptor();
        setupManualBoxLabelInterceptor();
        console.log('✅ Shipment ID interceptor ready');
        console.log('✅ Manual Box Label click interceptor ready');
        console.log('💡 URL interceptor will be installed automatically on manual Box Label click');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeInterceptors);
    } else {
        initializeInterceptors();
    }

    console.log('📌 Shipment ID interceptor ready');
    console.log('📌 URL interceptor: ON-DEMAND (manual Box Label click only)');
    console.log('📌 Auto-click: View Invoice ONLY (Box Label requires manual click)');
})();
