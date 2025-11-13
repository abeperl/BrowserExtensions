/**
 * Global Box Label Monitor
 * Monitors all windows for box label content and injects formatting CSS
 * Works with or without TabManager
 */

(function() {
    'use strict';

    // Only install once globally
    if (window._globalBoxLabelMonitorInstalled) {
        console.log('ℹ️ Global Box Label Monitor already installed');
        return;
    }

    console.log('📋 Installing Global Box Label Monitor...');

    // CSS to inject
    const customCSS = `
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
    `;

    // Track windows we've tried to inject into
    const processedWindows = new WeakSet();

    // Function to inject CSS
    function injectBoxLabelCSS(targetWindow) {
        try {
            if (!targetWindow || !targetWindow.document) {
                return false;
            }

            // Skip if we've already processed this window
            if (processedWindows.has(targetWindow)) {
                return false;
            }

            const doc = targetWindow.document;

            // Check if document is ready
            if (doc.readyState === 'loading') {
                console.log('⏳ Document still loading, will retry...');
                return false;
            }

            // Check if this is a box label print window
            const boxLabelElement = doc.querySelector('.box-label-wrp');
            
            if (boxLabelElement) {
                console.log('🎯 Box label element detected!');

                // Check if CSS already injected
                if (!doc.getElementById('box-label-format-override')) {
                    const style = doc.createElement('style');
                    style.id = 'box-label-format-override';
                    style.textContent = customCSS;
                    doc.head.appendChild(style);

                    console.log('✅ BOX LABEL CSS INJECTED SUCCESSFULLY');
                    processedWindows.add(targetWindow);
                    return true;
                } else {
                    console.log('ℹ️ CSS already present');
                    processedWindows.add(targetWindow);
                    return false;
                }
            }
        } catch (error) {
            console.error('❌ Error in injectBoxLabelCSS:', error);
        }
        return false;
    }

    // Keep track of all opened windows
    const monitoredWindows = new Set();

    // Intercept window.open at the EARLIEST point
    const originalOpen = window.open;
    
    window.open = function(...args) {
        console.log('🔍 GLOBAL Box Label Monitor: window.open called');
        
        // Call original (or any previously wrapped version)
        const newWindow = originalOpen.apply(this, args);
        
        if (newWindow && !monitoredWindows.has(newWindow)) {
            console.log('👁️ Monitoring new window for box labels...');
            monitoredWindows.add(newWindow);
            
            // EARLY: Override print to ensure CSS is injected before dialog
            try {
                if (typeof newWindow.print === 'function') {
                    const origPrint = newWindow.print.bind(newWindow);
                    newWindow.print = function() {
                        const done = injectBoxLabelCSS(newWindow);
                        if (!done) {
                            setTimeout(() => {
                                injectBoxLabelCSS(newWindow);
                                try { origPrint(); } catch(_) {}
                            }, 50);
                            return;
                        }
                        return origPrint();
                    };
                    newWindow.addEventListener?.('beforeprint', () => injectBoxLabelCSS(newWindow));
                }
            } catch (_) { /* cross-origin or not allowed */ }

            // Try to inject immediately
            injectBoxLabelCSS(newWindow);
            
            // Try multiple times with increasing delays
            const attempts = [100, 300, 500, 800, 1200, 1500, 2000, 2500, 3000];
            attempts.forEach(delay => {
                setTimeout(() => {
                    injectBoxLabelCSS(newWindow);
                }, delay);
            });

            // Also listen for DOMContentLoaded if possible
            try {
                if (newWindow.document) {
                    newWindow.document.addEventListener('DOMContentLoaded', () => {
                        console.log('📄 DOMContentLoaded in new window');
                        injectBoxLabelCSS(newWindow);
                    });

                    // Watch for readystatechange
                    newWindow.document.addEventListener('readystatechange', () => {
                        if (newWindow.document.readyState === 'interactive' || 
                            newWindow.document.readyState === 'complete') {
                            console.log('📄 Document ready:', newWindow.document.readyState);
                            injectBoxLabelCSS(newWindow);
                        }
                    });
                }
            } catch (e) {
                console.log('⚠️ Cannot add event listeners (cross-origin?)');
            }
        }
        
        return newWindow;
    };

    // Monitor TabManager tabs if available
    function monitorTabManager() {
        if (typeof TabManager !== 'undefined' && TabManager.tabs) {
            console.log('🔍 Found TabManager, monitoring its tabs...');
            
            setInterval(() => {
                try {
                    for (const [url, tabInfo] of Object.entries(TabManager.tabs)) {
                        if (tabInfo.window && !tabInfo.window.closed && !monitoredWindows.has(tabInfo.window)) {
                            console.log('🆕 Found new TabManager window');
                            monitoredWindows.add(tabInfo.window);
                            
                            // Try to inject
                            injectBoxLabelCSS(tabInfo.window);
                            
                            // Keep trying
                            const attempts = [100, 300, 500, 800, 1200, 1500, 2000];
                            attempts.forEach(delay => {
                                setTimeout(() => {
                                    injectBoxLabelCSS(tabInfo.window);
                                }, delay);
                            });
                        }
                        
                        // Always try existing windows
                        if (tabInfo.window && !tabInfo.window.closed) {
                            injectBoxLabelCSS(tabInfo.window);
                        }
                    }
                } catch (error) {
                    console.error('Error monitoring TabManager:', error);
                }
            }, 300); // Check every 300ms
        }
    }

    // Try to monitor TabManager immediately
    monitorTabManager();

    // Keep checking if TabManager becomes available
    const checkTabManagerInterval = setInterval(() => {
        if (typeof TabManager !== 'undefined') {
            monitorTabManager();
            clearInterval(checkTabManagerInterval);
        }
    }, 500);

    // Stop checking after 10 seconds
    setTimeout(() => {
        clearInterval(checkTabManagerInterval);
    }, 10000);

    window._globalBoxLabelMonitorInstalled = true;
    console.log('✅ Global Box Label Monitor installed');
    console.log('💡 Monitor status: window._globalBoxLabelMonitorInstalled');

})();
