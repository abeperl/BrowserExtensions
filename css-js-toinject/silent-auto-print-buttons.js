/**
 * Silent Auto Print Buttons - JSPrintManager Integration
 *
 * Provides silent printing capabilities using JSPrintManager for:
 * 1. Packing Slip
 * 2. Carton Label
 *
 * APPROACH:
 * - Intercepts window.open() calls from existing print buttons
 * - Captures complete HTML from opened windows (reuses working button logic)
 * - Sends captured HTML to JSPrintManager for silent printing
 * - No manual HTML generation - leverages existing, tested print window code
 *
 * Features:
 * - Silent printing directly to configured printers
 * - Printer validation with fallback to default printer
 * - Robust fallback mechanisms (localhost:8080, window-based)
 * - User notifications via OverlayManager
 * - Configuration-based print mode switching
 * - Automatic JSPrintManager loading (local file + CDN fallbacks)
 *
 * Dependencies:
 * - JSPrintManager 8.0 (local file: ./JSPrintManager.js)
 * - OverlayManager (for notifications)
 * - jQuery (for window operations)
 *
 * @see css-js-toinject/docs/SILENT-AUTO-PRINT-SPEC.md
 */

// =============================================================================
// JSPRINTMANAGER CDN LOADER
// =============================================================================

(function() {
    'use strict';

    // Check if JSPrintManager is already loaded
    if (typeof JSPM !== 'undefined') {
        console.log('✅ JSPrintManager already loaded');
        return;
    }

    console.log('📦 JSPrintManager not found, loading...');

    // Script URLs (in priority order: local first, then CDNs)
    const CDN_URLS = [
        './JSPrintManager.js',  // Local copy (primary)
        'https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.js',  // jsDelivr CDN
        'https://unpkg.com/jsprintmanager@8.0.0/JSPrintManager.js',  // unpkg CDN
        'https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js'  // Official CDN (often blocked)
    ];

    let currentCdnIndex = 0;

    /**
     * Load script from CDN with fallback support
     * @param {string} url - CDN URL to load from
     * @returns {Promise}
     */
    function loadScript(url) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = url;
            script.async = true;

            script.onload = () => {
                // Verify JSPM object is available
                if (typeof JSPM !== 'undefined') {
                    console.log(`✅ JSPrintManager loaded successfully from: ${url}`);
                    resolve();
                } else {
                    console.warn(`⚠️ Script loaded but JSPM not available from: ${url}`);
                    reject(new Error('JSPM not available after load'));
                }
            };

            script.onerror = () => {
                console.warn(`❌ Failed to load JSPrintManager from: ${url}`);
                reject(new Error(`Failed to load from ${url}`));
            };

            // Add to document
            (document.head || document.documentElement).appendChild(script);
        });
    }

    /**
     * Try loading from local file and CDNs with fallback
     */
    async function loadWithFallback() {
        while (currentCdnIndex < CDN_URLS.length) {
            const url = CDN_URLS[currentCdnIndex];
            const source = url.startsWith('./') ? 'local file' : 'CDN';
            console.log(`🔄 Attempting to load from ${source}: ${url}`);

            try {
                await loadScript(url);
                return; // Success!
            } catch (error) {
                currentCdnIndex++;
                if (currentCdnIndex < CDN_URLS.length) {
                    console.log(`⚠️ Trying fallback source (${currentCdnIndex + 1}/${CDN_URLS.length})...`);
                }
            }
        }

        // All sources failed
        console.error('❌ Failed to load JSPrintManager from all sources');
        console.error('⚠️ Silent printing will not be available');
        console.error('💡 Check that JSPrintManager.js exists in css-js-toinject/ folder');
        console.error('📖 See: css-js-toinject/docs/JSPRINTMANAGER-LOCAL-SETUP.md');
    }

    // Start loading
    loadWithFallback();
})();

// =============================================================================
// CONFIGURATION
// =============================================================================

const SILENT_AUTO_PRINT_CONFIG = {
    // Print mode selection
    printMode: 'jsprintmanager',  // Options: 'jsprintmanager' | 'windows'

    // Printer configuration (must match Windows printer name exactly)
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',

    // Auto-click behavior
    autoClickEnabled: true,         // Enable/disable auto-clicking
    autoClickDelay: 500,           // Delay before auto-trigger (ms)

    // JSPrintManager settings
    jsprintmanagerTimeout: 5000,   // Timeout for client detection (ms)

    // Fallback settings
    fallbackToLocalhost: true,     // Enable localhost:8080 fallback
    fallbackToWindows: true,       // Enable window-based fallback

    // Debug and logging
    debugMode: true                // Enable detailed console logging
};

