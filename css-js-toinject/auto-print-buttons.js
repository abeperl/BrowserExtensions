/**
 * Auto Print Buttons - Shipment Created Modal Enhancement
 *
 * Provides functions to add a combined "Print All" button to the shipment created modal
 * that automatically triggers:
 * 1. Packing Slip
 * 2. Print Carton Label
 *
 * NOTE: This file only contains functions. The router.js handles MutationObserver setup.
 */

// Configuration
const AUTO_PRINT_CONFIG = {
    autoClickEnabled: true,      // Set to false to disable auto-clicking
    autoClickDelay: 500,         // Delay in ms before auto-clicking (gives time for modal to fully load)
    betweenPrintsDelay: 2000,    // Delay in ms between Carton Label and Packing Slip (default: 2 seconds)
    debugMode: true              // Enable detailed logging
};

/**
 * Finds and clicks the Packing Slip button
 * Note: TabManager handles window.open interception automatically
 */
function clickPackingSlipButton() {
    const packingSlipBtn = document.getElementById('btnPrintPackSlip');

    if (!packingSlipBtn) {
        console.warn('⚠️ Packing Slip button not found');
        return false;
    }

    // Check if button is visible and enabled
    console.log('📊 Packing Slip button check:');
    console.log('   Button exists:', !!packingSlipBtn);
    console.log('   Button visible:', packingSlipBtn.offsetParent !== null);
    console.log('   Button disabled:', packingSlipBtn.disabled);
    console.log('   Button display:', window.getComputedStyle(packingSlipBtn).display);

    if (AUTO_PRINT_CONFIG.debugMode) {
        console.log('🖨️ Clicking Packing Slip button (TabManager will handle window.open)');
    }

    // Click the button (TabManager handles window.open interception)
    packingSlipBtn.click();
    console.log('✅ Packing Slip button clicked');

    return true;
}

/**
 * Finds and clicks the Print Carton Label button
 * Note: TabManager handles window.open interception automatically
 */
function clickCartonLabelButton() {
    const cartonLabelBtn = document.getElementById('box-label');

    if (!cartonLabelBtn) {
        console.warn('⚠️ Carton Label button not found');
        return Promise.reject(new Error('Carton Label button not found'));
    }

    if (AUTO_PRINT_CONFIG.debugMode) {
        console.log('🖨️ Clicking Carton Label button (TabManager will handle window.open)');
    }

    // Return a promise that resolves after button click
    return new Promise((resolve) => {
        // Click the button (TabManager handles window.open interception)
        cartonLabelBtn.click();

        // Small delay to ensure tab opens
        setTimeout(() => {
            if (AUTO_PRINT_CONFIG.debugMode) {
                console.log('✅ Carton Label button clicked');
            }
            resolve(true);
        }, 500);
    });
}

/**
 * Combined action that clicks both buttons simultaneously
 * Both tabs open at the same time (modal may close after first click)
 * Note: TabManager handles window.open interception automatically
 */
async function printAllButtons() {
    console.log('🖨️🖨️ Print All - Starting...');
    console.log('💡 Strategy: Click PACKING SLIP FIRST, then CARTON LABEL');
    console.log('   (Carton label click closes modal, so packing slip must go first)');

    try {
        // Step 1: Click PACKING SLIP button FIRST (before modal closes)
        console.log('📄 Step 1: Clicking Packing Slip...');
        const packingBtn = document.getElementById('btnPrintPackSlip');
        if (packingBtn) {
            console.log('   ✅ Packing slip button found');
            console.log('   Button visible:', packingBtn.offsetParent !== null);
            console.log('   Button disabled:', packingBtn.disabled);
            console.log('   Button onclick:', packingBtn.onclick);
            console.log('   Button getAttribute onclick:', packingBtn.getAttribute('onclick'));
            console.log('   Button data-value:', packingBtn.getAttribute('data-value'));

            packingBtn.click();
            console.log('   ✅ Packing slip button clicked');
        } else {
            console.warn('⚠️ Packing Slip button not found');
        }

        // Small delay between clicks
        await new Promise(resolve => setTimeout(resolve, 100));

        // Step 2: Click CARTON LABEL button SECOND
        console.log('📦 Step 2: Clicking Carton Label...');
        const cartonBtn = document.getElementById('box-label');
        if (cartonBtn) {
            console.log('   ✅ Carton label button found');
            console.log('   Button onclick:', cartonBtn.onclick);
            console.log('   Button getAttribute onclick:', cartonBtn.getAttribute('onclick'));
            console.log('   Button data-value:', cartonBtn.getAttribute('data-value'));

            cartonBtn.click();
            console.log('   ✅ Carton label button clicked');
        } else {
            console.warn('⚠️ Carton Label button not found');
        }

        // Wait for both tabs to open
        await new Promise(resolve => setTimeout(resolve, 1000));

        console.log('✅ Print All completed - both tabs should be open');
        console.log('   Tab 1: Packing Slip (opened first)');
        console.log('   Tab 2: Carton Label (opened second)');
        return true;

    } catch (error) {
        console.error('❌ Print All failed:', error);
        return false;
    }
}

