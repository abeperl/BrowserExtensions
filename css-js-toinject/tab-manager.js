/**
 * Tab Manager - Smart Tab Reuse System
 *
 * Intercepts window.open() calls and converts them to reusable tabs.
 * Each unique URL (ignoring query params) gets its own persistent tab.
 *
 * Features:
 * - Converts window.open() popups to tabs
 * - Reuses existing tabs for the same base URL
 * - Ignores URL parameters when determining tab identity
 * - No waiting between tabs (all open in parallel)
 */

const TabManager = (() => {
    // Store tab references by normalized URL
    const tabRegistry = new Map();

    // Store original window.open for direct calls
    let originalWindowOpen = null;

    // Configuration
    const config = {
        enabled: true,
        debugMode: true,
        reuseDelay: 100  // Small delay to ensure tab is ready for reuse
    };

    /**
     * Normalizes a URL by removing query parameters and hash fragments
     * @param {string} url - The URL to normalize
     * @returns {string} - The normalized URL (base path only)
     */
    function normalizeUrl(url) {
        if (!url) return '';

        // Handle relative URLs (hash-based routes)
        if (url.startsWith('#')) {
            // Remove query params from hash
            const hashBase = url.split('?')[0];
            return hashBase;
        }

        // Handle absolute URLs
        try {
            const urlObj = new URL(url, window.location.origin);
            // Return origin + pathname (no search params or hash)
            return urlObj.origin + urlObj.pathname;
        } catch (e) {
            // If URL parsing fails, just split on '?'
            return url.split('?')[0].split('#')[0];
        }
    }

    /**
     * Gets or creates a tab for the given URL
     * @param {string} url - The URL to open
     * @param {string} normalizedUrl - The normalized URL for tab identity
     * @returns {Window} - The tab window reference
     */
    function getOrCreateTab(url, normalizedUrl) {
        // Check if we have an existing tab for this normalized URL
        if (tabRegistry.has(normalizedUrl)) {
            const existingTab = tabRegistry.get(normalizedUrl);

            // Check if tab is still open and valid
            if (existingTab && !existingTab.closed) {
                if (config.debugMode) {
                    console.log('♻️ Reusing existing tab:', normalizedUrl);
                }

                // Navigate the existing tab to the new URL (may have different params)
                try {
                    existingTab.location.href = url;
                    existingTab.focus();
                    return existingTab;
                } catch (e) {
                    // Tab may be on different origin, can't navigate
                    console.warn('⚠️ Cannot navigate existing tab, opening new:', e.message);
                    // Remove dead reference
                    tabRegistry.delete(normalizedUrl);
                }
            } else {
                // Tab was closed, remove from registry
                if (config.debugMode) {
                    console.log('🗑️ Removing closed tab from registry:', normalizedUrl);
                }
                tabRegistry.delete(normalizedUrl);
            }
        }

        // Create new tab using ORIGINAL window.open (not intercepted version)
        if (config.debugMode) {
            console.log('🆕 Creating new tab:', normalizedUrl);
        }

        // CRITICAL: Use originalWindowOpen to avoid infinite recursion
        const newTab = originalWindowOpen.call(window, url, '_blank');

        if (newTab) {
            tabRegistry.set(normalizedUrl, newTab);
        }

        return newTab;
    }

    /**
     * Intercepts window.open and converts to managed tabs
     */
    function installInterceptor() {
        if (window.open._tabManagerInstalled) {
            console.log('ℹ️ Tab Manager interceptor already installed');
            return;
        }

        // Store the original window.open function
        originalWindowOpen = window.open;

        window.open = function(url, target, features) {
            if (!config.enabled) {
                // Pass through if disabled
                return originalWindowOpen.apply(this, arguments);
            }

            // If specific target is requested (not _blank or undefined), honor it
            if (target && target !== '_blank' && target !== '') {
                if (config.debugMode) {
                    console.log('ℹ️ Specific target requested, using original:', target);
                }
                return originalWindowOpen.apply(this, arguments);
            }

            if (config.debugMode) {
                console.log('🪟 Tab Manager intercepting window.open:');
                console.log('   URL:', url);
                console.log('   Target:', target);
                console.log('   Features:', features);
            }

            // Normalize the URL to determine tab identity
            const normalizedUrl = normalizeUrl(url);

            if (config.debugMode) {
                console.log('   Normalized URL:', normalizedUrl);
            }

            // Get or create tab
            const tab = getOrCreateTab(url, normalizedUrl);

            if (config.debugMode) {
                console.log('✅ Tab opened/reused');
                console.log('   Total tabs managed:', tabRegistry.size);
            }

            return tab;
        };

        // Mark as installed
        window.open._tabManagerInstalled = true;

        console.log('✅ Tab Manager interceptor installed');
        console.log('💡 All window.open() calls will now use reusable tabs');
    }

    /**
     * Uninstalls the interceptor (for debugging)
     */
    function uninstallInterceptor() {
        // Note: We can't easily restore the original without storing it globally
        console.warn('⚠️ Cannot uninstall interceptor after installation');
        config.enabled = false;
        console.log('ℹ️ Tab Manager disabled (interceptor still installed but pass-through)');
    }

    /**
     * Clears all tab references (doesn't close tabs)
     */
    function clearRegistry() {
        const count = tabRegistry.size;
        tabRegistry.clear();
        console.log(`🗑️ Cleared ${count} tab reference(s) from registry`);
    }

    /**
     * Closes all managed tabs
     */
    function closeAllTabs() {
        let closedCount = 0;

        tabRegistry.forEach((tab, url) => {
            try {
                if (tab && !tab.closed) {
                    tab.close();
                    closedCount++;
                }
            } catch (e) {
                console.warn('⚠️ Could not close tab:', url, e.message);
            }
        });

        tabRegistry.clear();
        console.log(`🗑️ Closed ${closedCount} tab(s)`);
    }

    /**
     * Gets status of managed tabs
     */
    function getStatus() {
        const status = {
            totalTabs: tabRegistry.size,
            openTabs: 0,
            closedTabs: 0,
            tabs: []
        };

        tabRegistry.forEach((tab, url) => {
            const isOpen = tab && !tab.closed;

            if (isOpen) {
                status.openTabs++;
            } else {
                status.closedTabs++;
            }

            status.tabs.push({
                url,
                isOpen,
                reference: tab
            });
        });

        return status;
    }

    /**
     * Prints debug information
     */
    function printStatus() {
        const status = getStatus();

        console.log('📊 Tab Manager Status:');
        console.log('   Total tabs:', status.totalTabs);
        console.log('   Open tabs:', status.openTabs);
        console.log('   Closed tabs:', status.closedTabs);
        console.log('   Enabled:', config.enabled);
        console.log('   Debug mode:', config.debugMode);

        if (status.tabs.length > 0) {
            console.log('   Tabs:');
            status.tabs.forEach(tab => {
                console.log(`     - ${tab.url} (${tab.isOpen ? 'OPEN' : 'CLOSED'})`);
            });
        }
    }

    // Public API
    return {
        install: installInterceptor,
        uninstall: uninstallInterceptor,
        clearRegistry,
        closeAllTabs,
        getStatus,
        printStatus,
        config,

        // Helper methods
        enable: () => {
            config.enabled = true;
            console.log('✅ Tab Manager enabled');
        },
        disable: () => {
            config.enabled = false;
            console.log('⚠️ Tab Manager disabled');
        },
        setDebug: (enabled) => {
            config.debugMode = enabled;
            console.log(`🔧 Debug mode ${enabled ? 'enabled' : 'disabled'}`);
        }
    };
})();

// Auto-install when loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        TabManager.install();
    });
} else {
    TabManager.install();
}

// Expose global API for debugging
if (typeof window !== 'undefined') {
    window.TabManager = TabManager;

    console.log('✅ Tab Manager loaded');
    console.log('🔧 Debug with: window.TabManager');
    console.log('💡 Usage:');
    console.log('   TabManager.printStatus()  - View current tabs');
    console.log('   TabManager.closeAllTabs() - Close all managed tabs');
    console.log('   TabManager.clearRegistry() - Clear tab references');
    console.log('   TabManager.disable()      - Disable tab management');
    console.log('   TabManager.enable()       - Enable tab management');
}
