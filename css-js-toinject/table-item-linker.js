/**
 * SKU Item Linker - Makes SKU items clickable
 * Call this function via router when on outbound/packing page
 */

// =============================================================================
// UPC DATA STRING CONVERTER - INSTALL FIRST!
// =============================================================================

/**
 * Intercept jQuery data() method to ensure upc2 and upc3 are always strings
 *
 * Problem: UPC values stored as numbers don't have .trim() method, causing errors
 * Solution: Automatically convert to strings when setting or getting data
 *
 * This fixes errors in code like:
 *   var thisUpc2 = $(this).find(".upc-field").data("upc2");
 *   if(thisUpc2) thisUpc2 = thisUpc2.trim();  // Error if upc2 is a number
 *
 * IMPORTANT: This must run BEFORE any code tries to use .data("upc2") or .data("upc3")
 */
(function setupUpcStringConverter() {
    // Check if already installed
    if (typeof $ !== 'undefined' && $.fn && $.fn.data && $.fn.data._upcConverterInstalled) {
        console.log('ℹ️ UPC String Converter already installed, skipping');
        return;
    }

    // Check if jQuery is available
    if (typeof $ === 'undefined' || typeof $.fn === 'undefined') {
        console.warn('⚠️ jQuery not available - UPC string converter not installed');
        // Retry after a delay
        setTimeout(setupUpcStringConverter, 100);
        return;
    }

    console.log('🔧 Installing UPC String Converter...');

    // Save original jQuery data method
    const originalData = $.fn.data;

    // Replace with interceptor
    $.fn.data = function(key, value) {
        // If setting data for upc2/upc3, convert to string
        if (arguments.length === 2 && (key === 'upc2' || key === 'upc3')) {
            const stringValue = value != null ? String(value) : value;
            if (value != null && typeof value !== 'string') {
                console.log(`🔄 Converting ${key} to string on SET:`, value, '→', stringValue);
            }
            return originalData.call(this, key, stringValue);
        }

        // If getting data for upc2/upc3, ensure it's a string
        if (arguments.length === 1 && (key === 'upc2' || key === 'upc3')) {
            const result = originalData.call(this, key);
            if (result != null && typeof result !== 'string') {
                const stringResult = String(result);
                console.log(`🔄 Converting ${key} to string on GET:`, result, '→', stringResult);
                // Update stored value to string for next time
                originalData.call(this, key, stringResult);
                return stringResult;
            }
            return result;
        }

        // All other cases, use original method
        return originalData.apply(this, arguments);
    };

    // Mark as installed
    $.fn.data._upcConverterInstalled = true;

    console.log('✅ UPC String Converter installed - upc2 and upc3 will always be strings');
    console.log('💡 This prevents ".trim() is not a function" errors on packing page');

    // Fix any existing UPC data that's already stored as numbers
    function fixExistingUpcData() {
        const upcFields = document.querySelectorAll('.upc-field');
        let fixedCount = 0;

        upcFields.forEach(field => {
            const $field = $(field);

            // Check and fix upc2
            const upc2 = originalData.call($field, 'upc2');
            if (upc2 != null && typeof upc2 !== 'string') {
                originalData.call($field, 'upc2', String(upc2));
                console.log('🔧 Fixed existing upc2:', upc2, '→', String(upc2));
                fixedCount++;
            }

            // Check and fix upc3
            const upc3 = originalData.call($field, 'upc3');
            if (upc3 != null && typeof upc3 !== 'string') {
                originalData.call($field, 'upc3', String(upc3));
                console.log('🔧 Fixed existing upc3:', upc3, '→', String(upc3));
                fixedCount++;
            }
        });

        if (fixedCount > 0) {
            console.log(`✅ Fixed ${fixedCount} existing UPC data values`);
        }
    }

    // Fix existing data immediately
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fixExistingUpcData);
    } else {
        fixExistingUpcData();
    }

    // Also fix when new elements are added
    const observer = new MutationObserver(() => {
        fixExistingUpcData();
    });

    // Wait for body to be available
    if (document.body) {
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    } else {
        setTimeout(() => {
            if (document.body) {
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });
            }
        }, 100);
    }
})();

// ========== SKU AND QTY CLICKABLE FUNCTIONS ==========

function makeSkuItemsClickable() {
    console.log('makeSkuItemsClickable() called');

    const skuElements = document.querySelectorAll('p.sku[data-repeat-item="Sku"]');
    const inputFields = document.querySelectorAll('input#scan_item[type="text"].form-control');

    console.log(`Found ${skuElements.length} SKU elements`);
    console.log(`Found ${inputFields.length} input fields`);

    if (skuElements.length === 0 || inputFields.length === 0) {
        return;
    }

    const targetInput = inputFields[0];
    console.log('Target input field:', targetInput);

    let processedCount = 0;
    skuElements.forEach(skuElement => {
        if (skuElement.querySelector('a')) {
            return;
        }

        const text = skuElement.textContent.trim();

        if (text && text.length > 0) {
            const link = document.createElement('a');
            link.href = '#';
            link.textContent = text;
            link.className = 'sku-item-link';
            link.style.cursor = 'pointer';

            link.addEventListener('click', function(e) {
                e.preventDefault();
                console.log(`SKU clicked: "${text}"`);
                targetInput.value = text;
                targetInput.focus();

                const enterEvent = new KeyboardEvent('keypress', {
                    key: 'Enter',
                    keyCode: 13,
                    which: 13,
                    bubbles: true
                });
                targetInput.dispatchEvent(enterEvent);

                const inputEvent = new Event('input', { bubbles: true });
                targetInput.dispatchEvent(inputEvent);

                const changeEvent = new Event('change', { bubbles: true });
                targetInput.dispatchEvent(changeEvent);

                console.log(`Value set to input: "${text}"`);
            });

            skuElement.innerHTML = '';
            skuElement.appendChild(link);
            processedCount++;
        }
    });

    console.log(`Processed ${processedCount} SKU elements into clickable links`);
}