/**
 * Adds the "Print All" button to the modal
 */
function addPrintAllButton() {
    const modal = document.getElementById('shipment-created');

    if (!modal) {
        if (AUTO_PRINT_CONFIG.debugMode) {
            console.log('⚠️ Shipment created modal not found');
        }
        return false;
    }

    // Check if button already exists
    if (modal.querySelector('#btn-print-all-combined')) {
        if (AUTO_PRINT_CONFIG.debugMode) {
            console.log('ℹ️ Print All button already exists');
        }
        return true;
    }

    // Find the button container
    const buttonContainer = modal.querySelector('.modal-box-controls .inner-flex');

    if (!buttonContainer) {
        console.warn('⚠️ Button container not found in modal');
        return false;
    }

    // Create the new combined button
    const printAllBtn = document.createElement('button');
    printAllBtn.id = 'btn-print-all-combined';
    printAllBtn.className = 'btn modal-box-button';
    printAllBtn.style.cssText = `
        background-color: #28a745 !important;
        color: white !important;
        font-weight: bold !important;
        border: 2px solid #1e7e34 !important;
        font-size: 14px !important;
        padding: 10px 20px !important;
    `;
    printAllBtn.textContent = '🖨️ Print All (Slip + Label)';
    printAllBtn.setAttribute('data-value', 'print-all');

    // Add click handler
    printAllBtn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        printAllButtons();
    });

    // Insert as first button
    buttonContainer.insertBefore(printAllBtn, buttonContainer.firstChild);

    console.log('✅ Print All button added to modal');
    return true;
}

/**
 * Auto-clicks the Print All button if enabled
 */
function autoClickPrintAll() {
    if (!AUTO_PRINT_CONFIG.autoClickEnabled) {
        console.log('ℹ️ Auto-click disabled');
        return;
    }

    const printAllBtn = document.getElementById('btn-print-all-combined');

    if (!printAllBtn) {
        console.warn('⚠️ Print All button not found for auto-click');
        return;
    }

    console.log('🤖 Auto-clicking Print All button...');
    printAllBtn.click();
}

/**
 * DEPRECATED: This function is no longer used.
 * We don't monitor the "Create Shipment" button anymore.
 * Instead, we auto-click ONLY when the success modal appears.
 */
function setupCreateShipmentButtonListener() {
    console.log('ℹ️ setupCreateShipmentButtonListener called but NOT setting up listener');
    console.log('   Auto-click will trigger based on modal appearance only');
    return true;
}

/**
 * Waits for buttons to become visible before clicking them
 * Returns a promise that resolves when both buttons are visible
 */
async function waitForButtonsVisible() {
    console.log('⏳ Waiting for buttons to become visible...');

    const maxAttempts = 20;
    const delayMs = 100;

    for (let i = 0; i < maxAttempts; i++) {
        const packingBtn = document.getElementById('btnPrintPackSlip');
        const cartonBtn = document.getElementById('box-label');

        if (packingBtn && cartonBtn) {
            const packingVisible = packingBtn.offsetParent !== null;
            const cartonVisible = cartonBtn.offsetParent !== null;

            console.log(`   Attempt ${i + 1}/${maxAttempts}:`, {
                packingVisible,
                cartonVisible
            });

            if (packingVisible && cartonVisible) {
                console.log('✅ Both buttons are now visible!');
                return true;
            }
        }

        await new Promise(resolve => setTimeout(resolve, delayMs));
    }

    console.warn('⚠️ Timeout waiting for buttons to become visible');
    return false;
}

/**
 * Main handler for shipment modal appearance
 * This should be called by the router when the modal is detected
 * IMPORTANT: Only triggers on "Shipment Created Success" modal (id="shipment-created")
 * DOES NOT trigger on "Create Shipment Details" modal (id="shipment-detail")
 */
