/**
 * Box Label Format Injector
 * Intercepts window.open for print windows and injects custom CSS
 * Targets the carton label print functionality
 */

(function() {
    'use strict';

    console.log('📋 Box Label Format Injector loading...');

    // CSS content to inject
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
        }

        /* Ensure customer info section has proper spacing */
        .ship-info-section {
            padding: 0 20px !important;
        }

        /* Adjust spacing between phone numbers */
        .top-info-section .text[style*="font-size: 30px"]:last-of-type {
            margin-right: 0 !important;
        }

        /* Center phone numbers within their container */
        .top-info-section {
            text-align: center !important;
        }
    `;

    // Store original window.open
    const originalWindowOpen = window.open;

    // Override window.open to ensure CSS is injected BEFORE print dialog
    window.open = function(...args) {
        console.log('🪟 window.open intercepted:', args);

        // Call original window.open
        const newWindow = originalWindowOpen.apply(this, args);

        if (!newWindow) {
            return newWindow;
        }

        console.log('✅ New window opened, preparing early CSS injection');

        // Track injection state per window
        let injected = false;

        function tryInject(targetWin, attemptNumber = 0) {
            try {
                if (!targetWin || !targetWin.document || injected) return false;
                const doc = targetWin.document;

                // Debug: Log document state
                if (attemptNumber === 1) {
                    console.log('🔍 Checking window:', {
                        readyState: doc.readyState,
                        hasBoxLabel: !!doc.querySelector('.box-label-wrp'),
                        bodyChildren: doc.body?.children.length || 0
                    });
                }

                // Only inject for box label windows
                const boxLabelElements = doc.querySelectorAll('.box-label-wrp');
                if (boxLabelElements.length === 0) return false;

                // Check if already injected
                if (doc.getElementById('box-label-format-override')) {
                    injected = true;
                    return true;
                }

                // Inject the CSS
                const style = doc.createElement('style');
                style.id = 'box-label-format-override';
                style.textContent = customCSS;
                doc.head.appendChild(style);
                injected = true;
                console.log('✅ BOX LABEL FORMAT CSS INJECTED!', {
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

        // Override print ASAP to gate printing until CSS is present (same-origin only)
        try {
            if (typeof newWindow.print === 'function') {
                const originalPrint = newWindow.print.bind(newWindow);
                newWindow.print = function() {
                    // Attempt immediate injection; if not ready, delay a tick
                    const done = tryInject(newWindow);
                    if (!done) {
                        // If document still loading or selector not yet present, wait briefly
                        setTimeout(() => {
                            tryInject(newWindow);
                            try { originalPrint(); } catch(_) { /* ignore */ }
                        }, 50);
                        return;
                    }
                    // CSS present – proceed to print
                    return originalPrint();
                };

                // Also handle beforeprint to double‑ensure
                newWindow.addEventListener?.('beforeprint', () => {
                    tryInject(newWindow);
                });
            }
        } catch (_) {
            // Cross-origin print override not possible – fall back to timed attempts
        }

        // Fire MANY rapid attempts to catch DOM after document.write() and before print()
        // The app calls print() at 1000ms, so we need to inject before that
        // Extended timing: check frequently from 50ms to 950ms
        const delays = [50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 800, 850, 900, 950];
        delays.forEach((delay, index) => {
            setTimeout(() => tryInject(newWindow, index + 1), delay);
        });

        // Also inject on DOM ready if accessible
        try {
            newWindow.document?.addEventListener('DOMContentLoaded', () => {
                console.log('📄 DOMContentLoaded event fired in print window');
                tryInject(newWindow, 'DOMContentLoaded');
            });
            newWindow.document?.addEventListener('readystatechange', () => {
                if (newWindow.document.readyState === 'complete') {
                    console.log('📄 Document ready state: complete');
                    tryInject(newWindow, 'readystatechange');
                }
            });
        } catch (_) { /* ignore */ }

        // Add MutationObserver to watch for box-label-wrp being added
        try {
            // Wait a bit for document.body to exist
            setTimeout(() => {
                try {
                    if (!newWindow.document?.body) return;

                    const observer = new MutationObserver((mutations) => {
                        if (injected) {
                            observer.disconnect();
                            return;
                        }

                        // Check if any mutations added .box-label-wrp
                        const hasBoxLabel = mutations.some(mutation => {
                            return Array.from(mutation.addedNodes).some(node => {
                                return node.nodeType === 1 && (
                                    node.classList?.contains('box-label-wrp') ||
                                    node.querySelector?.('.box-label-wrp')
                                );
                            });
                        });

                        if (hasBoxLabel) {
                            console.log('👀 MutationObserver detected .box-label-wrp added to DOM');
                            if (tryInject(newWindow, 'MutationObserver')) {
                                observer.disconnect();
                            }
                        }
                    });

                    observer.observe(newWindow.document.body, {
                        childList: true,
                        subtree: true
                    });

                    console.log('👀 MutationObserver installed on print window');
                } catch (_) { /* ignore */ }
            }, 10);
        } catch (_) { /* ignore */ }

        return newWindow;
    };

    console.log('✅ Box Label Format Injector installed');
    console.log('💡 All window.open() calls will be monitored for box label print windows');

})();
