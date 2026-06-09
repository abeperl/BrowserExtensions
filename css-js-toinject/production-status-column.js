/**
 * Production Status Column Enhancement
 * Adds "Production Status" column to SO/Order Details items table
 * Fetches ItemStatusId from API response and displays status name from session
 */

(function() {
    'use strict';

    console.log('📊 Production Status Column Functions Loading...');

    /**
     * Cache for status list - loaded once from session
     */
    let statusCache = null;

    /**
     * Get third-party item statuses from localStorage (same as status-dropdown.js)
     * @returns {Array} Array of status objects with id and valueListName
     */
    function getThirdPartyStatuses() {
        // Return cached statuses if available
        if (statusCache) {
            console.log('✅ Using cached statuses');
            return statusCache;
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

            // Cache the statuses
            statusCache = statuses;
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
     * Store to save API response data for order items
     */
    const apiResponseStore = {
        data: null,

        /**
         * Save API response data
         * @param {Object} responseData - The API response data
         */
        save(responseData) {
            this.data = responseData;
            console.log('💾 Saved API response data:', responseData);
        },

        /**
         * Get ItemStatusId for a specific item
         * @param {string} rowIndex - Row index in the table
         * @param {string} itemLineId - ItemLineId from the row (for matching)
         * @returns {number|null} ItemStatusId or null if not found
         */
        getItemStatusId(rowIndex, itemLineId) {
            if (!this.data || !Array.isArray(this.data.data)) {
                console.warn('⚠️ No API data available');
                return null;
            }

            // If ItemLineId is provided, try to find by that first (more reliable)
            if (itemLineId) {
                const item = this.data.data.find(i => String(i.ItemLineId) === String(itemLineId));
                if (item) {
                    console.log(`✅ Found item by ItemLineId ${itemLineId}, StatusId: ${item.ItemStatusId}`);
                    return item.ItemStatusId;
                }
            }

            // Fall back to index matching
            const itemIndex = Number(rowIndex);
            if (!isNaN(itemIndex) && this.data.data[itemIndex]) {
                console.log(`⚠️ Using row index ${itemIndex}, StatusId: ${this.data.data[itemIndex].ItemStatusId}`);
                return this.data.data[itemIndex].ItemStatusId;
            }

            console.warn(`❌ Could not find item for rowIndex=${rowIndex}, itemLineId=${itemLineId}`);
            return null;
        },

        /**
         * Clear stored data
         */
        clear() {
            this.data = null;
            console.log('🗑️ Cleared API response data');
        }
    };

    /**
     * Intercept API calls to capture order items data
     */
    function setupApiInterceptor() {
        console.log('🔌 Setting up API interceptor for order items');

        // Intercept XMLHttpRequest
        const originalXHROpen = XMLHttpRequest.prototype.open;
        const originalXHRSend = XMLHttpRequest.prototype.send;

        XMLHttpRequest.prototype.open = function(method, url, ...args) {
            this._url = url;
            this._method = method;
            return originalXHROpen.apply(this, [method, url, ...args]);
        };

        XMLHttpRequest.prototype.send = function(body) {
            const xhr = this;

            // Log ALL API calls to help identify the correct endpoint
            if (xhr._url) {
                console.log('🔍 API Call detected:', xhr._url);
            }

            // Check if this is an order items API call
            const isOrderItemsCall = xhr._url && (
                xhr._url.includes('/api/order/showOrderDetail') ||
                xhr._url.includes('showOrderDetail')
            );

            if (isOrderItemsCall) {
                console.log('🎯 Intercepted order items API call:', xhr._url);

                // Listen for response
                xhr.addEventListener('load', function() {
                    try {
                        const responseData = JSON.parse(xhr.responseText);
                        console.log('📦 Captured order items response:', responseData);

                        // Check if response has OrderItems array
                        if (responseData.data && responseData.data.OrderItems && Array.isArray(responseData.data.OrderItems)) {
                            // Transform to expected structure
                            const transformedData = {
                                data: responseData.data.OrderItems
                            };
                            apiResponseStore.save(transformedData);
                            console.log('✅ Saved transformed data with', transformedData.data.length, 'items');

                            // Trigger column update after data is saved
                            setTimeout(() => {
                                window.addProductionStatusColumn?.();
                            }, 100);
                        } else {
                            console.warn('⚠️ API response does not have OrderItems array:', responseData);
                        }
                    } catch (error) {
                        console.error('❌ Error parsing API response:', error);
                    }
                });
            }

            return originalXHRSend.apply(this, arguments);
        };

        // Also intercept fetch API
        const originalFetch = window.fetch;
        window.fetch = async function(...args) {
            const [url] = args;

            const isOrderItemsCall = url && (
                url.includes('/api/order/showOrderDetail') ||
                url.includes('showOrderDetail')
            );

            if (isOrderItemsCall) {
                console.log('🎯 Intercepted order items fetch call:', url);
            }

            const response = await originalFetch.apply(this, args);

            if (isOrderItemsCall) {
                const clonedResponse = response.clone();
                try {
                    const responseData = await clonedResponse.json();
                    console.log('📦 Captured order items response (fetch):', responseData);

                    // Check if response has OrderItems array
                    if (responseData.data && responseData.data.OrderItems && Array.isArray(responseData.data.OrderItems)) {
                        // Transform to expected structure
                        const transformedData = {
                            data: responseData.data.OrderItems
                        };
                        apiResponseStore.save(transformedData);
                        console.log('✅ Saved transformed data with', transformedData.data.length, 'items');

                        // Trigger column update after data is saved
                        setTimeout(() => {
                            window.addProductionStatusColumn?.();
                        }, 100);
                    }
                } catch (error) {
                    console.error('❌ Error parsing fetch response:', error);
                }
            }

            return response;
        };

        console.log('✅ API interceptor installed');
    }

    /**
     * Add "Production Status" column to the order items table
     * @returns {boolean} Success status
     */
    window.addProductionStatusColumn = function() {
        console.log('🔄 Adding Production Status column to order items table');

        const table = document.querySelector('table#sorderItemsdet');
        if (!table) {
            console.warn('⚠️ Order items table not found');
            return false;
        }

        // Check if column already exists
        const existingHeader = table.querySelector('th.production-status-header');
        if (existingHeader) {
            console.log('ℹ️ Production Status column already exists, updating data');
            updateProductionStatusCells();
            return true;
        }

        // Find the ProductTitle header
        const headers = table.querySelectorAll('thead th');
        let titleHeaderIndex = -1;

        headers.forEach((header, index) => {
            const headerText = header.textContent.trim();
            if (headerText.includes('Product Title') ||
                header.classList.contains('title_column') ||
                header.hasAttribute('data-repeat-item') && header.getAttribute('data-repeat-item') === 'ProductTitle') {
                titleHeaderIndex = index;
            }
        });

        if (titleHeaderIndex === -1) {
            console.warn('⚠️ Could not find Product Title header');
            return false;
        }

        console.log(`✅ Found Product Title header at index ${titleHeaderIndex}`);

        // Add header cell after Product Title
        const headerRow = table.querySelector('thead tr');
        if (!headerRow) {
            console.warn('⚠️ Header row not found');
            return false;
        }

        const newHeader = document.createElement('th');
        newHeader.className = 'production-status-header';
        newHeader.textContent = 'Production Status';
        newHeader.style.fontWeight = 'bold';
        newHeader.style.backgroundColor = '#f8f9fa';
        newHeader.style.padding = '10px';
        newHeader.style.border = '1px solid #dee2e6';

        // Insert after the title column
        const titleHeader = headers[titleHeaderIndex];
        if (titleHeader.nextSibling) {
            headerRow.insertBefore(newHeader, titleHeader.nextSibling);
        } else {
            headerRow.appendChild(newHeader);
        }

        console.log('✅ Added Production Status header');

        // Add cells to each data row
        const dataRows = table.querySelectorAll('tbody tr');
        dataRows.forEach((row, rowIndex) => {
            const cells = row.querySelectorAll('td');
            const titleCell = cells[titleHeaderIndex];

            if (!titleCell) {
                console.warn(`⚠️ Title cell not found in row ${rowIndex}`);
                return;
            }

            // Try to extract ItemLineId from the row (look for data-repeat-item or similar)
            let itemLineId = null;

            // Strategy 1: Look for a cell with data-repeat-item="ItemLineId"
            const itemLineIdCell = row.querySelector('td[data-repeat-item="ItemLineId"]');
            if (itemLineIdCell) {
                itemLineId = itemLineIdCell.textContent.trim();
                console.log(`📌 Found ItemLineId in cell: ${itemLineId}`);
            }

            // Strategy 2: Check if the row itself has a data attribute
            if (!itemLineId && row.dataset.itemLineId) {
                itemLineId = row.dataset.itemLineId;
                console.log(`📌 Found ItemLineId in row dataset: ${itemLineId}`);
            }

            const newCell = document.createElement('td');
            newCell.className = 'production-status-cell';
            newCell.setAttribute('data-row-index', rowIndex);
            if (itemLineId) {
                newCell.setAttribute('data-item-line-id', itemLineId);
            }
            newCell.style.padding = '10px';
            newCell.style.border = '1px solid #dee2e6';
            newCell.textContent = 'Loading...';

            // Insert after the title cell
            if (titleCell.nextSibling) {
                row.insertBefore(newCell, titleCell.nextSibling);
            } else {
                row.appendChild(newCell);
            }
        });

        console.log(`✅ Added Production Status cells to ${dataRows.length} rows`);

        // Update cells with actual data
        updateProductionStatusCells();

        return true;
    };

    /**
     * Update production status cells with data from API response
     */
    function updateProductionStatusCells() {
        const cells = document.querySelectorAll('td.production-status-cell');

        cells.forEach(cell => {
            const rowIndex = cell.getAttribute('data-row-index');
            const itemLineId = cell.getAttribute('data-item-line-id');

            console.log(`🔍 Processing cell - rowIndex: ${rowIndex}, itemLineId: ${itemLineId}`);

            const statusId = apiResponseStore.getItemStatusId(rowIndex, itemLineId);

            if (statusId !== null && statusId !== undefined) {
                const statusName = getStatusNameById(statusId);

                if (statusName) {
                    // Status found - display name in green
                    cell.textContent = statusName;
                    cell.style.fontWeight = 'bold';
                    cell.style.color = '#28a745';
                } else {
                    // Status ID exists but no match - show ID for debugging
                    cell.textContent = `ID: ${statusId}`;
                    cell.style.fontWeight = 'normal';
                    cell.style.color = '#dc3545'; // Red for no match
                    console.warn(`⚠️ No status name found for ID: ${statusId} in row ${rowIndex}`);
                }
            } else {
                // No status ID in API data
                cell.textContent = 'N/A';
                cell.style.color = '#6c757d';
            }
        });

        console.log(`✅ Updated ${cells.length} production status cells`);
    }

    /**
     * Initialize the production status column feature
     */
    window.initProductionStatusColumn = function() {
        console.log('🚀 Initializing Production Status Column feature');

        // Setup API interceptor
        setupApiInterceptor();

        // Try to add column immediately (in case data is already loaded)
        setTimeout(() => {
            window.addProductionStatusColumn();
        }, 1000);

        // Watch for table to be added/updated
        const observer = new MutationObserver((mutations) => {
            const hasTableChanges = mutations.some(mutation => {
                return Array.from(mutation.addedNodes).some(node => {
                    return node.nodeType === 1 && (
                        node.matches?.('table#sorderItemsdet') ||
                        node.querySelector?.('table#sorderItemsdet') ||
                        node.matches?.('tr') ||
                        node.querySelector?.('tr')
                    );
                });
            });

            if (hasTableChanges) {
                console.log('🔄 Table structure changed, re-adding Production Status column');
                setTimeout(() => {
                    window.addProductionStatusColumn();
                }, 200);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        console.log('✅ Production Status Column feature initialized');
    };

    // Expose API response store for debugging
    window.productionStatusAPI = {
        store: apiResponseStore,
        getStatusList: getThirdPartyStatuses,
        getStatusName: getStatusNameById,
        refreshColumn: window.addProductionStatusColumn,

        /**
         * Debug helper: Show all status IDs from current API data
         */
        showItemStatusIds() {
            if (!apiResponseStore.data || !apiResponseStore.data.data) {
                console.warn('⚠️ No API data available');
                return [];
            }

            const statusIds = apiResponseStore.data.data.map((item, index) => ({
                index,
                ItemStatusId: item.ItemStatusId,
                statusName: getStatusNameById(item.ItemStatusId) || '❌ NO MATCH'
            }));

            console.table(statusIds);
            return statusIds;
        },

        /**
         * Debug helper: Show all available statuses
         */
        showAllStatuses() {
            const statuses = getThirdPartyStatuses();
            console.table(statuses);
            return statuses;
        },

        /**
         * Debug helper: Inspect table rows to understand structure
         */
        inspectTable() {
            const table = document.querySelector('table#sorderItemsdet');
            if (!table) {
                console.warn('⚠️ Table not found');
                return null;
            }

            const headers = Array.from(table.querySelectorAll('thead th')).map(th => th.textContent.trim());
            const rows = Array.from(table.querySelectorAll('tbody tr')).map((row, index) => {
                const cells = Array.from(row.querySelectorAll('td')).map(td => td.textContent.trim());
                return { index, cells };
            });

            console.log('📊 Table Headers:', headers);
            console.table(rows);
            return { headers, rows };
        },

        /**
         * Debug helper: Show current API response structure
         */
        inspectApiResponse() {
            if (!apiResponseStore.data) {
                console.warn('⚠️ No API data captured yet');
                return null;
            }

            console.log('📦 API Response Structure:');
            console.log('- Has data array:', !!apiResponseStore.data.data);
            console.log('- Data length:', apiResponseStore.data.data?.length);

            if (apiResponseStore.data.data && apiResponseStore.data.data.length > 0) {
                console.log('- First item keys:', Object.keys(apiResponseStore.data.data[0]));
                console.log('- First item sample:', apiResponseStore.data.data[0]);
            }

            return apiResponseStore.data;
        }
    };

    console.log('✅ Production Status Column Functions Loaded');
    console.log('💡 Debug API: window.productionStatusAPI');
})();
