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
 * Intercepts window.open to change popup to new tab
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
        console.log('🖨️ Clicking Packing Slip button');
    }

    // Intercept window.open to change popup to tab
    const originalWindowOpen = window.open;
    let intercepted = false;

    window.open = function(url, target, features) {
        if (!intercepted) {
            intercepted = true;
            if (AUTO_PRINT_CONFIG.debugMode) {
                console.log('✅ Packing Slip - intercepting window.open');
                console.log('   URL:', url);
                console.log('   Original target:', target);
                console.log('   Original features:', features);
            }

            // Restore original window.open
            window.open = originalWindowOpen;

            // Open in NEW TAB instead of popup window
            const result = originalWindowOpen.call(this, url, '_blank');

            if (AUTO_PRINT_CONFIG.debugMode) {
                console.log('✅ Packing Slip opened in new tab');
            }

            return result;
        }
        return originalWindowOpen.apply(this, arguments);
    };

    // Click the button
    packingSlipBtn.click();
    console.log('✅ Packing Slip button clicked');

    // Restore after timeout if not intercepted
    setTimeout(() => {
        if (!intercepted) {
            window.open = originalWindowOpen;
            console.warn('⚠️⚠️⚠️ Packing Slip did NOT call window.open!');
            console.log('   This means the button either:');
            console.log('   1. Modal closed before we could click');
            console.log('   2. Button uses different method to open window');
            console.log('   3. Button is disabled or not clickable');
        }
    }, 1000);

    return true;
}

/**
 * Finds and clicks the Print Carton Label button
 * Intercepts window.open to change popup to new tab
 * Returns a Promise that resolves when the print window opens
 */
function clickCartonLabelButton() {
    const cartonLabelBtn = document.getElementById('box-label');

    if (!cartonLabelBtn) {
        console.warn('⚠️ Carton Label button not found');
        return Promise.reject(new Error('Carton Label button not found'));
    }

    if (AUTO_PRINT_CONFIG.debugMode) {
        console.log('🖨️ Clicking Carton Label button');
    }

    // Return a promise that resolves when the window.open is called
    return new Promise((resolve) => {
        // Intercept window.open temporarily to change popup to tab
        const originalWindowOpen = window.open;
        let intercepted = false;

        window.open = function(url, target, features) {
            if (!intercepted) {
                intercepted = true;
                if (AUTO_PRINT_CONFIG.debugMode) {
                    console.log('✅ Carton Label - intercepting window.open');
                    console.log('   Original target:', target);
                    console.log('   Original features:', features);
                }

                // Restore original window.open
                window.open = originalWindowOpen;

                // Open in NEW TAB instead of popup window
                // Use '_blank' target without features to open as tab
                const result = originalWindowOpen.call(this, url, '_blank');

                if (AUTO_PRINT_CONFIG.debugMode) {
                    console.log('✅ Carton Label opened in new tab');
                }

                // Resolve after a short delay to ensure tab is ready
                setTimeout(() => resolve(result), 1500);

                return result;
            }
            return originalWindowOpen.apply(this, arguments);
        };

        // Click the button
        cartonLabelBtn.click();

        // Fallback timeout in case window.open isn't called
        setTimeout(() => {
            if (!intercepted) {
                window.open = originalWindowOpen;
                if (AUTO_PRINT_CONFIG.debugMode) {
                    console.log('⚠️ Carton Label window.open not detected, proceeding anyway');
                }
                resolve(null);
            }
        }, 3000);
    });
}

/**
 * Combined action that clicks both buttons simultaneously
 * Both tabs open at the same time (modal may close after first click)
 */
