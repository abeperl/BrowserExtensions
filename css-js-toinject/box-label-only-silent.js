/**
 * Box Label Only - Silent Auto Print
 *
 * Simplified version that ONLY prints box labels (carton labels) silently
 * to a preselected printer. Reuses original button logic.
 *
 * Steps:
 * 1. Fetch shipment data (same as original)
 * 2. Generate HTML for labels (same as original)
 * 3. Generate barcodes (same as original)
 * 4. Print silently to configured printer (NEW - using JSPrintManager)
 */

// =============================================================================
// CONFIGURATION
// =============================================================================

const BOX_LABEL_CONFIG = {
    // Target printer name (must match Windows printer name exactly)
    printerName: 'Brother HL-L6200DW series',

    // Auto-trigger settings
    autoTriggerEnabled: true,
    autoTriggerDelay: 2000,  // Wait 2s after modal appears

    // Debug logging
    debugMode: true
};

// =============================================================================
// STEP 1: FETCH DATA (Same as original)
// =============================================================================

/**
 * Fetch shipment data from API
 * @param {number} shipmentId - Shipment ID
 * @returns {Promise<Object>} API response data
 */
async function fetchShipmentData(shipmentId) {
    return new Promise((resolve, reject) => {
        const url = `OutbondShipment/GetPackingSlipDetailByShipmentId?ShipmentId=${shipmentId}`;

        if (BOX_LABEL_CONFIG.debugMode) {
            console.log(`📡 Fetching shipment data: ${url}`);
        }

        tf.service.get(url, {}, function(data) {
            if (data.responseCode === 0) {
                console.log('✅ Data fetched successfully');
                resolve(data);
            } else {
                console.error('❌ API error:', data);
                reject(new Error('API returned error: ' + data.responseCode));
            }
        });
    });
}

// =============================================================================
// STEP 2: GENERATE HTML (Same as original)
// =============================================================================

/**
 * Generate HTML for box labels
 * @param {Object} apiData - Response from API
 * @returns {Object} { html: string, barcodeData: Array }
 */
