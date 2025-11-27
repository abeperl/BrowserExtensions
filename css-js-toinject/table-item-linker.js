/**
 * SKU Item Linker - Makes SKU items clickable
 * Call this function via router when on outbound/packing page
 */

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