/**
 * Qty Item Linker - Makes quantity values clickable in added items table
 * When clicked, finds the SKU in the added items table and updates its quantity
 */
function makeQtyItemsClickable() {
    console.log('makeQtyItemsClickable() called');

    // Find all quantity spans (RemainingQty) in the items list
    const qtySpans = document.querySelectorAll('span.item-order-qty[data-repeat-item="RemainingQty"]');

    console.log(`Found ${qtySpans.length} quantity elements`);

    if (qtySpans.length === 0) {
        return;
    }

    let processedCount = 0;
    qtySpans.forEach(qtySpan => {
        // Skip if already has a link
        if (qtySpan.querySelector('a')) {
            return;
        }

        const qtyText = qtySpan.textContent.trim();

        // Find the SKU in the same row
        const row = qtySpan.closest('tr');
        if (!row) {
            return;
        }

        const skuElement = row.querySelector('p.sku[data-repeat-item="Sku"]');
        if (!skuElement) {
            return;
        }

        const skuText = skuElement.textContent.trim();

        if (qtyText && qtyText.length > 0 && skuText && skuText.length > 0) {
            const link = document.createElement('a');
            link.href = '#';
            link.textContent = qtyText;
            link.className = 'qty-item-link';
            link.style.cursor = 'pointer';

            link.addEventListener('click', function(e) {
                e.preventDefault();
                console.log(`Quantity clicked: ${qtyText} for SKU: ${skuText}`);

                // Find the added items table
                const addedItemsTable = document.querySelector('table#table-items-added tbody');

                if (!addedItemsTable) {
                    console.error('Could not find added items table');
                    return;
                }

                // Find the row with matching SKU
                const rows = addedItemsTable.querySelectorAll('tr');
                let targetRow = null;

                for (const row of rows) {
                    const skuInSpan = row.querySelector('span.sku-in');
                    if (skuInSpan && skuInSpan.textContent.trim() === skuText) {
                        targetRow = row;
                        break;
                    }
                }

                if (!targetRow) {
                    console.error(`Could not find row with SKU: ${skuText}`);
                    return;
                }

                console.log('Found target row for SKU:', skuText);

                // Find the qty-wrp element (which handles the click logic)
                const qtyWrp = targetRow.querySelector('.qty-wrp');

                if (!qtyWrp) {
                    console.error('Could not find .qty-wrp element');
                    return;
                }

                // Trigger click on qty-wrp to invoke the existing click handler
                // This handles personalized items, disabled state, and showing/hiding inputs
                console.log('Triggering click on .qty-wrp');
                qtyWrp.click();

                // Wait for the click handler to show the input, then set the value
                setTimeout(() => {
                    const qtyInput = qtyWrp.querySelector('input.qty-mn');

                    if (!qtyInput) {
                        console.error('Could not find qty input after click');
                        return;
                    }

                    // Check if input is now visible
                    if (qtyInput.classList.contains('hide')) {
                        console.warn('Qty input still hidden - item may be personalized or disabled');
                        return;
                    }

                    // Set the value
                    qtyInput.value = qtyText;
                    qtyInput.focus();
                    qtyInput.select();

                    console.log(`Set quantity to: ${qtyText}`);

                    // Trigger Enter keypress to invoke the page's quantity update logic
                    // This triggers the jQuery handler: $("#table-items-added").on("keypress", ".qty-mn", ...)
                    // Try jQuery trigger first (most reliable for jQuery event handlers)
                    if (typeof $ !== 'undefined' && $.fn) {
                        $(qtyInput).trigger($.Event('keypress', { keyCode: 13, which: 13 }));
                        console.log('Triggered Enter keypress via jQuery');
                    } else {
                        // Fallback to native event
                        const keypressEvent = new KeyboardEvent('keypress', {
                            key: 'Enter',
                            code: 'Enter',
                            bubbles: true,
                            cancelable: true
                        });

                        // Add deprecated properties for compatibility
                        Object.defineProperty(keypressEvent, 'keyCode', {
                            get: () => 13
                        });
                        Object.defineProperty(keypressEvent, 'which', {
                            get: () => 13
                        });

                        qtyInput.dispatchEvent(keypressEvent);
                        console.log('Triggered Enter keypress via native event');
                    }
                }, 100);
            });

            qtySpan.innerHTML = '';
            qtySpan.appendChild(link);
            processedCount++;
        }
    });

    console.log(`Processed ${processedCount} quantity elements into clickable links`);
}

console.log('✅ table-item-linker.js loaded - makeSkuItemsClickable and makeQtyItemsClickable available');
