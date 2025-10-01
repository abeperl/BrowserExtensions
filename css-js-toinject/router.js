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
                if (typeof makeSkuItemsClickable === 'function') {
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
                            console.log('🔄 Table data changed, updating clickable SKUs');
                            makeSkuItemsClickable();
                        }
                    });

                    // Start observing the document body for changes
                    observer.observe(document.body, {
                        childList: true,
                        subtree: true
                    });

                    // Also try immediately in case elements already exist
                    setTimeout(() => {
                        console.log('🔄 Initial attempt to make SKUs clickable');
                        makeSkuItemsClickable();
                    }, 500);

                    console.log('✅ MutationObserver set up for SKU table monitoring');
                } else {
                    console.warn('⚠️ makeSkuItemsClickable not loaded');
                }
            },
            description: 'SKU item linker for outbound packing page'
        },
        {
            name: 'PO Test Route',
            pattern: /^#PO$/i,
            action: () => {
                console.log('🚀 Matched #PO route');
                if (typeof func1 === 'function') {
                    func1();
                } else {
                    console.warn('⚠️ func1 not loaded');
                }
            },
            description: 'Test route for PO page'
        },
        {
            name: 'Inbound Test Route',
            pattern: /^#inbound$/i,
            action: () => {
                console.log('🚀 Matched #inbound route');
                if (typeof func3 === 'function') {
                    func3();
                } else {
                    console.warn('⚠️ func3 not loaded');
                }
            },
            description: 'Test route for Inbound page'
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