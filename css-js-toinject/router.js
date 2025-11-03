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

                // ========== API RESPONSE OVERLAY FEATURE ==========
                if (typeof OverlayManager !== 'undefined') {
                    console.log('🎯 API response overlay feature enabled');
                    console.log('✅ OverlayManager available:', typeof OverlayManager);

                    // Intercept tf.service.post (primary method used by the app)
                    function interceptTfService() {
                        if (typeof window.tf !== 'undefined' && window.tf.service && window.tf.service.post) {
                            console.log('✅ tf.service.post found, installing interceptor');

                            const originalPost = window.tf.service.post;

                            window.tf.service.post = function(endpoint, data, callback, errorCallback) {
                                console.log('📡 tf.service.post intercepted:', endpoint);

                                // Check if this is the PersonalizedAndCustomizedOrders endpoint
                                if (endpoint.includes('PersonalizedAndCustomizedOrders')) {
                                    console.log('🎯🎯🎯 PersonalizedAndCustomizedOrders API MATCHED!');
                                    console.log('Request data:', data);

                                    // Wrap the callback to intercept response
                                    const wrappedCallback = function(response) {
                                        console.log('📦 API Response:', response);

                                        const isSuccess = response.responseCode === 0 || response.responseCode === 200;

                                        if (isSuccess) {
                                            const statusCount = data.statuses?.length || 0;
                                            const picklistCount = data.picklist?.length || 0;
                                            const message = `Successfully processed ${statusCount} status update(s) and ${picklistCount} picklist item(s)`;

                                            OverlayManager.success({
                                                message: message,
                                                duration: 3000
                                            });
                                            console.log('✅', message);
                                        } else {
                                            const errorMsg = response.responseMessage || response.message || 'Unknown error';
                                            OverlayManager.error({
                                                message: `Update failed: ${errorMsg}`,
                                                duration: 4000
                                            });
                                            console.log('❌ Update failed:', errorMsg);

                                            // Clear inputs and refocus on error
                                            if (typeof window._handleScanError === 'function') {
                                                window._handleScanError();
                                            }
                                        }

                                        // Call original callback
                                        if (callback) callback(response);
                                    };

                                    // Wrap error callback too
                                    const wrappedErrorCallback = function(error) {
                                        console.log('❌ API Error:', error);
                                        const errorMsg = error.message || error.responseMessage || 'Request failed';
                                        OverlayManager.error({
                                            message: `Error: ${errorMsg}`,
                                            duration: 4000
                                        });

                                        // Clear inputs and refocus on error
                                        if (typeof window._handleScanError === 'function') {
                                            window._handleScanError();
                                        }

                                        // Call original error callback
                                        if (errorCallback) errorCallback(error);
                                    };

                                    // Call original with wrapped callbacks
                                    return originalPost.call(this, endpoint, data, wrappedCallback, wrappedErrorCallback);
                                }

                                // Not the target endpoint, call original
                                return originalPost.apply(this, arguments);
                            };

                            console.log('✅ tf.service.post interceptor installed');
                            console.log('💡 Test overlay with: OverlayManager.success({message: "Test!", duration: 3000})');
                            return true;
                        }
                        return false;
                    }

                    // Try to intercept immediately
                    if (!interceptTfService()) {
                        // tf.service not ready yet, wait for it
                        console.log('⏳ Waiting for tf.service to be available...');

                        const checkInterval = setInterval(() => {
                            if (interceptTfService()) {
                                clearInterval(checkInterval);
                            }
                        }, 100);

                        // Give up after 10 seconds
                        setTimeout(() => {
                            clearInterval(checkInterval);
                            if (typeof window.tf === 'undefined' || !window.tf.service) {
                                console.warn('⚠️ tf.service not found after 10 seconds');
                            }
                        }, 10000);
                    }

                    // Also intercept fetch for PersonalizedAndCustomizedOrders API (fallback)
                    const originalFetch = window.fetch;
                    if (!window.fetch._apiOverlayIntercepted) {
                        window.fetch = async function(...args) {
                            const url = args[0];
                            const options = args[1] || {};

                            // Debug: Log all fetch calls
                            console.log('🌐 Fetch intercepted:', url);

                            // Check if this is the PersonalizedAndCustomizedOrders API
                            const isTargetAPI = typeof url === 'string' &&
                                url.includes('/api/Order/PersonalizedAndCustomizedOrders');

                            if (isTargetAPI) {
                                console.log('🎯🎯🎯 PersonalizedAndCustomizedOrders API MATCHED!');
                                console.log('Request body:', options.body);

                                try {
                                    // Make the actual request
                                    const response = await originalFetch.apply(this, args);

                                    // Clone response so we can read it
                                    const responseClone = response.clone();

                                    // Read the response
                                    const data = await responseClone.json();

                                    console.log('📦 API Response:', data);

                                    // Determine success/failure
                                    const isSuccess = response.ok && response.status >= 200 && response.status < 300;

                                    // Extract request data for display
                                    let requestData = {};
                                    try {
                                        if (options.body) {
                                            requestData = JSON.parse(options.body);
                                        }
                                    } catch (e) {
                                        console.warn('Could not parse request body:', e);
                                    }

                                    // Build message based on response
                                    let message = '';

                                    if (isSuccess) {
                                        // Count processed items
                                        const statusCount = requestData.statuses?.length || 0;
                                        const picklistCount = requestData.picklist?.length || 0;

                                        message = `Successfully processed ${statusCount} status update(s) and ${picklistCount} picklist item(s)`;

                                        // Show success overlay
                                        OverlayManager.success({
                                            message: message,
                                            duration: 3000
                                        });
                                    } else {
                                        // Error handling
                                        const errorMsg = data.message || data.error || 'Unknown error';
                                        message = `API Error: ${errorMsg}`;

                                        // Show error overlay
                                        OverlayManager.error({
                                            message: message,
                                            duration: 4000
                                        });

                                        // Clear inputs and refocus on error
                                        if (typeof window._handleScanError === 'function') {
                                            window._handleScanError();
                                        }
                                    }

                                    // Log to popup controller for history
                                    console.log(`${isSuccess ? '✅' : '❌'} ${message}`);

                                    return response;
                                } catch (error) {
                                    console.error('❌ API Request failed:', error);

                                    // Show error overlay
                                    OverlayManager.error({
                                        message: `Request failed: ${error.message}`,
                                        duration: 4000
                                    });

                                    // Clear inputs and refocus on error
                                    if (typeof window._handleScanError === 'function') {
                                        window._handleScanError();
                                    }

                                    throw error;
                                }
                            }

                            // Not the target API, proceed normally
                            return originalFetch.apply(this, args);
                        };
                        window.fetch._apiOverlayIntercepted = true;
                        console.log('✅ API overlay interceptor installed');
                        console.log('💡 Test overlay with: OverlayManager.success({message: "Test!", duration: 3000})');
                    }

                    // Also intercept XMLHttpRequest
                    const originalXHRSend = XMLHttpRequest.prototype.send;
                    if (!XMLHttpRequest.prototype.send._apiOverlayIntercepted) {
                        XMLHttpRequest.prototype.send = function(...args) {
                            const url = this._url || '';

                            if (url.includes('/api/Order/PersonalizedAndCustomizedOrders')) {
                                console.log('🎯 PersonalizedAndCustomizedOrders XHR detected');

                                this.addEventListener('readystatechange', function() {
                                    if (this.readyState === 4) {
                                        try {
                                            const isSuccess = this.status >= 200 && this.status < 300;
                                            let data = {};

                                            try {
                                                data = JSON.parse(this.responseText);
                                            } catch (e) {
                                                console.warn('Could not parse XHR response:', e);
                                            }

                                            if (isSuccess) {
                                                OverlayManager.success({
                                                    message: 'Order status updated successfully',
                                                    duration: 3000
                                                });
                                            } else {
                                                const errorMsg = data.message || data.error || `HTTP ${this.status}`;
                                                OverlayManager.error({
                                                    message: `Update failed: ${errorMsg}`,
                                                    duration: 4000
                                                });

                                                // Clear inputs and refocus on error
                                                if (typeof window._handleScanError === 'function') {
                                                    window._handleScanError();
                                                }
                                            }
                                        } catch (error) {
                                            console.error('Error handling XHR response:', error);
                                        }
                                    }
                                });
                            }

                            return originalXHRSend.apply(this, args);
                        };

                        // Also intercept XHR open to capture URL
                        const originalXHROpen = XMLHttpRequest.prototype.open;
                        XMLHttpRequest.prototype.open = function(method, url, ...rest) {
                            this._url = url;
                            return originalXHROpen.apply(this, [method, url, ...rest]);
                        };

                        XMLHttpRequest.prototype.send._apiOverlayIntercepted = true;
                        console.log('✅ XHR API overlay interceptor installed');
                    }
                } else {
                    console.warn('⚠️ OverlayManager or PopupController not loaded');
                }

                if (typeof addItemLineIdColumn === 'function' && typeof populateItemLineIds === 'function') {
                    // Add column immediately
                    console.log('🔄 Adding Item Line ID column');
                    addItemLineIdColumn();

                    // Set up MutationObserver to watch for table changes
                    const observer = new MutationObserver((mutations) => {
                        const hasTableChanges = mutations.some(mutation => {
                            return Array.from(mutation.addedNodes).some(node => {
                                return node.nodeType === 1 && (
                                    node.matches?.('table[id^="inner-table-orderwise"]') ||
                                    node.querySelector?.('table[id^="inner-table-orderwise"]') ||
                                    node.matches?.('tr.item-row') ||
                                    node.querySelector?.('tr.item-row')
                                );
                            });
                        });

                        if (hasTableChanges) {
                            console.log('🔄 Table structure changed, re-adding column');
                            setTimeout(() => {
                                addItemLineIdColumn();
                                populateItemLineIds();
                            }, 100);
                        }
                    });

                    // Start observing the document body for changes
                    observer.observe(document.body, {
                        childList: true,
                        subtree: true
                    });

                    // Intercept fetch for API calls
                    const originalFetch = window.fetch;
                    if (!window.fetch._itemLineIdIntercepted) {
                        window.fetch = function(...args) {
                            const result = originalFetch.apply(this, args);

                            result.then(response => {
                                const url = args[0];
                                if (typeof url === 'string' && url.includes('/api/Order/GetPersonalizedOrderItems')) {
                                    console.log('🎯 GetPersonalizedOrderItems API detected, populating Item Line IDs');
                                    if (response.status === 200) {
                                        setTimeout(() => {
                                            addItemLineIdColumn();
                                            populateItemLineIds();
                                        }, 500);
                                    }
                                }
                            }).catch(() => {});

                            return result;
                        };
                        window.fetch._itemLineIdIntercepted = true;
                    }

                    // Intercept XMLHttpRequest
                    const originalXHRSend = XMLHttpRequest.prototype.send;
                    if (!XMLHttpRequest.prototype.send._itemLineIdIntercepted) {
                        XMLHttpRequest.prototype.send = function(...args) {
                            this.addEventListener('readystatechange', function() {
                                if (this.readyState === 4 && this.status === 200) {
                                    if (this._url && this._url.includes('/api/Order/GetPersonalizedOrderItems')) {
                                        console.log('🎯 GetPersonalizedOrderItems XHR detected, populating Item Line IDs');
                                        setTimeout(() => {
                                            addItemLineIdColumn();
                                            populateItemLineIds();
                                        }, 500);
                                    }
                                }
                            });
                            return originalXHRSend.apply(this, args);
                        };
                        XMLHttpRequest.prototype.send._itemLineIdIntercepted = true;
                    }

                    // Try populating after initial delay
                    setTimeout(() => {
                        console.log('🔄 Initial population attempt');
                        addItemLineIdColumn();
                        populateItemLineIds();
                    }, 1000);

                    console.log('✅ Item Line ID route configured with observers and API interceptors');
                } else {
                    console.warn('⚠️ Item Line ID functions not loaded');
                }
            },
            description: 'Item Line ID column manager for personalized order items'
        },
        {
            name: 'Shipment Details Route',
            pattern: /^#Outbound\/shipmentdetails(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #Outbound/shipmentdetails route');

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
            },
            description: 'Placard text enhancer for shipment details page'
        },
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

                // ========== AUTO PRINT BUTTONS FEATURE ==========
                if (typeof handleShipmentModalAppearance === 'function') {
                    console.log('🖨️ Auto Print Buttons feature enabled');
                    console.log('💡 Auto-click triggers ONLY when "Shipment Created Success" modal appears');

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
                                        setTimeout(handleShipmentModalAppearance, 100);
                                    }
                                }
                            });

                            // Check for attribute changes (like style changes that show the modal)
                            if (mutation.type === 'attributes' &&
                                mutation.target.id === '_modal_block_ui') {

                                setTimeout(handleShipmentModalAppearance, 100);
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
                        handleShipmentModalAppearance();
                    }, 500);

                    console.log('✅ Auto Print Buttons observer set up');
                } else {
                    console.warn('⚠️ Auto Print Buttons functions not loaded');
                }
            },
            description: 'SKU and Qty item linker + Auto print buttons for outbound packing page'
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