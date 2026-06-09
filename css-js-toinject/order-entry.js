/**
 * Order Entry Page Enhancement Functions
 * Pure functions only - no automatic execution
 * Router controls which features are enabled
 */

(function() {
    'use strict';

    console.log('📦 Order Entry Functions Loading...');

    // ==================== TABLE CUSTOMIZATION FUNCTIONS ====================

    /**
     * Apply base CSS styles for natural heights and horizontal checkboxes
     */
    window.applyTableBaseStyles = function() {
        const styleId = 'table-custom-styles';
        if (document.getElementById(styleId)) {
            console.log('ℹ️ Table base styles already applied');
            return;
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            /* Basic table structure - ensure full width with no empty space */
            #so-items-table {
                width: 100% !important;
                table-layout: fixed !important;
                border-collapse: collapse !important;
                margin: 0 !important;
                border-spacing: 0 !important;
            }

            /* Ensure table container takes full width without empty space */
            #so-items-scroll {
                width: 100% !important;
                overflow-x: auto !important;
                margin: 0 !important;
                padding: 0 !important;
                position: relative !important;
            }

            /* Compact row heights - minimal padding for tight layout */
            #so-items-table thead tr,
            #so-items-table tbody tr {
                height: auto !important;
                min-height: auto !important;
                max-height: none !important;
                margin: 0 !important;
                padding: 0 !important;
                line-height: 1.2 !important;
            }

            #so-items-table thead th,
            #so-items-table tbody td {
                height: auto !important;
                min-height: auto !important;
                max-height: none !important;
                padding: 4px 6px !important;
                margin: 0 !important;
                vertical-align: middle !important;
                overflow: hidden !important;
                text-overflow: ellipsis !important;
                white-space: nowrap !important;
                line-height: 1.2 !important;
            }

            /* Header styling - compact */
            #so-items-table thead th {
                font-weight: bold !important;
                font-size: 11px !important;
                text-align: center !important;
                border-bottom: 1px solid #ddd !important;
                padding: 4px 6px !important;
            }

            /* Horizontal checkbox layout */
            #so-items-table thead th[class="text-center"],
            #so-items-table tbody td.handed-over-column {
                display: flex !important;
                flex-direction: row !important;
                flex-wrap: nowrap !important;
                align-items: center !important;
                justify-content: center !important;
                gap: 8px !important;
            }

            #so-items-table thead th[class="text-center"] .checkbox-cs,
            #so-items-table tbody td.handed-over-column .checkbox-cs {
                display: inline-flex !important;
                align-items: center !important;
                justify-content: center !important;
                margin: 0 !important;
                padding: 0 !important;
            }

            /* Hide columns when table has columns-hidden class */
            #so-items-table.columns-hidden thead th:nth-child(5),
            #so-items-table.columns-hidden tbody td:nth-child(5),
            #so-items-table.columns-hidden thead th:nth-child(8),
            #so-items-table.columns-hidden tbody td:nth-child(8),
            #so-items-table.columns-hidden thead th:nth-child(9),
            #so-items-table.columns-hidden tbody td:nth-child(9),
            #so-items-table.columns-hidden thead th:nth-child(10),
            #so-items-table.columns-hidden tbody td:nth-child(10) {
                display: none !important;
            }

            /* Toggle button styles */
            #column-toggle-btn {
                position: fixed;
                top: 10px;
                right: 10px;
                z-index: 2147483647;
                padding: 8px 16px;
                background: #007bff;
                color: white;
                border: none;
                border-radius: 4px;
                cursor: pointer;
                font-size: 14px;
                box-shadow: 0 2px 4px rgba(0,0,0,0.2);
                pointer-events: auto;
            }

            #column-toggle-btn:hover {
                background: #0056b3;
            }
        `;
        document.head.appendChild(style);
        console.log('✅ Applied table base styles');
    };

    /**
     * Add column toggle button
     */
    window.addColumnToggleButton = function() {
        const table = document.getElementById('so-items-table');
        if (!table) {
            console.warn('⚠️ Table not found, cannot add toggle button');
            return false;
        }

        // Check if button already exists
        if (document.getElementById('column-toggle-btn')) {
            console.log('ℹ️ Toggle button already exists');
            return true;
        }

        const button = document.createElement('button');
        button.id = 'column-toggle-btn';
        button.textContent = 'Hide Details';
        button.title = 'Toggle visibility of List Price, Unit Disc, Disc %, and Unit Tax columns';

        button.addEventListener('click', () => {
            const isHidden = table.classList.toggle('columns-hidden');
            button.textContent = isHidden ? 'Show Details' : 'Hide Details';
            console.log(`📊 Columns ${isHidden ? 'hidden' : 'visible'}`);
        });

        document.body.appendChild(button);
        console.log('✅ Added column toggle button');
        return true;
    };

    // ==================== SKU TO QTY FOCUS REDIRECT FUNCTIONS ====================

    /**
     * Add Auto-Focus checkbox to page buttons
     * @returns {boolean} Success status
     */
    window.addAutoFocusCheckbox = function() {
        const pageBtn = document.querySelector('.page-btn');
        if (!pageBtn) {
            console.warn('⚠️ .page-btn not found, cannot add checkbox');
            return false;
        }

        // Check if checkbox already exists
        if (document.getElementById('auto-focus-qty-checkbox')) {
            console.log('ℹ️ Auto-focus checkbox already exists');
            return true;
        }

        // Create checkbox container
        const checkboxContainer = document.createElement('div');
        checkboxContainer.id = 'auto-focus-qty-container';
        checkboxContainer.style.display = 'inline-block';
        checkboxContainer.style.marginLeft = '10px';
        checkboxContainer.style.verticalAlign = 'middle';

        // Create checkbox
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.id = 'auto-focus-qty-checkbox';
        checkbox.checked = false; // Default OFF
        checkbox.style.marginRight = '5px';
        checkbox.style.cursor = 'pointer';

        // Create label
        const label = document.createElement('label');
        label.htmlFor = 'auto-focus-qty-checkbox';
        label.textContent = 'Auto-Focus Qty';
        label.style.cursor = 'pointer';
        label.style.fontSize = '14px';
        label.style.fontWeight = 'normal';
        label.style.userSelect = 'none';

        // Add event listener to save state
        checkbox.addEventListener('change', function() {
            const isEnabled = this.checked;
            localStorage.setItem('autoFocusQty', isEnabled);
            console.log(`🎯 Auto-focus qty ${isEnabled ? 'enabled' : 'disabled'}`);
        });

        // Restore saved state from localStorage
        const savedState = localStorage.getItem('autoFocusQty');
        if (savedState === 'true') {
            checkbox.checked = true;
        }

        // Assemble and append
        checkboxContainer.appendChild(checkbox);
        checkboxContainer.appendChild(label);
        pageBtn.appendChild(checkboxContainer);

        console.log('✅ Added auto-focus checkbox');
        return true;
    };

    /**
     * Check if auto-focus is enabled
     * @returns {boolean}
     */
    window.isAutoFocusEnabled = function() {
        const checkbox = document.getElementById('auto-focus-qty-checkbox');
        return checkbox ? checkbox.checked : false;
    };

    /**
     * Find the quantity input element in the LAST ROW of the table
     * @returns {HTMLElement|null}
     */
    window.findQtyInput = function() {
        const table = document.getElementById('so-items-table');
        if (!table) {
            console.warn('⚠️ Table not found');
            return null;
        }

        const tbody = table.querySelector('tbody');
        if (!tbody) {
            console.warn('⚠️ Table tbody not found');
            return null;
        }

        const rows = tbody.querySelectorAll('tr');
        if (rows.length === 0) {
            console.warn('⚠️ No rows found in table');
            return null;
        }

        // Get the last row
        const lastRow = rows[rows.length - 1];

        // Try multiple selectors in the last row
        const selectors = [
            'input.item-qty',
            'input[data-repeat-name="saleQty"]',
            'input.form-control.alignright.item-qty',
            'input[type="text"][onfocus*="select"]'
        ];

        for (const selector of selectors) {
            const input = lastRow.querySelector(selector);
            if (input) {
                console.log(`✅ Found qty input in last row: ${selector}`);
                return input;
            }
        }

        console.warn('⚠️ Qty input not found in last row');
        return null;
    };

    /**
     * Focus and select the quantity input
     * @param {number} delay - Delay in ms before focusing
     */
    window.focusQtyInput = function(delay = 100) {
        // Check if auto-focus is enabled
        if (!window.isAutoFocusEnabled()) {
            console.log('ℹ️ Auto-focus disabled, skipping');
            return;
        }

        setTimeout(() => {
            const qtyInput = window.findQtyInput();
            if (qtyInput) {
                try {
                    qtyInput.focus();
                    qtyInput.select();
                    console.log('✅ Qty input focused and selected');

                    // Add Tab key handler to return to SKU input (if not already added)
                    if (!qtyInput.dataset.tabListenerAdded) {
                        qtyInput.addEventListener('keydown', function(e) {
                            if (e.key === 'Tab') {
                                e.preventDefault();
                                const skuInput = document.getElementById('sku-autocomplete');
                                if (skuInput) {
                                    skuInput.focus();
                                    skuInput.select();
                                    console.log('⬅️ Tab pressed, returning to SKU input');
                                }
                            }
                        });
                        qtyInput.dataset.tabListenerAdded = 'true';
                        console.log('✅ Tab key handler attached to qty input');
                    }
                } catch (error) {
                    console.error('❌ Error focusing qty input:', error);
                }
            }
        }, delay);
    };

    /**
     * Check if SKU input has a value
     * @param {HTMLElement} skuInput
     * @returns {boolean}
     */
    window.hasSkuValue = function(skuInput) {
        return skuInput && skuInput.value.trim().length > 0;
    };

    /**
     * Setup SKU input listeners
     * @returns {boolean} Success status
     */
    window.setupSkuListeners = function() {
        const skuAutocomplete = document.getElementById('sku-autocomplete');
        if (!skuAutocomplete) {
            console.warn('⚠️ SKU autocomplete not found');
            return false;
        }

        let lastValue = '';
        let lastProcessedValue = null;

        // Method 1: Enter key
        skuAutocomplete.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && window.hasSkuValue(this)) {
                console.log('⌨️ Enter key in SKU input');
                const currentValue = this.value.trim();
                if (currentValue !== lastProcessedValue) {
                    lastProcessedValue = currentValue;
                    window.focusQtyInput(150);
                }
            }
        }, true);

        // Method 2: Scan detection
        skuAutocomplete.addEventListener('input', function() {
            const currentValue = this.value.trim();
            if (currentValue.length > lastValue.length + 3 && currentValue.length > 5) {
                console.log('📝 SKU scan detected');
                lastValue = currentValue;
                setTimeout(() => {
                    if (window.hasSkuValue(skuAutocomplete)) {
                        window.focusQtyInput(200);
                    }
                }, 200);
            } else {
                lastValue = currentValue;
            }
        });

        // Method 3: Blur event
        skuAutocomplete.addEventListener('blur', function() {
            if (window.hasSkuValue(this)) {
                console.log('👁️ SKU input blurred');
                setTimeout(() => {
                    if (!document.activeElement ||
                        document.activeElement.tagName.toLowerCase() !== 'input') {
                        window.focusQtyInput(100);
                    }
                }, 100);
            }
        });

        console.log('✅ SKU listeners attached');
        return true;
    };

    /**
     * Setup autocomplete dropdown watchers
     * @returns {boolean} Success status
     */
    window.setupAutocompleteWatchers = function() {
        const wrapper = document.querySelector('.matech-autocomplete-wrapper');
        if (!wrapper) {
            console.warn('⚠️ Autocomplete wrapper not found');
            return false;
        }

        // Watch for dropdown items
        const observer = new MutationObserver((mutations) => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === 1 && node.classList?.contains('auto-complete-dropdown')) {
                        const items = node.querySelectorAll('.auto-complete-item');
                        items.forEach(item => {
                            item.addEventListener('click', () => {
                                console.log('🖱️ Autocomplete item clicked');
                                window.focusQtyInput(200);
                            });
                        });
                    }
                });
            });
        });

        observer.observe(wrapper, { childList: true, subtree: true });

        // Watch dropdown visibility
        const dropdown = wrapper.querySelector('.auto-complete-dropdown');
        if (dropdown) {
            const dropdownObserver = new MutationObserver((mutations) => {
                mutations.forEach(mutation => {
                    if (mutation.attributeName === 'style' && dropdown.style.display === 'none') {
                        const skuInput = document.getElementById('sku-autocomplete');
                        if (window.hasSkuValue(skuInput)) {
                            console.log('📦 Dropdown hidden with value');
                            window.focusQtyInput(150);
                        }
                    }
                });
            });

            dropdownObserver.observe(dropdown, {
                attributes: true,
                attributeFilter: ['style']
            });
        }

        console.log('✅ Autocomplete watchers setup');
        return true;
    };

    // ==================== HANDOVER AUTO-SELECT FUNCTIONS ====================

    /**
     * Add Handover dropdown next to Auto-Focus checkbox
     * @returns {boolean} Success status
     */
    window.addHandoverDropdown = function() {
        const autoFocusContainer = document.getElementById('auto-focus-qty-container');
        if (!autoFocusContainer) {
            console.warn('⚠️ Auto-focus container not found, cannot add handover dropdown');
            return false;
        }

        // Check if dropdown already exists
        if (document.getElementById('handover-dropdown-container')) {
            console.log('ℹ️ Handover dropdown already exists');
            return true;
        }

        // Create dropdown container
        const dropdownContainer = document.createElement('div');
        dropdownContainer.id = 'handover-dropdown-container';
        dropdownContainer.style.display = 'inline-block';
        dropdownContainer.style.marginLeft = '15px';
        dropdownContainer.style.verticalAlign = 'middle';

        // Create label
        const label = document.createElement('label');
        label.htmlFor = 'handover-dropdown';
        label.textContent = 'Handover:';
        label.style.marginRight = '5px';
        label.style.fontSize = '14px';
        label.style.fontWeight = 'normal';

        // Create dropdown
        const dropdown = document.createElement('select');
        dropdown.id = 'handover-dropdown';
        dropdown.style.padding = '2px 8px';
        dropdown.style.fontSize = '14px';
        dropdown.style.cursor = 'pointer';
        dropdown.style.border = '1px solid #ccc';
        dropdown.style.borderRadius = '4px';

        // Add options
        const options = [
            { value: '', text: 'None' },
            { value: 'H', text: 'H - Handover' },
            { value: 'S', text: 'S - In Store' },
            { value: 'W', text: 'W - To Warehouse' },
            { value: 'P', text: 'P - Warehouse PickUp' }
        ];

        options.forEach(opt => {
            const option = document.createElement('option');
            option.value = opt.value;
            option.textContent = opt.text;
            dropdown.appendChild(option);
        });

        // Event listener to save state
        dropdown.addEventListener('change', function() {
            const selectedValue = this.value;
            localStorage.setItem('handoverDefault', selectedValue);
            console.log(`🤝 Handover default set to: ${selectedValue || 'None'}`);
        });

        // Restore saved state
        const savedValue = localStorage.getItem('handoverDefault');
        if (savedValue) {
            dropdown.value = savedValue;
        }

        // Assemble and append
        dropdownContainer.appendChild(label);
        dropdownContainer.appendChild(dropdown);
        autoFocusContainer.parentNode.insertBefore(dropdownContainer, autoFocusContainer.nextSibling);

        console.log('✅ Added handover dropdown');
        return true;
    };

    /**
     * Get selected handover value
     * @returns {string} Selected handover value ('H', 'S', 'W', 'P', or '')
     */
    window.getHandoverSelection = function() {
        const dropdown = document.getElementById('handover-dropdown');
        return dropdown ? dropdown.value : '';
    };

    /**
     * Apply handover selection to a newly added row
     * @param {HTMLElement} row - The table row element
     */
    window.applyHandoverToRow = function(row) {
        const handoverValue = window.getHandoverSelection();
        if (!handoverValue || !row) {
            return;
        }

        // Find the handed-over-column in this row
        const handoverCell = row.querySelector('.handed-over-column');
        if (!handoverCell) {
            console.warn('⚠️ Handover column not found in row');
            return;
        }

        // Map handover values to checkbox selectors
        const checkboxMap = {
            'H': 'input[data-repeat-name="IsHandOver"]',
            'S': 'input[data-repeat-name="ToStore"]',
            'W': 'input[data-repeat-name="ToWarehouse"]',
            'P': 'input[data-repeat-name="WarehousePickUp"]'
        };

        const checkboxSelector = checkboxMap[handoverValue];
        if (!checkboxSelector) {
            console.warn(`⚠️ Unknown handover value: ${handoverValue}`);
            return;
        }

        const checkbox = handoverCell.querySelector(checkboxSelector);
        if (checkbox) {
            checkbox.checked = true;
            console.log(`✅ Auto-checked ${handoverValue} handover checkbox in new row`);

            // Trigger change event in case there are listeners
            const event = new Event('change', { bubbles: true });
            checkbox.dispatchEvent(event);
        } else {
            console.warn(`⚠️ Checkbox not found for handover: ${handoverValue}`);
        }
    };

    /**
     * Setup table row observer to auto-check handover on new rows
     * @returns {boolean} Success status
     */
    window.setupHandoverRowObserver = function() {
        const table = document.getElementById('so-items-table');
        if (!table) {
            console.warn('⚠️ Table not found, cannot setup handover observer');
            return false;
        }

        const tbody = table.querySelector('tbody');
        if (!tbody) {
            console.warn('⚠️ Table tbody not found');
            return false;
        }

        // Watch for new rows being added
        const observer = new MutationObserver((mutations) => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === 1 && node.tagName === 'TR') {
                        console.log('🆕 New row detected, applying handover selection');
                        // Small delay to ensure row is fully rendered
                        setTimeout(() => {
                            window.applyHandoverToRow(node);
                        }, 100);
                    }
                });
            });
        });

        observer.observe(tbody, {
            childList: true,
            subtree: false
        });

        console.log('✅ Handover row observer setup');
        return true;
    };

    console.log('✅ Order Entry Functions Loaded');
})();
