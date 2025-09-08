// ===== SIMPLE SIDEBAR FIX - ENHANCED WITH DEBUGGING =====

(function() {
    'use strict';

    console.log('🎯 Enhanced Sidebar Fix - Starting...');

    // Enhanced debugging function
    function debugLog(message, data) {
        console.log(`🔍 ${message}`, data || '');
    }

    // Find sidebar element with multiple selectors
    function findSidebar() {
        const sidebarSelectors = [
            '#sidebar',
            '.sidebar',
            '.offcanvas',
            '.offcanvas-start',
            '[data-bs-target="#sidebar"]',
            '.sidebar-menu',
            '.navigation',
            '.nav-sidebar',
            '.main-sidebar'
        ];

        for (const selector of sidebarSelectors) {
            const element = document.querySelector(selector);
            if (element) {
                // Ignore elements we injected ourselves
                if (element.dataset && element.dataset.__injected) continue;
                debugLog(`Found sidebar with selector: ${selector}`, element);
                return element;
            }
        }

        // Look for any element that might be a sidebar
        const allElements = document.querySelectorAll('*');
        for (const element of allElements) {
            // Skip any element we injected
            try {
                if (element.dataset && element.dataset.__injected) continue;
            } catch (e) {}
            const classList = (element.className || '').toLowerCase();
            const id = (element.id || '').toLowerCase();
            if (classList.includes('sidebar') || classList.includes('nav') ||
                id.includes('sidebar') || id.includes('nav')) {
                debugLog('Found potential sidebar by class/id scan:', element);
                return element;
            }
        }

        debugLog('No sidebar found with any selector');
        return null;
    }

    // Enhanced manual toggle function (two-mode: open or closed)
    function manualToggleSidebar(sidebar) {
        debugLog('Using enhanced manual toggle');

        if (!sidebar) {
            console.error('❌ No sidebar element provided to toggle');
            return;
        }

        // Determine open state by a single source of truth: the body class if present, else by computed visibility
        const cs = window.getComputedStyle(sidebar);
        const isOpen = document.body.classList.contains('enhanced-sidebar-open') || sidebar.classList.contains('show') || (cs.display !== 'none' && cs.visibility !== 'hidden');
        debugLog('Current sidebar state (isOpen):', { isOpen, classList: sidebar.className });

        if (isOpen) {
            // Close
            sidebar.classList.remove('show', 'open');
            sidebar.classList.add('hide');
            try { sidebar.style.setProperty('transform', 'translateX(-100%)', 'important'); } catch (e) { sidebar.style.transform = 'translateX(-100%)'; }
            try { sidebar.style.setProperty('display', 'none', 'important'); } catch (e) { sidebar.style.display = 'none'; }
            try { sidebar.style.setProperty('visibility', 'hidden', 'important'); } catch (e) { sidebar.style.visibility = 'hidden'; }
            try { sidebar.style.setProperty('opacity', '0', 'important'); } catch (e) { sidebar.style.opacity = '0'; }
            sidebar.setAttribute('aria-hidden', 'true');

            const backdrop = document.querySelector('.sidebar-backdrop');
            if (backdrop) backdrop.remove();

            // Remove page-shift marker and CSS var
            try { document.body.classList.remove('enhanced-sidebar-open'); } catch (e) {}
            try { document.documentElement.style.removeProperty('--enhanced-sidebar-width'); } catch (e) {}

            debugLog('Sidebar hidden (closed mode)');
            return;
        }

        // Open: prefer site's toggler or bootstrap if available
        const siteToggler = document.querySelector('#toggle-sidenav') || document.querySelector('[toggle][toggle-class]') || document.querySelector('[data-toggle][data-toggle-class]') || document.querySelector('[toggle="#sidebar"]') || document.querySelector('[data-toggle="#sidebar"]') || document.querySelector('button[aria-controls="sidebar"]');
        if (siteToggler) {
            try {
                debugLog('Attempting to use site toggler to open sidebar:', siteToggler);
                siteToggler.click();
            } catch (err) {
                debugLog('Site toggler click failed, will fallback', err);
            }

            // Fallback after site toggler
            setTimeout(() => {
                const s = findSidebar();
                if (!s) return debugLog('Site toggler fallback: no sidebar found');
                const cs2 = window.getComputedStyle(s);
                const rect = s.getBoundingClientRect();
                const visibleByStyle = cs2 && cs2.display !== 'none' && cs2.visibility !== 'hidden' && parseFloat(cs2.opacity || '1') > 0;
                const visibleByRect = rect && (rect.width > 0 || rect.height > 0);
                const becameVisible = visibleByStyle && visibleByRect;
                debugLog('Site toggler fallback check:', { visibleByStyle, visibleByRect, classList: s.className, computedStyle: { display: cs2.display, visibility: cs2.visibility, transform: cs2.transform } });
                if (!becameVisible) {
                    debugLog('Site toggler did not render sidebar; forcing manual show');
                    doManualShow(s || sidebar);
                } else {
                    // Ensure content shifts when the site toggler succeeded
                    try {
                        const w = rect.width ? `${Math.round(rect.width)}px` : '260px';
                        document.documentElement.style.setProperty('--enhanced-sidebar-width', w);
                        document.body.classList.add('enhanced-sidebar-open');
                    } catch (e) {}
                    debugLog('Site toggler opened sidebar');
                }
            }, 350);

            return;
        }

        // Try bootstrap offcanvas if available
        if (window.bootstrap && window.bootstrap.Offcanvas && sidebar) {
            try {
                let inst = window.bootstrap.Offcanvas.getInstance(sidebar);
                if (!inst) inst = new window.bootstrap.Offcanvas(sidebar);
                inst.toggle();
                debugLog('Tried bootstrap Offcanvas.toggle()');

                // Fallback check
                setTimeout(() => {
                    const s = findSidebar();
                    if (!s) return debugLog('Bootstrap fallback check: no sidebar found');
                    const cs3 = window.getComputedStyle(s);
                    const rect = s.getBoundingClientRect();
                    const visibleByStyle = cs3 && cs3.display !== 'none' && cs3.visibility !== 'hidden' && parseFloat(cs3.opacity || '1') > 0;
                    const visibleByRect = rect && (rect.width > 0 || rect.height > 0);
                    const becameVisible = visibleByStyle && visibleByRect;
                    debugLog('Bootstrap fallback check state:', { visibleByStyle, visibleByRect, classList: s.className, computedStyle: { display: cs3.display, visibility: cs3.visibility, transform: cs3.transform } });
                    if (!becameVisible) {
                        debugLog('Bootstrap toggle did not actually render sidebar; invoking manual toggle fallback');
                        doManualShow(s || sidebar);
                    } else {
                        try {
                            const w = rect.width ? `${Math.round(rect.width)}px` : '260px';
                            document.documentElement.style.setProperty('--enhanced-sidebar-width', w);
                            document.body.classList.add('enhanced-sidebar-open');
                        } catch (e) {}
                        debugLog('Bootstrap toggle opened sidebar (rendered), no fallback needed');
                    }
                }, 350);

                return;
            } catch (err) {
                debugLog('Bootstrap Offcanvas toggle attempt failed, will fallback if needed', err);
            }
        }

        // Last resort: manual show
        doManualShow(sidebar);
    }

    // Helper that forcibly shows the sidebar and defends against quick re-hides from site scripts
    function doManualShow(s) {
        if (!s) return debugLog('doManualShow: no sidebar provided');

        try {
            // Basic show styles
            const affirm = () => {
                try { s.classList.add('show'); } catch (e) {}
                try { s.classList.remove('hide'); } catch (e) {}
                try { s.style.setProperty('display', 'block', 'important'); } catch (e) { s.style.display = 'block'; }
                try { s.style.setProperty('visibility', 'visible', 'important'); } catch (e) { s.style.visibility = 'visible'; }
                try { s.style.setProperty('opacity', '1', 'important'); } catch (e) { s.style.opacity = '1'; }
                try { s.style.setProperty('transform', 'translateX(0)', 'important'); } catch (e) { s.style.transform = 'translateX(0)'; }
                try { s.style.setProperty('z-index', '1050', 'important'); } catch (e) { s.style.zIndex = '1050'; }
                try { s.setAttribute('aria-hidden', 'false'); } catch (e) {}

                // set page-level CSS var and marker to shift main content
                try {
                    const rect = s.getBoundingClientRect();
                    const w = rect.width ? `${Math.round(rect.width)}px` : '260px';
                    document.documentElement.style.setProperty('--enhanced-sidebar-width', w);
                    document.body.classList.add('enhanced-sidebar-open');
                } catch (e) {}
            };

            // Do immediate affirm
            affirm();

            // Ensure backdrop
            let backdrop = document.querySelector('.sidebar-backdrop');
            if (!backdrop) {
                backdrop = document.createElement('div');
                backdrop.className = 'sidebar-backdrop';
                backdrop.setAttribute('aria-hidden', 'true');
                backdrop.dataset.__injected = 'true';
                document.body.appendChild(backdrop);
            }
            backdrop.onclick = () => manualToggleSidebar(s);

            debugLog('Sidebar shown (manual - forced)');

            // Re-assert display repeatedly for a short window to outlast racing scripts
            const intervalMs = 150;
            const totalMs = 2500;
            let elapsed = 0;
            const ti = setInterval(() => {
                affirm();
                elapsed += intervalMs;
                if (elapsed >= totalMs) {
                    clearInterval(ti);
                }
            }, intervalMs);

            // Also observe attribute changes and re-assert if needed for a short period
            const obs = new MutationObserver((mutations) => {
                for (const m of mutations) {
                    if (m.type === 'attributes' && (m.attributeName === 'style' || m.attributeName === 'class')) {
                        const cs2 = window.getComputedStyle(s);
                        const rect2 = s.getBoundingClientRect();
                        if ((cs2.display === 'none' || cs2.visibility === 'hidden') || (rect2.width === 0 && rect2.height === 0)) {
                            debugLog('MutationObserver detected re-hide; reasserting display:block');
                            try { s.style.setProperty('display', 'block', 'important'); } catch (e) { s.style.display = 'block'; }
                            try { s.style.setProperty('visibility', 'visible', 'important'); } catch (e) { s.style.visibility = 'visible'; }
                            try { s.style.setProperty('transform', 'translateX(0)', 'important'); } catch (e) { s.style.transform = 'translateX(0)'; }
                        }
                    }
                }
            });
            obs.observe(s, { attributes: true, attributeFilter: ['style', 'class'], subtree: false });
            setTimeout(() => { try { obs.disconnect(); } catch (e) {} }, totalMs + 200);

        } catch (err) {
            debugLog('Error while forcing manual show', err);
        }
    }

    // Find and enhance the existing mobile button
    function enhanceExistingButton() {
        // More comprehensive button selectors
        const buttonSelectors = [
            'button[data-bs-toggle="offcanvas"]',
            'button[data-toggle="offcanvas"]',
            '.navbar-toggler',
            '.mobile-btn',
            '#mobile_btn',
            '.btn[data-bs-toggle="offcanvas"]',
            '.hamburger',
            '.menu-btn',
            '.sidebar-toggle',
            'button[aria-controls="sidebar"]',
            'button[aria-label*="menu"]',
            'button[aria-label*="sidebar"]',
            '.fa-bars',
            '.glyphicon-menu-hamburger',
            '[class*="hamburger"]',
            '[class*="menu"]'
        ];

        let foundButton = null;

        debugLog('Searching for button with selectors:', buttonSelectors);

        for (const selector of buttonSelectors) {
            foundButton = document.querySelector(selector);
            if (foundButton) {
                debugLog(`✅ Found existing button: ${selector}`, foundButton);
                break;
            }
        }

        // If no button found, look for any button that might be a menu button
        if (!foundButton) {
            const allButtons = document.querySelectorAll('button, .btn, [role="button"]');
            for (const button of allButtons) {
                const text = button.textContent.toLowerCase();
                const ariaLabel = button.getAttribute('aria-label') || '';
                const classList = button.className.toLowerCase();

                if (text.includes('menu') || text.includes('☰') || text.includes('≡') ||
                    ariaLabel.toLowerCase().includes('menu') ||
                    classList.includes('hamburger') || classList.includes('menu')) {
                    foundButton = button;
                    debugLog('Found potential menu button by content scan:', button);
                    break;
                }
            }
        }

        if (foundButton) {
            debugLog('Enhancing found button (non-invasive):', foundButton);

            // Make it visible but avoid overriding all the site's styles.
            try {
                foundButton.style.zIndex = foundButton.style.zIndex || '10000';
                // Only set minimal layout styles if the button appears hidden
                const cs = window.getComputedStyle(foundButton);
                if (cs.display === 'none' || cs.visibility === 'hidden' || parseFloat(cs.opacity) === 0) {
                    foundButton.style.display = 'inline-flex';
                    foundButton.style.visibility = 'visible';
                    foundButton.style.opacity = '1';
                    foundButton.style.position = foundButton.style.position || 'fixed';
                    foundButton.style.top = foundButton.style.top || '15px';
                    foundButton.style.left = foundButton.style.left || '15px';
                }
            } catch (err) {
                debugLog('Could not safely adjust button styles', err);
            }

            // If the element already has native attributes or an onclick handler, prefer triggering it
            const hasNativeToggle = foundButton.getAttribute('data-bs-toggle') || foundButton.getAttribute('data-toggle') || foundButton.onclick;
            if (!foundButton.id) foundButton.id = 'injected-found-sidebar-btn';

            // Attach a gentle listener that delegates to the site's own handler when possible
            foundButton.addEventListener('click', function delegatedClick(e) {
                debugLog('Enhanced button clicked');

                // Prefer using Bootstrap's Offcanvas API when available (this will run native animation and state)
                const sidebar = findSidebar();
                if (window.bootstrap && window.bootstrap.Offcanvas && sidebar) {
                    try {
                        let inst = window.bootstrap.Offcanvas.getInstance(sidebar);
                        if (!inst) inst = new window.bootstrap.Offcanvas(sidebar);
                        inst.toggle();
                        debugLog('Tried bootstrap Offcanvas.toggle()');

                        // Schedule fallback check similar to native attribute fallback: if bootstrap didn't render the sidebar
                        setTimeout(() => {
                            const s = findSidebar();
                            if (!s) return debugLog('Bootstrap fallback check: no sidebar found');
                            const cs = window.getComputedStyle(s);
                            const rect = s.getBoundingClientRect();
                            const visibleByStyle = cs && cs.display !== 'none' && cs.visibility !== 'hidden' && parseFloat(cs.opacity || '1') > 0;
                            const visibleByRect = rect && (rect.width > 0 || rect.height > 0);
                            const becameVisible = visibleByStyle && visibleByRect;

                            debugLog('Bootstrap fallback check state:', { visibleByStyle, visibleByRect, classList: s.className, computedStyle: { display: cs.display, visibility: cs.visibility, transform: cs.transform } });

                            if (!becameVisible) {
                                debugLog('Bootstrap toggle did not actually render sidebar; invoking manual toggle fallback');
                                manualToggleSidebar(s);
                            } else {
                                debugLog('Bootstrap toggle opened sidebar (rendered), no fallback needed');
                            }
                        }, 350);

                        return;
                    } catch (err) {
                        debugLog('Bootstrap Offcanvas toggle attempt failed, will fallback if needed', err);
                    }
                }

                // If element declares native attributes (data-bs-toggle) we let the native handler run,
                // but schedule a short fallback check: if the sidebar did not become visible, perform our manual toggle.
                if (hasNativeToggle) {
                    debugLog('Has native toggle attribute - delegating to native behavior and scheduling fallback check');
                    // Give site handlers a moment to run and change DOM; if nothing actually rendered, fallback
                    setTimeout(() => {
                        const s = findSidebar();
                        if (!s) return debugLog('Fallback check: no sidebar found');
                        const cs = window.getComputedStyle(s);
                        const rect = s.getBoundingClientRect();
                        const visibleByStyle = cs && cs.display !== 'none' && cs.visibility !== 'hidden' && parseFloat(cs.opacity || '1') > 0;
                        const visibleByRect = rect && (rect.width > 0 || rect.height > 0);
                        const becameVisible = visibleByStyle && visibleByRect;

                        debugLog('Fallback check state:', { visibleByStyle, visibleByRect, classList: s.className, computedStyle: { display: cs.display, visibility: cs.visibility, transform: cs.transform } });

                        if (!becameVisible) {
                            debugLog('Native toggle did not actually render sidebar; invoking manual toggle fallback');
                            manualToggleSidebar(s);
                        } else {
                            debugLog('Native toggle opened sidebar (rendered), no fallback needed');
                        }
                    }, 350);
                    return;
                }

                // Otherwise, perform our toggle (without preventing default/propagation)
                if (sidebar) {
                    manualToggleSidebar(sidebar);
                } else {
                    debugLog('No sidebar found on enhanced click');
                }
            }, { passive: true });

            // Keyboard shortcut
            document.addEventListener('keydown', function(e) {
                if (e.altKey && e.key === 'm') {
                    e.preventDefault();
                    foundButton.click();
                }
            });

            debugLog('✅ Enhanced existing button (non-invasive)');
            return true;
        }

        debugLog('❌ No existing button found');
        return false;
    }

    // Create a manual button if no existing button is found
    function createManualButton() {
        debugLog('Creating manual sidebar button');

    const button = document.createElement('button');
    button.id = 'enhanced-menu-btn';
    button.dataset.__injected = 'true';
        button.innerHTML = '☰';
        button.title = 'Toggle Sidebar (Alt+M)';
        button.style.cssText = `
            position: fixed !important;
            top: 15px !important;
            left: 15px !important;
            z-index: 10000 !important;
            background: #ff8c00 !important;
            border: none !important;
            border-radius: 6px !important;
            padding: 8px 12px !important;
            cursor: pointer !important;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1) !important;
            color: white !important;
            font-size: 18px !important;
            width: 40px !important;
            height: 40px !important;
            display: flex !important;
            align-items: center !important;
            justify-content: center !important;
            font-weight: bold !important;
        `;

        button.addEventListener('click', function(e) {
            e.preventDefault();
            debugLog('Manual button clicked');

            const sidebar = findSidebar();
            if (sidebar) {
                manualToggleSidebar(sidebar);
            } else {
                console.error('❌ No sidebar found for manual toggle');
            }
        });

        // Add keyboard shortcut
        document.addEventListener('keydown', function(e) {
            if (e.altKey && e.key === 'm') {
                e.preventDefault();
                button.click();
            }
        });

        document.body.appendChild(button);
        debugLog('✅ Created manual sidebar button');
        return true;
    }

    // Initialize the enhanced fix
    function initEnhancedFix() {
        debugLog('🚀 Initializing Enhanced Sidebar Fix...');

        // First, try to enhance existing button
        if (enhanceExistingButton()) {
            debugLog('✅ Enhanced existing button');
        } else {
            // If no existing button, create a manual one
            debugLog('No existing button found, creating manual button');
            createManualButton();
        }

        // Ensure sidebar is properly configured
        const sidebar = findSidebar();
        if (sidebar) {
            // Make sure sidebar has proper classes
            if (!sidebar.classList.contains('offcanvas')) {
                sidebar.classList.add('offcanvas', 'offcanvas-start');
            }

            // Ensure proper attributes
            sidebar.setAttribute('tabindex', '-1');
            sidebar.setAttribute('aria-labelledby', 'sidebarLabel');

            // Start hidden
            if (!sidebar.classList.contains('show')) {
                sidebar.style.display = 'none';
                sidebar.style.transform = 'translateX(-100%)';
                sidebar.setAttribute('aria-hidden', 'true');
            }

            debugLog('✅ Sidebar configured properly');
        } else {
            debugLog('⚠️ No sidebar found to configure');
        }

        // Global API for manual control
        window.enhancedSidebar = {
            findButton: enhanceExistingButton,
            findSidebar: findSidebar,
            toggle: function() {
                const btn = document.querySelector('#enhanced-menu-btn, #injected-found-sidebar-btn, button[data-bs-toggle="offcanvas"]');
                if (btn) {
                    btn.click();
                } else {
                    const sidebar = findSidebar();
                    if (sidebar) {
                        manualToggleSidebar(sidebar);
                    }
                }
            },
            debug: function() {
            debugLog('Enhanced Sidebar Debug Info:', {
                button: document.querySelector('#enhanced-menu-btn, #injected-found-sidebar-btn, button[data-bs-toggle="offcanvas"]'),
                    sidebar: findSidebar(),
                    bootstrap: !!(window.bootstrap && window.bootstrap.Offcanvas)
                });
            }
        };

        console.log('✅ Enhanced Sidebar Fix initialized! Use Alt+M or click the orange button to toggle.');
        console.log('🔧 Debug with: window.enhancedSidebar.debug()');
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initEnhancedFix);
    } else {
        initEnhancedFix();
    }

    // Fallback initialization with multiple attempts
    setTimeout(initEnhancedFix, 500);
    setTimeout(initEnhancedFix, 2000);
    setTimeout(initEnhancedFix, 5000);

})();