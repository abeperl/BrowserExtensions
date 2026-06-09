# Box Label Format Injector - Debugging Guide

## How to Test

1. Navigate to a page with the box label print functionality
2. Open browser console (F12)
3. Trigger a box label print
4. Watch the console for injection messages

## Expected Console Output

### Successful Injection

```
🪟 window.open intercepted: ["", "", "height=700,width=1100"]
✅ New window opened, preparing early CSS injection
👀 MutationObserver installed on print window
🔍 Checking window: {readyState: "loading", hasBoxLabel: false, bodyChildren: 0}
👀 MutationObserver detected .box-label-wrp added to DOM
✅ BOX LABEL FORMAT CSS INJECTED! {boxLabelCount: 3, readyState: "complete", attemptNumber: "MutationObserver"}
```

### If Not Working

You might see:
```
🪟 window.open intercepted: ["", "", "height=700,width=1100"]
✅ New window opened, preparing early CSS injection
🔍 Checking window: {readyState: "loading", hasBoxLabel: false, bodyChildren: 0}
```

But NO "✅ BOX LABEL FORMAT CSS INJECTED!" message.

## Debugging Steps

### Step 1: Check if Interceptor is Loaded

```javascript
// Check in main window console
console.log(window.open.toString().includes('Box Label'));
// Should return: true
```

### Step 2: Manual Injection Test

Open a box label print window, then in the **main window** console:

```javascript
// Get reference to print window (while it's still open)
let printWindow = window.open('', 'test', 'width=800,height=600');

// Check if box-label-wrp exists
printWindow.document.querySelector('.box-label-wrp')

// Manually inject CSS
let style = printWindow.document.createElement('style');
style.id = 'box-label-format-override';
style.textContent = `
    .top-info-section .text:first-child { display: none !important; }
    .ship-info-section > .text.sm-text:first-child { display: none !important; }
    .ship-info-section > .text:nth-child(2) { display: none !important; }
`;
printWindow.document.head.appendChild(style);
```

### Step 3: Check Timing

The print window flow:
1. `window.open()` - creates blank window (0ms)
2. `document.write()` - writes HTML content (immediate)
3. `document.close()` - closes writing (immediate)
4. `setTimeout(() => window.print(), 1000)` - prints at 1000ms

Our injector attempts at: 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 800, 850, 900, 950ms

Plus:
- MutationObserver (watches for DOM changes)
- DOMContentLoaded event
- readystatechange event

### Step 4: Check for Cross-Origin Issues

If the print window is cross-origin, injection won't work:

```javascript
// In main window console
try {
    printWindow.document.querySelector('.box-label-wrp');
    console.log('✅ Same origin - injection possible');
} catch (e) {
    console.log('❌ Cross-origin - injection blocked:', e.message);
}
```

## Common Issues

### Issue 1: CSS Not Applied

**Symptoms**: Console shows "✅ BOX LABEL FORMAT CSS INJECTED!" but styles not visible

**Solution**: Check if another stylesheet is overriding with higher specificity

```javascript
// Check if style exists
printWindow.document.getElementById('box-label-format-override')

// Check computed styles
let element = printWindow.document.querySelector('.top-info-section .text:first-child');
printWindow.getComputedStyle(element).display  // Should be "none"
```

### Issue 2: Injection Too Late

**Symptoms**: Print dialog opens before CSS is injected

**Solution**: The print() call has a gate - it should wait for CSS. Check console for print override message.

### Issue 3: MutationObserver Not Firing

**Symptoms**: No "👀 MutationObserver detected" message

**Check**:
```javascript
// Document body exists?
printWindow.document.body !== null

// Box label added after observer?
// (If box label added before observer starts, polling should catch it)
```

## Force Injection (Emergency Debug)

If all else fails, add this to the application code itself (NOT recommended for production):

```javascript
// After document.close(), before setTimeout
if (window.opener && window.opener._injectBoxLabelCSS) {
    window.opener._injectBoxLabelCSS(mywindow);
}
```

## Verification

### Visual Check

After printing, the box label should:
- ✅ Hide "Box No : X" line
- ✅ Hide "Customer ID: XXX" line
- ✅ Hide "To:" label
- ✅ Show larger phone number

### Code Check

```javascript
// Check if CSS rule is present
let styleElement = printWindow.document.getElementById('box-label-format-override');
styleElement.textContent.includes('display: none')  // Should be true
```

## Performance Notes

- **19 polling attempts** from 50ms to 950ms
- **MutationObserver** catches immediate DOM changes
- **Event listeners** for DOMContentLoaded and readystatechange
- **Print override** gates printing until CSS is present

Total: ~20-25 attempts to catch the right moment for injection

## Success Metrics

Look for this exact message:
```
✅ BOX LABEL FORMAT CSS INJECTED! {boxLabelCount: N, readyState: "complete", attemptNumber: X}
```

Where:
- `boxLabelCount` = number of labels found (should be > 0)
- `readyState` = document state (ideally "complete")
- `attemptNumber` = which method succeeded (number, "MutationObserver", "DOMContentLoaded", etc.)
