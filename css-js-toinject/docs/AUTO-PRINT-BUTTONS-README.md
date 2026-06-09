# Auto Print Buttons Feature

## Overview

This feature automatically combines and triggers printing actions when you click the "Create Shipment" button on the `#outbound/packing` page.

## What It Does

1. **Monitors Create Shipment Button**: Watches for clicks on the "Create Shipment" button
2. **Adds a Combined Button**: Creates a new "🖨️ Print All (Slip + Label)" button in the shipment modal
3. **Auto-Clicks After Create Shipment**: Automatically triggers the print buttons ONLY when "Create Shipment" was clicked
4. **Combines Actions**: Clicks both "Packing Slip" and "Print Carton Label" buttons in sequence

## Files Involved

- `auto-print-buttons.js` - Main script that handles button creation and auto-clicking
- `router.js` - Updated to note that auto-print feature is active on the packing route

## How It Works

### Create Shipment Button Detection

The script looks for this button on the page:
- **Create Shipment**: `#create-shipment` button

When you click "Create Shipment":
1. Script sets a flag (`window._shouldAutoClickPrintAll = true`)
2. Waits for the shipment modal to appear

### Button Discovery

The script looks for these existing buttons in the shipment modal:
- **Packing Slip**: `#btnPrintPackSlip` (data-value="pslip")
- **Print Carton Label**: `#box-label` (data-value="pboxlabel")

### Modal Detection

The script uses a MutationObserver to watch for:
- Modal container: `#_modal_block_ui` with class `loader_block_ui`
- Modal content: `#shipment-created`

When the modal appears, the script:
1. Adds the combined "Print All" button
2. Checks if "Create Shipment" was clicked
3. If yes, waits 500ms (configurable delay) and auto-clicks
4. If no, only adds the button without auto-clicking

### Button Click Sequence

When the "Print All" button is clicked (manually or automatically):
1. Clicks "Print Carton Label" button **first** (opens print window)
2. Waits for carton label window to open (detects via window.open interception)
3. Waits additional 2000ms (configurable) for window to fully render
4. Clicks "Packing Slip" button

**Why this order?**
The carton label uses an async API call (`tf.service.get()`) and takes longer to render. By printing it first and waiting for completion, we prevent the windows from overlapping and ensure both print correctly.

## Configuration

### Enable/Disable Auto-Click

```javascript
// In browser console
window.autoPrintButtons.setAutoClick(false);  // Disable
window.autoPrintButtons.setAutoClick(true);   // Enable
```

### Change Auto-Click Delay

```javascript
// Wait 1 second before auto-clicking the Print All button
window.autoPrintButtons.setDelay(1000);
```

### Change Delay Between Prints

```javascript
// Wait 3 seconds between Carton Label and Packing Slip
window.autoPrintButtons.setBetweenPrintsDelay(3000);

// Reduce to 1 second if prints are fast
window.autoPrintButtons.setBetweenPrintsDelay(1000);
```

### Manual Testing

```javascript
// Manually trigger print all
window.autoPrintButtons.printAll();

// Click individual buttons
window.autoPrintButtons.clickPackingSlip();
window.autoPrintButtons.clickCartonLabel();

// Add button to existing modal
window.autoPrintButtons.addButton();
```

## Installation

The script runs automatically when injected into the page. No manual setup required.

Make sure both scripts are loaded in your browser extension's content scripts:
1. `router.js` (must load first)
2. `auto-print-buttons.js` (loads globally)

## Debugging

### Check if Script is Loaded

```javascript
// Should return an object with methods
window.autoPrintButtons
```

### View Configuration

```javascript
// Show current config
window.autoPrintButtons.config
```

### Enable Debug Mode

```javascript
// Shows detailed logging
window.autoPrintButtons.config.debugMode = true;
```

### Console Logs to Watch For

- `🖨️ Auto Print Buttons - Loading...` - Script started
- `✅ Auto Print Buttons initialized` - Ready to use
- `✅ Modal observer set up` - Watching for modal
- `🎯 Shipment created modal detected!` - Modal found
- `✅ Print All button added to modal` - Button inserted
- `🤖 Auto-clicking Print All button...` - Auto-click triggered
- `🖨️🖨️ Print All - Starting...` - Combined action started
- `✅ Print All completed successfully` - All actions done

## Troubleshooting

### Button Not Auto-Clicking

1. Check if auto-click is enabled:
   ```javascript
   window.autoPrintButtons.config.autoClickEnabled
   ```

2. Check if button was added:
   ```javascript
   document.getElementById('btn-print-all-combined')
   ```

3. Manually trigger:
   ```javascript
   window.autoPrintButtons.handleModal();
   ```

### Buttons Not Found

If you see warnings like "⚠️ Packing Slip button not found":

1. Check if modal is visible:
   ```javascript
   document.getElementById('shipment-created')
   ```

2. Check if target buttons exist:
   ```javascript
   document.getElementById('btnPrintPackSlip')    // Packing Slip
   document.getElementById('box-label')            // Carton Label
   ```

3. The button IDs may have changed. Update the script with new IDs.

### Modal Not Detected

1. Check if observer is running:
   ```javascript
   window.autoPrintButtons
   ```

2. Manually trigger detection:
   ```javascript
   window.autoPrintButtons.handleModal();
   ```

## Customization

### Change Button Style

Edit the `addPrintAllButton()` function in `auto-print-buttons.js`:

```javascript
printAllBtn.style.cssText = `
    background-color: #28a745 !important;  // Green background
    color: white !important;
    font-weight: bold !important;
    // ... add your styles
`;
```

### Change Button Text

```javascript
printAllBtn.textContent = '🖨️ Your Custom Text';
```

### Add More Buttons to Sequence

Edit the `printAll()` function:

```javascript
function printAll() {
    clickPackingSlipButton();

    setTimeout(() => {
        clickCartonLabelButton();
    }, 300);

    setTimeout(() => {
        // Add your custom button click here
        document.getElementById('your-button-id').click();
    }, 600);
}
```

## Future Enhancements

Possible improvements:
1. Add settings panel for user configuration
2. Store preferences in localStorage
3. Add visual feedback during auto-click
4. Support for additional button combinations
5. Configurable button order
6. Retry logic for failed clicks

## Technical Details

### Browser Compatibility

- Chrome/Edge: ✅ Full support
- Firefox: ✅ Full support
- Safari: ⚠️ MutationObserver support required

### Performance

- Minimal overhead: MutationObserver only watches for modal changes
- No polling: Event-driven approach
- Cleanup: Observer runs continuously but is very lightweight

### Security

- No external requests
- No data collection
- Only interacts with DOM elements
- Does not modify website code

## Support

For issues or questions:
1. Check browser console for error messages
2. Verify script is loaded: `window.autoPrintButtons`
3. Test manual trigger: `window.autoPrintButtons.printAll()`
4. Check configuration: `window.autoPrintButtons.config`
