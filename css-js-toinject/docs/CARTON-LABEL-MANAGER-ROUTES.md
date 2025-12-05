# Carton Label Manager - Route Configuration

## Active Routes

The **carton-label-manager.js** is now active on the following routes:

### ✅ **Route 1: Shipment Details**
**URL Pattern:** `https://mj.3plnext.com/#Outbound/shipmentdetails?id=*`

**Features Active:**
- ✅ CSS Formatting (via window.open interception)
- ✅ URL Redirect (carton labels → `https://server:5555/print`)
- ⚠️ Auto-Print (No modal on this page, so auto-click not triggered)

**Router Code:** Lines 502-511 in `router.js`

**What Happens:**
1. When you click "Print Carton Label" button
2. window.open() is intercepted
3. CSS is injected into print window
4. POST request is redirected to new server

**Debug:**
```javascript
window.cartonLabelManager.stats();
```

---

### ✅ **Route 2: Outbound Packing**
**URL Pattern:** `https://mj.3plnext.com/#outbound/packing`

**Features Active:**
- ✅ CSS Formatting (via window.open interception)
- ✅ URL Redirect (carton labels → `https://server:5555/print`)
- ✅ Auto-Print (auto-clicks View Invoice when shipment created)

**Router Code:** Lines 660-717 in `router.js`

**What Happens:**
1. When shipment is created, modal appears
2. "View Invoice" button is auto-clicked
3. When you manually click "Print Carton Label":
   - window.open() is intercepted
   - CSS is injected into print window
   - POST request is redirected to new server

**Debug:**
```javascript
window.cartonLabelManager.stats();
window.cartonLabelManager.setAutoClick(false); // Disable auto-click
```

---

## Route Comparison

| Feature | Shipment Details | Outbound Packing |
|---------|-----------------|------------------|
| CSS Injection | ✅ Yes | ✅ Yes |
| URL Redirect | ✅ Yes | ✅ Yes |
| Auto-Print (View Invoice) | ❌ No modal | ✅ Yes |
| Manual Carton Label | ✅ Works with redirect | ✅ Works with redirect |

---

## How Routes Work

### **Shipment Details Route**
```javascript
// Pattern matches:
#Outbound/shipmentdetails
#Outbound/shipmentdetails?id=123
#Outbound/shipmentdetails?id=456&other=params
```

**Use Case:** Viewing existing shipment details and printing labels

**Typical Workflow:**
1. Navigate to shipment details page
2. View shipment information
3. Click "Print Carton Label" button
4. Label prints with CSS formatting
5. Request goes to new server (`https://server:5555/print`)

---

### **Outbound Packing Route**
```javascript
// Pattern matches:
#outbound/packing
#outbound/packing?any=params
```

**Use Case:** Creating new shipments and auto-printing

**Typical Workflow:**
1. Navigate to packing page
2. Scan items and create shipment
3. **Modal appears** (shipment created)
4. **View Invoice auto-clicks** (if enabled)
5. Manually click "Print Carton Label"
6. Label prints with CSS formatting
7. Request goes to new server (`https://server:5555/print`)

---

## Global Behavior

The cartonLabelManager is loaded **globally** but only certain features activate based on the route:

### **Global Features (Always Active)**
- ✅ window.open() interception for CSS injection
- ✅ Fetch/XHR/jQuery redirect interception
- ✅ Button click tracking

### **Route-Specific Features**
- ⚠️ **Auto-Print** - Only on routes with modal observer setup
  - Currently: `#outbound/packing` ✅
  - Not on: `#Outbound/shipmentdetails` ❌

---

## Testing Each Route

### **Test Shipment Details Route**

1. Navigate to: `https://mj.3plnext.com/#Outbound/shipmentdetails?id=4527`

2. Open console and check:
```javascript
// Should see:
// 🚀 Matched #Outbound/shipmentdetails route
// 📦 Carton Label Manager enabled on Shipment Details
```

3. Click "Print Carton Label" button

4. Check console for:
```javascript
// Should see:
// 🖱️ CARTON LABEL BUTTON CLICKED
// 🪟 window.open intercepted
// 📦 CARTON LABEL DETECTED - REDIRECTING
// ✅ CARTON LABEL CSS INJECTED!
```

5. Check Network tab:
```
POST https://server:5555/print
```

---

### **Test Outbound Packing Route**

1. Navigate to: `https://mj.3plnext.com/#outbound/packing`

2. Open console and check:
```javascript
// Should see:
// 🚀 Matched #outbound/packing route
// 📦 Carton Label Manager enabled
// ✅ Carton Label Manager observer set up
```

3. Create a shipment (scan items, click "Create Shipment")

4. Check console for auto-print:
```javascript
// Should see:
// 📌 Captured shipment ID: 12345
// 🎯 Modal appearance detected
// ✅ Success modal confirmed
// 🖱️ Auto-clicking View Invoice button...
// ✅ View Invoice button clicked
```

5. Manually click "Print Carton Label"

6. Check console for redirect:
```javascript
// Should see:
// 🖱️ CARTON LABEL BUTTON CLICKED
// 📦 CARTON LABEL DETECTED - REDIRECTING
```

---

## Configuration Per Route

You can configure settings differently per route if needed:

```javascript
// In router.js, for specific route:

// Disable auto-click on packing page
if (window.location.hash.includes('outbound/packing')) {
    window.cartonLabelManager.setAutoClick(false);
}

// Change redirect URL for shipment details only
if (window.location.hash.includes('shipmentdetails')) {
    window.cartonLabelManager.config.newUrl = 'https://different-server:5555/print';
}
```

---

## Troubleshooting by Route

### **Shipment Details Issues**

**Issue:** CSS not applied
```javascript
// Check if cartonLabelManager loaded
typeof window.cartonLabelManager // Should be 'object'

// Check console for CSS injection logs
// Look for: "✅ CARTON LABEL CSS INJECTED!"
```

**Issue:** Not redirecting
```javascript
// Check redirect count
window.cartonLabelManager.redirectCount()

// Should increment when you click Print Carton Label
```

---

### **Outbound Packing Issues**

**Issue:** Auto-click not working
```javascript
// Check if enabled
window.cartonLabelManager.config.autoClickEnabled // Should be true

// Check if modal is detected
// Look for: "🎯 Modal appearance detected"

// Enable auto-click
window.cartonLabelManager.setAutoClick(true);
```

**Issue:** Redirecting other prints
```javascript
// This should NOT happen!
// Only carton labels should redirect

// Check console - only carton label should show:
// "📦 CARTON LABEL DETECTED - REDIRECTING"

// Invoice/Packing Slip should show:
// "ℹ️ Print request (NOT carton label - passing through)"
```

---

## Summary

### **URLs Where cartonLabelManager is Active:**

1. ✅ `https://mj.3plnext.com/#Outbound/shipmentdetails?id=*`
   - CSS: Yes
   - Redirect: Yes
   - Auto-Print: No (no modal)

2. ✅ `https://mj.3plnext.com/#outbound/packing`
   - CSS: Yes
   - Redirect: Yes
   - Auto-Print: Yes (with modal)

### **Global API (Available on all pages):**
```javascript
window.cartonLabelManager.stats()
window.cartonLabelManager.redirectCount()
window.cartonLabelManager.setAutoClick(true/false)
window.cartonLabelManager.config
```

### **Files Required:**
- ✅ `carton-label-manager.js` - Must be loaded globally
- ✅ `router.js` - Routes updated to activate features

Ready to test! 🎉