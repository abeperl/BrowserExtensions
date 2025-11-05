/**
 * Status Dropdown Enhancement Functions
 * Adds dropdown for third-party item statuses
 * Auto-fills status-scan input with selected value
 */

(function() {
    'use strict';

    console.log('📦 Status Dropdown Functions Loading...');

    // =============================================================================
    // SENTRY CDN LOADER & INITIALIZATION
    // =============================================================================

    /**
     * Load Sentry SDK from CDN and initialize
     */
    function loadAndInitSentry() {
        // Check if Sentry is already loaded
        if (typeof Sentry !== 'undefined') {
            initializeSentry();
            return;
        }

        // Check if document.head is available
        if (!document.head) {
            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', loadAndInitSentry);
                return;
            }
            // DOM is ready but head still doesn't exist - this shouldn't happen
            console.warn('⚠️ document.head not available - cannot load Sentry SDK');
            return;
        }

        // Load Sentry SDK from CDN
        const script = document.createElement('script');
        script.src = 'https://js.sentry-cdn.com/104595fd1de31dc644958f35b4f325ec.min.js';
        script.crossOrigin = 'anonymous';
        script.onload = function() {
            console.log('✅ Sentry SDK loaded from CDN');
            initializeSentry();
        };
        script.onerror = function() {
            console.warn('⚠️ Failed to load Sentry SDK from CDN - error tracking disabled');
        };
        document.head.appendChild(script);
    }

    /**
     * Initialize Sentry with configuration
     */
    function initializeSentry() {
        if (typeof Sentry !== 'undefined') {
            try {
                Sentry.init({
                    dsn: "https://104595fd1de31dc644958f35b4f325ec@o4510313441853440.ingest.us.sentry.io/4510313480454144",
                    // Add context about the script
                    beforeSend(event) {
                        event.tags = event.tags || {};
                        event.tags.script = 'status-dropdown.js';
                        event.tags.feature = 'status-auto-fill';
                        return event;
                    },
                    // Capture unhandled errors
                    tracesSampleRate: 0,
                    environment: 'production'
                });
                console.log('✅ Sentry initialized for status-dropdown.js');
            } catch (error) {
                console.warn('⚠️ Failed to initialize Sentry:', error.message);
            }
        }
    }

    // Load Sentry
    loadAndInitSentry();

    // =============================================================================
    // ERROR TRACKING - Monitor for business logic errors
    // =============================================================================

    /**
     * Monitor for snackbar errors and log to Sentry
     */
    function setupErrorMonitoring() {
        console.log('🔍 Setting up error monitoring...');

        // Wait for document.body to be available
        function setupMutationObserver() {
            if (!document.body) {
                // Wait for DOM to be ready
                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', setupMutationObserver);
                    return;
                }
                // Retry after a short delay
                setTimeout(setupMutationObserver, 100);
                return;
            }

            // Monitor for error overlays appearing in the DOM
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    mutation.addedNodes.forEach((node) => {
                        // Check if this is an error overlay
                        if (node.nodeType === 1 && node.classList) {
                            if (node.classList.contains('error-overlay') ||
                                node.id === 'error-overlay' ||
                                (node.classList.contains('overlay') && node.textContent.includes('Error'))) {

                                const errorMessage = node.textContent || node.innerText || 'Unknown error';
                                console.warn(`⚠️ Error overlay detected: ${errorMessage}`);

                                if (typeof Sentry !== 'undefined') {
                                    const productValue = document.getElementById('product-scan')?.value;
                                    const statusValue = document.getElementById('status-scan')?.value;

                                    Sentry.captureMessage(`Error Overlay: ${errorMessage}`, {
                                        level: 'error',
                                        tags: {
                                            action: 'product-scan-error',
                                            error_type: 'overlay_error',
                                            source: 'mutation_observer'
                                        },
                                        extra: {
                                            errorMessage: errorMessage.trim(),
                                            productScanned: productValue || 'none',
                                            statusScanned: statusValue || 'none',
                                            timestamp: new Date().toISOString(),
                                            overlayHtml: node.outerHTML?.substring(0, 500)
                                        }
                                    });
                                }
                            }
                        }
                    });
                });
            });

            // Start observing the document body for added nodes
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });

            console.log('✅ MutationObserver setup - watching for error overlays');
        }

        setupMutationObserver();

        // Intercept snackbar.show calls to track server-side errors
        function interceptSnackbar() {
            if (typeof snackbar !== 'undefined' && snackbar.show) {
                const originalShow = snackbar.show;

                // Check if already wrapped
                if (originalShow._sentryWrapped) {
                    console.log('ℹ️ snackbar.show already wrapped for Sentry');
                    return;
                }

                snackbar.show = function(message, type) {
                    // Log errors and warnings to Sentry
                    if (type === 'cs-danger' || type === 'cs-warning') {
                        console.warn(`⚠️ Server error via snackbar: ${message} (${type})`);

                        if (typeof Sentry !== 'undefined') {
                            const productValue = document.getElementById('product-scan')?.value;
                            const statusValue = document.getElementById('status-scan')?.value;

                            Sentry.captureMessage(`Server Error: ${message}`, {
                                level: type === 'cs-danger' ? 'error' : 'warning',
                                tags: {
                                    action: 'product-scan-error',
                                    error_type: 'server_validation',
                                    snackbar_type: type,
                                    source: 'snackbar_interception'
                                },
                                extra: {
                                    errorMessage: message,
                                    productScanned: productValue || 'none',
                                    statusScanned: statusValue || 'none',
                                    timestamp: new Date().toISOString()
                                }
                            });
                        }
                    }

                    // Call original function
                    return originalShow.apply(this, arguments);
                };

                snackbar.show._sentryWrapped = true;
                console.log('✅ snackbar.show wrapped for Sentry tracking');
            } else {
                // Retry after a delay
                setTimeout(interceptSnackbar, 500);
            }
        }

        interceptSnackbar();
        console.log('✅ Error monitoring enabled - watching for overlays and snackbar errors');
    }

    // Setup error monitoring immediately
    setupErrorMonitoring();

    /**
     * Get third-party item statuses from localStorage
     * @returns {Array} Array of status objects
     */
    window.getThirdPartyStatuses = function() {
        try {
            const sessionData = localStorage.getItem('tf__session');

            if (!sessionData) {
                console.warn('⚠️ tf__session not found in localStorage');
                return [];
            }

            // Parse the outer JSON
            const session = JSON.parse(sessionData);

            // Get the _thirdpartyitemstatuses value (which is a JSON string)
            const statusesString = session._thirdpartyitemstatuses;

            if (!statusesString) {
                console.warn('⚠️ _thirdpartyitemstatuses not found in session');
                return [];
            }

            // Parse the inner JSON string to get the actual array
            const statuses = JSON.parse(statusesString);

            if (!Array.isArray(statuses)) {
                console.warn('⚠️ _thirdpartyitemstatuses is not an array');
                return [];
            }

            console.log(`✅ Found ${statuses.length} third-party statuses`);
            return statuses;
        } catch (error) {
            console.error('❌ Error reading third-party statuses:', error);
            return [];
        }
    };

    /**
     * Add status dropdown to page tools
     * @returns {boolean} Success status
     */
    window.addStatusDropdown = function() {
        const pageTools = document.querySelector('.page-tools');
        if (!pageTools) {
            console.warn('⚠️ .page-tools not found, cannot add dropdown');
            return false;
        }

        // Check if dropdown already exists
        if (document.getElementById('status-dropdown-container')) {
            console.log('ℹ️ Status dropdown already exists');
            return true;
        }

        // Get statuses
        const statuses = window.getThirdPartyStatuses();
        if (statuses.length === 0) {
            console.warn('⚠️ No statuses available, cannot create dropdown');
            return false;
        }

        // Create dropdown container
        const container = document.createElement('li');
        container.id = 'status-dropdown-container';
        container.className = 'toolbar-item';
        container.style.marginRight = '10px';

        // Create label
        const label = document.createElement('label');
        label.textContent = 'Status: ';
        label.style.marginRight = '5px';
        label.style.fontSize = '14px';
        label.style.fontWeight = 'bold';

        // Create dropdown
        const select = document.createElement('select');
        select.id = 'status-dropdown';
        select.className = 'form-control';
        select.style.display = 'inline-block';
        select.style.width = 'auto';
        select.style.minWidth = '200px';
        select.style.padding = '5px 10px';
        select.style.fontSize = '14px';

        // Add default option
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '-- Select Status --';
        select.appendChild(defaultOption);

        // Add status options
        statuses.forEach(status => {
            if (status.valueListName) {
                const option = document.createElement('option');
                option.value = status.valueListName;
                option.textContent = status.valueListName;
                select.appendChild(option);
            }
        });

        // Save selected value to localStorage
        select.addEventListener('change', function() {
            const selectedValue = this.value;
            localStorage.setItem('selectedItemStatus', selectedValue);
            console.log(`✅ Selected status: ${selectedValue || '(none)'}`);
        });

        // Restore saved selection
        const savedStatus = localStorage.getItem('selectedItemStatus');
        if (savedStatus) {
            select.value = savedStatus;
            console.log(`✅ Restored saved status: ${savedStatus}`);
        }

        // Assemble and append
        container.appendChild(label);
        container.appendChild(select);

        // Add to toolbar at the beginning
        const toolbar = pageTools.querySelector('.toolbar');
        if (toolbar && toolbar.firstChild) {
            toolbar.insertBefore(container, toolbar.firstChild);
        } else {
            pageTools.appendChild(container);
        }

        console.log('✅ Added status dropdown');
        return true;
    };

    /**
     * Get currently selected status
     * @returns {string} Selected status value
     */
    window.getSelectedStatus = function() {
        const dropdown = document.getElementById('status-dropdown');
        return dropdown ? dropdown.value : '';
    };

    /**
     * Setup auto-fill and auto-submit for status-scan input
     * Streamlined workflow: auto-fills from dropdown, auto-submits, returns focus to product-scan
     * @returns {boolean} Success status
     */
    window.setupStatusAutoFill = function() {
        const statusInput = document.getElementById('status-scan');
        if (!statusInput) {
            console.warn('⚠️ status-scan input not found');
            return false;
        }

        console.log('📎 Setting up streamlined status auto-fill and submit');

        // Sanitize input to prevent control characters from triggering browser shortcuts
        statusInput.addEventListener('input', function(event) {
            const originalValue = this.value;

            // Check for Line Feed (LF = 10 = \n) or Carriage Return (CR = 13 = \r)
            const hasLineFeed = originalValue.includes('\n');
            const hasCarriageReturn = originalValue.includes('\r');

            // Remove ALL control characters (0-31) and DEL (127)
            const sanitized = originalValue.replace(/[\x00-\x1F\x7F]/g, '');

            if (sanitized !== originalValue) {
                console.warn('⚠️ Removed control characters from status scan');
                this.value = sanitized;
            }

            // If scanner sent LF or CR, trigger Enter key event
            if (hasLineFeed || hasCarriageReturn) {
                console.log('✅ Detected Line Feed/CR from scanner - triggering Enter');

                setTimeout(() => {
                    const enterEvent = new KeyboardEvent('keydown', {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true
                    });
                    this.dispatchEvent(enterEvent);
                }, 10);
            }
        });

        // Convert Ctrl+J (Line Feed from scanner) to Enter key
        statusInput.addEventListener('keydown', function(event) {
            // Scanner sends Ctrl+J (Line Feed) instead of Enter
            if (event.ctrlKey && event.key.toLowerCase() === 'j') {
                console.log('🔄 Converting Ctrl+J (Line Feed) to Enter');
                event.preventDefault();
                event.stopPropagation();

                // Trigger Enter key event
                setTimeout(() => {
                    const enterEvent = new KeyboardEvent('keydown', {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true
                    });
                    this.dispatchEvent(enterEvent);
                }, 10);
                return false;
            }

            // Block other Ctrl shortcuts
            if (event.ctrlKey && !event.shiftKey && !event.altKey) {
                console.warn(`⚠️ Blocked Ctrl+${event.key} shortcut from scanner`);
                event.preventDefault();
                event.stopPropagation();
                return false;
            }
        });

        // Auto-fill and auto-submit when input receives focus
        statusInput.addEventListener('focus', function() {
            const selectedStatus = window.getSelectedStatus();

            if (selectedStatus) {
                console.log(`✅ Auto-filling status: ${selectedStatus}`);

                try {
                    // Fill the input
                    this.value = selectedStatus;

                    // Trigger input event
                    const inputEvent = new Event('input', { bubbles: true });
                    this.dispatchEvent(inputEvent);

                    // Trigger change event
                    const changeEvent = new Event('change', { bubbles: true });
                    this.dispatchEvent(changeEvent);

                    // Small delay to ensure value is set, then auto-submit
                    const statusInputElement = this;
                    setTimeout(() => {
                        try {
                            console.log('✅ Auto-submitting via Enter key');

                            // Capture context for error logging
                            const context = {
                                statusValue: selectedStatus,
                                productValue: document.getElementById('product-scan')?.value,
                                modalVisible: !!document.querySelector('#scan-product-modal'),
                                submitButtonExists: !!document.querySelector('#scan-product-modal .modal-box-button[type="submit"]'),
                                timestamp: new Date().toISOString()
                            };

                            // Simulate Enter key press to trigger submit
                            const enterEvent = new KeyboardEvent('keydown', {
                                key: 'Enter',
                                code: 'Enter',
                                keyCode: 13,
                                which: 13,
                                bubbles: true,
                                cancelable: true
                            });
                            statusInputElement.dispatchEvent(enterEvent);

                            // Also try clicking submit button as fallback
                            const submitButton = document.querySelector('#scan-product-modal .modal-box-button[type="submit"]');
                            if (submitButton) {
                                console.log('🔘 Clicking submit button');
                                submitButton.click();
                            } else {
                                // Submit button not found - log to Sentry
                                const error = new Error('Submit button not found in modal');
                                console.error('❌ Submit failed:', error.message);

                                if (typeof Sentry !== 'undefined') {
                                    Sentry.captureException(error, {
                                        level: 'warning',
                                        tags: {
                                            action: 'auto-submit-status',
                                            reason: 'submit-button-missing'
                                        },
                                        extra: context
                                    });
                                }
                            }

                            // Monitor for submission success/failure
                            setTimeout(() => {
                                // Check if modal is still visible (submission failed)
                                const modalStillVisible = !!document.querySelector('#scan-product-modal:not(.hide)');

                                if (modalStillVisible) {
                                    const error = new Error('Status submit may have failed - modal still visible');
                                    console.error('❌ Submit verification failed:', error.message);

                                    if (typeof Sentry !== 'undefined') {
                                        Sentry.captureException(error, {
                                            level: 'warning',
                                            tags: {
                                                action: 'auto-submit-status',
                                                reason: 'modal-still-visible'
                                            },
                                            extra: {
                                                ...context,
                                                verificationTime: new Date().toISOString()
                                            }
                                        });
                                    }
                                } else {
                                    console.log('✅ Submit verification passed - modal closed');
                                }
                            }, 500);

                            // Let the form submission handle focus naturally
                            console.log('⏳ Waiting for form submission to complete...');
                        } catch (error) {
                            console.error('❌ Error during auto-submit:', error);

                            if (typeof Sentry !== 'undefined') {
                                Sentry.captureException(error, {
                                    level: 'error',
                                    tags: {
                                        action: 'auto-submit-status',
                                        reason: 'exception-during-submit'
                                    },
                                    extra: {
                                        statusValue: selectedStatus,
                                        productValue: document.getElementById('product-scan')?.value,
                                        timestamp: new Date().toISOString()
                                    }
                                });
                            }
                        }
                    }, 100);
                } catch (error) {
                    console.error('❌ Error during status auto-fill:', error);

                    if (typeof Sentry !== 'undefined') {
                        Sentry.captureException(error, {
                            level: 'error',
                            tags: {
                                action: 'auto-fill-status',
                                reason: 'exception-during-fill'
                            },
                            extra: {
                                statusValue: selectedStatus,
                                timestamp: new Date().toISOString()
                            }
                        });
                    }
                }
            } else {
                console.log('⚠️ No status selected in dropdown - cannot auto-submit');
            }
        });

        console.log('✅ Streamlined status auto-fill and submit configured');
        return true;
    };

    /**
     * Setup product-scan input to move to status-scan
     * Streamlined workflow: scan product -> move to status-scan (which handles the rest)
     * @returns {boolean} Success status
     */
    window.setupProductScanAutoSubmit = function() {
        const productInput = document.getElementById('product-scan');
        if (!productInput) {
            console.warn('⚠️ product-scan input not found');
            return false;
        }

        console.log('🚀 Setting up product-scan workflow');

        // Sanitize input to prevent control characters
        productInput.addEventListener('input', function(event) {
            const originalValue = this.value;

            // Check for Line Feed (LF = 10 = \n) or Carriage Return (CR = 13 = \r)
            const hasLineFeed = originalValue.includes('\n');
            const hasCarriageReturn = originalValue.includes('\r');

            // Remove ALL control characters (0-31) and DEL (127)
            const sanitized = originalValue.replace(/[\x00-\x1F\x7F]/g, '');

            if (sanitized !== originalValue) {
                console.warn('⚠️ Removed control characters from product scan');
                this.value = sanitized;
            }

            // If scanner sent LF or CR, trigger Enter key event to move to next field
            if (hasLineFeed || hasCarriageReturn) {
                console.log('✅ Detected Line Feed/CR from scanner - triggering Enter');

                setTimeout(() => {
                    const enterEvent = new KeyboardEvent('keydown', {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true
                    });
                    this.dispatchEvent(enterEvent);
                }, 10);
            }
        });

        // Convert Ctrl+J (Line Feed from scanner) to Enter key
        productInput.addEventListener('keydown', function(event) {
            // Scanner sends Ctrl+J (Line Feed) instead of Enter
            if (event.ctrlKey && event.key.toLowerCase() === 'j') {
                console.log('🔄 Converting Ctrl+J (Line Feed) to Enter');
                event.preventDefault();
                event.stopPropagation();

                // Trigger Enter key event to move to next field
                setTimeout(() => {
                    const enterEvent = new KeyboardEvent('keydown', {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true
                    });
                    this.dispatchEvent(enterEvent);
                }, 10);
                return false;
            }

            // Block other Ctrl combinations (except Ctrl+A, Ctrl+C, Ctrl+V for user)
            if (event.ctrlKey && !['a', 'c', 'v'].includes(event.key.toLowerCase())) {
                console.warn(`⚠️ Blocked Ctrl+${event.key} shortcut from scanner`);
                event.preventDefault();
                event.stopPropagation();
                return false;
            }
        });

        // Log product scan on Enter/Tab key (let them work naturally)
        productInput.addEventListener('keydown', function(event) {
            if (event.key === 'Enter' || event.keyCode === 13 || event.key === 'Tab') {
                const productValue = this.value.trim();

                if (productValue) {
                    console.log(`📦 Product scanned: "${productValue}" - allowing natural navigation`);
                } else {
                    console.log('⚠️ Product value is empty');
                }

                // Don't prevent default - let Enter/Tab work naturally
            }
        });

        console.log('✅ Product-scan workflow configured');
        return true;
    };

    console.log('✅ Status Dropdown Functions Loaded');
})();
