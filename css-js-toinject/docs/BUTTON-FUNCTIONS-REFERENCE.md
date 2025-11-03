# Button Functions Reference

## How Buttons Are Triggered

### Original Modal HTML

```html
<div id="shipment-created" class="panel-box modal-box shipment-created-modal">
    <div class="page-foot">
        <div class="modal-box-controls">
            <div class="inner-flex">
                <!-- Original Buttons -->
                <button id="btnPrintPackSlip" data-value="pslip">Packing Slip</button>
                <button id="box-label" data-value="pboxlabel">Print Carton Label</button>

                <!-- Other buttons -->
                <button id="btnPrintLabel" data-value="plabel">Print Label (0)</button>
                <button data-value="pinvoice">View Invoice</button>
                <button id="send-email" data-value="SendEmail">Send Email</button>
            </div>
        </div>
    </div>
</div>
```

## JavaScript Button Triggering

### Method 1: Direct Element Click (Recommended)

This is what our script uses:

```javascript
// Packing Slip
const packingSlipBtn = document.getElementById('btnPrintPackSlip');
if (packingSlipBtn) {
    packingSlipBtn.click();  // ✅ Triggers click event
}

// Carton Label
const cartonLabelBtn = document.getElementById('box-label');
if (cartonLabelBtn) {
    cartonLabelBtn.click();  // ✅ Triggers click event
}
```

### Method 2: Simulate Mouse Event (Alternative)

```javascript
// Create and dispatch a click event
const clickEvent = new MouseEvent('click', {
    view: window,
    bubbles: true,
    cancelable: true
});

const button = document.getElementById('btnPrintPackSlip');
button.dispatchEvent(clickEvent);
```

### Method 3: jQuery (If Available on Page)

```javascript
// If the website uses jQuery
$('#btnPrintPackSlip').trigger('click');
$('#box-label').trigger('click');
```

## Button Detection Strategy

### 1. Wait for Modal to Appear

```javascript
const observer = new MutationObserver((mutations) => {
    const modal = document.getElementById('shipment-created');
    if (modal) {
        // Modal detected, proceed with button clicks
    }
});

observer.observe(document.body, {
    childList: true,
    subtree: true
});
```

### 2. Check Modal Visibility

```javascript
const modalBlockUI = document.getElementById('_modal_block_ui');
const isVisible = modalBlockUI &&
                  modalBlockUI.classList.contains('loader_block_ui') &&
                  modalBlockUI.style.display !== 'none';
```

### 3. Find Buttons

```javascript
// Try multiple selectors
const packingSlipBtn =
    document.getElementById('btnPrintPackSlip') ||
    document.querySelector('button[data-value="pslip"]') ||
    document.querySelector('button:contains("Packing Slip")');
```

## Event Handlers on Original Website

The website likely has event listeners attached to these buttons. Common patterns:

### Pattern 1: Direct Event Listener

```javascript
// Website's code (hypothetical)
document.getElementById('btnPrintPackSlip').addEventListener('click', function() {
    // Open packing slip window
    window.open('/print/packingslip?id=123', '_blank');
});
```

### Pattern 2: Event Delegation

```javascript
// Website's code (hypothetical)
document.querySelector('.modal-box-controls').addEventListener('click', function(e) {
    const target = e.target;
    if (target.getAttribute('data-value') === 'pslip') {
        // Handle packing slip
    }
});
```

### Pattern 3: jQuery Event Handler

```javascript
// Website's code (hypothetical)
$('#btnPrintPackSlip').on('click', function() {
    // Handle click
});
```

## Our Implementation

### Combined Button Click Handler

```javascript
// In auto-print-buttons.js
function printAll() {
    console.log('🖨️🖨️ Print All - Starting...');

    // Step 1: Click Packing Slip
    const packingSlipBtn = document.getElementById('btnPrintPackSlip');
    if (packingSlipBtn) {
        packingSlipBtn.click();
        console.log('✅ Clicked Packing Slip');
    }

    // Step 2: Wait 300ms, then click Carton Label
    setTimeout(() => {
        const cartonLabelBtn = document.getElementById('box-label');
        if (cartonLabelBtn) {
            cartonLabelBtn.click();
            console.log('✅ Clicked Carton Label');
        }
    }, 300);
}
```

### Why the Delay?

The 300ms delay between clicks prevents:
1. Race conditions
2. Browser blocking multiple window.open() calls
3. Modal state conflicts

## Testing the Buttons

### Manual Test in Browser Console

```javascript
// Test individual buttons
document.getElementById('btnPrintPackSlip').click();
document.getElementById('box-label').click();

// Test combined action
window.autoPrintButtons.printAll();
```

### Verify Button Elements

```javascript
// Check if buttons exist
console.log('Packing Slip:', document.getElementById('btnPrintPackSlip'));
console.log('Carton Label:', document.getElementById('box-label'));

// Check button properties
const btn = document.getElementById('btnPrintPackSlip');
console.log('ID:', btn.id);
console.log('Data-value:', btn.getAttribute('data-value'));
console.log('Text:', btn.textContent);
console.log('Visible:', btn.offsetParent !== null);
```

### Check Event Listeners

```javascript
// Get all event listeners (Chrome DevTools)
getEventListeners(document.getElementById('btnPrintPackSlip'));
```

## Common Issues and Solutions

### Issue: Button Click Doesn't Work

**Possible Causes:**
1. Button not fully loaded
2. Modal not visible
3. JavaScript security restrictions
4. Event listener not attached yet

**Solutions:**
```javascript
// Add delay after modal appears
setTimeout(() => {
    document.getElementById('btnPrintPackSlip').click();
}, 500);

// Check if button is clickable
const btn = document.getElementById('btnPrintPackSlip');
if (btn && btn.offsetParent !== null && !btn.disabled) {
    btn.click();
}
```

### Issue: Multiple Windows Open

**Solution:** Add delays between clicks
```javascript
setTimeout(() => btn1.click(), 0);
setTimeout(() => btn2.click(), 300);  // ✅ Delay prevents blocking
```

### Issue: Modal Closes Immediately

**Solution:** Check if button has `data-value="close"` or closes modal
```javascript
// Don't click close button
if (btn.getAttribute('data-value') !== 'close') {
    btn.click();
}
```

## Advanced: Intercepting Print Actions

If you need to modify print behavior:

```javascript
// Intercept window.open
const originalOpen = window.open;
window.open = function(url, target, features) {
    console.log('Opening:', url);

    // Modify URL or prevent opening
    if (url.includes('/print/packingslip')) {
        // Custom logic here
    }

    return originalOpen.apply(this, arguments);
};
```

## Summary

**Our Script Uses:**
- ✅ Direct `element.click()` method
- ✅ Element ID selectors
- ✅ MutationObserver for modal detection
- ✅ Delays between clicks
- ✅ Visibility checks before clicking

**Why This Works:**
- Native browser event triggering
- Respects website's event handlers
- No website code modification required
- Compatible with jQuery or vanilla JS
