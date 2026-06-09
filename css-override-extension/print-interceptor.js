// Print Interceptor for Carton Label Printing
// Intercepts print requests and redirects to custom HTML printer service

(function() {
    'use strict';

    const NEW_PRINT_SERVER = 'https://192.168.1.254:5555/print';

    console.log('[Print Interceptor] Initializing carton label print interception');

    // Store original fetch
    const originalFetch = window.fetch;

    // Override fetch to intercept print requests
    window.fetch = function(...args) {
        const url = args[0];
        const options = args[1] || {};

        // Check if this is a carton label print request
        if (shouldInterceptPrintRequest(url, options)) {
            console.log('[Print Interceptor] Intercepted carton label print request:', url);

            // Extract HTML content from the request
            return extractHtmlContent(args).then(htmlContent => {
                if (htmlContent) {
                    console.log('[Print Interceptor] Sending to new server:', NEW_PRINT_SERVER);
                    return sendToNewPrintServer(htmlContent);
                } else {
                    console.warn('[Print Interceptor] Could not extract HTML content, using original request');
                    return originalFetch.apply(this, args);
                }
            }).catch(error => {
                console.error('[Print Interceptor] Error processing print request:', error);
                return originalFetch.apply(this, args);
            });
        }

        // Not a print request, use original fetch
        return originalFetch.apply(this, args);
    };

    // Override XMLHttpRequest for older AJAX calls
    const originalXHROpen = XMLHttpRequest.prototype.open;
    const originalXHRSend = XMLHttpRequest.prototype.send;

    XMLHttpRequest.prototype.open = function(method, url, ...rest) {
        this._interceptor_url = url;
        this._interceptor_method = method;
        return originalXHROpen.apply(this, [method, url, ...rest]);
    };

    XMLHttpRequest.prototype.send = function(data) {
        const xhr = this;

        // Check if this is a carton label print request
        if (shouldInterceptPrintRequest(this._interceptor_url, { method: this._interceptor_method, body: data })) {
            console.log('[Print Interceptor] Intercepted XHR carton label print request:', this._interceptor_url);

            // Extract HTML content
            const htmlContent = extractHtmlFromData(data);

            if (htmlContent) {
                console.log('[Print Interceptor] Sending to new server via XHR:', NEW_PRINT_SERVER);

                // Send to new server
                originalFetch(NEW_PRINT_SERVER, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'text/html'
                    },
                    body: htmlContent
                })
                .then(response => response.json())
                .then(result => {
                    console.log('[Print Interceptor] Print successful:', result);

                    // Simulate successful response
                    Object.defineProperty(xhr, 'status', { value: 200, writable: false });
                    Object.defineProperty(xhr, 'statusText', { value: 'OK', writable: false });
                    Object.defineProperty(xhr, 'responseText', { value: JSON.stringify(result), writable: false });
                    Object.defineProperty(xhr, 'response', { value: JSON.stringify(result), writable: false });
                    Object.defineProperty(xhr, 'readyState', { value: 4, writable: false });

                    if (xhr.onload) xhr.onload();
                    if (xhr.onreadystatechange) xhr.onreadystatechange();
                })
                .catch(error => {
                    console.error('[Print Interceptor] Print failed:', error);

                    // Simulate error response
                    Object.defineProperty(xhr, 'status', { value: 500, writable: false });
                    Object.defineProperty(xhr, 'statusText', { value: 'Internal Server Error', writable: false });
                    Object.defineProperty(xhr, 'readyState', { value: 4, writable: false });

                    if (xhr.onerror) xhr.onerror();
                    if (xhr.onreadystatechange) xhr.onreadystatechange();
                });

                return; // Don't send original request
            }
        }

        // Not a print request or couldn't extract HTML, use original send
        return originalXHRSend.apply(this, arguments);
    };

    // Helper function to determine if request should be intercepted
    function shouldInterceptPrintRequest(url, options = {}) {
        if (!url) return false;

        const urlStr = url.toString().toLowerCase();
        const method = (options.method || 'GET').toUpperCase();

        // Check for carton label related URLs
        const cartonLabelPatterns = [
            'cartonlabel',
            'carton-label',
            'carton_label',
            'printcarton',
            'print-carton',
            'print_carton',
            'labelprint',
            'label-print',
            'label_print',
            '/print/carton',
            '/label/carton',
            'shipping/print',
            'shipment/print',
            'packingslip', // Sometimes carton labels are part of packing slip
            'packing-slip',
            'packing_slip'
        ];

        // Check if URL matches any carton label pattern
        const matchesPattern = cartonLabelPatterns.some(pattern => urlStr.includes(pattern));

        // Only intercept POST requests that match the pattern
        if (matchesPattern && method === 'POST') {
            return true;
        }

        // Check for HTML content in body (common for label printing)
        if (method === 'POST' && options.body) {
            const body = options.body.toString();
            if (body.includes('<html') || body.includes('<!DOCTYPE') || body.includes('<div')) {
                // Check if it contains label-related content
                const hasLabelContent =
                    body.toLowerCase().includes('label') ||
                    body.toLowerCase().includes('carton') ||
                    body.toLowerCase().includes('shipping') ||
                    body.toLowerCase().includes('barcode');

                if (hasLabelContent) {
                    return true;
                }
            }
        }

        return false;
    }

    // Extract HTML content from fetch arguments
    async function extractHtmlContent(fetchArgs) {
        const options = fetchArgs[1] || {};

        if (options.body) {
            // Body might be string, FormData, Blob, etc.
            if (typeof options.body === 'string') {
                return options.body;
            } else if (options.body instanceof FormData) {
                // Try to extract HTML from FormData
                for (let [key, value] of options.body.entries()) {
                    if (typeof value === 'string' && (value.includes('<html') || value.includes('<!DOCTYPE'))) {
                        return value;
                    }
                }
            } else if (options.body instanceof Blob) {
                return await options.body.text();
            }
        }

        return null;
    }

    // Extract HTML from XHR data
    function extractHtmlFromData(data) {
        if (!data) return null;

        if (typeof data === 'string') {
            if (data.includes('<html') || data.includes('<!DOCTYPE')) {
                return data;
            }

            // Try to parse as JSON and extract HTML field
            try {
                const json = JSON.parse(data);
                if (json.html) return json.html;
                if (json.content) return json.content;
                if (json.body) return json.body;
            } catch (e) {
                // Not JSON
            }
        } else if (data instanceof FormData) {
            for (let [key, value] of data.entries()) {
                if (typeof value === 'string' && (value.includes('<html') || value.includes('<!DOCTYPE'))) {
                    return value;
                }
            }
        }

        return null;
    }

    // Send HTML content to new print server
    async function sendToNewPrintServer(htmlContent) {
        try {
            const response = await originalFetch(NEW_PRINT_SERVER, {
                method: 'POST',
                headers: {
                    'Content-Type': 'text/html'
                },
                body: htmlContent
            });

            const result = await response.json();
            console.log('[Print Interceptor] Print job submitted:', result);

            // Return a response that mimics the original server's response
            return new Response(JSON.stringify({
                success: true,
                message: 'Print job sent to HTML printer service',
                jobId: result.jobId,
                timestamp: result.timestamp
            }), {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            });

        } catch (error) {
            console.error('[Print Interceptor] Failed to send to print server:', error);

            // Return error response
            return new Response(JSON.stringify({
                success: false,
                error: error.message
            }), {
                status: 500,
                headers: { 'Content-Type': 'application/json' }
            });
        }
    }

    // Also intercept common printing libraries
    // Override window.print for any direct print calls
    const originalWindowPrint = window.print;
    window.print = function() {
        console.log('[Print Interceptor] window.print() called');

        // Check if current page contains carton label content
        const bodyText = document.body.innerText.toLowerCase();
        if (bodyText.includes('carton') || bodyText.includes('shipping label') || bodyText.includes('barcode')) {
            console.log('[Print Interceptor] Detected carton label content in window.print()');

            // Get the full HTML
            const html = document.documentElement.outerHTML;

            // Send to new server
            sendToNewPrintServer(html)
                .then(response => {
                    console.log('[Print Interceptor] Print job submitted from window.print()');
                })
                .catch(error => {
                    console.error('[Print Interceptor] Failed to send print job:', error);
                    // Fallback to original print
                    originalWindowPrint.call(window);
                });
        } else {
            // Not a carton label, use original print
            originalWindowPrint.call(window);
        }
    };

    console.log('[Print Interceptor] Carton label print interception active');

})();