function generateBoxLabelHTML(apiData) {
    console.log('📄 Generating box label HTML...');

    const masterData = apiData.data.Shipment;
    const shipmentDetails = apiData.data.ShipmentDetails;

    // Group by BoxNo (same as original)
    const boxesArray = common.groupArrayBy(shipmentDetails, "BoxNo");
    const boxesArrayResult = [];
    for (var c in boxesArray) {
        boxesArrayResult.push([c, boxesArray[c]]);
    }

    // Configuration (same as original)
    const HideUPCShowOrderRefBarcodeCartonLabel = common.getConfigByName("HideUPCShowOrderRefBarcodeCartonLabel", true);

    const fontSize1 = HideUPCShowOrderRefBarcodeCartonLabel ? "40px" : "40px";
    const fontSize2 = HideUPCShowOrderRefBarcodeCartonLabel ? "40px" : "30px";

    let labellHtml = "";
    const barcodeData = [];  // Store barcode info for later generation

    // Generate HTML for each box (same as original)
    for (var i = 0; i < boxesArrayResult.length; i++) {
        const dataDl = boxesArrayResult[i];
        const boxNo = dataDl[0];
        const cartonNo = i + 1;

        // Build UPC codes string
        let upcCodes = "";
        for (var u = 0; u < dataDl[1].length; u++) {
            upcCodes += dataDl[1][u].UPC;
            if (u + 1 < dataDl[1].length) {
                upcCodes += ",";
            }
        }

        // Start box label wrapper
        labellHtml += "<div class='box-label-wrp' style='page-break-after: always;'>";

        // Top info section (organization)
        labellHtml += "<div class='top-info-section' style='text-align: center;font-size: " + fontSize2 + ";font-family: sans-serif;line-height: 1.2;border-bottom: 5px solid #000;padding-bottom: 20px;margin-bottom: 20px;padding-top: 10px;letter-spacing: 1px;padding-bottom: 50px;'>";
        labellHtml += "<div class='text'>Box No : " + boxNo + "</div>";
        labellHtml += "<div class='text'>" + tf.organization().Name + "</div>";
        labellHtml += "<div class='text'>" + tf.organization().Address1 + "</div>";
        labellHtml += "<div class='text' style='font-size: 30px'>" + tf.organization().CityName + ", " + tf.organization().StateCode + " " + tf.organization().Zipcode + "</div>";
        labellHtml += "<div class='text' style='font-size: 30px'>" + tf.organization().Contact + "</div>";
        labellHtml += "<div class='text' style='font-size: 30px'>" + tf.organization().Fax + "</div>";
        labellHtml += "</div>";

        // Ship info section
        labellHtml += "<div class='ship-info-section' style='font-size: 35px;padding: 0 20px;line-height: 1.5;'>";
        labellHtml += "<div class='text sm-text' style='text-align: center;font-size: 25px;'>Customer ID: " + masterData.CustomerCode + "</div>";
        labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>To:</div>";
        labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>" + masterData.CustomerName + "</div>";
        labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>" + masterData.ShipAddress1 + "</div>";
        labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>" + masterData.ShipAddress2 + "</div>";

        if (masterData.ShipCity) {
            labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>" + masterData.ShipCity + ", " + masterData.StateCode + " " + masterData.ShipZipCode + "</div>";
        } else {
            labellHtml += "<div class='text' style='font-size: " + fontSize2 + ";'>" + masterData.CityName + ", " + masterData.ShipStateName + " " + masterData.ShipZipCode + "</div>";
        }

        labellHtml += "<div class='text sm-text' style='font-size: 25px;margin-top: 10px;'>Tel: " + masterData.ShipContact + "</div>";
        labellHtml += "<div class='text' style='margin-top: 10px;font-size: " + fontSize2 + ";'>" + tf.langData().Carrier + ": " + masterData.CarrierName + "</div>";

        // UPC or Barcode section
        if (!HideUPCShowOrderRefBarcodeCartonLabel) {
            labellHtml += "<div class='text'>" + tf.langData().UPC + ": " + upcCodes + "</div>";
        } else {
            // Barcode section with unique ID
            const barcodeId = 'barcode' + i;
            labellHtml += "<div class='order-ref' style='text-align:center;margin:25px 0 10px;'>";
            labellHtml += "<svg class='barcode' id='" + barcodeId + "'></svg>";
            labellHtml += "<div class='order-ref-text' style='font-size:28px;font-weight:600;margin-top:8px;'>" + masterData.OrderRefNumber + "</div>";
            labellHtml += "</div>";

            // Store barcode data for generation after HTML is added to DOM
            barcodeData.push({
                id: barcodeId,
                value: masterData.OrderRefNumber
            });
        }

        labellHtml += "</div>";

        // Carton count
        labellHtml += "<div class='carton-count' style='margin: 0 auto;display: table;border: 1px solid #000;padding: 5px 40px;font-size: 40px;'>" + cartonNo + " of " + boxesArrayResult.length + "</div>";

        labellHtml += "</div>";  // End box-label-wrp
    }

    console.log(`✅ Generated HTML for ${boxesArrayResult.length} box(es)`);

    return {
        html: labellHtml,
        barcodeData: barcodeData
    };
}

// =============================================================================
// STEP 3: GENERATE BARCODES (Same as original)
// =============================================================================

/**
 * Generate barcodes in the DOM
 * @param {HTMLElement} container - Container element with label HTML
 * @param {Array} barcodeData - Array of {id, value} objects
 */
function generateBarcodes(container, barcodeData) {
    console.log(`📊 Generating ${barcodeData.length} barcode(s)...`);

    barcodeData.forEach(item => {
        const svgElement = container.querySelector('#' + item.id);
        if (svgElement && item.value) {
            JsBarcode('#' + item.id, item.value, {
                format: "CODE128",
                lineColor: "#000",
                width: 4,
                height: 150,
                displayValue: false
            });
            console.log(`  ✅ Barcode generated: ${item.id}`);
        } else {
            console.warn(`  ⚠️ Barcode element not found: ${item.id}`);
        }
    });

    console.log('✅ All barcodes generated');
}

// =============================================================================
// STEP 4: SILENT PRINT TO CONFIGURED PRINTER (NEW)
// =============================================================================

/**
 * Check if JSPrintManager is available
 * @returns {Promise<boolean>}
 */
async function checkJSPrintManager() {
    try {
        if (typeof JSPM === 'undefined') {
            console.warn('⚠️ JSPrintManager library not loaded');
            return false;
        }

        // Try to connect with 3 second timeout
        const timeoutPromise = new Promise((_, reject) =>
            setTimeout(() => reject(new Error('Timeout')), 3000)
        );

        await Promise.race([JSPM.JSPrintManager.start(), timeoutPromise]);

        console.log('✅ JSPrintManager connected');
        return true;

    } catch (error) {
        console.warn('⚠️ JSPrintManager not available:', error.message);
        return false;
    }
}

