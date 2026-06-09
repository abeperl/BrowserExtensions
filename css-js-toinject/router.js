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
                        /* Collapse Order# and Customer Note columns - use width collapse instead of display:none */
                        th[locale-res="OrderNo"],
                        th[locale-res="CustomerNote"],
                        td.order-no-hidden,
                        td.customer-note-hidden {
                            width: 0 !important;
                            min-width: 0 !important;
                            max-width: 0 !important;
                            padding: 0 !important;
                            margin: 0 !important;
                            border: 0 !important;
                            overflow: hidden !important;
                            visibility: collapse !important;
                        }

                        /* Hide Customer Note detail block (screen + print) */
                        .remarks[column-permission="CustomerNote"],
                        [column-permission="CustomerNote"] {
                            display: none !important;
                            visibility: hidden !important;
                        }

                        /* SKU column with barcode styling - SCREEN */
                        td.sku-with-barcode {
                            text-align: center !important;
                            padding: 2px 1px !important;
                            vertical-align: middle !important;
                        }

                        .sku-text {
                            display: block !important;
                            font-size: 11px;
                            font-weight: 500;
                            margin-bottom: 2px;
                        }

                        .sku-barcode-container {
                            display: block !important;
                            text-align: center;
                            margin-top: 2px;
                        }

                        .sku-barcode-container canvas {
                            display: inline-block !important;
                            max-width: 100%;
                            height: auto;
                        }

                        /* Print styles - Canvas has excellent PDF compatibility */
                        @media print {
                            /* Compact entire picklist table for print (override site's 20px font) */
                            table.picklist_table th,
                            table.picklist_table td,
                            table.picklist_table tfoot td {
                                font-size: 10px !important;
                                padding: 1px 2px !important;
                                line-height: 1.1 !important;
                                white-space: normal !important;
                            }

                            /* Reduce row heights */
                            table.picklist_table tr {
                                height: auto !important;
                            }

                            /* Force barcode visibility in print */
                            td.sku-with-barcode {
                                padding: 1px 2px !important;
                                text-align: center !important;
                                vertical-align: middle !important;
                                min-height: 0 !important;
                            }

                            .sku-text {
                                display: block !important;
                                visibility: visible !important;
                                font-size: 9px !important;
                                font-weight: 600 !important;
                                margin-bottom: 1px !important;
                            }

                            .sku-barcode-container {
                                display: block !important;
                                visibility: visible !important;
                                text-align: center !important;
                                margin-top: 0 !important;
                                page-break-inside: avoid !important;
                            }

                            .sku-barcode-container canvas {
                                display: inline-block !important;
                                visibility: visible !important;
                                max-width: 100% !important;
                                height: auto !important;
                            }
                        }
                    `;
                    document.head.appendChild(style);
                    console.log('✅ Injected Manual Picking print styles');
                }

                // Function to hide Order# column and add barcodes to SKU column
                function addBarcodesToSkuColumn() {
                    // Find the main data table
                    const table = document.querySelector('table.picklist_table');
                    if (!table) {
                        console.warn('⚠️ Could not find picklist table');
                        return false;
                    }

                    // Find the Order# header
                    const orderHeader = table.querySelector('th[locale-res="OrderNo"]');
                    if (!orderHeader) {
                        console.warn('⚠️ Could not find Order# header');
                        return false;
                    }

                    // Mark header as hidden (collapse it)
                    orderHeader.classList.add('order-no-hidden');
                    console.log('✅ Collapsed Order# column header');

                    // Get the header row first
                    const headerRow = orderHeader.closest('tr');

                    // Find and hide Customer Note column (by text content)
                    const allHeaders = Array.from(headerRow.querySelectorAll('th'));
                    const customerNoteHeader = allHeaders.find(th => {
                        const text = th.textContent.trim().toUpperCase();
                        return text.includes('CUSTOMER') && text.includes('NOTE');
                    });
                    if (customerNoteHeader) {
                        customerNoteHeader.classList.add('customer-note-hidden');
                        console.log('✅ Collapsed Customer Note column header:', customerNoteHeader.textContent);
                    } else {
                        console.log('⚠️ Customer Note column not found');
                    }
                    const headers = Array.from(headerRow.querySelectorAll('th:not([style*="display: none"])'));
                    const orderColumnIndex = Array.from(headerRow.querySelectorAll('th')).indexOf(orderHeader);

                    // Find SKU header
                    let skuHeader = table.querySelector('th.sku_width');
                    if (!skuHeader) {
                        console.log('🔍 Trying to find SKU header by locale-res...');
                        skuHeader = table.querySelector('th[locale-res*="SKU"]');
                    }

                    if (!skuHeader) {
                        console.warn('⚠️ Could not find SKU header');
                        return false;
                    }

                    const skuColumnIndex = Array.from(headerRow.querySelectorAll('th')).indexOf(skuHeader);
                    
                    // Get Customer Note column index (header already found above)
                    const customerNoteColumnIndex = customerNoteHeader ? Array.from(headerRow.querySelectorAll('th')).indexOf(customerNoteHeader) : -1;
                    
                    console.log(`🔍 SKU column at index ${skuColumnIndex}, Order# column at index ${orderColumnIndex}, Customer Note at index ${customerNoteColumnIndex}`);

                    // Find all table rows
                    const tbody = table.querySelector('tbody');
                    if (!tbody) {
                        console.warn('⚠️ Could not find table body');
                        return false;
                    }

                    const rows = tbody.querySelectorAll('tr');
                    console.log(`🔍 Found ${rows.length} table rows`);

                    if (rows.length === 0) {
                        return false;
                    }

                    let processedCount = 0;
                    let barcodeCount = 0;

                    rows.forEach((row, index) => {
                        const cells = row.querySelectorAll('td');

                        if (cells.length <= Math.max(orderColumnIndex, skuColumnIndex)) {
                            console.warn(`⚠️ Row ${index + 1}: Not enough cells (${cells.length})`);
                            return;
                        }

                        const orderCell = cells[orderColumnIndex];
                        const skuCell = cells[skuColumnIndex];
                        const customerNoteCell = customerNoteColumnIndex >= 0 ? cells[customerNoteColumnIndex] : null;

                        // Skip if already processed
                        if (skuCell.classList.contains('sku-with-barcode')) {
                            return;
                        }

                        // Get SKU text - look for span with class 'sku'
                        let skuText = '';
                        const skuSpan = skuCell.querySelector('span.sku');
                        if (skuSpan) {
                            skuText = skuSpan.textContent.trim();
                        } else {
                            skuText = skuCell.textContent.trim().split('/')[0].trim();
                        }

                        if (!skuText) {
                            console.warn(`⚠️ Row ${index + 1}: SKU cell is empty`);
                            return;
                        }

                        console.log(`📝 Row ${index + 1}: Processing SKU "${skuText}"`);

                        // Hide Order# cell and Customer Note cell
                        orderCell.classList.add('order-no-hidden');
                        if (customerNoteCell) {
                            customerNoteCell.classList.add('customer-note-hidden');
                        }

                        // Clear SKU cell content and rebuild
                        skuCell.innerHTML = '';
                        skuCell.classList.add('sku-with-barcode');

                        // Add SKU text
                        const skuTextDiv = document.createElement('div');
                        skuTextDiv.className = 'sku-text';
                        skuTextDiv.textContent = skuText;
                        skuCell.appendChild(skuTextDiv);

                        // Add barcode container with CANVAS (better PDF compatibility than SVG)
                        const barcodeContainer = document.createElement('div');
                        barcodeContainer.className = 'sku-barcode-container';
                        const barcodeCanvas = document.createElement('canvas');
                        barcodeCanvas.className = 'barcode-canvas';
                        barcodeContainer.appendChild(barcodeCanvas);
                        skuCell.appendChild(barcodeContainer);

                        // Generate barcode using JsBarcode if available (Canvas output for PDF compatibility)
                        try {
                            if (typeof JsBarcode !== 'undefined') {
                                JsBarcode(barcodeCanvas, skuText, {
                                    format: 'CODE128',
                                    width: 2,
                                    height: 34,
                                    displayValue: false,
                                    fontSize: 9,
                                    margin: 10,
                                    background: '#FFFFFF',
                                    lineColor: '#000000'
                                });
                                console.log(`✅ Row ${index + 1}: Generated CODE128 barcode (Canvas) for "${skuText}"`);
                                barcodeCount++;
                            } else {
                                console.warn('⚠️ JsBarcode not available');
                                barcodeCanvas.remove();
                                const fallback = document.createElement('div');
                                fallback.style.cssText = 'font-size:12px; color:#666; margin-top:5px;';
                                fallback.textContent = `[${skuText}]`;
                                barcodeContainer.appendChild(fallback);
                            }
                        } catch (error) {
                            console.error(`❌ Error generating barcode for row ${index + 1}:`, error);
                            barcodeCanvas.remove();
                            const errorDiv = document.createElement('div');
                            errorDiv.textContent = 'Error';
                            barcodeContainer.appendChild(errorDiv);
                        }

                        processedCount++;
                    });

                    if (processedCount > 0) {
                        console.log(`✅ Processed ${processedCount} rows, generated ${barcodeCount} barcodes`);
                        return true;
                    }

                    return false;
                }

                // Inject styles immediately
                injectManualPickingPrintStyles();

                // Check if JsBarcode is available
                console.log('🔍 Checking for JsBarcode library...');
                if (typeof JsBarcode !== 'undefined') {
                    console.log('✅ JsBarcode is available, version:', JsBarcode.VERSION || 'unknown');
                } else {
                    console.warn('⚠️ JsBarcode is NOT available yet');
                }

                // Try to process table with delays to ensure DOM is ready
                setTimeout(() => {
                    console.log('🔄 First attempt to process table (500ms)...');
                    const result = addBarcodesToSkuColumn();
                    if (!result) {
                        console.warn('⚠️ No table found yet');
                    }
                }, 500);

                // Retry after 1 second
                setTimeout(() => {
                    console.log('🔄 Second attempt to process table (1000ms)...');
                    addBarcodesToSkuColumn();
                }, 1000);

                // Set up MutationObserver to watch for table changes
                const observer = new MutationObserver((mutations) => {
                    const hasTableChanges = mutations.some(mutation => {
                        return Array.from(mutation.addedNodes).some(node => {
                            return node.nodeType === 1 && (
                                node.classList?.contains('picklist_table') ||
                                node.querySelector?.('table.picklist_table') ||
                                node.matches?.('tbody tr') ||
                                node.querySelector?.('tbody tr')
                            );
                        });
                    });

                    if (hasTableChanges) {
                        console.log('🔄 Table data changed, updating barcodes...');
                        setTimeout(() => {
                            addBarcodesToSkuColumn();
                        }, 200);
                    }
                });

                // Start observing
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                console.log('✅ Manual Picking route configured');
                console.log('💡 Order# column: COLLAPSED (maintains header alignment)');
                console.log('💡 SKU column: Enhanced with CODE128 barcodes');
            },
            description: 'Hide Order# column and add CODE128 barcodes to SKU column for manual picking'
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