async function handleShipmentModalAppearance() {
    console.log('🎯 handleShipmentModalAppearance called');

    // Check if modal container is visible
    const modalBlockUI = document.getElementById('_modal_block_ui');
    const isVisible = modalBlockUI &&
                     modalBlockUI.classList.contains('loader_block_ui') &&
                     modalBlockUI.style.display !== 'none';

    console.log('📊 Modal container visibility check:');
    console.log('   modalBlockUI exists:', !!modalBlockUI);
    console.log('   has loader_block_ui class:', modalBlockUI?.classList.contains('loader_block_ui'));
    console.log('   display style:', modalBlockUI?.style.display);
    console.log('   isVisible:', isVisible);

    if (!isVisible) {
        console.log('⚠️ Modal container not visible, exiting');
        return;
    }

    // CRITICAL: Check which modal is displayed
    // BOTH modals exist in the DOM simultaneously but only ONE is visible at a time:
    // 1. id="shipment-detail" - The "Create Shipment" modal (WRONG - do NOT trigger)
    // 2. id="shipment-created" - The "Success" modal (RIGHT - trigger here)

    const createModal = document.getElementById('shipment-detail');
    const successModal = document.getElementById('shipment-created');

    console.log('📊 Modal identification:');
    console.log('   "Create Shipment" modal (shipment-detail) exists:', !!createModal);
    console.log('   "Success" modal (shipment-created) exists:', !!successModal);

    // Check which modal is actually VISIBLE (not just present in DOM)
    const createModalVisible = createModal && createModal.offsetParent !== null;
    const successModalVisible = successModal && successModal.offsetParent !== null;

    console.log('📊 Modal visibility:');
    console.log('   "Create Shipment" modal visible:', createModalVisible);
    console.log('   "Success" modal visible:', successModalVisible);

    // If the "Create Shipment" modal is VISIBLE, DO NOT TRIGGER
    if (createModalVisible) {
        console.log('❌ WRONG MODAL: The "Create Shipment Details" modal is VISIBLE (id="shipment-detail")');
        console.log('   Button text: "Create Shipment"');
        console.log('   We DO NOT trigger on this modal - exiting');
        return;
    }

    // If the "Success" modal is NOT visible, exit
    if (!successModalVisible) {
        console.log('⚠️ Success modal (id="shipment-created") not visible, exiting');
        return;
    }

    console.log('✅ Correct modal detected: "Shipment Created Success" modal is VISIBLE (id="shipment-created")!');

    // Check for print buttons
    const packingSlipBtn = successModal.querySelector('#btnPrintPackSlip');
    const cartonLabelBtn = successModal.querySelector('#box-label');

    console.log('📊 Print button check:');
    console.log('   Packing Slip button found:', !!packingSlipBtn);
    console.log('   Carton Label button found:', !!cartonLabelBtn);

    if (!packingSlipBtn || !cartonLabelBtn) {
        console.log('⚠️ Print buttons not found in success modal, exiting');
        return;
    }

    // Wait for buttons to become visible
    const packingVisible = packingSlipBtn.offsetParent !== null;
    const cartonVisible = cartonLabelBtn.offsetParent !== null;

    console.log('📊 Initial button visibility:');
    console.log('   Packing Slip visible:', packingVisible);
    console.log('   Carton Label visible:', cartonVisible);

    if (!packingVisible && !cartonVisible) {
        console.log('⏳ Buttons not visible yet, waiting...');
        const buttonsVisible = await waitForButtonsVisible();

        if (!buttonsVisible) {
            console.log('❌ Buttons never became visible, exiting');
            return;
        }
    }

    console.log('✅ Print buttons are visible!');

    // Add the Print All button
    if (addPrintAllButton()) {
        console.log('📊 Auto-click check:');
        console.log('   AUTO_PRINT_CONFIG.autoClickEnabled:', AUTO_PRINT_CONFIG.autoClickEnabled);

        if (AUTO_PRINT_CONFIG.autoClickEnabled) {
            console.log('✅✅✅ CONDITIONS MET - Auto-clicking Print All in ' + AUTO_PRINT_CONFIG.autoClickDelay + 'ms');

            setTimeout(() => {
                autoClickPrintAll();
            }, AUTO_PRINT_CONFIG.autoClickDelay);
        } else {
            console.log('❌ Auto-click disabled in config');
        }
    } else {
        console.log('❌ Failed to add Print All button');
    }
}

// Expose global API for debugging and manual control
if (typeof window !== 'undefined') {
    window.autoPrintButtons = {
        config: AUTO_PRINT_CONFIG,
        printAll: printAllButtons,
        clickPackingSlip: clickPackingSlipButton,
        clickCartonLabel: clickCartonLabelButton,
        addButton: addPrintAllButton,
        handleModal: handleShipmentModalAppearance,
        setupCreateShipmentListener: setupCreateShipmentButtonListener,

        // Helper to enable/disable auto-click
        setAutoClick: (enabled) => {
            AUTO_PRINT_CONFIG.autoClickEnabled = enabled;
            console.log(`🔧 Auto-click ${enabled ? 'enabled' : 'disabled'}`);
        },

        // Helper to adjust delay
        setDelay: (delayMs) => {
            AUTO_PRINT_CONFIG.autoClickDelay = delayMs;
            console.log(`🔧 Auto-click delay set to ${delayMs}ms`);
        },

        // Helper to adjust delay between prints
        setBetweenPrintsDelay: (delayMs) => {
            AUTO_PRINT_CONFIG.betweenPrintsDelay = delayMs;
            console.log(`🔧 Between-prints delay set to ${delayMs}ms`);
        }
    };

    console.log('✅ Auto Print Buttons functions loaded');
    console.log('🔧 Debug with: window.autoPrintButtons');
    console.log('💡 Disable auto-click: window.autoPrintButtons.setAutoClick(false)');
    console.log('💡 Manual trigger: window.autoPrintButtons.printAll()');
}