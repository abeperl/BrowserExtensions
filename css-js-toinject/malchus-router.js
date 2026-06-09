/**
 * Malchus Domain JS Router
 * Routes to appropriate JS files based on page content detection
 * Domain: https://malchus.3plnext.com
 * Controls feature enablement and observability
 */

(function() {
    'use strict';

    console.log('🚀 Malchus Extension JS Router - Starting...');

    // Configuration: Map URL patterns to functions
    const ROUTES = [
        {
            name: 'Sales Order Create Route',
            pattern: /^#SO\/socreate(\?.*)?$/i,
            action: () => {
                console.log('🚀 Matched #SO/socreate route');

                // ========== FEATURE FLAGS ==========
                const FEATURES = {
                    tableCustomization: false,  // DISABLED - needs rework
                    skuToQtyFocus: true,        // ENABLED
                    handoverAutoSelect: true,   // ENABLED
                    autoOkButton: true          // ENABLED - Auto-click OK buttons
                };

                // ========== TABLE CUSTOMIZATION (if enabled) ==========
                if (FEATURES.tableCustomization) {
                    if (typeof applyTableBaseStyles === 'function' &&
                        typeof addColumnToggleButton === 'function') {

                        console.log('📊 Table customization enabled, setting up observer');

                        // Wait for table to exist
                        const tableObserver = new MutationObserver((mutations, obs) => {
                            const table = document.getElementById('so-items-table');
                            if (table) {
                                console.log('✅ Table found, applying customization');
                                applyTableBaseStyles();
                                addColumnToggleButton();
                                obs.disconnect();
                            }
                        });

                        tableObserver.observe(document.body, {
                            childList: true,
                            subtree: true
                        });

                        // Also try immediately
                        setTimeout(() => {
                            const table = document.getElementById('so-items-table');
                            if (table) {
                                applyTableBaseStyles();
                                addColumnToggleButton();
                                tableObserver.disconnect();
                            }
                        }, 500);
                    } else {
                        console.warn('⚠️ Table customization functions not loaded');
                    }
                } else {
                    console.log('ℹ️ Table customization feature disabled');
                }

                // ========== SKU TO QTY FOCUS REDIRECT (if enabled) ==========
                if (FEATURES.skuToQtyFocus) {
                    if (typeof addAutoFocusCheckbox === 'function' &&
                        typeof setupSkuListeners === 'function' &&
                        typeof setupAutocompleteWatchers === 'function') {

                        console.log('🎯 SKU to Qty focus redirect enabled, setting up observer');

                        // Add checkbox first
                        const pageBtnObserver = new MutationObserver((mutations, obs) => {
                            const pageBtn = document.querySelector('.page-btn');
                            if (pageBtn) {
                                console.log('✅ .page-btn found, adding checkbox');
                                addAutoFocusCheckbox();
                                obs.disconnect();
                            }
                        });

                        pageBtnObserver.observe(document.body, {
                            childList: true,
                            subtree: true
                        });

                        // Try immediately
                        setTimeout(() => {
                            const pageBtn = document.querySelector('.page-btn');
                            if (pageBtn) {
                                addAutoFocusCheckbox();
                                pageBtnObserver.disconnect();
                            }
                        }, 500);

                        // Wait for SKU input to exist
                        const skuObserver = new MutationObserver((mutations, obs) => {
                            const skuInput = document.getElementById('sku-autocomplete');
                            if (skuInput) {
                                console.log('✅ SKU input found, setting up listeners');

                                setTimeout(() => {
                                    const skuSuccess = setupSkuListeners();
                                    const autocompleteSuccess = setupAutocompleteWatchers();

                                    if (skuSuccess && autocompleteSuccess) {
                                        console.log('🎉 SKU to Qty focus redirect fully configured');
                                    }
                                }, 500);

                                obs.disconnect();
                            }
                        });

                        skuObserver.observe(document.body, {
                            childList: true,
                            subtree: true
                        });

                        // Also try immediately
                        setTimeout(() => {
                            const skuInput = document.getElementById('sku-autocomplete');
                            if (skuInput) {
                                setupSkuListeners();
                                setupAutocompleteWatchers();
                                skuObserver.disconnect();
                            }
                        }, 500);
                    } else {
                        console.warn('⚠️ SKU focus redirect functions not loaded');
                    }
                } else {
                    console.log('ℹ️ SKU to Qty focus redirect feature disabled');
                }

                // ========== HANDOVER AUTO-SELECT (if enabled) ==========
                if (FEATURES.handoverAutoSelect) {
                    if (typeof addHandoverDropdown === 'function' &&
                        typeof setupHandoverRowObserver === 'function') {

                        console.log('🤝 Handover auto-select enabled, setting up components');

                        // Add dropdown after auto-focus checkbox
                        const dropdownObserver = new MutationObserver((mutations, obs) => {
                            const autoFocusContainer = document.getElementById('auto-focus-qty-container');
                            if (autoFocusContainer) {
                                console.log('✅ Auto-focus container found, adding handover dropdown');
                                addHandoverDropdown();
                                obs.disconnect();
                            }
                        });

                        dropdownObserver.observe(document.body, {
                            childList: true,
                            subtree: true
                        });

                        // Try immediately
                        setTimeout(() => {
                            const autoFocusContainer = document.getElementById('auto-focus-qty-container');
                            if (autoFocusContainer) {
                                addHandoverDropdown();
                                dropdownObserver.disconnect();
                            }
                        }, 600);

                        // Setup table row observer
                        const tableObserver = new MutationObserver((mutations, obs) => {
                            const table = document.getElementById('so-items-table');
                            if (table) {
                                console.log('✅ Table found, setting up handover row observer');
                                setupHandoverRowObserver();
                                obs.disconnect();
                            }
                        });

                        tableObserver.observe(document.body, {
                            childList: true,
                            subtree: true
                        });

                        // Try immediately
                        setTimeout(() => {
                            const table = document.getElementById('so-items-table');
                            if (table) {
                                setupHandoverRowObserver();
                                tableObserver.disconnect();
                            }
                        }, 600);
                    } else {
                        console.warn('⚠️ Handover auto-select functions not loaded');
                    }
                } else {
                    console.log('ℹ️ Handover auto-select feature disabled');
                }

                // ========== AUTO OK BUTTON (if enabled) ==========
                if (FEATURES.autoOkButton) {
                    if (typeof window.autoOkButton !== 'undefined' &&
                        typeof window.autoOkButton.clickOkButton === 'function') {

                        console.log('🔘 Auto OK button enabled, setting up observer...');

                        // Setup MutationObserver to watch for modals
                        const modalObserver = new MutationObserver((mutations) => {
                            for (const mutation of mutations) {
                                // Check if a modal was added
                                for (const node of mutation.addedNodes) {
                                    if (node.nodeType === Node.ELEMENT_NODE) {
                                        // Check if it's a modal or contains a modal
                                        if (node.classList?.contains('modal-box') ||
                                            node.classList?.contains('modal-show') ||
                                            node.querySelector?.('.modal-box')) {

                                            console.log('🔔 Modal detected, attempting auto-click...');

                                            // Wait a brief moment for modal to render, then click OK
                                            setTimeout(() => {
                                                window.autoOkButton.clickOkButton();
                                            }, 100);
                                        }
                                    }
                                }

                                // Also check if body gets modal-show class
                                if (mutation.type === 'attributes' &&
                                    mutation.target === document.body &&
                                    mutation.attributeName === 'class') {

                                    if (document.body.classList.contains('modal-show')) {
                                        console.log('🔔 Modal-show class detected on body!');

                                        setTimeout(() => {
                                            window.autoOkButton.clickOkButton();
                                        }, 100);
                                    }
                                }
                            }
                        });

                        // Observe body for modal additions
                        modalObserver.observe(document.body, {
                            childList: true,
                            subtree: true,
                            attributes: true,
                            attributeFilter: ['class']
                        });

                        console.log('✅ Auto OK button observer active');

                        // Also check if there's already a modal visible
                        setTimeout(() => {
                            if (window.autoOkButton.isModalVisible()) {
                                console.log('🔔 Modal already visible, attempting auto-click...');
                                window.autoOkButton.clickOkButton();
                            }
                        }, 500);
                    } else {
                        console.warn('⚠️ Auto OK button module not loaded');
                    }
                } else {
                    console.log('ℹ️ Auto OK button feature disabled');
                }
            },
            description: 'Sales order creation page enhancements'
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
    window.malchusRouter = {
        routes: ROUTES,
        routeScripts: routeScripts,
        currentHash: () => window.location.hash
    };

    console.log('✅ Malchus Extension JS Router initialized');
    console.log('🔧 Debug with: window.malchusRouter');
})();
