/**
 * Browser Extension JS Router
 * Routes to appropriate JS files based on page content detection
 * Handles URLs with hash fragments that extensions can't match directly
 */

(function() {
    'use strict';

    console.log('🚀 Browser Extension JS Router - Starting...');

    // Configuration: Map URL patterns to functions
    const ROUTES = [
        {
            name: 'Process Personalized Order Items Route',
            pattern: /^#outbound\/ProcessPersonalizedOrderItems(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #outbound/ProcessPersonalizedOrderItems route');

                // Inject CSS to enlarge scan modal
                function injectScanModalStyles() {
                    if (document.getElementById('scan-modal-enlarger-styles')) {
                        return;
                    }

                    const style = document.createElement('style');
                    style.id = 'scan-modal-enlarger-styles';
                    style.textContent = `
                        /* Make the modal much bigger */
                        #scan-product-modal.modal-small {
                            width: 90vw !important;
                            max-width: 1200px !important;
                            min-height: 60vh !important;
                        }

                        /* Make modal header bigger */
                        #scan-product-modal .modal-head {
                            padding: 30px 40px !important;
                            min-height: 80px !important;
                        }

                        #scan-product-modal .modal-head h4 {
                            font-size: 32px !important;
                            font-weight: 600 !important;
                        }

                        #scan-product-modal .modal-head .close-icon-btn {
                            width: 50px !important;
                            height: 50px !important;
                            font-size: 32px !important;
                        }

                        /* Make modal body bigger */
                        #scan-product-modal .page-body {
                            padding: 40px 50px !important;
                            min-height: 400px !important;
                        }

                        #scan-product-modal .form-group {
                            margin-bottom: 40px !important;
                        }

                        /* Make labels bigger */
                        #scan-product-modal .form-group label {
                            font-size: 24px !important;
                            font-weight: 600 !important;
                            margin-bottom: 15px !important;
                            display: block !important;
                        }

                        /* Make input boxes MUCH bigger */
                        #scan-product-modal .form-control,
                        #scan-product-modal #product-scan,
                        #scan-product-modal #status-scan {
                            height: 80px !important;
                            font-size: 28px !important;
                            padding: 20px 25px !important;
                            border: 3px solid #ccc !important;
                            border-radius: 8px !important;
                            font-weight: 500 !important;
                        }

                        #scan-product-modal .form-control:focus {
                            border-color: #007bff !important;
                            box-shadow: 0 0 0 4px rgba(0, 123, 255, 0.25) !important;
                        }

                        /* Make footer and buttons bigger */
                        #scan-product-modal .page-foot {
                            padding: 30px 40px !important;
                        }

                        #scan-product-modal .modal-box-button {
                            height: 70px !important;
                            min-width: 180px !important;
                            font-size: 24px !important;
                            padding: 20px 40px !important;
                            font-weight: 600 !important;
                        }

                        /* Center modal backdrop */
                        #_modal_block_ui.loader_block_ui {
                            display: flex !important;
                            align-items: center !important;
                            justify-content: center !important;
                            position: fixed !important;
                            top: 0 !important;
                            left: 0 !important;
                            width: 100vw !important;
                            height: 100vh !important;
                            z-index: 9999 !important;
                        }

                        /* Center modal - reset positioning */
                        #_modal_block_ui .panel-box,
                        #scan-product-modal {
                            position: relative !important;
                            top: auto !important;
                            left: auto !important;
                            right: auto !important;
                            bottom: auto !important;
                            transform: none !important;
                            margin: auto !important;
                        }

                        /* Visual feedback */
                        #scan-product-modal #product-scan {
                            background-color: #f0f8ff !important;
                        }

                        #scan-product-modal #status-scan {
                            background-color: #fff5f0 !important;
                        }
                    `;
                    document.head.appendChild(style);
                    console.log('✅ Injected scan modal enlarger styles');
                }

                // Inject styles immediately
                injectScanModalStyles();

                // Watch for modal to be added to DOM
                const modalObserver = new MutationObserver(() => {
                    const modal = document.getElementById('scan-product-modal');
                    if (modal && !document.getElementById('scan-modal-enlarger-styles')) {
                        injectScanModalStyles();
                    }
                });

                modalObserver.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                // ========== STATUS DROPDOWN FEATURE ==========
                if (typeof addStatusDropdown === 'function' &&
                    typeof setupStatusAutoFill === 'function') {

                    console.log('📋 Status dropdown feature enabled');

                    // Add dropdown to page tools
                    const pageToolsObserver = new MutationObserver((mutations, obs) => {
                        const pageTools = document.querySelector('.page-tools');
                        if (pageTools) {
                            console.log('✅ .page-tools found, adding dropdown');
                            addStatusDropdown();
                            obs.disconnect();
                        }
                    });

                    pageToolsObserver.observe(document.body, {
                        childList: true,
                        subtree: true
                    });

                    // Try immediately
                    setTimeout(() => {
                        const pageTools = document.querySelector('.page-tools');
                        if (pageTools) {
                            addStatusDropdown();
                            pageToolsObserver.disconnect();
                        }
                    }, 500);

                    // Setup auto-fill for status-scan input and auto-submit for product-scan
                    const modalInputsObserver = new MutationObserver((mutations, obs) => {
                        const statusInput = document.getElementById('status-scan');
                        const productInput = document.getElementById('product-scan');

                        if (statusInput && productInput) {
                            console.log('✅ Both inputs found, setting up auto-fill and auto-submit');
                            setupStatusAutoFill();

                            // Setup auto-submit if function is available
                            if (typeof setupProductScanAutoSubmit === 'function') {
                                setupProductScanAutoSubmit();
                            }

                            obs.disconnect();
                        }
                    });

                    modalInputsObserver.observe(document.body, {
                        childList: true,
                        subtree: true
                    });

                    // Try immediately
                    setTimeout(() => {
                        const statusInput = document.getElementById('status-scan');
                        const productInput = document.getElementById('product-scan');

                        if (statusInput && productInput) {
                            setupStatusAutoFill();

                            if (typeof setupProductScanAutoSubmit === 'function') {
                                setupProductScanAutoSubmit();
                            }

                            modalInputsObserver.disconnect();
                        }
                    }, 500);
                } else {
                    console.warn('⚠️ Status dropdown functions not loaded');
                }

                // ========== SNACKBAR INTERCEPTOR ==========
                if (typeof OverlayManager !== 'undefined') {
                    console.log('🎯 Snackbar interceptor feature enabled');

                    // Helper function to clear inputs and refocus on error (shared with API interceptor)
                    window._handleScanError = function() {
                        const productInput = document.getElementById('product-scan');
                        const statusInput = document.getElementById('status-scan');

                        if (productInput) {
                            productInput.value = '';
                            console.log('🧹 Cleared product-scan input');
                        }

                        if (statusInput) {
                            statusInput.value = '';
                            console.log('🧹 Cleared status-scan input');
                        }

                        // Refocus on product-scan after a short delay
                        setTimeout(() => {
                            if (productInput) {
                                productInput.focus();
                                console.log('🎯 Refocused on product-scan input');
                            }
                        }, 100);
                    };

                    // Intercept snackbar.show calls and replace with OverlayManager
                    function interceptSnackbar() {
                        if (typeof window.snackbar !== 'undefined' && window.snackbar.show) {
                            console.log('✅ snackbar.show found, installing interceptor');

                            // Store original in case we need fallback
                            // const originalSnackbarShow = window.snackbar.show;

                            window.snackbar.show = function(message, type) {
                                console.log('📢 snackbar.show intercepted:', message, type);

                                // Map snackbar types to OverlayManager types
                                // cs-danger -> error, cs-success -> success, cs-warning -> warning, default -> info
                                let overlayType = 'info';
                                if (type === 'cs-danger' || type === 'danger') {
                                    overlayType = 'error';
                                } else if (type === 'cs-success' || type === 'success') {
                                    overlayType = 'success';
                                } else if (type === 'cs-warning' || type === 'warning') {
                                    overlayType = 'warning';
                                }

                                // Show using OverlayManager instead
                                OverlayManager[overlayType]({
                                    message: message,
                                    duration: 3000
                                });

                                console.log(`✅ Replaced snackbar with ${overlayType} overlay:`, message);

                                // If it's an error or warning, clear inputs and refocus
                                if (overlayType === 'error' || overlayType === 'warning') {
                                    window._handleScanError();
                                }

                                // Don't call original snackbar
                                // If you want to call original as fallback, uncomment:
                                // return originalSnackbarShow.call(this, message, type);
                            };

                            console.log('✅ snackbar.show interceptor installed');
                            return true;
                        }
                        return false;
                    }

                    // Try to intercept immediately
                    if (!interceptSnackbar()) {
                        // snackbar not ready yet, wait for it
                        console.log('⏳ Waiting for snackbar to be available...');

                        const checkSnackbarInterval = setInterval(() => {
                            if (interceptSnackbar()) {
                                clearInterval(checkSnackbarInterval);
                            }
                        }, 100);

                        // Give up after 10 seconds
                        setTimeout(() => {
                            clearInterval(checkSnackbarInterval);
                            if (typeof window.snackbar === 'undefined' || !window.snackbar.show) {
                                console.warn('⚠️ snackbar not found after 10 seconds');
                            }
                        }, 10000);
                    }
                } else {
                    console.warn('⚠️ OverlayManager not loaded - snackbar interception disabled');
                }

                // ========== MODAL INTERCEPTOR ==========
                if (typeof window.modalInterceptor !== 'undefined' && typeof OverlayManager !== 'undefined') {
                    console.log('🎯 Modal interceptor feature enabled');
                    console.log('💡 Native modals will be replaced with OverlayManager displays');
                    console.log('💡 Disable with: window.modalInterceptor.disable()');
                    console.log('💡 Restore with: window.modalInterceptor.restore()');
                } else {
                    console.warn('⚠️ Modal interceptor or OverlayManager not loaded');
                }
            },
            description: 'Status dropdown and overlay features for personalized order items'
        },
        {
            name: 'Shipment Details Route',
            pattern: /^#Outbound\/shipmentdetails(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #Outbound/shipmentdetails route');

                // EARLY: Disable TabManager reuse for this route so print windows are true popups
                // Rationale: Reused tabs caused late CSS injection after print dialog opened.
                if (typeof TabManager !== 'undefined') {
                    try {
                        TabManager.disable();
                        console.log('⚙️ TabManager disabled for Shipment Details route to allow early print CSS injection');
                    } catch(e) { /* ignore */ }
                }

                // ========== BOX LABEL FORMAT INJECTOR ==========
                if (!window._boxLabelMonitorInstalled) {
                    console.log('📋 Installing box label format monitor...');

                    const customCSS = `
                        /* Hide "Box No" line in top section */
                        .top-info-section .text:first-child { display: none !important; }
                        /* Hide Customer ID (first child) */
                        .ship-info-section > .text.sm-text:first-child { display: none !important; }
                        /* Hide "To:" (second child after Customer ID) */
                        .ship-info-section > .text:nth-child(2) { display: none !important; }
                        /* Enlarge Tel line (any remaining sm-text elements) */
                        .ship-info-section .text.sm-text { font-size: 40px !important; margin-top: 10px !important; }
                        /* Adjust top section padding */
                        .top-info-section { padding-top: 10px !important; padding-bottom: 20px !important; margin-bottom: 20px !important; text-align: center !important; }
                        /* Ensure ship section horizontal padding retained */
                        .ship-info-section { padding: 0 20px !important; }
                    `;

                    const processedWindows = new WeakSet();

                    // Make injector globally accessible (no window.open wrapping)
                    window._injectBoxLabelCSS = function(targetWindow) {
                        try {
                            if (!targetWindow || !targetWindow.document || processedWindows.has(targetWindow)) {
                                return false;
                            }

                            const doc = targetWindow.document;
                            
                            if (doc.readyState === 'loading') {
                                return false;
                            }

                            const isBoxLabelWindow = doc.querySelector('.box-label-wrp') !== null;
                            
                            if (isBoxLabelWindow && !doc.getElementById('box-label-format-override')) {
                                const style = doc.createElement('style');
                                style.id = 'box-label-format-override';
                                style.textContent = customCSS;
                                doc.head.appendChild(style);
                                console.log('✅ BOX LABEL CSS INJECTED');
                                processedWindows.add(targetWindow);
                                return true;
                            }
                        } catch (error) {
                            // Silent fail for cross-origin
                        }
                        return false;
                    };

                    window._boxLabelMonitorInstalled = true;
                    console.log('✅ Box label format monitor function installed (no window.open wrapping)');
                } else {
                    console.log('ℹ️ Box label format monitor already installed');
                }

                // Function to inject CSS overrides into document or iframe
                function injectEnhancedStyles(doc) {
                    // Check if already injected
                    if (doc.getElementById('placard-enhancement-styles')) {
                        return;
                    }

                    const style = doc.createElement('style');
                    style.id = 'placard-enhancement-styles';
                    style.textContent = `
                        /* Double text sizes and make bold */
                        .box-label-wrp .text {
                            font-size: 60px !important;
                            font-weight: bold !important;
                        }

                        .box-label-wrp .text.sm-text {
                            font-size: 50px !important;
                            font-weight: bold !important;
                        }

                        .box-label-wrp .order-ref-text {
                            font-size: 56px !important;
                            font-weight: bold !important;
                        }

                        .box-label-wrp .carton-count {
                            font-size: 80px !important;
                            font-weight: bold !important;
                        }

                        .top-info-section .text {
                            font-size: 60px !important;
                            font-weight: bold !important;
                        }

                        .ship-info-section .text {
                            font-size: 60px !important;
                            font-weight: bold !important;
                        }
                    `;
                    doc.head.appendChild(style);
                    console.log('✅ Injected enhanced placard styles');
                }

                // Inject into main document
                injectEnhancedStyles(document);

                // Set up MutationObserver to watch for iframes
                const observer = new MutationObserver((mutations) => {
                    mutations.forEach(mutation => {
                        mutation.addedNodes.forEach(node => {
                            if (node.nodeType === 1 && node.tagName === 'IFRAME') {
                                // Wait for iframe to load
                                node.addEventListener('load', () => {
                                    try {
                                        const iframeDoc = node.contentDocument || node.contentWindow?.document;
                                        if (iframeDoc) {
                                            console.log('🔄 New iframe detected, injecting styles');
                                            injectEnhancedStyles(iframeDoc);
                                        }
                                    } catch (e) {
                                        console.log('⚠️ Cannot access iframe (cross-origin)');
                                    }
                                });
                            }
                        });
                    });
                });

                // Start observing for iframe additions
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                // Also check existing iframes
                setTimeout(() => {
                    const iframes = document.querySelectorAll('iframe');
                    iframes.forEach(iframe => {
                        try {
                            const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
                            if (iframeDoc) {
                                console.log('🔄 Injecting styles into existing iframe');
                                injectEnhancedStyles(iframeDoc);
                            }
                        } catch (e) {
                            console.log('⚠️ Cannot access iframe (cross-origin)');
                        }
                    });
                }, 500);

                console.log('✅ Placard enhancement observer set up');

                // ========== CARTON LABEL MANAGER - CONSOLIDATED ==========
                if (typeof window.cartonLabelManager !== 'undefined') {
                    console.log('📦 Carton Label Manager enabled on Shipment Details');
                    console.log('💡 Features: CSS, URL Redirect, Auto-Print');
                    console.log('💡 Stats: window.cartonLabelManager.stats()');
                } else {
                    console.error('❌ Carton Label Manager (carton-label-manager.js) NOT LOADED');
                    console.error('💡 Make sure carton-label-manager.js is included in your scripts');
                    console.error('💡 Expected: window.cartonLabelManager to be defined');
                }
            },
            description: 'Placard text enhancer + Carton Label Manager for shipment details page'
        },
        {
            name: 'Outbound Packing Route',
            pattern: /^#outbound\/packing(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #outbound/packing route');

                // EARLY: Disable TabManager for packing route to ensure window.open interception
                // by box label injector occurs BEFORE user triggers print.
                if (typeof TabManager !== 'undefined') {
                    try {
                        TabManager.disable();
                        console.log('⚙️ TabManager disabled for Outbound Packing route to avoid delayed CSS injection');
                    } catch(e) { /* ignore */ }
                }

                // ========== BOX LABEL FORMAT INJECTOR ==========
                if (!window._boxLabelMonitorInstalled) {
                    console.log('📋 Installing box label format monitor...');

                    const customCSS = `
                        /* Hide "Box No" line */
                        .top-info-section .text:first-child { display: none !important; }
                        /* Hide Customer ID (first child) */
                        .ship-info-section > .text.sm-text:first-child { display: none !important; }
                        /* Hide "To:" (second child) */
                        .ship-info-section > .text:nth-child(2) { display: none !important; }
                        /* Enlarge Tel line (remaining sm-text) */
                        .ship-info-section .text.sm-text { font-size: 40px !important; margin-top: 10px !important; }
                        /* Adjust top section spacing */
                        .top-info-section { padding-top: 10px !important; padding-bottom: 20px !important; margin-bottom: 20px !important; text-align: center !important; }
                        .ship-info-section { padding: 0 20px !important; }
                    `;
                    
                    // Track all windows we've tried
                    const processedWindows = new WeakSet();

                    // Function to inject CSS (globally accessible)
                    window._injectBoxLabelCSS = function(targetWindow) {
                        try {
                            if (!targetWindow || !targetWindow.document || processedWindows.has(targetWindow)) {
                                return false;
                            }

                            const doc = targetWindow.document;
                            
                            // Check if document is ready
                            if (doc.readyState === 'loading') {
                                return false;
                            }

                            const isBoxLabelWindow = doc.querySelector('.box-label-wrp') !== null;
                            
                            if (isBoxLabelWindow && !doc.getElementById('box-label-format-override')) {
                                const style = doc.createElement('style');
                                style.id = 'box-label-format-override';
                                style.textContent = customCSS;
                                doc.head.appendChild(style);
                                console.log('✅ BOX LABEL CSS INJECTED');
                                processedWindows.add(targetWindow);
                                return true;
                            }
                        } catch (error) {
                            // Silent fail for cross-origin
                        }
                        return false;
                    };

                    window._boxLabelMonitorInstalled = true;
                    console.log('✅ Box label format monitor function installed (no window.open wrapping)');
                } else {
                    console.log('ℹ️ Box label format monitor already installed');
                }

                // ========== TAB MANAGER FOR WINDOW.OPEN - INSTALL SECOND ==========
                if (typeof TabManager !== 'undefined') {
                    console.log('🪟 Tab Manager feature enabled');

                    // Ensure TabManager is installed
                    if (!window.open._tabManagerInstalled) {
                        TabManager.install();
                        console.log('✅ Tab Manager installed');
                    } else {
                        console.log('ℹ️ Tab Manager already installed');
                    }

                    // Enable debug mode for visibility
                    TabManager.setDebug(true);

                    // Monitor TabManager's tabs for box labels
                    if (TabManager.tabs && window._injectBoxLabelCSS) {
                        console.log('🔍 Setting up interval to monitor TabManager tabs for box labels...');
                        setInterval(() => {
                            for (const [url, tabInfo] of Object.entries(TabManager.tabs)) {
                                if (tabInfo.window && !tabInfo.window.closed) {
                                    try {
                                        window._injectBoxLabelCSS(tabInfo.window);
                                    } catch (e) { /* ignore */ }
                                }
                            }
                        }, 300);
                        console.log('✅ TabManager tab monitoring active');
                    }

                    console.log('💡 All window.open() calls will now use reusable tabs WITH box label monitoring');
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

                // ========== CARTON LABEL MANAGER - CONSOLIDATED ==========
                if (typeof window.cartonLabelManager !== 'undefined') {
                    console.log('📦 Carton Label Manager enabled');
                    console.log('💡 Auto-click:', window.cartonLabelManager.config.autoClickEnabled ? 'ENABLED' : 'DISABLED');
                    console.log('💡 Features: CSS, URL Redirect, Auto-Print');

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
                                            window.cartonLabelManager.handleModal();
                                        }, 100);
                                    }
                                }
                            });

                            // Check for attribute changes (like style changes that show the modal)
                            if (mutation.type === 'attributes' &&
                                mutation.target.id === '_modal_block_ui') {

                                setTimeout(() => {
                                    window.cartonLabelManager.handleModal();
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
                        window.cartonLabelManager.handleModal();
                    }, 500);

                    console.log('✅ Carton Label Manager observer set up');
                    console.log('💡 Stats: window.cartonLabelManager.stats()');
                    console.log('💡 Disable auto: window.cartonLabelManager.setAutoClick(false)');

                } else {
                    console.error('❌ Carton Label Manager (carton-label-manager.js) NOT LOADED');
                    console.error('💡 Make sure carton-label-manager.js is included in your scripts');
                    console.error('💡 Expected: window.cartonLabelManager to be defined');
                }
            },
            description: 'SKU and Qty item linker + Silent auto print for outbound packing page'
        },
        {
            name: 'Outbound Shipment List Route',
            pattern: /^#outbound\/shipment/i,
            action: () => {
                console.log('🚀 Matched #outbound/shipment route');
                console.log('📍 Full hash:', window.location.hash);

                function addPackingSlipColumn() {
                    const table = document.getElementById('outbshipment-table');
                    if (!table) {
                        console.log('⚠️ Table #outbshipment-table not found');
                        return;
                    }

                    const thead = table.querySelector('thead tr');
                    const tbody = table.querySelector('tbody');

                    if (!thead || !tbody) {
                        console.log('⚠️ Table header or body not found');
                        return;
                    }

                    // Add header if it doesn't exist
                    if (!thead.querySelector('th.packing-slip-column')) {
                        // Find the "Shipment #" column to insert before it
                        const shipmentHeader = Array.from(thead.querySelectorAll('th')).find(th =>
                            th.textContent.includes('Shipment') && th.textContent.includes('#')
                        );

                        if (!shipmentHeader) {
                            console.log('⚠️ Could not find Shipment # column');
                            return;
                        }

                        // Create new header
                        const newHeader = document.createElement('th');
                        newHeader.className = 'packing-slip-column numberid_width';
                        newHeader.textContent = 'Packing Slip';
                        newHeader.style.width = '98px';
                        newHeader.setAttribute('locale-res', 'PackingSlip');

                        // Insert before Shipment # column
                        shipmentHeader.parentNode.insertBefore(newHeader, shipmentHeader);
                        console.log('✅ Added packing slip header');
                    }

                    // Process rows (always run this, even if header exists)
                    const rows = tbody.querySelectorAll('tr');
                    let addedCount = 0;

                    console.log(`📊 Processing ${rows.length} rows for packing slip links`);

                    rows.forEach(row => {
                        // Skip if already has packing slip cell
                        if (row.querySelector('td.packing-slip-column')) {
                            return;
                        }

                        // Find the hidden shipment ID cell
                        const hiddenIdCell = row.querySelector('td.hide_column.dtfc-fixed-left');
                        if (!hiddenIdCell) {
                            console.log('⚠️ Could not find hidden ID cell in row');
                            return;
                        }

                        const shipmentId = hiddenIdCell.textContent.trim();

                        // Find the shipment # cell to insert before it
                        const shipmentCell = row.querySelector('td.shipmentno');
                        if (!shipmentCell) {
                            console.log('⚠️ Could not find shipment # cell in row');
                            return;
                        }

                        // Create new cell with link
                        const newCell = document.createElement('td');
                        newCell.className = 'packing-slip-column';

                        const link = document.createElement('a');
                        link.href = `#outbound/packingSlipdetail?id=${shipmentId}`;
                        link.textContent = 'View';
                        link.style.color = '#007bff';
                        link.style.textDecoration = 'underline';
                        link.style.cursor = 'pointer';

                        newCell.appendChild(link);

                        // Insert before shipment # cell
                        shipmentCell.parentNode.insertBefore(newCell, shipmentCell);
                        addedCount++;
                    });

                    console.log(`✅ Added packing slip links to ${addedCount} rows`);
                }

                // Try adding column immediately
                setTimeout(() => {
                    console.log('🔄 Initial attempt to add packing slip column');
                    addPackingSlipColumn();
                }, 500);

                // Set up MutationObserver to watch for table changes
                const observer = new MutationObserver((mutations) => {
                    const hasTableChanges = mutations.some(mutation => {
                        return Array.from(mutation.addedNodes).some(node => {
                            return node.nodeType === 1 && (
                                node.matches?.('table#outbshipment-table') ||
                                node.querySelector?.('table#outbshipment-table') ||
                                node.matches?.('tr[role="row"]') ||
                                node.querySelector?.('tr[role="row"]')
                            );
                        });
                    });

                    if (hasTableChanges) {
                        console.log('🔄 Table structure changed, re-adding packing slip column');
                        setTimeout(() => {
                            addPackingSlipColumn();
                        }, 100);
                    }
                });

                // Start observing the document body for changes
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                console.log('✅ Packing slip column route configured with observer');
            },
            description: 'Adds packing slip column to outbound shipment table'
        },
        {
            name: 'Order Details Production Status Route',
            pattern: /^#SO\/orderdetails(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #SO/orderdetails route');

                // Initialize production status column feature
                if (typeof window.initProductionStatusColumn === 'function') {
                    console.log('📊 Initializing Production Status Column feature');
                    window.initProductionStatusColumn();
                } else {
                    console.warn('⚠️ Production Status Column functions not loaded');
                    console.warn('⚠️ Make sure production-status-column.js is injected before router.js');
                }

                console.log('✅ Order details production status route configured');
            },
            description: 'Adds Production Status column to order items table with API data'
        },
        {
            name: 'Manual Picking Route',
            pattern: /^#Outbound\/ManualPicking(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #Outbound/ManualPicking route');

                // Function to inject print styles
                function injectManualPickingPrintStyles() {
                    if (document.getElementById('manual-picking-print-styles')) {
                        return;
                    }

                    const style = document.createElement('style');
                    style.id = 'manual-picking-print-styles';
                    style.textContent = `
                        /* Hide specific fields when printing - using class .hideprint that already exists */
                        @media print {
                            /* Hide fields that have hideprint class */
                            .hideprint {
                                display: none !important;
                            }

                            /* Hide the SKU column header and cells */
                            th[locale-res="Sku"],
                            td.sku-replaced {
                                display: none !important;
                            }

                            /* Show both SKU text and barcode in Order# column when printing */
                            td.order-cell-modified .order-no-text {
                                display: none !important;
                            }

                            td.order-cell-modified .sku-text {
                                display: block !important;
                                font-size: 16px !important;
                                font-weight: 600 !important;
                                margin-bottom: 5px !important;
                                text-align: center !important;
                            }

                            td.order-cell-modified .sku-barcode {
                                display: block !important;
                            }
                        }

                        /* Always hide original Order# text and show SKU text in Order# column */
                        td.order-cell-modified .order-no-text {
                            display: none;
                        }

                        td.order-cell-modified .sku-text {
                            display: block;
                            font-size: 14px;
                            font-weight: 500;
                        }

                        /* Hide barcode on screen, show only when printing */
                        td.order-cell-modified .sku-barcode {
                            display: none;
                        }
                    `;
                    document.head.appendChild(style);
                    console.log('✅ Injected Manual Picking print styles');
                }

                // Function to replace Order# column with SKU data and add barcodes
                function replaceOrderColumnWithSku() {
                    // Find the Order# header to get the column index
                    const orderHeader = document.querySelector('th[locale-res="OrderNo"]');
                    if (!orderHeader) {
                        console.warn('⚠️ Could not find Order# header');
                        return false;
                    }

                    // Get the column index
                    const headerRow = orderHeader.closest('tr');
                    const headers = Array.from(headerRow.querySelectorAll('th'));
                    const orderColumnIndex = headers.indexOf(orderHeader);

                    console.log(`🔍 Order# column is at index ${orderColumnIndex}`);

                    // Find all table rows
                    const tbody = document.querySelector('tbody');
                    if (!tbody) {
                        console.warn('⚠️ Could not find table body');
                        return false;
                    }

                    const rows = tbody.querySelectorAll('tr');
                    console.log(`🔍 Found ${rows.length} table rows`);

                    if (rows.length === 0) {
                        return false;
                    }

                    // Find SKU column index - try multiple methods
                    let skuHeader = document.querySelector('th[locale-res="Sku"]');

                    // If not found by attribute, try finding by text content
                    if (!skuHeader) {
                        console.log('🔍 Trying to find SKU header by text content...');
                        skuHeader = Array.from(headers).find(th =>
                            th.textContent.trim().toUpperCase() === 'SKU'
                        );
                    }

                    if (!skuHeader) {
                        console.warn('⚠️ Could not find SKU header');
                        console.log('📋 Available headers:', Array.from(headers).map(h => h.textContent.trim()));
                        return false;
                    }

                    const skuColumnIndex = headers.indexOf(skuHeader);
                    console.log(`🔍 SKU column is at index ${skuColumnIndex}`);

                    let processedCount = 0;

                    rows.forEach((row, index) => {
                        const cells = row.querySelectorAll('td');

                        if (cells.length <= Math.max(orderColumnIndex, skuColumnIndex)) {
                            console.warn(`⚠️ Row ${index + 1}: Not enough cells`);
                            return;
                        }

                        const orderCell = cells[orderColumnIndex];
                        const skuCell = cells[skuColumnIndex];

                        // Skip if already processed
                        if (orderCell.dataset.skuReplaced === 'true') {
                            console.log(`⏭️ Row ${index + 1}: Already processed, skipping`);
                            return;
                        }

                        // Get SKU text
                        const skuText = skuCell.textContent.trim();
                        if (!skuText) {
                            console.warn(`⚠️ Row ${index + 1}: SKU cell is empty`);
                            return;
                        }

                        console.log(`📝 Row ${index + 1}: Processing SKU "${skuText}"`);

                        // Get original Order# text (for reference)
                        const orderNoText = orderCell.textContent.trim();

                        // Create new content structure
                        const newContent = document.createElement('div');
                        newContent.innerHTML = `
                            <div class="order-no-text" style="display: none;">${orderNoText}</div>
                            <div class="sku-text">${skuText}</div>
                            <div class="sku-barcode" style="text-align: center; margin-top: 5px;">
                                <svg class="barcode-svg"></svg>
                            </div>
                        `;

                        // Clear and append new content
                        orderCell.innerHTML = '';
                        orderCell.appendChild(newContent);

                        // Add class to mark this cell as modified
                        orderCell.classList.add('order-cell-modified');

                        // Add class to SKU cell so we can hide it when printing
                        skuCell.classList.add('sku-replaced');

                        // Generate barcode using Code128 if available
                        try {
                            const barcodeSvg = newContent.querySelector('.barcode-svg');
                            if (typeof JsBarcode !== 'undefined' && barcodeSvg) {
                                JsBarcode(barcodeSvg, skuText, {
                                    format: 'CODE128',
                                    width: 2,
                                    height: 60,
                                    displayValue: true,
                                    fontSize: 14,
                                    margin: 5
                                });
                                console.log(`✅ Row ${index + 1}: Generated barcode for "${skuText}"`);
                            } else {
                                // Fallback: just show text if JsBarcode not available
                                barcodeSvg.textContent = skuText;
                                console.warn('⚠️ JsBarcode not available, showing SKU as text');
                            }
                        } catch (error) {
                            console.error('❌ Error generating barcode:', error);
                        }

                        orderCell.dataset.skuReplaced = 'true';
                        processedCount++;
                    });

                    if (processedCount > 0) {
                        console.log(`✅ Replaced Order# with SKU in ${processedCount} rows`);
                        return true;
                    }

                    return false;
                }

                // Function to update table header
                function updateTableHeader() {
                    const orderHeader = document.querySelector('th[locale-res="OrderNo"]');
                    if (orderHeader && !orderHeader.dataset.headerUpdated) {
                        orderHeader.textContent = 'SKU';
                        orderHeader.dataset.headerUpdated = 'true';
                        console.log('✅ Updated Order# header to SKU');
                    }
                }

                // Inject styles immediately
                injectManualPickingPrintStyles();

                // Check if JsBarcode is available
                console.log('🔍 Checking for JsBarcode library...');
                if (typeof JsBarcode !== 'undefined') {
                    console.log('✅ JsBarcode is available');
                } else {
                    console.warn('⚠️ JsBarcode is NOT available - barcodes will not render');
                    console.warn('💡 Add JsBarcode via: <script src="https://cdn.jsdelivr.net/npm/jsbarcode@3.11.6/dist/JsBarcode.all.min.js"></script>');
                }

                // Try to process table immediately
                setTimeout(() => {
                    console.log('🔄 Attempting to process table...');
                    updateTableHeader();
                    const result = replaceOrderColumnWithSku();
                    if (!result) {
                        console.warn('⚠️ No Order# cells found yet, will try again when table loads');
                    }
                }, 500);

                // Set up MutationObserver to watch for table changes
                const observer = new MutationObserver((mutations) => {
                    const hasTableChanges = mutations.some(mutation => {
                        return Array.from(mutation.addedNodes).some(node => {
                            return node.nodeType === 1 && (
                                node.matches?.('table') ||
                                node.querySelector?.('table') ||
                                node.matches?.('tr') ||
                                node.querySelector?.('tr')
                            );
                        });
                    });

                    if (hasTableChanges) {
                        console.log('🔄 Table changed, updating Order# to SKU');
                        setTimeout(() => {
                            updateTableHeader();
                            replaceOrderColumnWithSku();
                        }, 100);
                    }
                });

                // Start observing
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                console.log('✅ Manual Picking route configured');
                console.log('💡 Fields hidden: Pallet Count, Sortable, Started Since, Waiting Since, Customer Note');
                console.log('💡 Order# column replaced with SKU (with barcode on print)');
            },
            description: 'Print styling for manual picking page - hides fields and replaces Order# with SKU'
        }
    ];


    // Main routing function
    function routeScripts() {
        const hash = window.location.hash;
        console.log(`🔍 Current URL hash: ${hash || '(none)'}`);

        if (!hash) {
            console.log('ℹ️ No hash in URL - no routes to match');
            return;
        }

        const matchedRoutes = [];

        // Check each route
        ROUTES.forEach(route => {
            if (route.pattern.test(hash)) {
                console.log(`🎯 Matched route: ${route.name} - ${route.description}`);
                matchedRoutes.push(route);

                // Execute the action
                try {
                    route.action();
                } catch (error) {
                    console.error(`❌ Error executing action for ${route.name}:`, error);
                }
            }
        });

        if (matchedRoutes.length === 0) {
            console.log(`ℹ️ No routes matched for hash: ${hash}`);
            return;
        }

        console.log(`✅ Executed ${matchedRoutes.length} route action(s)`);
        console.log('🎉 Routing complete!');
    }

    // Wait for DOM to be ready, then route
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', routeScripts);
    } else {
        // Small delay to ensure page is fully loaded
        setTimeout(routeScripts, 100);
    }

    // Listen for hash changes (for SPA navigation)
    window.addEventListener('hashchange', () => {
        console.log('🔄 Hash changed, re-routing scripts...');
        setTimeout(routeScripts, 300);
    });

    // Global API for debugging
    window.extensionRouter = {
        routes: ROUTES,
        routeScripts: routeScripts,
        currentHash: () => window.location.hash
    };

    console.log('✅ Browser Extension JS Router initialized');
    console.log('🔧 Debug with: window.extensionRouter');
})();