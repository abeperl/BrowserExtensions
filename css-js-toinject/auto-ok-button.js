/**
 * Auto OK Button Clicker
 * Simple utility to click OK buttons in modals
 * Router handles when to call this
 * Domain: https://malchus.3plnext.com
 */

(function() {
    'use strict';

    console.log('🔘 Auto OK Button Clicker - Module loaded');

    /**
     * Finds and clicks OK/Yes button in the modal
     * @returns {boolean} True if button was found and clicked
     */
    function clickOkButton() {
        // Look for OK/Yes button in various common selectors
        const selectors = [
            '.modal-box-button',
            '.modal-box-button.btn-primary',
            '.page-foot button.btn-primary',
            '.modal-box button.btn-primary',
            'button.btn.btn-primary',
            'button[type="button"].btn-primary',
            '.modal-box button',
            '.page-foot button'
        ];

        // Acceptable button text values (case-insensitive)
        const acceptableTexts = ['ok', 'okay', 'yes', 'confirm', 'accept', 'continue'];

        // Try each selector
        for (const selector of selectors) {
            const buttons = document.querySelectorAll(selector);

            for (const button of buttons) {
                const buttonText = button.textContent.trim().toLowerCase();

                // Check if this is an acceptable confirmation button
                if (acceptableTexts.includes(buttonText)) {
                    console.log(`✅ Found confirmation button (${buttonText}):`, button);
                    console.log('🖱️ Clicking button automatically...');

                    // Click the button
                    button.click();

                    return true;
                }
            }
        }

        console.log('⚠️ No OK/Yes button found');
        return false;
    }

    /**
     * Check if a modal is currently visible
     * @returns {boolean} True if modal is visible
     */
    function isModalVisible() {
        return document.body.classList.contains('modal-show') ||
               document.querySelector('.modal-box') !== null;
    }

    // Export functions for use in router
    window.autoOkButton = {
        clickOkButton: clickOkButton,
        isModalVisible: isModalVisible
    };

    console.log('📦 Auto OK Button utility ready');
})();