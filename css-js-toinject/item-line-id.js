/**
 * Item Line ID Column Manager
 * Adds Item Line ID column to order tables and populates with data
 * Call functions via router when on ProcessPersonalizedOrderItems page
 */

// Function called when Item Line ID link is clicked
window.itemLineIdFunction = function(itemLineId) {
    console.log('Item Line ID clicked:', itemLineId);

    // Find and click the Scan Status button
    const scanStatusBtn = document.getElementById('scan-status-btn');
    if (!scanStatusBtn) {
        console.error('❌ Scan Status button not found');
        alert('Error: Scan Status button not found on page');
        return;
    }

    console.log('✅ Found Scan Status button, clicking...');
    scanStatusBtn.click();

    // Wait for overlay to appear and populate input
    let attempts = 0;
    const maxAttempts = 20;

    function waitForInput() {
        attempts++;
        const productScanInput = document.getElementById('product-scan');

        if (productScanInput) {
            console.log('✅ Found product-scan input, populating with:', itemLineId);
            productScanInput.value = itemLineId;
            productScanInput.focus();

            // Trigger input events to ensure any listeners are notified
            const inputEvent = new Event('input', { bubbles: true });
            productScanInput.dispatchEvent(inputEvent);

            const changeEvent = new Event('change', { bubbles: true });
            productScanInput.dispatchEvent(changeEvent);

            // If the input has a debounced attribute, also trigger keyup
            if (productScanInput.hasAttribute('data-debounced')) {
                const keyupEvent = new KeyboardEvent('keyup', { bubbles: true });
                productScanInput.dispatchEvent(keyupEvent);
            }

            console.log('✅ Item Line ID populated in scan input');
        } else if (attempts < maxAttempts) {
            // Retry with increasing delay
            const delay = 50 * attempts; // 50ms, 100ms, 150ms, etc.
            console.log(`⏳ Input not found yet, retry ${attempts}/${maxAttempts} in ${delay}ms`);
            setTimeout(waitForInput, delay);
        } else {
            console.error('❌ Max attempts reached, product-scan input not found');
            alert('Error: Could not find scan input field in overlay');
        }
    }

    // Start waiting for input after a short delay to allow overlay to render
    setTimeout(waitForInput, 100);
};

function addItemLineIdColumn() {
    console.log('addItemLineIdColumn() called');

    // Find all inner tables
    const innerTables = document.querySelectorAll('table[id^="inner-table-orderwise"]');
    console.log(`Found ${innerTables.length} inner tables`);

    innerTables.forEach(table => {
        // Add header
        const headerRow = table.querySelector('thead tr');
        if (headerRow && !headerRow.querySelector('.item-line-id-header')) {
            const th = document.createElement('th');
            th.textContent = 'Item Line ID';
            th.className = 'item-line-id-header';
            th.style.width = '120px';
            headerRow.appendChild(th);
        }

        // Add cells to item rows
        const itemRows = table.querySelectorAll('tbody tr.item-row');
        itemRows.forEach(row => {
            if (!row.querySelector('.item-line-id-cell')) {
                const td = document.createElement('td');
                td.className = 'item-line-id-cell';
                row.appendChild(td);
            }
        });
    });

    console.log('Item Line ID column added to tables');
}

function populateItemLineIds() {
    console.log('populateItemLineIds() called');

    const itemRows = document.querySelectorAll('tr.item-row');
    console.log(`Found ${itemRows.length} item rows to populate`);

    itemRows.forEach(row => {
        const productInfo = row.querySelector('input.product-info');
        const cell = row.querySelector('.item-line-id-cell');

        if (productInfo && cell && !cell.querySelector('.item-line-id-link')) {
            const itemLineId = productInfo.getAttribute('data-lineitemid');
            if (itemLineId) {
                const link = document.createElement('a');
                link.href = '#';
                link.textContent = itemLineId;
                link.className = 'item-line-id-link';
                link.style.cursor = 'pointer';
                link.style.textDecoration = 'underline';
                link.style.color = '#0066cc';
                
                link.addEventListener('click', function(e) {
                    e.preventDefault();
                    window.itemLineIdFunction(itemLineId);
                });

                cell.appendChild(link);
            }
        }
    });

    console.log('Item Line IDs populated');
}

console.log('✅ item-line-id.js loaded - addItemLineIdColumn and populateItemLineIds available');
