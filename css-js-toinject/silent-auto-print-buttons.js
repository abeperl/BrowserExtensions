/**
 * Silent Auto Print Buttons - JSPrintManager Integration
 *
 * Provides silent printing capabilities using JSPrintManager for:
 * 1. Packing Slip
 * 2. Carton Label
 *
 * Features:
 * - Silent printing directly to configured printers
 * - Printer validation with fallback to default printer
 * - Robust fallback mechanisms (localhost:8080, window-based)
 * - User notifications via OverlayManager
 * - Configuration-based print mode switching
 *
 * Dependencies:
 * - JSPrintManager 8.0 (loaded via CDN)
 * - OverlayManager (for notifications)
 * - jQuery (for API calls)
 *
 * @see css-js-toinject/docs/SILENT-AUTO-PRINT-SPEC.md
 */

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
// CONTENT GENERATION
// =============================================================================

/**
 * Helper function to group array by property
 * @param {Array} array - Array to group
 * @param {string} key - Property key to group by
 * @returns {Object}
 */
function groupArrayBy(array, key) {
    return array.reduce((result, item) => {
        const groupKey = item[key];
        if (!result[groupKey]) {
            result[groupKey] = [];
        }
        result[groupKey].push(item);
        return result;
    }, {});
}

/**
 * Generate Packing Slip HTML content
 * @param {number} shipmentId - Shipment ID
 * @returns {Promise<{html: string, css: string}>}
 */
async function generatePackingSlipHTML(shipmentId) {
    try {
        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log('📄 Generating Packing Slip HTML for shipment:', shipmentId);
        }

        // Fetch data from API
        const response = await $.ajax({
            url: `OutbondShipment/GetPackingSlipDetailByShipmentId?ShipmentId=${shipmentId}`,
            method: 'GET'
        });

        if (response.responseCode !== 0 && response.responseCode !== 200) {
            throw new Error('Failed to fetch packing slip data: ' + response.message);
        }

        // Calculate total quantity
        let TotalQty = 0;
        for (let i = 0; i < response.data.ShipmentDetails.length; i++) {
            TotalQty += parseInt(response.data.ShipmentDetails[i].Quantity) || 0;
        }
        Object.assign(response.data.Shipment, { TotalQty });

        // Group by box number
        const boxesArray = groupArrayBy(response.data.ShipmentDetails, 'BoxNo');
        const boxesArrayResult = Object.entries(boxesArray);

        // Generate HTML using template
        const html = generatePackingSlipTemplate(response.data, boxesArrayResult);

        // Fetch CSS
        const css = await $.ajax({
            url: 'pages/Outbound/packing-slip.css',
            method: 'GET',
            dataType: 'text'
        }).catch(() => {
            // If CSS file doesn't exist, use minimal CSS
            console.warn('⚠️ packing-slip.css not found, using minimal CSS');
            return '* { font-family: sans-serif; } body { margin: 20px; }';
        });

        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log('   ✅ Packing Slip HTML generated');
        }

        return { html, css };

    } catch (error) {
        console.error('❌ Failed to generate Packing Slip HTML:', error);
        throw error;
    }
}

/**
 * Generate Packing Slip HTML template
 * @param {Object} data - Shipment data
 * @param {Array} boxesArrayResult - Grouped boxes array
 * @returns {string} HTML string
 */
function generatePackingSlipTemplate(data, boxesArrayResult) {
    // Get language data helper
    const getLang = (key) => {
        if (typeof tf !== 'undefined' && tf.langData) {
            return tf.langData()[key] || key;
        }
        return key;
    };

    // Get config helper
    const getConfig = (key, defaultValue) => {
        if (typeof common !== 'undefined' && common.getConfigByName) {
            return common.getConfigByName(key, defaultValue);
        }
        return defaultValue;
    };

    const BoxWisePackagingInformation = getConfig('BoxWisePackagingInformation', true);
    const shipment = data.Shipment;

    let html = `
        <div id="master-data">
            <h1>Packing Slip - ${shipment.ShipmentNumber || ''}</h1>
            <div class="shipment-info">
                <p><strong>Order Number:</strong> ${shipment.OrderNumber || ''}</p>
                <p><strong>Shipment Date:</strong> ${shipment.ShipmentDate || ''}</p>
                <p><strong>Total Quantity:</strong> ${shipment.TotalQty || 0}</p>
            </div>
        </div>
        <div id="items-table-wrp">
    `;

    // Generate table for each box
    boxesArrayResult.forEach((boxData, index) => {
        const boxNo = boxData[0];
        const items = boxData[1];

        html += '<div class="table-pakiingsli-wrp">';

        // Box header
        if (BoxWisePackagingInformation && items[0] && items[0].PackageSize && items[0].PackageType) {
            html += `
                <h3 class="panel-box-title text-left packgae-title">
                    <span>${getLang('PackageNo')}: ${boxNo}</span>
                    <span class="ml-4">${getLang('PackageType')}: ${items[0].PackageType}</span>
                    <span class="ml-4">${getLang('PackageSize')}: ${items[0].PackageSize}</span>
                </h3>
            `;
        } else {
            html += `<h3 class="panel-box-title text-left packgae-title">${getLang('PackageNo')}: ${boxNo}</h3>`;
        }

        // Items table
        html += `
            <table class="no_whitespace mb-20" width="100%">
                <thead>
                    <tr>
                        <th>SKU</th>
                        <th>Description</th>
                        <th>Quantity</th>
                    </tr>
                </thead>
                <tbody>
        `;

        items.forEach(item => {
            html += `
                <tr>
                    <td>${item.Sku || ''}</td>
                    <td>${item.ProductName || ''}</td>
                    <td>${item.Quantity || 0}</td>
                </tr>
            `;
        });

        html += `
                </tbody>
            </table>
        </div>
        `;
    });

    html += '</div>';

    return html;
}