// =============================================================================
// JSPRINTMANAGER INTEGRATION
// =============================================================================

/**
 * Check if JSPrintManager client is available and connected
 * @returns {Promise<boolean>}
 */
async function checkJSPrintManagerAvailable() {
    try {
        if (typeof JSPM === 'undefined') {
            console.warn('⚠️ JSPrintManager library not loaded');
            return false;
        }

        // Set timeout for client detection
        const timeoutPromise = new Promise((_, reject) =>
            setTimeout(() => reject(new Error('JSPrintManager client timeout')),
                SILENT_AUTO_PRINT_CONFIG.jsprintmanagerTimeout)
        );

        const startPromise = JSPM.JSPrintManager.start();

        await Promise.race([startPromise, timeoutPromise]);

        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log('✅ JSPrintManager client detected and connected');
        }
        return true;

    } catch (error) {
        console.warn('⚠️ JSPrintManager client not available:', error.message);
        return false;
    }
}

/**
 * Get list of available printers
 * @returns {Promise<Array>}
 */
async function getAvailablePrinters() {
    try {
        const printers = await JSPM.JSPrintManager.getPrinters();
        console.log('📋 Available printers:', printers.map(p => p.name));
        return printers;
    } catch (error) {
        console.error('❌ Failed to get printers:', error);
        return [];
    }
}

/**
 * Validate that a printer exists in the system
 * @param {string} printerName - Name of printer to validate
 * @returns {Promise<boolean>}
 */
async function validatePrinterExists(printerName) {
    try {
        const printers = await JSPM.JSPrintManager.getPrinters();
        const exists = printers.some(p => p.name === printerName);

        if (!exists) {
            console.warn(`⚠️ Printer "${printerName}" not found`);
            console.log('💡 Available printers:', printers.map(p => p.name));
        }

        return exists;

    } catch (error) {
        console.error('❌ Failed to validate printer:', error);
        return false;
    }
}

/**
 * Print an HTML document to a specific printer
 * @param {string} printerName - Target printer name
 * @param {string} htmlContent - HTML content to print
 * @param {string} cssContent - CSS styles to apply
 * @param {string} jobName - Print job name (for tracking)
 * @returns {Promise<boolean>}
 */
