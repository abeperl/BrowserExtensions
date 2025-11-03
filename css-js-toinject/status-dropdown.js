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
     * Setup auto-fill for status-scan input
     * @returns {boolean} Success status
     */
    window.setupStatusAutoFill = function() {
        const statusInput = document.getElementById('status-scan');
        if (!statusInput) {
            console.warn('⚠️ status-scan input not found');
            return false;
        }

        console.log('📎 Setting up status auto-fill');

        // Auto-fill when input receives focus
        statusInput.addEventListener('focus', function() {
            const selectedStatus = window.getSelectedStatus();

            if (selectedStatus && !this.value) {
                this.value = selectedStatus;
                console.log(`✅ Auto-filled status: ${selectedStatus}`);

                // Trigger input event in case the app listens to it
                const inputEvent = new Event('input', { bubbles: true });
                this.dispatchEvent(inputEvent);

                // Also trigger change event
                const changeEvent = new Event('change', { bubbles: true });
                this.dispatchEvent(changeEvent);
            } else if (selectedStatus && this.value) {
                console.log('ℹ️ Input already has value, not overwriting');
            } else if (!selectedStatus) {
                console.log('ℹ️ No status selected in dropdown');
            }
        });

        // Also auto-fill on mouseenter (in case focus doesn't trigger)
        statusInput.addEventListener('mouseenter', function() {
            const selectedStatus = window.getSelectedStatus();

            if (selectedStatus && !this.value) {
                this.value = selectedStatus;
                console.log(`✅ Auto-filled status (mouseenter): ${selectedStatus}`);
            }
        });

        console.log('✅ Status auto-fill configured');
        return true;
    };

    /**
     * Setup auto-submit for product-scan input
     * Monitors product-scan input and auto-submits when status is already filled
     * @returns {boolean} Success status
     */
    window.setupProductScanAutoSubmit = function() {
        const productInput = document.getElementById('product-scan');
        if (!productInput) {
            console.warn('⚠️ product-scan input not found');
            return false;
        }

        console.log('🚀 Setting up product-scan auto-submit');

        // Monitor input changes on product-scan
        productInput.addEventListener('input', function() {
            const productValue = this.value.trim();
            const statusInput = document.getElementById('status-scan');
            const statusValue = statusInput ? statusInput.value.trim() : '';

            console.log(`📦 Product scanned: "${productValue}", Status: "${statusValue}"`);

            // If product has value AND status has value, auto-submit
            if (productValue && statusValue) {
                console.log('✅ Both product and status filled, auto-submitting...');

                // Small delay to ensure value is fully set
                setTimeout(() => {
                    // Simulate Enter key press on the status input
                    const enterEvent = new KeyboardEvent('keydown', {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true
                    });

                    statusInput.dispatchEvent(enterEvent);

                    // Also try triggering on the product input
                    productInput.dispatchEvent(enterEvent);

                    // Try finding and clicking the submit button as fallback
                    const submitButton = document.querySelector('#scan-product-modal .modal-box-button[type="submit"]');
                    if (submitButton) {
                        console.log('🔘 Clicking submit button as fallback');
                        submitButton.click();
                    }

                    console.log('✅ Auto-submit triggered');
                }, 100);
            }
        });

        console.log('✅ Product-scan auto-submit configured');
        return true;
    };

    console.log('✅ Status Dropdown Functions Loaded');
})();