/**
 * Print HTML to configured printer using JSPrintManager
 * @param {string} htmlContent - Complete HTML document
 * @returns {Promise<boolean>}
 */
async function printToJSPM(htmlContent) {
    try {
        console.log('🖨️ Preparing print job...');
        console.log(`   Target printer: ${BOX_LABEL_CONFIG.printerName}`);

        // Create print job
        const cpj = new JSPM.ClientPrintJob();

        // Set printer (with fallback to default)
        const printers = await JSPM.JSPrintManager.getPrinters();
        const printerExists = printers.some(p => p.name === BOX_LABEL_CONFIG.printerName);

        if (printerExists) {
            cpj.clientPrinter = new JSPM.InstalledPrinter(BOX_LABEL_CONFIG.printerName);
            console.log(`   ✅ Using configured printer`);
        } else {
            cpj.clientPrinter = new JSPM.DefaultPrinter();
            console.warn('   ⚠️ Configured printer not found, using default');
            console.log('   Available printers:', printers.map(p => p.name));
        }

        // Create complete HTML document with CSS
        const fullHTML = `
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <link href="pages/Outbound/placard.css" rel="stylesheet">
    <style media="print">
        @media print {
            body {
                -webkit-print-color-adjust: exact !important;
            }
            .box-label-wrp {
                page-break-inside: auto;
                page-break-inside: avoid;
                page-break-after: auto;
            }
            @page {
                size: letter;
            }
        }
    </style>
</head>
<body>
${htmlContent}
</body>
</html>
        `;

        // Create print file
        const printFile = new JSPM.PrintFile(
            fullHTML,
            JSPM.FileSourceType.BLOB,
            'box-label.html',
            1  // Number of copies
        );

        cpj.files.push(printFile);

        // Send to client
        await cpj.sendToClient();

        console.log('✅ Print job sent successfully');
        return true;

    } catch (error) {
        console.error('❌ JSPrintManager print failed:', error);
        throw error;
    }
}

/**
 * Fallback: Open window for manual printing
 * @param {string} htmlContent - Label HTML
 */
function printWithWindow(htmlContent) {
    console.log('🔄 Falling back to window-based printing...');

    const mywindow = window.open("", "", "height=700,width=1100");

    mywindow.document.write(`
        <html>
        <head>
            <link href="pages/Outbound/placard.css" rel="stylesheet">
            <title>Box Labels</title>
            <style media="print">
                @media print {
                    body {
                        -webkit-print-color-adjust: exact !important;
                    }
                    .box-label-wrp {
                        page-break-inside: auto;
                        page-break-inside: avoid;
                        page-break-after: auto;
                    }
                    @page {
                        size: letter;
                    }
                }
            </style>
        </head>
        <body>
            ${htmlContent}
        </body>
        </html>
    `);

    mywindow.document.close();

    // Trigger print dialog after slight delay
    setTimeout(() => {
        mywindow.print();
    }, 1000);
}

// =============================================================================
// MAIN WORKFLOW
// =============================================================================

/**
 * Main function: Print box labels silently
 * @param {number} shipmentId - Shipment ID
 * @returns {Promise<boolean>}
 */