async function printAllButtons() {
    console.log('🖨️🖨️ Print All - Starting...');
    console.log('💡 Strategy: Click BOTH buttons immediately (tabs will open independently)');

    try {
        // Intercept window.open globally to convert all popups to tabs
        const originalWindowOpen = window.open;
        const openedWindows = [];

        window.open = function(url, _target, features) {
            console.log('🪟 window.open intercepted:');
            console.log('   URL:', url);
            console.log('   Original features:', features);

            // Open in tab instead
            const result = originalWindowOpen.call(this, url, '_blank');
            openedWindows.push(result);

            console.log('✅ Opened as tab');
            return result;
        };

        // Click both buttons with minimal delay
        console.log('📦 Clicking Carton Label...');
        const cartonBtn = document.getElementById('box-label');
        if (cartonBtn) {
            cartonBtn.click();
        } else {
            console.warn('⚠️ Carton Label button not found');
        }

        // Small delay to ensure first click registers
        await new Promise(resolve => setTimeout(resolve, 100));

        console.log('📄 Clicking Packing Slip...');
        const packingBtn = document.getElementById('btnPrintPackSlip');
        if (packingBtn) {
            packingBtn.click();
        } else {
            console.warn('⚠️ Packing Slip button not found');
        }

        // Wait for windows to open
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Restore original window.open
        window.open = originalWindowOpen;

        console.log(`✅ Print All completed - ${openedWindows.length} tab(s) opened`);
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
    printAllBtn.textContent = '🖨️ Print All (2 Tabs)';
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
 * Monitors the "Create Shipment" button and triggers print all after it's clicked
 */
function setupCreateShipmentButtonListener() {
    const createShipmentBtn = document.getElementById('create-shipment');

    if (!createShipmentBtn) {
        if (AUTO_PRINT_CONFIG.debugMode) {
            console.log('⚠️ Create Shipment button not found');
        }
        return false;
    }

    // Check if already has listener
    if (createShipmentBtn._autoPrintListenerAdded) {
        if (AUTO_PRINT_CONFIG.debugMode) {
            console.log('ℹ️ Create Shipment button already has listener');
        }
        return true;
    }

    // Initialize flag
    window._shouldAutoClickPrintAll = false;

    // Add click event listener
    createShipmentBtn.addEventListener('click', () => {
        console.log('🔔🔔🔔 CREATE SHIPMENT BUTTON CLICKED! 🔔🔔🔔');
        console.log('🔔 Setting auto-click flag to TRUE');

        // Set flag to auto-click when modal appears
        window._shouldAutoClickPrintAll = true;

        console.log('🔔 Flag value:', window._shouldAutoClickPrintAll);
    }, true); // Use capture phase to ensure we catch the event

    createShipmentBtn._autoPrintListenerAdded = true;

    console.log('✅ Create Shipment button listener added');
    console.log('   Button ID:', createShipmentBtn.id);
    console.log('   Button text:', createShipmentBtn.textContent.trim());
    return true;
}

/**
 * Main handler for shipment modal appearance
 * This should be called by the router when the modal is detected
 */
function handleShipmentModalAppearance() {
    console.log('🎯 handleShipmentModalAppearance called');

    const modal = document.getElementById('shipment-created');

    if (!modal) {
        console.log('⚠️ Modal #shipment-created not found');
        return;
    }

    // Check if modal is visible
    const modalBlockUI = document.getElementById('_modal_block_ui');
    const isVisible = modalBlockUI &&
                     modalBlockUI.classList.contains('loader_block_ui') &&
                     modalBlockUI.style.display !== 'none';

    console.log('📊 Modal visibility check:');
    console.log('   modalBlockUI exists:', !!modalBlockUI);
    console.log('   has loader_block_ui class:', modalBlockUI?.classList.contains('loader_block_ui'));
    console.log('   display style:', modalBlockUI?.style.display);
    console.log('   isVisible:', isVisible);

    if (!isVisible) {
        console.log('⚠️ Modal not visible, exiting');
        return;
    }

    console.log('✅ Shipment created modal detected and visible!');

    // Add the button
    if (addPrintAllButton()) {
        console.log('📊 Auto-click check:');
        console.log('   window._shouldAutoClickPrintAll:', window._shouldAutoClickPrintAll);
        console.log('   AUTO_PRINT_CONFIG.autoClickEnabled:', AUTO_PRINT_CONFIG.autoClickEnabled);

        // Only auto-click if Create Shipment button was pressed
        if (window._shouldAutoClickPrintAll && AUTO_PRINT_CONFIG.autoClickEnabled) {
            console.log('✅✅✅ CONDITIONS MET - Auto-clicking Print All in ' + AUTO_PRINT_CONFIG.autoClickDelay + 'ms');

            setTimeout(() => {
                autoClickPrintAll();
                // Reset flag
                window._shouldAutoClickPrintAll = false;
                console.log('🔄 Flag reset to false');
            }, AUTO_PRINT_CONFIG.autoClickDelay);
        } else {
            console.log('❌ NOT auto-clicking:');
            if (!window._shouldAutoClickPrintAll) {
                console.log('   ❌ Create Shipment was NOT clicked (flag is false)');
            }
            if (!AUTO_PRINT_CONFIG.autoClickEnabled) {
                console.log('   ❌ Auto-click is disabled in config');
            }
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