async function printHTMLDocument(printerName, htmlContent, cssContent, jobName = 'Document') {
    try {
        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log(`🖨️ Preparing print job: ${jobName}`);
            console.log(`   Target printer: ${printerName}`);
        }

        // Create print job
        const cpj = new JSPM.ClientPrintJob();

        // Validate and set printer
        const printerValid = await validatePrinterExists(printerName);
        if (printerValid) {
            cpj.clientPrinter = new JSPM.InstalledPrinter(printerName);
            if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
                console.log(`   ✅ Using configured printer: ${printerName}`);
            }
        } else {
            // Fallback to default printer
            cpj.clientPrinter = new JSPM.DefaultPrinter();
            console.log('   ⚠️ Using default printer as fallback');
        }

        // Combine CSS and HTML into complete document
        const fullHTML = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <style>${cssContent}</style>
            </head>
            <body>
                ${htmlContent}
            </body>
            </html>
        `;

        // Create print file from HTML string
        const printFile = new JSPM.PrintFile(
            fullHTML,
            JSPM.FileSourceType.BLOB,
            `${jobName}.html`,
            1  // Number of copies
        );

        cpj.files.push(printFile);

        // Send to client
        await cpj.sendToClient();

        console.log(`✅ Print job sent successfully: ${jobName}`);
        return true;

    } catch (error) {
        console.error(`❌ Print job failed for ${jobName}:`, error);
        throw error;
    }
}

// =============================================================================
// WINDOW INTERCEPTION & HTML CAPTURE
// =============================================================================

/**
 * Storage for captured window HTML
 * @type {Object}
 */
const capturedWindowHTML = {
    packingSlip: null,
    cartonLabel: null
};

/**
 * Intercept window.open AND $.post to capture HTML from print operations
 * Works with TabManager by temporarily disabling it
 * @returns {Promise<Object>} Object with packing slip and carton label HTML
 */
function interceptPrintWindows() {
    return new Promise((resolve, reject) => {
        // Reset captured HTML
        capturedWindowHTML.packingSlip = null;
        capturedWindowHTML.cartonLabel = null;

        // Check if TabManager is installed and disable it temporarily
        const tabManagerWasEnabled = typeof TabManager !== 'undefined' && TabManager.config.enabled;

        const timeout = setTimeout(() => {
            // Restore interceptors
            if (typeof TabManager !== 'undefined' && tabManagerWasEnabled) {
                TabManager.enable();
            }
            if (originalPost) {
                $.post = originalPost;
            }
            reject(new Error('Timeout waiting for print content'));
        }, 15000); // 15 second timeout

        if (tabManagerWasEnabled) {
            console.log('⚠️ TabManager detected, temporarily disabling for window capture');
            TabManager.disable();
        }

        // Store originals
        const originalWindowOpen = window.open;
        const originalPost = $.post;

        let windowsOpened = 0;
        let windowsCaptured = 0;
        const maxWindows = 2; // Expecting 2 documents (packing slip + carton label)

        // Intercept $.post for carton label (which POSTs to localhost:8080)
        $.post = function(url, data, success, dataType) {
            console.log(`📋 $.post intercepted: ${url}`);

            // Check if this is the localhost print service
            if (url && url.includes('localhost:8080/printinvoice')) {
                console.log('📦 Intercepted carton label POST to localhost');

                // Capture the HTML being sent (only if we don't have it yet)
                if (typeof data === 'string' && data.length > 100) {
                    if (!capturedWindowHTML.cartonLabel) {
                        console.log('📦 Captured Carton Label HTML from POST');
                        capturedWindowHTML.cartonLabel = data;
                        windowsCaptured++;
                        console.log(`   Progress: ${windowsCaptured}/${maxWindows} documents captured`);

                        // Check if we have both documents
                        if (windowsCaptured >= maxWindows) {
                            console.log(`✅ Captured all ${maxWindows} documents`);

                            // Restore originals
                            window.open = originalWindowOpen;
                            $.post = originalPost;
                            clearTimeout(timeout);

                            // Re-enable TabManager
                            if (typeof TabManager !== 'undefined' && tabManagerWasEnabled) {
                                TabManager.enable();
                                console.log('✅ TabManager re-enabled');
                            }

                            resolve({
                                packingSlip: capturedWindowHTML.packingSlip,
                                cartonLabel: capturedWindowHTML.cartonLabel
                            });
                        }
                    } else {
                        console.log('📦 Carton label already captured, ignoring duplicate POST');
                    }
                }

                // Don't actually POST to localhost - just intercept
                return $.Deferred().resolve().promise();
            }

            // Pass through other POST requests
            return originalPost.apply(this, arguments);
        };

        // Override window.open
        window.open = function(url, target, features) {
            windowsOpened++;
            console.log(`📋 Window.open intercepted (${windowsOpened}/${maxWindows}):`, url);

            // Open the window normally
            const printWindow = originalWindowOpen.call(window, url, target, features);

            if (!printWindow) {
                console.error('❌ Failed to open window');
                return null;
            }

            // Function to capture HTML from window
            const captureHTML = () => {
                console.log(`✅ Capturing HTML from window: ${url}`);

                try {
                    // Get document HTML
                    const doc = printWindow.document;
                    const html = doc.documentElement.outerHTML;

                    // Determine which document this is based on URL or content
                    if (url.includes('packingSlipdetail') || url.includes('packingslip')) {
                        if (!capturedWindowHTML.packingSlip) {
                            console.log('📄 Captured Packing Slip HTML');
                            capturedWindowHTML.packingSlip = html;
                            windowsCaptured++;
                            console.log(`   Progress: ${windowsCaptured}/${maxWindows} documents captured`);
                        } else {
                            console.log('📄 Packing slip already captured, ignoring duplicate');
                        }
                    } else if (url.includes('placard') || url.includes('carton') || url.includes('label')) {
                        if (!capturedWindowHTML.cartonLabel) {
                            console.log('📦 Captured Carton Label HTML from window');
                            capturedWindowHTML.cartonLabel = html;
                            windowsCaptured++;
                            console.log(`   Progress: ${windowsCaptured}/${maxWindows} documents captured`);
                        } else {
                            console.log('📦 Carton label already captured, ignoring duplicate');
                        }
                    } else if (!url || url.trim() === '') {
                        // Empty URL - likely carton label that writes HTML directly to window
                        if (!capturedWindowHTML.cartonLabel) {
                            console.log('📦 Captured HTML from empty URL window (likely Carton Label)');
                            capturedWindowHTML.cartonLabel = html;
                            windowsCaptured++;
                            console.log(`   Progress: ${windowsCaptured}/${maxWindows} documents captured`);
                        } else {
                            console.log('📦 Carton label already captured, ignoring duplicate window');
                        }
                    } else {
                        console.warn('⚠️ Unknown window URL:', url);
                    }

                    // Close the window after capturing
                    setTimeout(() => {
                        printWindow.close();
                    }, 100);

                    // Check if we've captured all documents
                    if (windowsCaptured >= maxWindows) {
                        console.log(`✅ Captured all ${maxWindows} documents`);

                        // Restore interceptors
                        window.open = originalWindowOpen;
                        $.post = originalPost;
                        clearTimeout(timeout);

                        // Re-enable TabManager if it was enabled
                        if (typeof TabManager !== 'undefined' && tabManagerWasEnabled) {
                            TabManager.enable();
                            console.log('✅ TabManager re-enabled');
                        }

                        resolve({
                            packingSlip: capturedWindowHTML.packingSlip,
                            cartonLabel: capturedWindowHTML.cartonLabel
                        });
                    }

                } catch (error) {
                    console.error('❌ Failed to capture window HTML:', error);
                    printWindow.close();
                }
            };

            // For hash-based routes or empty URLs, wait for content to load using polling
            if (url.startsWith('#') || !url || url.trim() === '') {
                const routeType = !url || url.trim() === '' ? 'Empty URL (document.write)' : 'Hash-based route';
                console.log(`   ${routeType} detected, using polling strategy`);

                let attempts = 0;
                const maxAttempts = 50; // 5 seconds max (50 * 100ms)

                const checkContent = () => {
                    attempts++;

                    try {
                        const doc = printWindow.document;
                        const body = doc.body;

                        // Check if content has loaded (body has substantial content)
                        if (body && body.innerHTML && body.innerHTML.length > 1000) {
                            console.log(`   Content loaded after ${attempts * 100}ms`);
                            captureHTML();
                        } else if (attempts < maxAttempts) {
                            setTimeout(checkContent, 100);
                        } else {
                            console.error('❌ Timeout waiting for content to load');
                            printWindow.close();
                        }
                    } catch (e) {
                        console.error('❌ Error checking content:', e);
                        printWindow.close();
                    }
                };

                // Start polling after small delay
                setTimeout(checkContent, 200);

            } else {
                // For absolute URLs, use load event
                printWindow.addEventListener('load', captureHTML);
            }

            return printWindow;
        };

        console.log('🎯 Window and POST interceptors installed');
        console.log('   Waiting for: Packing Slip (window.open) + Carton Label ($.post)');
    });
}

/**
 * Click buttons to trigger print operations (reuses existing button logic)
 */
function clickPrintButtons() {
    console.log('🖱️ Clicking print buttons...');

    // Click Packing Slip button
    const packingSlipBtn = document.getElementById('btnPrintPackSlip');
    if (!packingSlipBtn) {
        throw new Error('Packing Slip button not found');
    }
    console.log('📄 Packing Slip button found:', {
        visible: packingSlipBtn.offsetParent !== null,
        disabled: packingSlipBtn.disabled,
        onclick: !!packingSlipBtn.onclick
    });
    console.log('📄 Clicking Packing Slip button (native .click())...');
    packingSlipBtn.click();
    console.log('📄 Packing Slip button clicked');

    // Small delay before clicking carton label
    setTimeout(() => {
        const cartonLabelBtn = document.getElementById('box-label');
        if (!cartonLabelBtn) {
            throw new Error('Carton Label button not found');
        }
        console.log('📦 Carton Label button found:', {
            visible: cartonLabelBtn.offsetParent !== null,
            disabled: cartonLabelBtn.disabled,
            onclick: !!cartonLabelBtn.onclick
        });
        console.log('📦 Clicking Carton Label button (native .click())...');
        cartonLabelBtn.click();
        console.log('📦 Carton Label button clicked');
    }, 200);
}

// =============================================================================
// MAIN PRINT WORKFLOW
// =============================================================================

/**
 * Main function to print both documents silently
 * Uses window interception to capture HTML from existing print windows
 * @param {number} shipmentId - Shipment ID
 * @returns {Promise<boolean>}
 */
async function printAllSilent(shipmentId) {
    console.log('🖨️🖨️ Silent Print All - Starting...');
    console.log('   Shipment ID:', shipmentId);
    console.log('   Print Mode:', SILENT_AUTO_PRINT_CONFIG.printMode);

    try {
        // Step 1: Check JSPrintManager availability
        const jsprintAvailable = await checkJSPrintManagerAvailable();

        if (!jsprintAvailable) {
            console.warn('⚠️ JSPrintManager not available, showing user options');
            await handleJSPrintManagerUnavailable(shipmentId);
            return false;
        }

        // Step 2: Set up window interceptor and click buttons
        console.log('📋 Setting up window interceptor...');

        // Start window interception (returns promise that resolves when both windows are captured)
        const capturePromise = interceptPrintWindows();

        // Click the print buttons (they will open windows that get intercepted)
        clickPrintButtons();

        // Wait for HTML to be captured from both documents
        console.log('⏳ Waiting for documents to be captured...');
        const capturedHTML = await capturePromise;

        // Detailed check of what we have
        console.log('📊 Capture status:');
        console.log(`   Packing Slip: ${capturedHTML.packingSlip ? 'YES (' + capturedHTML.packingSlip.length + ' chars)' : 'MISSING'}`);
        console.log(`   Carton Label: ${capturedHTML.cartonLabel ? 'YES (' + capturedHTML.cartonLabel.length + ' chars)' : 'MISSING'}`);

        if (!capturedHTML.packingSlip || !capturedHTML.cartonLabel) {
            const missing = [];
            if (!capturedHTML.packingSlip) missing.push('Packing Slip');
            if (!capturedHTML.cartonLabel) missing.push('Carton Label');
            throw new Error(`Failed to capture: ${missing.join(', ')}`);
        }

        console.log('✅ All documents captured successfully');

        // Step 3: Send to JSPrintManager
        console.log('🖨️ Sending to JSPrintManager...');

        const printPromises = [
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNamePackingSlip,
                capturedHTML.packingSlip,
                '',  // CSS already included in full HTML
                'Packing Slip'
            ),
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNameCartonLabel,
                capturedHTML.cartonLabel,
                '',  // CSS already included in full HTML
                'Carton Label'
            )
        ];

        const results = await Promise.allSettled(printPromises);

        // Check results
        const allSucceeded = results.every(r => r.status === 'fulfilled');

        if (allSucceeded) {
            // Step 4: Show success notification
            console.log('✅ Silent print completed successfully');

            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.success({
                    message: 'Documents sent to printer successfully',
                    duration: 2000
                });
            }

            return true;
        } else {
            // Some prints failed
            const failedJobs = results
                .map((r, i) => r.status === 'rejected' ? i : -1)
                .filter(i => i !== -1);

            throw new Error(`Print jobs failed: ${failedJobs.join(', ')}`);
        }

    } catch (error) {
        console.error('❌ Silent print failed:', error);
        await handlePrintFailure(error, shipmentId);
        return false;
    }
}

// =============================================================================
// FALLBACK MECHANISMS
// =============================================================================

/**
 * Handle case when JSPrintManager is unavailable
 * @param {number} shipmentId - Shipment ID
 */
async function handleJSPrintManagerUnavailable(shipmentId) {
    console.warn('⚠️ JSPrintManager not available - showing user options');

    // Show warning notification
    if (typeof OverlayManager !== 'undefined') {
        OverlayManager.warning({
            title: 'Silent Print Unavailable',
            message: 'JSPrintManager client is not running. Choosing fallback option...',
            duration: 3000
        });
    }

    // Wait for notification to be visible
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Ask user for choice
    const choice = await showFallbackChoice();

    if (choice === 'localhost') {
        await fallbackToLocalhost(shipmentId);
    } else if (choice === 'windows') {
        await fallbackToWindows(shipmentId);
    } else {
        console.log('ℹ️ User cancelled print operation');
    }
}

/**
 * Show fallback choice dialog
 * @returns {Promise<string>} 'localhost' | 'windows' | 'cancel'
 */
async function showFallbackChoice() {
    // Use browser confirm dialog
    const message = `
