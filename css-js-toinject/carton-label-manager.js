/**
 * Carton Label Manager - Consolidated
 *
 * Features:
 * 1. CSS Styling - Injects custom CSS for carton label formatting
 * 2. URL Redirect - Redirects carton label prints to new server (targeted, not all prints)
 * 3. Auto-Print - Auto-clicks View Invoice button on shipment creation
 *
 * Consolidated from:
 * - box-label-format-injector.js (CSS injection via window.open)
 * - carton-label-redirect.js (targeted URL redirect with content detection)
 * - simple-auto-print.js (auto-click View Invoice, manual carton label)
 *
 * EXCLUDED:
 * - Text doubling/enhancement (not needed)
 * - Silent print with JSPrintManager (not needed)
 * - Global redirect (dangerous, breaks other prints)
 */

(function() {
    'use strict';

    console.log('📦 Carton Label Manager - Loading...');

    // =============================================================================
    // CONFIGURATION
    // =============================================================================

    const CONFIG = {
        // Auto-click settings
        autoClickEnabled: true,
        autoClickDelay: 0,  // No delay - click immediately

        // URL redirect settings
        oldUrl: 'localhost:8080/printinvoice',
        newUrl: 'https://server:5555/print',  // Server name (no SSL issues)

        // Debug
        debugMode: true
    };

    // Custom CSS for carton labels
    const CARTON_LABEL_CSS = `
        /* Hide "Box No : 1" line */
        .top-info-section .text:first-child {
            display: none !important;
        }

        /* Hide "To:" text */
        .ship-info-section > .text:nth-child(1) {
            display: none !important;
        }

        /* Put phone numbers on same line */
        .top-info-section .text[style*="font-size: 30px"] {
            display: inline-block !important;
            margin-right: 20px !important;
        }

        /* Remove extra padding from top-info-section */
        .top-info-section {
            padding-top: 10px !important;
            padding-bottom: 20px !important;
            margin-bottom: 20px !important;
            text-align: center !important;
        }

        /* Ensure customer info section has proper spacing */
        .ship-info-section {
            padding: 0 20px !important;
        }

        /* Adjust spacing between phone numbers */
        .top-info-section .text[style*="font-size: 30px"]:last-of-type {
            margin-right: 0 !important;
        }
    `;

    // =============================================================================
    // FEATURE 1: CSS INJECTION (from box-label-format-injector.js)
    // =============================================================================

    // Track windows we've already processed
    const processedWindows = new WeakSet();

    /**
     * Try to inject CSS into a carton label window
     */
    function injectCartonLabelCSS(targetWindow, attemptNumber = 0) {
        try {
            if (!targetWindow || !targetWindow.document || processedWindows.has(targetWindow)) {
                return false;
            }

            const doc = targetWindow.document;

            // Debug: Log document state on first attempt
            if (attemptNumber === 1 && CONFIG.debugMode) {
                console.log('🔍 Checking window:', {
                    readyState: doc.readyState,
                    hasBoxLabel: !!doc.querySelector('.box-label-wrp'),
                    bodyChildren: doc.body?.children.length || 0
                });
            }

            // Only inject for box label windows
            const boxLabelElements = doc.querySelectorAll('.box-label-wrp');
            if (boxLabelElements.length === 0) {
                return false;
            }

            // Check if already injected
            if (doc.getElementById('carton-label-css-override')) {
                processedWindows.add(targetWindow);
                return true;
            }

            // Inject the CSS
            const style = doc.createElement('style');
            style.id = 'carton-label-css-override';
            style.textContent = CARTON_LABEL_CSS;
            doc.head.appendChild(style);
            processedWindows.add(targetWindow);

            console.log('✅ CARTON LABEL CSS INJECTED!', {
                boxLabelCount: boxLabelElements.length,
                readyState: doc.readyState,
                attemptNumber: attemptNumber
            });
            return true;

        } catch (e) {
            // Cross-origin or not ready yet – ignore
            return false;
        }
    }

    /**
     * Override window.open to inject CSS early
     */
    function setupWindowOpenInterceptor() {
        const originalWindowOpen = window.open;

        window.open = function(...args) {
            if (CONFIG.debugMode) {
                console.log('🪟 window.open intercepted:', args[0]);
            }

            // Call original window.open
            const newWindow = originalWindowOpen.apply(this, args);

            if (!newWindow) {
                return newWindow;
            }

            // Track injection state per window
            let injected = false;

            // Override print to ensure CSS is injected before print dialog
            try {
                if (typeof newWindow.print === 'function') {
                    const originalPrint = newWindow.print.bind(newWindow);
                    newWindow.print = function() {
                        const done = injectCartonLabelCSS(newWindow);
                        if (!done && !injected) {
                            // If document still loading, wait briefly
                            setTimeout(() => {
                                injectCartonLabelCSS(newWindow);
                                try { originalPrint(); } catch(_) {}
                            }, 50);
                            return;
                        }
                        injected = true;
                        return originalPrint();
                    };

                    // Also handle beforeprint event
                    newWindow.addEventListener?.('beforeprint', () => {
                        injectCartonLabelCSS(newWindow);
                        injected = true;
                    });
                }
            } catch (_) {
                // Cross-origin print override not possible
            }

            // Fire multiple rapid attempts to catch DOM before print()
            const delays = [50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 800, 850, 900, 950];
            delays.forEach((delay, index) => {
                setTimeout(() => {
                    if (!injected) {
                        const result = injectCartonLabelCSS(newWindow, index + 1);
                        if (result) injected = true;
                    }
                }, delay);
            });

            // Watch for DOM ready events
            try {
                newWindow.document?.addEventListener('DOMContentLoaded', () => {
                    if (!injected) {
                        console.log('📄 DOMContentLoaded event fired');
                        injectCartonLabelCSS(newWindow, 'DOMContentLoaded');
                        injected = true;
                    }
                });
                newWindow.document?.addEventListener('readystatechange', () => {
                    if (!injected && newWindow.document.readyState === 'complete') {
                        console.log('📄 Document ready state: complete');
                        injectCartonLabelCSS(newWindow, 'readystatechange');
                        injected = true;
                    }
                });
            } catch (_) {}

            // Add MutationObserver to watch for .box-label-wrp
            try {
                setTimeout(() => {
                    if (!injected && newWindow.document?.body) {
                        const observer = new MutationObserver((mutations) => {
                            if (injected) {
                                observer.disconnect();
                                return;
                            }

                            const hasBoxLabel = mutations.some(mutation => {
                                return Array.from(mutation.addedNodes).some(node => {
                                    return node.nodeType === 1 && (
                                        node.classList?.contains('box-label-wrp') ||
                                        node.querySelector?.('.box-label-wrp')
                                    );
                                });
                            });

                            if (hasBoxLabel) {
                                console.log('👀 MutationObserver detected .box-label-wrp');
                                if (injectCartonLabelCSS(newWindow, 'MutationObserver')) {
                                    injected = true;
                                    observer.disconnect();
                                }
                            }
                        });

                        observer.observe(newWindow.document.body, {
                            childList: true,
                            subtree: true
                        });

                        if (CONFIG.debugMode) {
                            console.log('👀 MutationObserver installed on print window');
                        }
                    }
                }, 10);
            } catch (_) {}

            return newWindow;
        };

        console.log('✅ window.open interceptor installed for CSS injection');
    }

    // =============================================================================
    // FEATURE 2: URL REDIRECT (from carton-label-redirect.js)
    // =============================================================================

    let redirectCount = 0;
    let lastRedirectTime = 0;

    /**
     * Check if request body contains carton label HTML
     * Only carton labels have the .box-label-wrp class
     */
    function isCartonLabelRequest(data) {
        if (!data || typeof data !== 'string') {
            return false;
        }

        const hasBoxLabelClass = data.includes('box-label-wrp');

        if (CONFIG.debugMode && hasBoxLabelClass) {
            console.log('🔍 Carton label content detected:', {
                hasBoxLabelClass: true,
                dataLength: data.length
            });
        }

        return hasBoxLabelClass;
    }

    /**
     * Install jQuery $.post interceptor
     */
    function installJQueryRedirectInterceptor() {
        if (typeof $ === 'undefined' || !$.post) {
            return false;
        }

        const originalPost = $.post;

        $.post = function(url, data, success, dataType) {
            // Check if this is a print request to old server
            if (typeof url === 'string' && url.includes(CONFIG.oldUrl)) {

                // Check if this is specifically a CARTON LABEL print
                if (isCartonLabelRequest(data)) {
                    redirectCount++;
                    lastRedirectTime = Date.now();

                    console.log('═══════════════════════════════════════════');
                    console.log('📦 CARTON LABEL DETECTED - REDIRECTING');
                    console.log('   Redirect #' + redirectCount);
                    console.log('   From:', url);
                    console.log('   To:', CONFIG.newUrl);
                    console.log('   Size:', data.length, 'bytes');
                    console.log('═══════════════════════════════════════════');

                    // Redirect to new server
                    return originalPost.call(this, CONFIG.newUrl, data, success, dataType);
                } else {
                    console.log('ℹ️ Print request (NOT carton label - passing through)');
                }
            }

            // All other requests pass through unchanged
            return originalPost.apply(this, arguments);
        };

        console.log('✅ jQuery $.post interceptor installed (targeted redirect)');
        return true;
    }

    /**
     * Install Fetch API interceptor
     */
    function setupFetchRedirectInterceptor() {
        const originalFetch = window.fetch;

        window.fetch = async function(...args) {
            let url = args[0];
            let options = args[1] || {};

            // Handle Request object
            if (url instanceof Request) {
                url = url.url;
            }

            // Check if this is a print request
            if (typeof url === 'string' && url.includes(CONFIG.oldUrl)) {
                let body = options.body;

                if (body && typeof body === 'string' && isCartonLabelRequest(body)) {
                    redirectCount++;
                    lastRedirectTime = Date.now();

                    console.log('═══════════════════════════════════════════');
                    console.log('📦 CARTON LABEL DETECTED (FETCH) - REDIRECTING');
                    console.log('   Redirect #' + redirectCount);
                    console.log('   From:', url);
                    console.log('   To:', CONFIG.newUrl);
                    console.log('═══════════════════════════════════════════');

                    args[0] = CONFIG.newUrl;
                }
            }

            return originalFetch.apply(this, args);
        };

        console.log('✅ Fetch API interceptor installed (targeted redirect)');
    }

    /**
     * Install XMLHttpRequest interceptor
     */
    function setupXHRRedirectInterceptor() {
        const originalXHROpen = XMLHttpRequest.prototype.open;
        const originalXHRSend = XMLHttpRequest.prototype.send;

        XMLHttpRequest.prototype.open = function(method, url, ...rest) {
            this._interceptor_url = url;
            this._interceptor_method = method;
            return originalXHROpen.apply(this, [method, url, ...rest]);
        };

        XMLHttpRequest.prototype.send = function(body) {
            // Check if this is a print request
            if (this._interceptor_url && this._interceptor_url.includes(CONFIG.oldUrl)) {

                if (body && isCartonLabelRequest(body)) {
                    redirectCount++;
                    lastRedirectTime = Date.now();

                    console.log('═══════════════════════════════════════════');
                    console.log('📦 CARTON LABEL DETECTED (XHR) - REDIRECTING');
                    console.log('   Redirect #' + redirectCount);
                    console.log('   To:', CONFIG.newUrl);
                    console.log('═══════════════════════════════════════════');

                    // Reopen with new URL
                    this._interceptor_url = CONFIG.newUrl;
                    originalXHROpen.call(this, this._interceptor_method, CONFIG.newUrl, true);
                }
            }

            return originalXHRSend.apply(this, arguments);
        };

        console.log('✅ XMLHttpRequest interceptor installed (targeted redirect)');
    }

    /**
     * Track button clicks for debugging
     */
    function setupButtonClickTracking() {
        document.addEventListener('click', function(e) {
            const target = e.target;

            if (target && target.getAttribute) {
                const dataValue = target.getAttribute('data-value');

                if (dataValue === 'pboxlabel') {
                    console.log('═══════════════════════════════════════════');
                    console.log('🖱️ CARTON LABEL BUTTON CLICKED');
                    console.log('   Next POST request will be checked...');
                    console.log('═══════════════════════════════════════════');
                } else if (dataValue === 'pinvoice') {
                    console.log('📄 View Invoice button clicked (will NOT be redirected)');
                } else if (dataValue === 'pslip') {
                    console.log('📋 Packing Slip button clicked (will NOT be redirected)');
                }
            }
        }, true);
    }

    // =============================================================================
    // FEATURE 3: AUTO-PRINT (from simple-auto-print.js)
    // =============================================================================

    // Shipment ID tracking
    window._lastShipmentId = null;

    // Print state guards
    let _printInProgress = false;
    let _lastPrintedShipmentId = null;
    let _lastPrintTimestamp = 0;
    const PRINT_COOLDOWN_MS = 4000;

    /**
     * Setup API interceptor to capture shipment ID
     */
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

    /**
     * Auto-click View Invoice button
     */
    async function clickViewInvoiceButton() {
        console.log('═══════════════════════════════════════════');
        console.log('🖱️ Auto-clicking View Invoice button...');
        console.log('═══════════════════════════════════════════');

        try {
            const viewInvoiceBtn = document.querySelector('button[data-value="pinvoice"]');

            if (!viewInvoiceBtn) {
                throw new Error('View Invoice button not found');
            }

            console.log('   Button found:', {
                visible: viewInvoiceBtn.offsetParent !== null,
                disabled: viewInvoiceBtn.disabled
            });

            viewInvoiceBtn.click();
            console.log('✅ View Invoice button clicked');
            console.log('💡 For carton label: click button manually (will auto-redirect)');

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
            _lastPrintedShipmentId = window._lastShipmentId || _lastPrintedShipmentId;
            _lastPrintTimestamp = Date.now();
            _printInProgress = false;
        }
    }

    /**
     * Handle modal appearance and trigger auto-print
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
        }

        // Duplicate suppression
        if (_printInProgress) {
            console.log('🚫 Print already in progress – modal trigger ignored');
            return;
        }
        if (shipmentId && _lastPrintedShipmentId === shipmentId && (Date.now() - _lastPrintTimestamp) < PRINT_COOLDOWN_MS) {
            console.log(`🛑 Duplicate shipment detected within cooldown – skipping`);
            return;
        }

        // Mark as in progress
        _printInProgress = true;

        // Auto-click with delay
        if (CONFIG.autoClickEnabled) {
            console.log('⏳ Scheduling auto-click...');
            setTimeout(() => {
                clickViewInvoiceButton();
            }, CONFIG.autoClickDelay);
        }
    }

    // =============================================================================
    // INITIALIZATION
    // =============================================================================

    function initialize() {
        console.log('🚀 Initializing Carton Label Manager...');

        // Setup CSS injection
        setupWindowOpenInterceptor();

        // Setup URL redirect
        setupFetchRedirectInterceptor();
        setupXHRRedirectInterceptor();
        setupButtonClickTracking();

        // Setup jQuery redirect (wait for jQuery)
        if (!installJQueryRedirectInterceptor()) {
            let attempts = 0;
            const maxAttempts = 50;
            const checkInterval = setInterval(() => {
                attempts++;
                if (installJQueryRedirectInterceptor() || attempts >= maxAttempts) {
                    clearInterval(checkInterval);
                }
            }, 100);
        }

        // Setup auto-print
        setupShipmentIdInterceptor();

        console.log('═══════════════════════════════════════════');
        console.log('✅ CARTON LABEL MANAGER READY');
        console.log('   Features:');
        console.log('   ✅ CSS Formatting (hides Box No, To:, etc.)');
        console.log('   ✅ URL Redirect (carton labels only)');
        console.log('   ✅ Auto-Print (View Invoice)');
        console.log('═══════════════════════════════════════════');
    }

    // =============================================================================
    // GLOBAL API
    // =============================================================================

    window.cartonLabelManager = {
        config: CONFIG,

        // Manual triggers
        handleModal: handleModalAppearance,
        clickViewInvoice: clickViewInvoiceButton,

        // Stats
        redirectCount: () => redirectCount,
        lastRedirect: () => lastRedirectTime ? new Date(lastRedirectTime).toLocaleString() : 'Never',

        // Configuration
        setAutoClick: (enabled) => {
            CONFIG.autoClickEnabled = enabled;
            console.log(`🔧 Auto-click ${enabled ? 'enabled' : 'disabled'}`);
        },

        stats: function() {
            console.log('═══════════════════════════════════════════');
            console.log('📊 CARTON LABEL MANAGER STATS');
            console.log('   Auto-click:', CONFIG.autoClickEnabled ? 'ENABLED' : 'DISABLED');
            console.log('   Total redirects:', redirectCount);
            console.log('   Last redirect:', this.lastRedirect());
            console.log('   Redirect from:', CONFIG.oldUrl);
            console.log('   Redirect to:', CONFIG.newUrl);
            console.log('═══════════════════════════════════════════');
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    console.log('💡 Debug: window.cartonLabelManager.stats()');

})();