# Table Item Linker - Quantity Click Update

## Summary

Updated `makeQtyItemsClickable()` in `table-item-linker.js` to trigger the existing `.qty-wrp` click handler instead of manually manipulating the DOM. This ensures all business logic (personalized items, disabled state, etc.) is properly executed.

**Updated**: 2025-01-03

---

## Problem

### Before (Manual DOM Manipulation)

The previous code manually:
- Found qty input and display span
- Showed/hid elements directly
- Set the value
- Did NOT execute the existing click handler logic

**Missing Logic:**
- ❌ Personalized item checks (`IsPersonalized`, `IsPersonalizedStatus`)
- ❌ Disabled state validation
- ❌ Qty editable setting check (`packingQtyEditable`)
- ❌ Global input hiding (hiding other qty inputs)

### Existing Click Handler (jQuery)

The application has a jQuery click handler on `.qty-wrp` that:

```javascript
$(document).on("click", ".qty-wrp", function () {
    var IsPersonalized = $(this).parents("tr").find(".personalized-td").data("id");
    var IsPersonalizedStatus = $(this).parents("tr").find(".personalized-td").data("status");
    var disabled = $(this).hasClass("disabled");

    if (tf.settings().packingQtyEditable != false && !disabled) {
        if (IsPersonalized == 0 || (IsPersonalized == 1 && IsPersonalizedStatus == 2)) {
            $(".qty-mn").addClass("hide");              // Hide ALL qty inputs
            $(".item_added").removeClass("hide");       // Show ALL qty displays

            var qty = $(this).children(".item_added").text();
            $(this).children(".qty-mn").removeClass("hide");    // Show THIS input
            $(this).children(".item_added").addClass("hide");   // Hide THIS display
            $(this).children(".qty-mn").val(qty).focus();       // Set value and focus
        }
    }
});
```

**This logic was being bypassed!**

---

## Solution

### New Approach: Trigger Click Event

Instead of manually manipulating the DOM, **trigger the existing click handler** and then set the value.

**Workflow:**
1. Find `.qty-wrp` element
2. **Trigger click** → Executes existing jQuery handler
3. Wait 100ms for handler to complete
4. Set the new quantity value
5. Trigger input/change events
6. Blur after 300ms

---

## Code Changes

### File Modified
- **`css-js-toinject/table-item-linker.js`**
- Lines 143-192 in `makeQtyItemsClickable()` function

### Before (Lines 145-190)

```javascript
// Find the qty column in this row
const qtyColumn = targetRow.querySelector('td.qty-column');

if (!qtyColumn) {
    console.error('Could not find qty column');
    return;
}

// Find the qty input field
const qtyInput = qtyColumn.querySelector('input.qty-mn');
const qtyDisplaySpan = qtyColumn.querySelector('span.item_added');

if (!qtyInput || !qtyDisplaySpan) {
    console.error('Could not find qty input or display span');
    return;
}

// Show the input, hide the display span
qtyInput.classList.remove('hide');
qtyDisplaySpan.style.display = 'none';

// Set the value and focus
qtyInput.value = qtyText;
qtyInput.focus();
qtyInput.select();

// ... rest of code
```

**Issues:**
- ❌ Manually shows/hides elements
- ❌ Bypasses business logic
- ❌ Doesn't check personalized/disabled state
- ❌ Doesn't hide other qty inputs globally

---

### After (Lines 145-192)

```javascript
// Find the qty-wrp element (which handles the click logic)
const qtyWrp = targetRow.querySelector('.qty-wrp');

if (!qtyWrp) {
    console.error('Could not find .qty-wrp element');
    return;
}

// Trigger click on qty-wrp to invoke the existing click handler
// This handles personalized items, disabled state, and showing/hiding inputs
console.log('Triggering click on .qty-wrp');
qtyWrp.click();

// Wait for the click handler to show the input, then set the value
setTimeout(() => {
    const qtyInput = qtyWrp.querySelector('input.qty-mn');

    if (!qtyInput) {
        console.error('Could not find qty input after click');
        return;
    }

    // Check if input is now visible
    if (qtyInput.classList.contains('hide')) {
        console.warn('Qty input still hidden - item may be personalized or disabled');
        return;
    }

    // Set the value
    qtyInput.value = qtyText;
    qtyInput.focus();
    qtyInput.select();

    console.log(`Set quantity to: ${qtyText}`);

    // Trigger events to ensure the change is registered
    const inputEvent = new Event('input', { bubbles: true });
    qtyInput.dispatchEvent(inputEvent);

    const changeEvent = new Event('change', { bubbles: true });
    qtyInput.dispatchEvent(changeEvent);

    // Simulate blur/exit after a short delay
    setTimeout(() => {
        qtyInput.blur();
        console.log('Exited qty input field');
    }, 300);
}, 100);
```