/**
 * Generate Carton Label HTML content
 * @param {number} shipmentId - Shipment ID
 * @returns {Promise<{html: string, css: string}>}
 */
async function generateCartonLabelHTML(shipmentId) {
    try {
        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log('📦 Generating Carton Label HTML for shipment:', shipmentId);
        }

        // Look for carton label content in the DOM
        // This assumes the label has already been generated by the existing system
        const wrp = document.querySelector('.carton-label-content, #carton-label-content, [data-carton-label]');

        if (!wrp) {
            console.warn('⚠️ Carton label content not found in DOM, attempting to generate...');

            // If not in DOM, we need to generate it
            // This would require calling the same logic that generates the label
            // For now, throw error to trigger fallback
            throw new Error('Carton label content not available');
        }

        const html = wrp.innerHTML;

        // Fetch CSS
        let css = await $.ajax({
            url: 'pages/Outbound/placard.css',
            method: 'GET',
            dataType: 'text'
        }).catch(() => {
            console.warn('⚠️ placard.css not found, using minimal CSS');
            return '';
        });

        // Add required CSS rules
        css += `
            * { font-family: sans-serif !important; }
            body {
                -webkit-print-color-adjust: exact !important;
                print-color-adjust: exact !important;
            }
            .box-label-wrp {
                page-break-inside: auto;
                page-break-inside: avoid;
                page-break-after: auto;
            }
            @page { size: letter; }
        `;

        if (SILENT_AUTO_PRINT_CONFIG.debugMode) {
            console.log('   ✅ Carton Label HTML generated');
        }

        return { html, css };

    } catch (error) {
        console.error('❌ Failed to generate Carton Label HTML:', error);
        throw error;
    }
}

// =============================================================================
// MAIN PRINT WORKFLOW
// =============================================================================

/**
 * Main function to print both documents silently
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

        // Step 2: Generate content for both documents in parallel
        console.log('📄 Generating print content...');

        const [packingSlipContent, cartonLabelContent] = await Promise.all([
            generatePackingSlipHTML(shipmentId).catch(error => {
                console.error('❌ Packing Slip generation failed:', error);
                return null;
            }),
            generateCartonLabelHTML(shipmentId).catch(error => {
                console.error('❌ Carton Label generation failed:', error);
                return null;
            })
        ]);

        // Check if both succeeded
        if (!packingSlipContent || !cartonLabelContent) {
            throw new Error('Failed to generate one or more documents');
        }

        // Step 3: Print both documents in parallel
        console.log('🖨️ Submitting print jobs to printer...');

        const printPromises = [
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNamePackingSlip,
                packingSlipContent.html,
                packingSlipContent.css,
                'Packing Slip'
            ),
            printHTMLDocument(
                SILENT_AUTO_PRINT_CONFIG.printerNameCartonLabel,
                cartonLabelContent.html,
                cartonLabelContent.css,
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
        // Generate content
        const [packingSlipContent, cartonLabelContent] = await Promise.all([
            generatePackingSlipHTML(shipmentId).catch(() => null),
            generateCartonLabelHTML(shipmentId).catch(() => null)
        ]);

        if (!packingSlipContent || !cartonLabelContent) {
            throw new Error('Failed to generate content for localhost printing');
        }

        // Combine HTML and CSS
        const packingSlipHTML = `<style>${packingSlipContent.css}</style>${packingSlipContent.html}`;
        const cartonLabelHTML = `<style>${cartonLabelContent.css}</style>${cartonLabelContent.html}`;

        // Send to localhost service
        const printPromises = [
            $.post('http://localhost:8080/printinvoice', packingSlipHTML),
            $.post('http://localhost:8080/printinvoice', cartonLabelHTML)
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

        // Content generation (for testing)
        generatePackingSlip: generatePackingSlipHTML,
        generateCartonLabel: generateCartonLabelHTML,

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
    console.log('');
    console.log('📖 Common commands:');
    console.log('   window.silentAutoPrint.checkJSPrintManager()  - Test JSPrintManager');
    console.log('   window.silentAutoPrint.listPrinters()         - List available printers');
    console.log('   window.silentAutoPrint.setPrintMode("windows") - Switch to window mode');
    console.log('   window.silentAutoPrint.setAutoClick(false)    - Disable auto-click');
}

// Setup shipment ID interceptor on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupShipmentIdInterceptor);
} else {
    setupShipmentIdInterceptor();
}