async function printBoxLabels(shipmentId) {
    console.log('═══════════════════════════════════════════════════');
    console.log('🏷️ BOX LABEL SILENT PRINT - Starting...');
    console.log(`   Shipment ID: ${shipmentId}`);
    console.log('═══════════════════════════════════════════════════');

    try {
        // STEP 1: Fetch data
        console.log('\n📡 STEP 1: Fetching shipment data...');
        const apiData = await fetchShipmentData(shipmentId);

        // STEP 2: Generate HTML
        console.log('\n📄 STEP 2: Generating label HTML...');
        const { html, barcodeData } = generateBoxLabelHTML(apiData);

        // STEP 3: Add to temp container and generate barcodes
        console.log('\n📊 STEP 3: Generating barcodes...');
        const tempContainer = document.createElement('div');
        tempContainer.id = 'temp-box-label-container';
        tempContainer.style.display = 'none';
        tempContainer.innerHTML = html;
        document.body.appendChild(tempContainer);

        generateBarcodes(tempContainer, barcodeData);

        // Get final HTML with barcodes
        const finalHTML = tempContainer.innerHTML;

        // Clean up temp container
        document.body.removeChild(tempContainer);

        // STEP 4: Print silently (or fallback to window)
        console.log('\n🖨️ STEP 4: Printing to configured printer...');

        const jsprintAvailable = await checkJSPrintManager();

        if (jsprintAvailable) {
            await printToJSPM(finalHTML);

            // Show success notification
            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.success({
                    message: 'Box labels sent to printer',
                    duration: 2000
                });
            }

            return true;

        } else {
            console.warn('⚠️ JSPrintManager not available, using window fallback');

            if (typeof OverlayManager !== 'undefined') {
                OverlayManager.warning({
                    message: 'Silent print unavailable - opening print window',
                    duration: 2000
                });
            }

            printWithWindow(finalHTML);
            return false;
        }

    } catch (error) {
        console.error('❌ Box label print failed:', error);

        if (typeof OverlayManager !== 'undefined') {
            OverlayManager.error({
                message: 'Print failed: ' + error.message,
                duration: 3000
            });
        }

        return false;
    }
}

// =============================================================================
// SHIPMENT ID CAPTURE
// =============================================================================

/**
 * Global variable to store last shipment ID from API
 * @type {number|null}
 */
window._lastShipmentId = null;

/**
 * Setup API interceptor to capture shipment ID
 * This intercepts the ProcessOutboundShipment API response
 */
function setupShipmentIdInterceptor() {
    // Intercept jQuery ajax calls
    if (typeof $ !== 'undefined' && $.ajax) {
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
                        console.log('📌 Captured shipment ID from API:', window._lastShipmentId);

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
    } else {
        console.warn('⚠️ jQuery not available, cannot setup shipment ID interceptor');
    }
}

// =============================================================================
// AUTO-TRIGGER ON MODAL
// =============================================================================

/**
 * Handle modal appearance and auto-trigger print
 */
function handleModalAppearance() {
    console.log('🎯 Modal appearance detected');

    // Extract shipment ID from stored value or modal
    let shipmentId = window._lastShipmentId;

    if (!shipmentId) {
        const modal = document.getElementById('shipment-created');
        if (modal && modal.dataset.shipmentId) {
            shipmentId = parseInt(modal.dataset.shipmentId);
        }
    }

    if (!shipmentId) {
        console.error('❌ Cannot find shipment ID');
        return;
    }

    console.log(`📌 Found shipment ID: ${shipmentId}`);

    if (BOX_LABEL_CONFIG.autoTriggerEnabled) {
        console.log(`⏳ Auto-triggering print in ${BOX_LABEL_CONFIG.autoTriggerDelay}ms...`);

        setTimeout(() => {
            printBoxLabels(shipmentId);
        }, BOX_LABEL_CONFIG.autoTriggerDelay);
    } else {
        console.log('ℹ️ Auto-trigger disabled, waiting for manual call');
    }
}

// =============================================================================
// GLOBAL API
// =============================================================================

if (typeof window !== 'undefined') {
    window.boxLabelSilent = {
        // Configuration
        config: BOX_LABEL_CONFIG,

        // Main function
        print: printBoxLabels,
        handleModal: handleModalAppearance,

        // Utilities
        checkJSPM: checkJSPrintManager,
        setAutoTrigger: (enabled) => {
            BOX_LABEL_CONFIG.autoTriggerEnabled = enabled;
            console.log(`🔧 Auto-trigger ${enabled ? 'enabled' : 'disabled'}`);
        },
        setPrinter: (printerName) => {
            BOX_LABEL_CONFIG.printerName = printerName;
            console.log(`🔧 Printer set to: ${printerName}`);
        }
    };

    console.log('\n✅ Box Label Silent Print loaded');
    console.log('🔧 Debug API: window.boxLabelSilent');
    console.log('💡 Manual trigger: window.boxLabelSilent.print(shipmentId)');
    console.log('💡 Check JSPM: window.boxLabelSilent.checkJSPM()');
    console.log('');
}

// =============================================================================
// INITIALIZATION
// =============================================================================

// Setup shipment ID interceptor on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupShipmentIdInterceptor);
} else {
    // DOM already loaded, setup immediately
    setupShipmentIdInterceptor();
}

console.log('📌 Shipment ID interceptor will capture from ProcessOutboundShipment API');