**Benefits:**
- ✅ Triggers existing click handler
- ✅ Respects business logic
- ✅ Checks personalized/disabled state
- ✅ Hides other qty inputs globally
- ✅ Handles edge cases properly

---

## How It Works

### Step-by-Step Flow

1. **User clicks quantity link** in the items list

2. **Find target row** with matching SKU in added items table

3. **Find `.qty-wrp` element** in that row

4. **Trigger click event**
   ```javascript
   qtyWrp.click();
   ```
   This executes the jQuery click handler which:
   - Checks if item is personalized
   - Checks if qty is editable
   - Checks if element is disabled
   - Hides ALL other qty inputs (global behavior)
   - Shows THIS specific qty input
   - Focuses on the input

5. **Wait 100ms** for click handler to complete

6. **Find and verify input** is now visible
   ```javascript
   const qtyInput = qtyWrp.querySelector('input.qty-mn');
   if (qtyInput.classList.contains('hide')) {
       // Input still hidden - personalized or disabled
       return;
   }
   ```

7. **Set the quantity value**
   ```javascript
   qtyInput.value = qtyText;
   qtyInput.focus();
   qtyInput.select();
   ```

8. **Trigger events** (input, change)

9. **Blur after 300ms** to exit editing mode

---

## Edge Cases Handled

### Personalized Items

**Scenario:** Item is personalized but not in correct status

**Old behavior:** Would forcefully show input (breaking rules)

**New behavior:**
- Click handler checks personalized status
- Only shows input if `IsPersonalized == 0` OR `(IsPersonalized == 1 && IsPersonalizedStatus == 2)`
- If input stays hidden, our code detects it and warns:
  ```
  ⚠️ Qty input still hidden - item may be personalized or disabled
  ```

### Disabled Qty

**Scenario:** Qty field is disabled (`.disabled` class)

**Old behavior:** Would forcefully show input (breaking rules)

**New behavior:**
- Click handler checks if disabled
- Skips if disabled
- Our code detects input is still hidden and exits gracefully

### Qty Not Editable

**Scenario:** `packingQtyEditable` setting is `false`

**Old behavior:** Would forcefully show input (breaking rules)

**New behavior:**
- Click handler checks `tf.settings().packingQtyEditable`
- Skips if not editable
- Our code detects and exits gracefully

### Multiple Inputs

**Scenario:** User clicks multiple qty links rapidly

**Old behavior:** Multiple inputs could show at once

**New behavior:**
- Click handler hides ALL qty inputs first: `$(".qty-mn").addClass("hide")`
- Then shows only the clicked one
- Proper global state management

---

## Console Output

### Successful Quantity Update

```
Quantity clicked: 5 for SKU: SKU12345
Found target row for SKU: SKU12345
Triggering click on .qty-wrp
Set quantity to: 5
Exited qty input field
```

### Personalized/Disabled Item

```
Quantity clicked: 5 for SKU: SKU12345
Found target row for SKU: SKU12345
Triggering click on .qty-wrp
⚠️ Qty input still hidden - item may be personalized or disabled
```

### Input Not Found

```
Quantity clicked: 5 for SKU: SKU12345
Found target row for SKU: SKU12345
Triggering click on .qty-wrp
❌ Could not find qty input after click
```

---

## Testing

### Test Cases

#### 1. Normal Item (Non-Personalized, Enabled)

**Steps:**
1. Click quantity link in items list
2. Verify input appears in added items table
3. Verify quantity value is set correctly
4. Verify input is focused and selected
5. Verify input blurs after 300ms

**Expected:** ✅ Works normally

---

#### 2. Personalized Item (Status Not 2)

**Steps:**
1. Click quantity link for personalized item
2. Check console

**Expected:**
- ⚠️ Warning: "Qty input still hidden - item may be personalized or disabled"
- Input does NOT appear
- Respects business rules

---

#### 3. Disabled Qty Field

**Steps:**
1. Click quantity link for disabled item
2. Check console

**Expected:**
- ⚠️ Warning: "Qty input still hidden - item may be personalized or disabled"
- Input does NOT appear
- Respects business rules

---

#### 4. Multiple Rapid Clicks

