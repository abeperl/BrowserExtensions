/**
 * SKU Item Linker - Makes SKU items clickable
 * Call this function via router when on outbound/packing page
 */

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

                // Find the qty column in this row
                const qtyColumn = targetRow.querySelector('td.qty-column');

                if (!qtyColumn) {
                    console.error('Could not find qty column');
                    return;
                }

                // Find the qty input field
                const qtyInput = qtyColumn.querySelector('input.qty-mn');
                const qtyDisplaySpan = qtyColumn.querySelector('span.item_added');

                if (!qtyInput || !qtyDisplaySpan) {
                    console.error('Could not find qty input or display span');
                    return;
                }

                // Show the input, hide the display span
                qtyInput.classList.remove('hide');
                qtyDisplaySpan.style.display = 'none';

                // Set the value and focus
                qtyInput.value = qtyText;
                qtyInput.focus();
                qtyInput.select();

                console.log(`Set quantity to: ${qtyText}`);

                // Trigger events to ensure the change is registered
                const inputEvent = new Event('input', { bubbles: true });
                qtyInput.dispatchEvent(inputEvent);

                const changeEvent = new Event('change', { bubbles: true });
                qtyInput.dispatchEvent(changeEvent);

                // Simulate blur/exit after a short delay
                setTimeout(() => {
                    qtyInput.blur();

                    // Hide input, show display span again
                    qtyInput.classList.add('hide');
                    qtyDisplaySpan.style.display = '';
                    qtyDisplaySpan.textContent = qtyText;

                    console.log('Exited qty input field');
                }, 300);
            });

            qtySpan.innerHTML = '';
            qtySpan.appendChild(link);
            processedCount++;
        }
    });

    console.log(`Processed ${processedCount} quantity elements into clickable links`);
}

console.log('✅ table-item-linker.js loaded - makeSkuItemsClickable and makeQtyItemsClickable available');
