/**
 * Production Status Column Injector
 * Injects a "Production Status" column into the pick list table
 * Matches items from picklist and order details responses
 * Looks up status names from session storage
 */
(function() {
    // Global cache for status list (lifecycle of application)
    window.__productionStatusCache = window.__productionStatusCache || null;

    /**
     * Get third-party statuses from session storage
     * @returns {Array} Array of status objects
     */
    function getThirdPartyStatuses() {
        // Return cached statuses if available
        if (window.__productionStatusCache) {
            console.log('✅ Using cached statuses');
            return window.__productionStatusCache;
        }

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

            // Cache the statuses globally
            window.__productionStatusCache = statuses;
            console.log(`✅ Loaded and cached ${statuses.length} third-party statuses`);
            return statuses;
        } catch (error) {
            console.error('❌ Error reading third-party statuses:', error);
            return [];
        }
    }

    /**
     * Create status lookup map from status array
     * @returns {Map} Map of status ID to status name
     */
    function createStatusLookupMap() {
        const statuses = getThirdPartyStatuses();
        const lookupMap = new Map();

        statuses.forEach(status => {
            // Use valueListId (not id) as the key
            if (status.valueListId && status.valueListName) {
                lookupMap.set(status.valueListId, status.valueListName);
            }
        });

        console.log(`📋 Created status lookup map with ${lookupMap.size} entries`);
        return lookupMap;
    }

    /**
     * Get status name by ID
     * @param {number|string} statusId - The ItemStatusId from API
     * @returns {string} Status name or empty string if not found
     */
    function getStatusNameById(statusId) {
        const lookupMap = createStatusLookupMap();
        const statusName = lookupMap.get(Number(statusId)) || '';

        if (!statusName) {
            console.warn(`⚠️ Status not found for ID: ${statusId}`);
        }

        return statusName;
    }

    /**
     * Get order details from memory
     * @returns {Object|null} Order details response data
     */
    function getOrderDetailsFromMemory() {
        try {
            // Check if orderDetails is stored in window object
            if (window.__orderDetailsResponse) {
                return window.__orderDetailsResponse;
            }

            // Check sessionStorage as fallback
            const stored = sessionStorage.getItem('__orderDetailsResponse');
            if (stored) {
                return JSON.parse(stored);
            }

            console.warn('⚠️ Order details not found in memory');
            return null;
        } catch (error) {
            console.error('❌ Error retrieving order details from memory:', error);
            return null;
        }
    }

    /**
     * Create OrderDetailsId to ItemStatusId mapping
     * @param {Object} orderDetailsResponse - The order details API response
     * @returns {Map} Map of OrderDetailsId to ItemStatusId
     */
    function createOrderDetailsMapping(orderDetailsResponse) {
        const mapping = new Map();

        if (!orderDetailsResponse?.data?.OrderItems) {
            console.warn('⚠️ Invalid order details response structure');
            return mapping;
        }

        orderDetailsResponse.data.OrderItems.forEach(item => {
            if (item.orderDetailsId && item.ItemStatusId !== undefined) {
                mapping.set(item.orderDetailsId, item.ItemStatusId);
            }
        });

        console.log(`📋 Created mapping for ${mapping.size} order items`);
        return mapping;
    }

    /**
     * Get picklist items from memory
     * @returns {Array} Array of picklist items
     */
    function getPickListItemsFromMemory() {
        try {
            if (window.__picklistResponse?.data?.PickListItems) {
                return window.__picklistResponse.data.PickListItems;
            }

            const stored = sessionStorage.getItem('__picklistResponse');
            if (stored) {
                const parsed = JSON.parse(stored);
                return parsed?.data?.PickListItems || [];
            }

            return [];
        } catch (error) {
            console.error('❌ Error getting picklist items:', error);
            return [];
        }
    }

    /**
     * Add Production Status column to the table
     */
    function addProductionStatusColumn() {
        console.log('🚀 Starting Production Status column injection');

        // Get the table
        const table = document.querySelector('#KortHyvdds');
        if (!table) {
            console.warn('⚠️ Pick list table not found');
            return;
        }

        // Get order details from memory
        const orderDetails = getOrderDetailsFromMemory();
        if (!orderDetails) {
            console.warn('⚠️ Order details not available yet, will retry...');
            return;
        }

        // Get picklist items from memory
        const pickListItems = getPickListItemsFromMemory();
        console.log(`📋 Found ${pickListItems.length} picklist items`);

        // Create mapping
        const orderDetailsMapping = createOrderDetailsMapping(orderDetails);

        // Add header (only once)
        const thead = table.querySelector('thead tr');
        if (thead && !thead.querySelector('.production-status-column')) {
            const statusHeader = thead.querySelector('th[locale-res="Status"]');
            if (statusHeader) {
                const newHeader = document.createElement('th');
                newHeader.className = 'production-status-column show_thermal';
                newHeader.textContent = 'Production Status';
                newHeader.style.minWidth = '120px';
                newHeader.style.fontWeight = '600';
                statusHeader.parentNode.insertBefore(newHeader, statusHeader);
                console.log('✅ Added Production Status header');
            }
        }

        // Add cells to each row
        const tbody = table.querySelector('tbody');
        if (!tbody) {
            console.warn('⚠️ Table tbody not found');
            return;
        }

        const rows = tbody.querySelectorAll('tr[data-row]');
        console.log(`📋 Processing ${rows.length} rows`);

        rows.forEach((row, index) => {
            // Skip if already has status cell
            if (row.querySelector('.production-status-cell')) return;

            const picklistItem = pickListItems[index];
            let statusName = 'N/A';

            if (picklistItem?.OrderDetailsId) {
                const statusId = orderDetailsMapping.get(picklistItem.OrderDetailsId);
                if (statusId !== null && statusId !== undefined) {
                    statusName = getStatusNameById(statusId) || 'N/A';
                    console.log(`📌 Row ${index}: OrderDetailsId=${picklistItem.OrderDetailsId}, StatusId=${statusId}, StatusName=${statusName}`);
                }
            }

            // Insert status cell before the Status column
            const statusCell = row.querySelector('.status_col');
            if (statusCell) {
                const newCell = document.createElement('td');
                newCell.className = 'production-status-cell show_thermal';
                newCell.textContent = statusName;
                newCell.style.padding = '8px';
                newCell.style.fontSize = '14px';

                // Add color class based on status
                if (statusName.toLowerCase().includes('pending')) {
                    newCell.style.color = '#ff9800';
                } else if (statusName.toLowerCase().includes('complete')) {
                    newCell.style.color = '#4caf50';
                } else if (statusName.toLowerCase().includes('progress')) {
                    newCell.style.color = '#2196f3';
                }

                statusCell.parentNode.insertBefore(newCell, statusCell);
            }
        });

        console.log('✅ Production Status column injection complete');
    }

    // Run after DOM is loaded and data is available
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setTimeout(addProductionStatusColumn, 500);
        });
    } else {
        setTimeout(addProductionStatusColumn, 500);
    }

    // Also run after additional delays to catch dynamic content
    setTimeout(addProductionStatusColumn, 1500);
    setTimeout(addProductionStatusColumn, 3000);

    // Expose function globally for manual testing
    window.addProductionStatusColumn = addProductionStatusColumn;
})();