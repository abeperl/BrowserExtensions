/**
 * Status Dropdown Enhancement Functions
 * Adds dropdown for third-party item statuses
 * Auto-fills status-scan input with selected value
 */

(function() {
    'use strict';

    console.log('📦 Status Dropdown Functions Loading...');

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
                                console.error('❌ Submit button not found in modal');
                            }

                            // Monitor for submission success/failure
                            setTimeout(() => {
                                // Check if modal is still visible (submission failed)
                                const modalStillVisible = !!document.querySelector('#scan-product-modal:not(.hide)');

                                if (modalStillVisible) {
                                    console.error('❌ Submit verification failed - modal still visible');
                                } else {
                                    console.log('✅ Submit verification passed - modal closed');
                                }
                            }, 500);

                            // Let the form submission handle focus naturally
                            console.log('⏳ Waiting for form submission to complete...');
                        } catch (error) {
                            console.error('❌ Error during auto-submit:', error);
                        }
                    }, 100);
                } catch (error) {
                    console.error('❌ Error during status auto-fill:', error);
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