JSPrintManager is not available.

OK = Use localhost:8080 print service
Cancel = Open print windows instead
    `.trim();

    const useLocalhost = confirm(message);

    return useLocalhost ? 'localhost' : 'windows';
}

/**
 * Fallback to localhost:8080 print service
 * @param {number} shipmentId - Shipment ID
 */
async function fallbackToLocalhost(shipmentId) {
    if (!SILENT_AUTO_PRINT_CONFIG.fallbackToLocalhost) {
        console.log('ℹ️ Localhost fallback disabled in config');
        await fallbackToWindows(shipmentId);
        return;
    }

    console.log('🔄 Falling back to localhost:8080 print service');

    try {
        // Use window interception to capture HTML
        console.log('📋 Setting up window interceptor for localhost fallback...');
        const capturePromise = interceptPrintWindows();
        clickPrintButtons();

        console.log('⏳ Waiting for windows to load...');
        const capturedHTML = await capturePromise;

        if (!capturedHTML.packingSlip || !capturedHTML.cartonLabel) {
            throw new Error('Failed to capture HTML for localhost printing');
        }

        console.log('✅ HTML captured, sending to localhost print service');

        // Send to localhost service
        const printPromises = [
            $.post('http://localhost:8080/printinvoice', capturedHTML.packingSlip),
            $.post('http://localhost:8080/printinvoice', capturedHTML.cartonLabel)
        ];

        await Promise.all(printPromises);

        console.log('✅ Localhost print service completed');

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.info({
                message: 'Documents sent to localhost print service',
                duration: 2000
            });
        }

    } catch (error) {
        console.error('❌ Localhost fallback failed:', error);

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'Print service failed. Opening windows...',
                duration: 2000
            });
        }

        // Final fallback to windows
        await fallbackToWindows(shipmentId);
    }
}

/**
 * Fallback to window-based printing
 * @param {number} shipmentId - Shipment ID
 */
async function fallbackToWindows(shipmentId) {
    if (!SILENT_AUTO_PRINT_CONFIG.fallbackToWindows) {
        console.error('❌ All print methods exhausted and windows fallback disabled');

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'All print methods failed. Please print manually.',
                duration: 5000
            });
        }
        return;
    }

    console.log('🔄 Falling back to window-based printing');

    // Use existing auto-print-buttons.js logic
    if (typeof window.autoPrintButtons !== 'undefined' && window.autoPrintButtons.printAll) {
        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.info({
                message: 'Opening print windows...',
                duration: 2000
            });
        }

        // Call existing print function
        await window.autoPrintButtons.printAll();

    } else {
        console.error('❌ Window-based printing not available');

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'All print methods failed. Please print manually using the modal buttons.',
                duration: 5000
            });
        }
    }
}

/**
 * Handle print failure
 * @param {Error} error - Error object
 * @param {number} shipmentId - Shipment ID
 */
async function handlePrintFailure(error, shipmentId) {
    console.error('❌ Print operation failed:', error);

    // Check if we should try fallback
    if (SILENT_AUTO_PRINT_CONFIG.fallbackToLocalhost) {
        console.log('🔄 Attempting localhost fallback...');
        await fallbackToLocalhost(shipmentId);
    } else if (SILENT_AUTO_PRINT_CONFIG.fallbackToWindows) {
        console.log('🔄 Attempting windows fallback...');
        await fallbackToWindows(shipmentId);
    } else {
        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'Print failed. Please print manually.',
                duration: 4000
            });
        }
    }
}

// =============================================================================
// MODAL DETECTION & SHIPMENT ID EXTRACTION
// =============================================================================

/**
 * Global variable to store last shipment ID from API
 * @type {number|null}
 */
window._lastShipmentId = null;

/**
 * Setup API interceptor to capture shipment ID
 * This should be called once on page load
 */
function setupShipmentIdInterceptor() {
    // Intercept jQuery ajax calls
    if (typeof $ !== 'undefined' && $.ajaxSetup) {
        const originalAjax = $.ajax;

        $.ajax = function(url, settings) {
            // Handle both formats: $.ajax(url, settings) and $.ajax(settings)
            let actualUrl = url;
            let actualSettings = settings;

            if (typeof url === 'object') {
                actualSettings = url;
                actualUrl = actualSettings.url;
            }

            // Create a new promise that wraps the original
            const promise = originalAjax.call(this, url, settings);

            // Intercept success
            promise.then(function(data) {
                // Check if this is ProcessOutboundShipment response
                if (actualUrl && actualUrl.includes('ProcessOutboundShipment')) {
                    if (data && data.data && data.data.shipmentId) {
                        window._lastShipmentId = data.data.shipmentId;
                        console.log('📌 Captured shipment ID:', window._lastShipmentId);

                        // Store on modal when it appears
                        setTimeout(() => {
                            const modal = document.getElementById('shipment-created');
                            if (modal) {
                                modal.dataset.shipmentId = window._lastShipmentId;
                                console.log('📌 Stored shipment ID on modal');
                            }
                        }, 100);
                    }
                }
            });

            return promise;
        };

        console.log('✅ Shipment ID interceptor setup complete');
    }
}

/**
 * Extract shipment ID from modal or stored value
 * @returns {number|null}
 */
function extractShipmentIdFromModal() {
    // Strategy 1: Check stored value from interceptor
    if (window._lastShipmentId) {
        console.log('📌 Using stored shipment ID:', window._lastShipmentId);
        return window._lastShipmentId;
    }

    // Strategy 2: Check modal data attribute
    const modal = document.getElementById('shipment-created');
    if (modal && modal.dataset.shipmentId) {
        console.log('📌 Found shipment ID on modal:', modal.dataset.shipmentId);
        return parseInt(modal.dataset.shipmentId);
    }

    // Strategy 3: Check button data attributes
    const packingSlipBtn = document.getElementById('btnPrintPackSlip');
    if (packingSlipBtn && packingSlipBtn.dataset.shipmentId) {
        console.log('📌 Found shipment ID on button:', packingSlipBtn.dataset.shipmentId);
        return parseInt(packingSlipBtn.dataset.shipmentId);
    }

    // Strategy 4: Parse from button onclick
    if (packingSlipBtn && packingSlipBtn.onclick) {
        const onclickStr = packingSlipBtn.onclick.toString();
        const match = onclickStr.match(/id=(\d+)/);
        if (match) {
            console.log('📌 Extracted shipment ID from onclick:', match[1]);
            return parseInt(match[1]);
        }
    }

    console.error('❌ Could not extract shipment ID from modal');
    return null;
}

/**
 * Prevent duplicate calls tracking
 */
let _lastHandleModalCall = {
    shipmentId: null,
    timestamp: 0
};

/**
 * Handle shipment modal appearance
 * This should be called by the router when the success modal is detected
 */
async function handleShipmentModalAppearance() {
    console.log('🎯 handleShipmentModalAppearance called');

    // Check if modal container is visible
    const modalBlockUI = document.getElementById('_modal_block_ui');
    const isVisible = modalBlockUI &&
                     modalBlockUI.classList.contains('loader_block_ui') &&
                     modalBlockUI.style.display !== 'none';

    if (!isVisible) {
        console.log('⚠️ Modal container not visible, exiting');
        return;
    }

    // Verify correct modal (success modal, not create modal)
    const createModal = document.getElementById('shipment-detail');
    const successModal = document.getElementById('shipment-created');

    const createModalVisible = createModal && createModal.offsetParent !== null;
    const successModalVisible = successModal && successModal.offsetParent !== null;

    console.log('📊 Modal visibility check:');
    console.log('   Create modal visible:', createModalVisible);
    console.log('   Success modal visible:', successModalVisible);

    // Don't trigger on create modal
    if (createModalVisible) {
        console.log('❌ Wrong modal (create modal is visible), exiting');
        return;
    }

    // Must be success modal
    if (!successModalVisible) {
        console.log('⚠️ Success modal not visible, exiting');
        return;
    }

    console.log('✅ Success modal detected');

    // Extract shipment ID
    const shipmentId = extractShipmentIdFromModal();

    if (!shipmentId) {
        console.error('❌ Cannot proceed without shipment ID');

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'Cannot auto-print: Shipment ID not found',
                duration: 3000
            });
        }
        return;
    }

    // PREVENT DUPLICATE CALLS: Check if we just processed this shipment
    const now = Date.now();
    if (_lastHandleModalCall.shipmentId === shipmentId &&
        (now - _lastHandleModalCall.timestamp) < 2000) {
        console.log('⚠️ Duplicate call detected, ignoring (same shipment within 2 seconds)');
        return;
    }

    // Update tracking
    _lastHandleModalCall = {
        shipmentId,
        timestamp: now
    };

    // Check print mode configuration
    if (SILENT_AUTO_PRINT_CONFIG.printMode === 'jsprintmanager') {
        console.log('🔧 Print mode: JSPrintManager (silent)');

        if (SILENT_AUTO_PRINT_CONFIG.autoClickEnabled) {
            console.log(`⏳ Auto-triggering silent print in ${SILENT_AUTO_PRINT_CONFIG.autoClickDelay}ms`);

            setTimeout(() => {
                printAllSilent(shipmentId);
            }, SILENT_AUTO_PRINT_CONFIG.autoClickDelay);

        } else {
            console.log('ℹ️ Auto-click disabled, waiting for manual trigger');
        }

    } else if (SILENT_AUTO_PRINT_CONFIG.printMode === 'windows') {
        console.log('🔧 Print mode: Windows (using existing auto-print-buttons.js)');

        // Use existing auto-print-buttons.js logic
        if (typeof window.autoPrintButtons !== 'undefined' && window.autoPrintButtons.handleModal) {
            window.autoPrintButtons.handleModal();
        } else {
            console.warn('⚠️ auto-print-buttons.js not available');
        }

    } else {
        console.warn('⚠️ Unknown print mode:', SILENT_AUTO_PRINT_CONFIG.printMode);
    }
}

// =============================================================================
// GLOBAL API
// =============================================================================

/**
 * Expose global API for debugging and manual control
 */
if (typeof window !== 'undefined') {
    window.silentAutoPrint = {
        // Configuration
        config: SILENT_AUTO_PRINT_CONFIG,

        // Main functions
        printAll: printAllSilent,
        handleModal: handleShipmentModalAppearance,

        // Window interception (for testing)
        interceptWindows: interceptPrintWindows,
        clickButtons: clickPrintButtons,

        // JSPrintManager integration
        checkJSPrintManager: checkJSPrintManagerAvailable,
        listPrinters: getAvailablePrinters,
        validatePrinter: validatePrinterExists,
        printHTML: printHTMLDocument,

        // Fallback functions
        fallbackLocalhost: fallbackToLocalhost,
        fallbackWindows: fallbackToWindows,

        // Utility functions
        setPrintMode: (mode) => {
            if (mode === 'jsprintmanager' || mode === 'windows') {
                SILENT_AUTO_PRINT_CONFIG.printMode = mode;
                console.log(`🔧 Print mode set to: ${mode}`);
            } else {
                console.error('❌ Invalid print mode. Use "jsprintmanager" or "windows"');
            }
        },

        setPrinter: (job, printerName) => {
            if (job === 'packingSlip') {
                SILENT_AUTO_PRINT_CONFIG.printerNamePackingSlip = printerName;
                console.log(`🔧 Packing Slip printer set to: ${printerName}`);
            } else if (job === 'cartonLabel') {
                SILENT_AUTO_PRINT_CONFIG.printerNameCartonLabel = printerName;
                console.log(`🔧 Carton Label printer set to: ${printerName}`);
            } else {
                console.error('❌ Invalid job type. Use "packingSlip" or "cartonLabel"');
            }
        },

        setAutoClick: (enabled) => {
            SILENT_AUTO_PRINT_CONFIG.autoClickEnabled = enabled;
            console.log(`🔧 Auto-click ${enabled ? 'enabled' : 'disabled'}`);
        },

        // Shipment ID utilities
        extractShipmentId: extractShipmentIdFromModal,
        getLastShipmentId: () => window._lastShipmentId
    };

    console.log('✅ Silent Auto Print Buttons loaded');
    console.log('🔧 Debug API available at: window.silentAutoPrint');
    console.log('💡 Configuration:', SILENT_AUTO_PRINT_CONFIG);
    console.log('💡 Approach: Intercepts window.open() to capture HTML from existing print windows');
    console.log('');
    console.log('📖 Common commands:');
    console.log('   window.silentAutoPrint.checkJSPrintManager()  - Test JSPrintManager');
    console.log('   window.silentAutoPrint.listPrinters()         - List available printers');
    console.log('   window.silentAutoPrint.setPrintMode("windows") - Switch to window mode');
    console.log('   window.silentAutoPrint.setAutoClick(false)    - Disable auto-click');
    console.log('   window.silentAutoPrint.interceptWindows()     - Test window interception');
}

// Setup shipment ID interceptor on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupShipmentIdInterceptor);
} else {
    setupShipmentIdInterceptor();
}
