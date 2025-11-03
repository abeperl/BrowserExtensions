# Sequential Printing Implementation

## Problem

When clicking both "Packing Slip" and "Print Carton Label" buttons simultaneously, the print windows overlap because:

1. Both buttons call `window.open()` at nearly the same time
2. The Carton Label button makes an async API call (`tf.service.get()`) that takes time to complete
3. The Packing Slip opens immediately
4. Result: Windows overlap, one may be hidden, print dialog confusion

## Solution

Print **Carton Label first**, wait for it to complete, then print **Packing Slip**.

### Why Carton Label First?

Looking at the actual implementation:

```javascript
PrintBoxLabelByPackingSlip: function(id, masterData) {
    // Step 1: API call (async)
    tf.service.get("OutbondShipment/GetPackingSlipDetailByShipmentId?ShipmentId=" + id, {},
        function(data) {
            // Step 2: Process data
            var boxesArray = common.groupArrayBy(data.data.ShipmentDetails, "BoxNo");

            // Step 3: Build HTML
            labellHtml += "<div class='box-label-wrp'>...</div>";

            // Step 4: Generate barcodes with JsBarcode
            JsBarcode("#barcode", codeVal, {...});

            // Step 5: Open print window
            var mywindow = window.open("", "", "height=700,width=1100");
            mywindow.document.write(labellHtml);

            // Step 6: Print after 1 second delay
            setTimeout(function() {
                mywindow.print();
            }, 1000);
        }
    );
}
```

**Timing breakdown:**
- API call: ~200-500ms
- Data processing: ~50-100ms
- Barcode generation: ~100-200ms
- Window rendering: ~500-1000ms
- **Total: ~1.5-2+ seconds**

The Packing Slip, by contrast, is much simpler and opens almost instantly.

## Implementation

### 1. Intercept window.open for Carton Label

```javascript
function clickCartonLabelButton() {
    return new Promise((resolve) => {
        const originalWindowOpen = window.open;
        let intercepted = false;

        window.open = function(...args) {
            if (!intercepted) {
                intercepted = true;

                // Restore original
                window.open = originalWindowOpen;

                // Open the window
                const result = originalWindowOpen.apply(this, args);

                // Wait 1.5s for window to render, then resolve
                setTimeout(() => resolve(result), 1500);

                return result;
            }
            return originalWindowOpen.apply(this, args);
        };

        // Click the button
        cartonLabelBtn.click();

        // Fallback timeout
        setTimeout(() => {
            if (!intercepted) {
                window.open = originalWindowOpen;
                resolve(null);
            }
        }, 3000);
    });
}
```

### 2. Sequential Print with async/await

```javascript
async function printAllButtons() {
    // Step 1: Print Carton Label
    await clickCartonLabelButton();
    console.log('✅ Carton Label opened');

    // Step 2: Wait additional time for full rendering
    await new Promise(resolve =>
        setTimeout(resolve, AUTO_PRINT_CONFIG.betweenPrintsDelay)
    );

    // Step 3: Print Packing Slip
    clickPackingSlipButton();
    console.log('✅ Packing Slip opened');
}
```

## Timeline Comparison

### ❌ Old Approach (Simultaneous)

```
Time 0ms:   Click Carton Label button
            Click Packing Slip button
Time 50ms:  Packing Slip window opens (fast!)
Time 200ms: API call completes
Time 500ms: Barcode rendering
Time 1500ms: Carton Label window finally opens (but hidden behind Packing Slip)
```

Result: **Overlapping windows, confusion, missed prints**

### ✅ New Approach (Sequential)

```
Time 0ms:    Click Carton Label button
Time 200ms:  API call completes
Time 500ms:  Barcode rendering
Time 1500ms: Carton Label window opens
             ↓ window.open intercepted, promise resolves
Time 3500ms: Additional 2s delay (configurable)
             Click Packing Slip button
Time 3550ms: Packing Slip window opens (no overlap!)
```

Result: **Clean sequential windows, no overlap, both print successfully**

## Configuration

Users can adjust the delay between prints:

```javascript
// Fast printer/network
window.autoPrintButtons.setBetweenPrintsDelay(1000); // 1 second

// Default
window.autoPrintButtons.setBetweenPrintsDelay(2000); // 2 seconds

// Slow printer/network
window.autoPrintButtons.setBetweenPrintsDelay(3000); // 3 seconds
```

## Technical Details

### Promise-Based Button Click

The `clickCartonLabelButton()` function returns a Promise that:
1. Temporarily intercepts `window.open`
2. Clicks the button
3. Waits for `window.open` to be called
4. Restores original `window.open`
5. Resolves after 1.5 second delay
6. Has 3 second timeout fallback

### Async/Await Flow

```javascript
async function printAllButtons() {
    try {
        // Wait for carton label
        await clickCartonLabelButton();

        // Wait for rendering
        await new Promise(resolve =>
            setTimeout(resolve, betweenPrintsDelay)
        );

        // Print packing slip
        clickPackingSlipButton();

        return true;
    } catch (error) {
        console.error('Print failed:', error);
        return false;
    }
}
```

## Benefits

1. **No Overlap**: Windows open sequentially, never on top of each other
2. **Reliable**: Waits for async operations to complete
3. **Configurable**: Users can adjust delays based on their network/printer speed
4. **Graceful Fallback**: Timeout ensures script doesn't hang if something fails
5. **Better UX**: Users see each print window clearly, can review before printing

## Edge Cases Handled

### Carton Label Fails to Open

```javascript
setTimeout(() => {
    if (!intercepted) {
        window.open = originalWindowOpen;
        resolve(null); // Resolve anyway to continue
    }
}, 3000);
```

After 3 seconds, if `window.open` wasn't called, we give up and continue to packing slip.

### API Call Fails

The promise still resolves (doesn't reject), so packing slip will still print even if carton label fails.

### User Closes Window Early

No impact - the script just waits the configured time and continues to packing slip.

## Future Enhancements

Possible improvements:
1. Detect when print dialog is closed (more accurate timing)
2. Visual progress indicator showing which print is active
3. Retry logic if window fails to open
4. User preference storage (remember delay settings)
5. Print queue with pause/resume controls

## Console Output Example

```
🖨️🖨️ Print All - Starting...
📦 Step 1: Printing Carton Label first...
🖨️ Clicking Carton Label button
✅ Carton Label print window opened
✅ Carton Label print window opened
📄 Step 2: Waiting 2000ms before printing Packing Slip...
📄 Step 2: Printing Packing Slip...
🖨️ Clicking Packing Slip button
✅ Print All completed successfully
```

## Conclusion

By understanding the async nature of the Carton Label printing (API call → processing → barcode generation → rendering), we can sequence the prints intelligently to avoid overlap and ensure both documents print successfully.

The key insight: **Print the slow one first, wait for it to complete, then print the fast one.**