**Steps:**
1. Click quantity link for item A
2. Immediately click quantity link for item B
3. Verify behavior

**Expected:**
- Only item B's input is visible
- Item A's input is hidden (global hide behavior works)
- No multiple inputs visible at once

---

#### 5. Qty Not Editable Setting

**Steps:**
1. Set `packingQtyEditable = false` in settings
2. Click quantity link
3. Check behavior

**Expected:**
- Input does NOT appear
- Click handler respects setting
- Business rules enforced

---

## Timing Considerations

### 100ms Delay After Click

**Why?**
- jQuery click handler needs time to execute
- DOM updates (show/hide) need to propagate
- Ensures input is visible before we set value

**Too short?** Input might not be visible yet
**Too long?** User sees delay

**100ms is optimal** for most cases

### 300ms Delay Before Blur

**Why?**
- User needs time to see the value
- Input/change events need to propagate
- Backend processing might happen

**Same as before** - no change to this timing

---

## Comparison: Old vs New

| Aspect | Old (Manual DOM) | New (Click Trigger) |
|--------|------------------|---------------------|
| **Business Logic** | ❌ Bypassed | ✅ Executed |
| **Personalized Check** | ❌ No | ✅ Yes |
| **Disabled Check** | ❌ No | ✅ Yes |
| **Editable Setting** | ❌ No | ✅ Yes |
| **Global Input Hide** | ❌ No | ✅ Yes |
| **Error Handling** | ❌ Basic | ✅ Comprehensive |
| **Console Warnings** | ❌ No | ✅ Yes |
| **Edge Cases** | ❌ Breaks rules | ✅ Handles properly |

---

## Benefits

### Reliability

✅ **Respects all business rules** - No more bypassing validation
✅ **Proper state management** - Global input hiding works
✅ **Edge case handling** - Personalized/disabled items handled

### Maintainability

✅ **Less duplicated logic** - Reuses existing click handler
✅ **Single source of truth** - Click handler is the authority
✅ **Easier to update** - Changes to click handler automatically apply

### User Experience

✅ **Consistent behavior** - Same as manual clicking
✅ **No broken states** - Multiple inputs don't appear
✅ **Clear feedback** - Console warnings for edge cases

---

## Potential Issues & Solutions

### Issue: `.qty-wrp` Not Found

**Cause:** DOM structure changed

**Debug:**
```javascript
// Check structure
const targetRow = /* ... */;
console.log('Row HTML:', targetRow.innerHTML);

// Find all qty-related elements
console.log('Qty column:', targetRow.querySelector('td.qty-column'));
console.log('Qty wrp:', targetRow.querySelector('.qty-wrp'));
```

**Solution:** Update selector to match actual DOM structure

---

### Issue: Click Doesn't Work

**Cause:** jQuery click handler not attached yet

**Debug:**
```javascript
// Check if jQuery handler exists
const events = $._data(document, 'events');
console.log('Click handlers:', events.click);
```

**Solution:** Ensure `table-item-linker.js` loads after main application JS

---

### Issue: Input Still Hidden After Click

**Cause:** Item is personalized, disabled, or qty not editable

**This is expected behavior!** The click handler is correctly preventing editing.

**Debug:**
```javascript
// Check item properties
const row = targetRow;
const personalized = row.querySelector('.personalized-td').dataset.id;
const status = row.querySelector('.personalized-td').dataset.status;
const disabled = qtyWrp.classList.contains('disabled');

console.log('IsPersonalized:', personalized);
console.log('Status:', status);
console.log('Disabled:', disabled);
console.log('PackingQtyEditable:', tf.settings().packingQtyEditable);
```

---

## Related Files

- **`table-item-linker.js`** - Main file (modified)
- **`router.js`** - Calls `makeQtyItemsClickable()`
- **Application JS** - Contains jQuery `.qty-wrp` click handler

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-01-03 | Changed to trigger click event instead of manual DOM manipulation |
| 1.0 | 2024 | Initial implementation with manual DOM manipulation |

---

## Summary

The update changes the quantity link click behavior from **manual DOM manipulation** to **triggering the existing click event**. This ensures all business logic is executed, respects validation rules, and handles edge cases properly.

**Key Change:**
```javascript
// Before: Manual DOM manipulation
qtyInput.classList.remove('hide');
qtyDisplaySpan.style.display = 'none';

// After: Trigger existing click handler
qtyWrp.click();
```

**Result:** More reliable, maintainable, and consistent with application behavior.
