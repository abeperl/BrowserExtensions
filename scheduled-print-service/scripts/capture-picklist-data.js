/**
 * Capture Picklist Data from window.tf.page.data
 * Stores it in window.__picklistResponse for Production Status column to use
 */
(function() {
    console.log('📦 Picklist data capture script loaded');

    const waitForData = setInterval(function() {
        if (window.tf && window.tf.page && window.tf.page.data) {
            const picklistData = window.tf.page.data;

            // Store in window for Production Status column
            window.__picklistResponse = {
                responseCode: 0,
                responseType: 'Success',
                data: picklistData
            };

            sessionStorage.setItem('__picklistResponse', JSON.stringify(window.__picklistResponse));
            console.log('✅ Picklist response captured from window.tf.page.data');
            console.log('   - PickListItems count:', picklistData.PickListItems?.length || 0);

            clearInterval(waitForData);
        }
    }, 100);

    // Timeout after 10 seconds
    setTimeout(function() {
        clearInterval(waitForData);
        if (!window.__picklistResponse) {
            console.warn('⚠️ Failed to capture picklist data within 10 seconds');
        }
    }, 10000);
})();