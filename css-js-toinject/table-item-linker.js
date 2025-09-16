(function() {
    'use strict';

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

    console.log('🚀 Script loaded - Setting up API interceptors');

    // Intercept fetch requests
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        console.log('🔍 Fetch intercepted:', args[0]);
        
        return originalFetch.apply(this, args).then(response => {
            const url = args[0];
            console.log('📡 Fetch response for:', url);
            
            if (typeof url === 'string' && (url.includes('/api/Bins/GetBinDetail') || url.includes('GetBinDetail'))) {
                console.log('🎯 GetBinDetail API call detected, will process SKUs after response');
                
                setTimeout(makeSkuItemsClickable, 500);
            } else if (typeof url === 'string' && url.includes('/api/')) {
                console.log('🌐 Other API call detected:', url);
                setTimeout(makeSkuItemsClickable, 1000);
            }
            return response;
        });
    };

    // Intercept XMLHttpRequest
    const originalXHROpen = XMLHttpRequest.prototype.open;
    const originalXHRSend = XMLHttpRequest.prototype.send;

    XMLHttpRequest.prototype.open = function(method, url, ...args) {
        this._url = url;
        this._method = method;
        console.log(`🔍 XHR ${method} opened:`, url);
        return originalXHROpen.apply(this, [method, url, ...args]);
    };

    XMLHttpRequest.prototype.send = function(...args) {
        console.log(`📤 XHR ${this._method} sending to:`, this._url);
        
        const isTargetAPI = this._url && (this._url.includes('/api/Bins/GetBinDetail') || this._url.includes('GetBinDetail'));
        const isAnyAPI = this._url && this._url.includes('/api/');
        
        if (isTargetAPI || isAnyAPI) {
            console.log(isTargetAPI ? '🎯 Target GetBinDetail API call detected:' : '🌐 Generic API call detected:', this._url);
            
            this.addEventListener('readystatechange', function() {
                if (this.readyState === 4) {
                    console.log(`📡 XHR response received (status: ${this.status}) for:`, this._url);
                    
                    if (this.status === 200) {
                        if (isTargetAPI) {
                            console.log('🎯 GetBinDetail API response received, will process SKUs');
                            setTimeout(makeSkuItemsClickable, 500);
                        } else {
                            console.log('🌐 Generic API response, will try processing SKUs');
                            setTimeout(makeSkuItemsClickable, 1000);
                        }
                    }
                }
            });
        }
        return originalXHRSend.apply(this, args);
    };

    console.log('✅ API interceptors ready - waiting for API calls...');
})();