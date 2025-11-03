# Tabs vs Popups - Implementation Details

## Problem

The original button click handlers used `window.open()` with window features (size, position), which creates **popup windows**:

```javascript
// Original code from the website
var mywindow = window.open("", "", "height=700,width=1100");
//                                  ^^^^^^^^^^^^^^^^^^^^
//                                  This creates a POPUP
```

**Issues with popups:**
1. One window can hide behind another
2. Can't independently manage each window
3. Confusing for users to find both windows
4. Some browsers block popups by default

## Solution

Intercept `window.open()` calls and change them to open **new tabs** instead:

```javascript
// Our interception
window.open = function(url, target, features) {
    // Ignore the features parameter, use '_blank' for new tab
    return originalWindowOpen.call(this, url, '_blank');
    //                                           ^^^^^^^^
    //                                           Opens as TAB
};
```

## How It Works

### Key Difference

```javascript
// POPUP (original)
window.open(url, "", "height=700,width=1100")
//                   ^^^^^^^^^^^^^^^^^^^^^^^ = popup

// TAB (our version)
window.open(url, "_blank")
//               ^^^^^^^^ = new tab (no features)
```

**Rule:** If you provide window features (third parameter), browser opens a popup. If you don't, it opens a tab.

## Implementation

### 1. Carton Label Button

```javascript
function clickCartonLabelButton() {
    return new Promise((resolve) => {
        const originalWindowOpen = window.open;

        // Intercept window.open
        window.open = function(url, target, features) {
            console.log('Original features:', features);
            // "height=700,width=1100" - would be popup

            // Restore immediately
            window.open = originalWindowOpen;

            // Call with '_blank' only - opens as tab
            return originalWindowOpen.call(this, url, '_blank');
        };

        // Click the button (will call our intercepted window.open)
        cartonLabelBtn.click();
    });
}
```

### 2. Packing Slip Button

```javascript
function clickPackingSlipButton() {
    const originalWindowOpen = window.open;

    window.open = function(url, target, features) {
        window.open = originalWindowOpen;
        // Open as tab instead of popup
        return originalWindowOpen.call(this, url, '_blank');
    };

    packingSlipBtn.click();
}
```

## Benefits

### Independent Tabs
Each print document opens in its own tab:
- **Tab 1:** Carton Label
- **Tab 2:** Packing Slip

Users can:
- Switch between tabs easily
- Close one without affecting the other
- Print each independently
- Review each document separately

### Better Browser Support
- Tabs are never blocked (unlike popups)
- Works consistently across all browsers
- Users prefer tabs over popups

### Sequential Opening
Since they're tabs, they don't overlap:

```
Timeline:
0ms    → Click Print All
100ms  → Carton Label tab opens (front)
2100ms → Packing Slip tab opens (front, switches focus)

User sees:
1. Carton Label appears (active tab)
2. Wait 2 seconds
3. Packing Slip appears (active tab)
4. Both tabs available in tab bar
```

## Technical Details

### window.open Signature

```javascript
window.open(url, target, features)
```

**target parameter:**
- `"_blank"` = new tab (or window if features specified)
- `"_self"` = current tab
- `"myWindow"` = named window/tab

**features parameter:**
- If provided = popup window with those features
- If omitted/empty = new tab (browser default)

### Examples

```javascript
// POPUP
window.open("page.html", "", "width=800,height=600")
window.open("page.html", "_blank", "width=800")
window.open("page.html", "myPopup", "width=800")

// TAB
window.open("page.html", "_blank")
window.open("page.html")
window.open("page.html", "_blank", "")
```

## Debugging

### Check What Opens

```javascript
// Original website code
window.open("", "", "height=700,width=1100")
// Result: Popup window 700x1100

// Our intercepted code
window.open("", "_blank")
// Result: New tab (full browser window)
```

### Console Output

When Print All runs, you'll see:

```
🖨️ Clicking Carton Label button
✅ Carton Label - intercepting window.open
   Original target:
   Original features: height=700,width=1100
✅ Carton Label opened in new tab

📄 Step 2: Waiting 2000ms before printing Packing Slip...

🖨️ Clicking Packing Slip button
✅ Packing Slip - intercepting window.open
   Original target:
   Original features: height=700,width=1100
✅ Packing Slip opened in new tab
```

## Edge Cases

### Popup Blockers
**Popups:** Might be blocked by browser
**Tabs:** Never blocked (user-initiated)

### Multiple Clicks
If user clicks Print All multiple times quickly:
- Each click opens 2 new tabs
- Tabs are independent
- No overlap or confusion

### Browser Differences
- Chrome/Edge: Opens in new tab
- Firefox: Opens in new tab
- Safari: Opens in new tab
- All modern browsers treat `_blank` without features as new tab

## User Experience

### Before (Popups)
```
User clicks Print All
  → Popup 1 opens (Carton Label)
  → Popup 2 opens (Packing Slip) - might hide Popup 1
  → User confused: "Where's the first one?"
  → Has to find and arrange windows
```

### After (Tabs)
```
User clicks Print All
  → Tab 1 opens (Carton Label) - clearly visible
  → Tab 2 opens (Packing Slip) - clearly visible
  → Both tabs shown in tab bar
  → User can easily switch between them
  → Print each independently
```

## Summary

| Aspect | Popups (Old) | Tabs (New) |
|--------|-------------|-----------|
| **Blocked?** | Yes (often) | No (never) |
| **Overlap?** | Yes | No |
| **Manage** | Hard | Easy |
| **Find** | Hard | Easy (tab bar) |
| **Independent** | No | Yes |
| **User Preference** | Disliked | Preferred |

**Bottom line:** Tabs are better in every way for this use case.

## Code Changes Required

Minimal! Just intercept `window.open` and ignore the features parameter:

```javascript
// Before clicking
window.open = function(url, target, features) {
    return originalWindowOpen.call(this, url, '_blank');
    //                                       ^^^^^^^^
    //                                       No features = tab
};

// Click the button
button.click();
```

That's it! The website's code doesn't need to change at all.
