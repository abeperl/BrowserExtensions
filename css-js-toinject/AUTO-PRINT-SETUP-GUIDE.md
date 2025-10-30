# Auto Print Setup Guide

This guide explains how to configure your browser for optimal auto-printing experience with the "Print All" button feature.

## Browser Configuration for Auto-Print

### Chrome/Edge - Skip Print Preview

To enable faster printing without the print preview dialog:

1. Open Chrome/Edge Settings
   - Chrome: `chrome://settings/printing`
   - Edge: `edge://settings/printing`

2. **Disable** "Use system print dialog"
3. **Set default printer** in your OS settings

### Chrome Kiosk Mode (Advanced)

For a true "silent print" experience, you can run Chrome in kiosk mode:

```bash
# Windows
chrome.exe --kiosk-printing --app=https://mj.3plnext.com

# Mac
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --kiosk-printing --app=https://mj.3plnext.com

# Linux
google-chrome --kiosk-printing --app=https://mj.3plnext.com
```

**Kiosk mode features:**
- Automatically prints to default printer
- No print preview dialog
- Uses default print settings
- Best for dedicated workstation/kiosk setups

## How the Auto Print Feature Works

1. When a shipment is successfully created, the success modal appears
2. The "Print All" button is automatically added to the modal
3. After 500ms delay, the button is auto-clicked
4. **Packing Slip** opens in first tab (or reuses existing tab)
5. **Carton Label** opens in second tab (or reuses existing tab)
6. Both tabs trigger print dialogs

## Tab Reuse Behavior

The system intelligently reuses tabs:
- Each print type gets its own persistent tab
- Subsequent prints update the same tabs instead of creating new ones
- Reduces tab clutter
- Faster than creating new tabs every time

## Manual Control

If you need to disable auto-clicking temporarily:

```javascript
// In browser console
window.autoPrintButtons.setAutoClick(false);  // Disable
window.autoPrintButtons.setAutoClick(true);   // Enable

// Manually trigger print all
window.autoPrintButtons.printAll();

// Adjust auto-click delay
window.autoPrintButtons.setDelay(1000);  // Wait 1 second before auto-click
```

## Keyboard Shortcuts

When print dialog appears:
- **Enter** - Print with current settings
- **Ctrl+P** - Open print settings (if not already open)
- **Esc** - Cancel print

## Troubleshooting

### Problem: Auto-click triggers on wrong modal

**Solution**: This is fixed in the current version. The system checks:
1. Modal container is visible
2. **"Create Shipment" modal is NOT visible** (wrong modal)
3. **"Success" modal IS visible** (correct modal)
4. Print buttons exist and become visible

### Problem: Tabs don't open

**Solution**: Check browser console for debugging logs. Common issues:
- Buttons not visible yet (system waits up to 2 seconds)
- Modal not fully loaded (increase auto-click delay)

### Problem: Print dialogs are slow

**Solutions**:
1. Use browser setting to skip print preview
2. Use Chrome kiosk mode for silent printing
3. Ensure default printer is configured in OS

## Best Practices

1. **Set default printer** in your OS settings
2. **Configure print preferences** (paper size, orientation) as defaults
3. **Use dedicated printer** for shipping labels if possible
4. **Keep print tabs open** - reusing tabs is faster than creating new ones
5. **Use keyboard shortcuts** - Press Enter to quickly accept print dialog

## Security Note

Browsers prevent true "silent printing" without user interaction for security reasons. The closest you can get is:
- Kiosk mode (requires Chrome launch flag)
- Browser setting to skip preview (still shows dialog)
- Browser extension (would need additional development)

## Future Enhancements

Potential improvements:
- Browser extension to auto-click "Print" button
- Integration with printer API for direct printing
- Print queue management
- Batch printing from multiple orders
