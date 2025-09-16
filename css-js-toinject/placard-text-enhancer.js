// Placard Text Enhancement - JavaScript component
// This script ensures text enhancement is applied even for dynamically loaded content

(function() {
    'use strict';
    
    // Function to enhance text elements
    function enhanceTextElements() {
        // Find all text elements within box-label-wrp
        const boxLabelWrap = document.querySelector('.box-label-wrp');
        if (!boxLabelWrap) return;
        
        // Get all text divs
        const textElements = boxLabelWrap.querySelectorAll('.text');
        
        textElements.forEach(element => {
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
        });
        
        // Handle order reference text specifically
        const orderRefText = boxLabelWrap.querySelector('.order-ref-text');
        if (orderRefText) {
            const currentSize = parseInt(window.getComputedStyle(orderRefText).fontSize) || 28;
            orderRefText.style.fontSize = (currentSize * 2) + 'px';
            orderRefText.style.fontWeight = 'bold';
        }
        
        // Handle carton count
        const cartonCount = boxLabelWrap.querySelector('.carton-count');
        if (cartonCount) {
            const currentSize = parseInt(window.getComputedStyle(cartonCount).fontSize) || 40;
            cartonCount.style.fontSize = (currentSize * 2) + 'px';
            cartonCount.style.fontWeight = 'bold';
        }
    }
    
    // Run enhancement when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceTextElements);
    } else {
        enhanceTextElements();
    }
    
    // Also run after a short delay to catch any late-loading content
    setTimeout(enhanceTextElements, 500);
    
    // Create a MutationObserver to handle dynamically added content
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                // Check if any added nodes contain our target elements
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        if (node.classList && (node.classList.contains('box-label-wrp') || 
                            node.querySelector('.box-label-wrp'))) {
                            setTimeout(enhanceTextElements, 100);
                        }
                    }
                });
            }
        });
    });
    
    // Start observing
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
    
    console.log('Placard text enhancer loaded - text will be doubled in size and made bold');
})();