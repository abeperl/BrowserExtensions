/**
 * Placard Text Enhancement
 * Doubles text size and makes it bold on shipping labels
 * Call this function via router when on shipment details page
 */

function enhanceTextElements() {
    console.log('enhanceTextElements() called');

    // Try to find .box-label-wrp in main document and all iframes
    let boxLabelWrap = document.querySelector('.box-label-wrp');
    
    // If not found, check iframes
    if (!boxLabelWrap) {
        const iframes = document.querySelectorAll('iframe');
        for (const iframe of iframes) {
            try {
                const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
                if (iframeDoc) {
                    boxLabelWrap = iframeDoc.querySelector('.box-label-wrp');
                    if (boxLabelWrap) {
                        console.log('✅ Found .box-label-wrp in iframe');
                        break;
                    }
                }
            } catch (e) {
                // Cross-origin iframe, skip
                console.log('⚠️ Cannot access iframe (cross-origin)');
            }
        }
    }

    if (!boxLabelWrap) {
        console.log('⚠️ .box-label-wrp not found in document or iframes');
        return;
    }

    console.log('✅ Found .box-label-wrp, enhancing text elements');

    // Get all text divs
    const textElements = boxLabelWrap.querySelectorAll('.text');

    let enhancedCount = 0;
    textElements.forEach(element => {
        // Skip if already enhanced (check for a marker)
        if (element.dataset.enhanced === 'true') {
            return;
        }

        // Get current font size from inline styles or computed styles
        let currentSize = 30; // default
        const inlineStyle = element.style.fontSize;

        if (inlineStyle) {
            const sizeMatch = inlineStyle.match(/(\d+)px/);
            if (sizeMatch) {
                currentSize = parseInt(sizeMatch[1]);
            }
        } else {
            // Get computed style if no inline style
            const computedStyle = window.getComputedStyle(element);
            const computedSize = computedStyle.fontSize;
            const sizeMatch = computedSize.match(/(\d+)px/);
            if (sizeMatch) {
                currentSize = parseInt(sizeMatch[1]);
            }
        }

        // Double the size and make bold
        element.style.fontSize = (currentSize * 2) + 'px';
        element.style.fontWeight = 'bold';
        element.dataset.enhanced = 'true';
        enhancedCount++;
    });

    // Handle order reference text specifically
    const orderRefText = boxLabelWrap.querySelector('.order-ref-text');
    if (orderRefText && orderRefText.dataset.enhanced !== 'true') {
        const currentSize = parseInt(window.getComputedStyle(orderRefText).fontSize) || 28;
        orderRefText.style.fontSize = (currentSize * 2) + 'px';
        orderRefText.style.fontWeight = 'bold';
        orderRefText.dataset.enhanced = 'true';
        enhancedCount++;
    }

    // Handle carton count
    const cartonCount = boxLabelWrap.querySelector('.carton-count');
    if (cartonCount && cartonCount.dataset.enhanced !== 'true') {
        const currentSize = parseInt(window.getComputedStyle(cartonCount).fontSize) || 40;
        cartonCount.style.fontSize = (currentSize * 2) + 'px';
        cartonCount.style.fontWeight = 'bold';
        cartonCount.dataset.enhanced = 'true';
        enhancedCount++;
    }

    console.log(`✅ Enhanced ${enhancedCount} text elements`);
}

console.log('✅ placard-text-enhancer.js loaded - enhanceTextElements available');
