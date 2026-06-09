# Auto Print Buttons - Quick Start Guide

## 🚀 What Does This Do?

Automatically prints both **Packing Slip** and **Carton Label** when you click the **"Create Shipment"** button on the `#outbound/packing` page.

## ✅ Installation

The script runs automatically when loaded. No setup required!

## 📋 How to Use

### Normal Operation (Default)

1. Complete packing as usual
2. Click the **"Create Shipment"** button
3. **Shipment Created** modal appears
4. Script automatically clicks in this order:
   - ✅ Print Carton Label **first** (opens in new window)
   - ⏱️ Waits ~2 seconds for it to fully render
   - ✅ Packing Slip **second** (opens in new window)
5. Done! Both documents ready to print

**Note:** Carton Label prints first because it uses an async API call and takes longer to load. This prevents the windows from overlapping.

**Important:** Auto-click only happens when you use the "Create Shipment" button. If the modal appears any other way, the "Print All" button will be there but won't auto-click.

### Manual Control

If you prefer to click manually:

```javascript
// Open browser console (F12)
window.autoPrintButtons.setAutoClick(false);
```

Now the "🖨️ Print All (Slip + Label)" button appears but doesn't auto-click.

## 🔧 Configuration

### In Browser Console (F12)

```javascript
// Disable auto-clicking
window.autoPrintButtons.setAutoClick(false);

// Enable auto-clicking
window.autoPrintButtons.setAutoClick(true);

// Change delay (milliseconds)
window.autoPrintButtons.setDelay(1000); // 1 second

// Manual trigger anytime
window.autoPrintButtons.printAll();
```

## 🎯 The New Button

Look for this button in the Shipment Created modal:

```
🖨️ Print All (Slip + Label)
```

- **Green background** = Combined action button
- Appears as the **first button** in the modal
- Auto-clicks after 500ms (by default)

## 🐛 Troubleshooting

### Button Doesn't Auto-Click

**Check if auto-click is enabled:**
```javascript
window.autoPrintButtons.config.autoClickEnabled
// Should return: true
```

**Re-enable if needed:**
```javascript
window.autoPrintButtons.setAutoClick(true);
```

### Button Not Appearing

**Check if script loaded:**
```javascript
window.autoPrintButtons
// Should return: {config: {...}, printAll: ƒ, ...}
```

**Manually add button:**
```javascript
window.autoPrintButtons.addButton();
```

### Wrong Documents Printing

**Check which buttons exist:**
```javascript
console.log('Packing Slip:', document.getElementById('btnPrintPackSlip'));
console.log('Carton Label:', document.getElementById('box-label'));
```

If `null`, the button IDs may have changed. Report this issue.

## 💡 Pro Tips

### Adjust Delays

Give yourself more time to review the modal before auto-click:
```javascript
window.autoPrintButtons.setDelay(2000); // 2 seconds
```

Adjust time between Carton Label and Packing Slip prints:
```javascript
// Increase if prints are slow/overlapping
window.autoPrintButtons.setBetweenPrintsDelay(3000); // 3 seconds

// Decrease if prints are fast
window.autoPrintButtons.setBetweenPrintsDelay(1000); // 1 second
```

### Disable for One Session

Disable without refreshing:
```javascript
window.autoPrintButtons.setAutoClick(false);
```

Enable again:
```javascript
window.autoPrintButtons.setAutoClick(true);
```

### Manual Trigger

Click the combined action anytime:
```javascript
window.autoPrintButtons.printAll();
```

### Debug Mode

See detailed logs:
```javascript
window.autoPrintButtons.config.debugMode = true;
```

## 📊 What You'll See in Console

When working correctly:
```
🖨️ Auto Print Buttons - Loading...
✅ Auto Print Buttons initialized
✅ Modal observer set up
🎯 Shipment created modal detected!
✅ Print All button added to modal
🤖 Auto-clicking Print All button...
🖨️🖨️ Print All - Starting...
✅ Print All completed successfully
```

## 🛑 How to Disable Completely

If you want to disable the feature entirely:

1. Open browser console (F12)
2. Run:
```javascript
window.autoPrintButtons.setAutoClick(false);
```

The button will still appear but won't auto-click.

## ❓ FAQ

### Q: Can I change the order of buttons?
**A:** Yes! Edit the `printAll()` function in `auto-print-buttons.js`

### Q: Can I add more buttons to the sequence?
**A:** Yes! Edit the `printAll()` function and add more `setTimeout()` calls

### Q: Does this work on other pages?
**A:** No, it's specifically for `#outbound/packing` shipment modals

### Q: Will my settings persist after refresh?
**A:** No, settings reset on page refresh. Future enhancement planned.

### Q: Can I change the button text?
**A:** Yes! Edit `addPrintAllButton()` function in `auto-print-buttons.js`

## 📚 More Documentation

- **Full Documentation**: `AUTO-PRINT-BUTTONS-README.md`
- **Technical Reference**: `BUTTON-FUNCTIONS-REFERENCE.md`
- **Implementation Details**: `IMPLEMENTATION-SUMMARY.md`
- **Project Overview**: `../CLAUDE.md`

## 🆘 Getting Help

1. Check browser console for errors
2. Verify script loaded: `window.autoPrintButtons`
3. Check configuration: `window.autoPrintButtons.config`
4. Try manual trigger: `window.autoPrintButtons.printAll()`
5. Review logs in console

## 🎉 That's It!

The script should work automatically. If you need to adjust anything, use the commands above in your browser console.

**Most Common Command:**
```javascript
window.autoPrintButtons.setAutoClick(false);  // Disable auto-click
```

---

**Next Steps:**
- Test on `#outbound/packing` page
- Complete a packing order
- Watch for the Shipment Created modal
- Verify both prints open automatically

Enjoy! 🖨️✨
