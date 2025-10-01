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

console.log('✅ table-item-linker.js loaded - makeSkuItemsClickable available');
