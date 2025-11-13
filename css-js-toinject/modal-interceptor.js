/**
 * Modal Interceptor - Replace native modal dialogs with OverlayManager
 *
 * Intercepts calls to modal.show() and displays them using OverlayManager
 * for a cleaner, non-blocking user experience.
 *
 * Route: #outbound/ProcessPersonalizedOrderItems
 */

(function() {
    'use strict';

    console.log('🔄 Modal Interceptor - Loading...');

    // =============================================================================
    // CONFIGURATION
    // =============================================================================

    const CONFIG = {
        enabled: true,
        defaultDuration: 3000,
        autoCallback: true,  // Automatically trigger "ok" callback after display
        debugMode: true
    };

    // =============================================================================
    // MODAL INTERCEPTION
    // =============================================================================

    let originalModal = null;
    let modalIntercepted = false;

    /**
     * Determine overlay type based on title/content
     */
    function detectModalType(title, content) {
        const titleStr = String(title).toLowerCase();
        const contentStr = String(content).toLowerCase();
        const combined = titleStr + ' ' + contentStr;

        // Check for error indicators
        if (combined.includes('error') ||
            combined.includes('failed') ||
            combined.includes('invalid') ||
            combined.includes('not found')) {
            return 'error';
        }

        // Check for warning indicators
        if (combined.includes('warning') ||
            combined.includes('caution') ||
            combined.includes('are you sure')) {
            return 'warning';
        }

        // Check for success indicators
        if (combined.includes('success') ||
            combined.includes('completed') ||
            combined.includes('saved')) {
            return 'success';
        }

        // Default to info
        return 'info';
    }

    /**
     * Extract text content from HTML or jQuery object
     */
    function extractContent(content) {
        if (typeof content === 'string') {
            // If HTML string, strip tags for display
            const temp = document.createElement('div');
            temp.innerHTML = content;
            return temp.textContent || temp.innerText || content;
        }

        if (typeof content === 'object') {
            // jQuery object or DOM element
            if (content.jquery) {
                return content.text() || content.html();
            }
            if (content.textContent) {
                return content.textContent;
            }
            if (content.innerText) {
                return content.innerText;
            }
        }

        return String(content);
    }

    /**
     * Extract title from various input types
     */
    function extractTitle(title) {
        if (typeof title === 'object') {
            // jQuery object - might be existing modal
            if (title.jquery) {
                const titleEl = title.find('.modal-box-title');
                if (titleEl.length) {
                    return titleEl.text();
                }
                return title.attr('id') || 'Modal';
            }

            // DOM element
            if (title.querySelector) {
                const titleEl = title.querySelector('.modal-box-title');
                return titleEl ? titleEl.textContent : 'Modal';
            }
        }

        return String(title);
    }

    /**
     * Intercept modal.show() calls
     */
    function interceptModal() {
        // Wait for modal object to be available
        if (typeof modal === 'undefined') {
            if (CONFIG.debugMode) {
                console.log('⏳ Waiting for modal object...');
            }
            return false;
        }

        if (modalIntercepted) {
            return true; // Already intercepted
        }

        // Save original modal.show
        originalModal = modal.show;

        // Replace with interceptor
        modal.show = function(title, content, buttons, callback, hideOnEscape) {
            if (CONFIG.debugMode) {
                console.log('🔔 Modal intercepted:', {
                    title: title,
                    content: typeof content === 'string' ? content.substring(0, 100) : content,
                    buttons: buttons,
                    hasCallback: !!callback
                });
            }

            // If disabled, use original modal
            if (!CONFIG.enabled) {
                return originalModal.apply(this, arguments);
            }

            // Extract readable text
            const titleText = extractTitle(title);
            const contentText = extractContent(content);
            const modalType = detectModalType(titleText, contentText);

            if (CONFIG.debugMode) {
                console.log('📊 Modal details:', {
                    type: modalType,
                    title: titleText,
                    message: contentText.substring(0, 100),
                    callback: !!callback
                });
            }

            // Check if OverlayManager is available
            if (typeof OverlayManager === 'undefined') {
                console.warn('⚠️ OverlayManager not available, using original modal');
                return originalModal.apply(this, arguments);
            }

            // Display with OverlayManager
            OverlayManager.show(modalType, {
                title: titleText,
                message: contentText,
                duration: CONFIG.defaultDuration
            });

            // Auto-trigger callback if enabled
            if (CONFIG.autoCallback && callback) {
                if (CONFIG.debugMode) {
                    console.log('🔄 Auto-triggering callback with "ok"');
                }
                setTimeout(() => {
                    try {
                        callback('ok');
                    } catch (error) {
                        console.error('❌ Callback error:', error);
                    }
                }, 100);
            }

            // Return undefined (no need to return anything)
            return undefined;
        };

        // Preserve original hide function
        modal._originalHide = modal.hide;

        modalIntercepted = true;
        console.log('✅ Modal interceptor installed');
        return true;
    }

    // =============================================================================
    // GLOBAL API
    // =============================================================================

    if (typeof window !== 'undefined') {
        window.modalInterceptor = {
            // Configuration
            config: CONFIG,

            // Control functions
            enable: () => {
                CONFIG.enabled = true;
                console.log('✅ Modal interceptor enabled');
            },

            disable: () => {
                CONFIG.enabled = false;
                console.log('❌ Modal interceptor disabled');
            },

            // Restore original modal
            restore: () => {
                if (originalModal && typeof modal !== 'undefined') {
                    modal.show = originalModal;
                    modalIntercepted = false;
                    console.log('🔙 Original modal restored');
                }
            },

            // Check status
            isIntercepted: () => modalIntercepted,

            // Utilities
            setDuration: (ms) => {
                CONFIG.defaultDuration = ms;
                console.log(`⏱️ Default duration set to ${ms}ms`);
            },

            setAutoCallback: (enabled) => {
                CONFIG.autoCallback = enabled;
                console.log(`🔧 Auto-callback ${enabled ? 'enabled' : 'disabled'}`);
            }
        };

        console.log('✅ Modal Interceptor API loaded');
        console.log('🔧 Debug API: window.modalInterceptor');
        console.log('💡 Disable: window.modalInterceptor.disable()');
        console.log('💡 Restore: window.modalInterceptor.restore()');
    }

    // =============================================================================
    // INITIALIZATION
    // =============================================================================

    function initialize() {
        // Try to intercept immediately
        if (!interceptModal()) {
            // If modal not available, try again after a delay
            if (CONFIG.debugMode) {
                console.log('⏳ Modal not ready, retrying in 1s...');
            }
            setTimeout(() => {
                if (!interceptModal()) {
                    console.warn('⚠️ Modal object not found after retry');
                }
            }, 1000);
        }
    }

    // Run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Also try after page is fully loaded
    window.addEventListener('load', () => {
        if (!modalIntercepted) {
            interceptModal();
        }
    });

    console.log('📌 Modal Interceptor ready');
})();
