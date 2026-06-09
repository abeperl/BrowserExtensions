# Coding Rules for css-js-toinject Scripts

## Architecture Pattern

### Rule #1: Separation of Concerns

**Individual feature files should ONLY contain functions, NOT setup logic.**

- ✅ **DO**: Define pure functions that perform actions
- ❌ **DON'T**: Include MutationObserver setup
- ❌ **DON'T**: Include initialization logic
- ❌ **DON'T**: Add DOMContentLoaded event listeners
- ❌ **DON'T**: Auto-execute code in IIFEs

### Rule #2: Router Handles Setup

**The router.js file is responsible for:**

- Route pattern matching (URL hash detection)
- MutationObserver setup for DOM changes
- Calling feature functions at the right time
- Managing timing and delays

## Example Pattern

### ❌ WRONG - Feature file with setup

```javascript
// DON'T DO THIS in feature files
(function() {
    'use strict';

    function myFeature() {
        // feature logic
    }

    // ❌ Don't set up observers in feature files
    const observer = new MutationObserver(() => {
        myFeature();
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
```

### ✅ CORRECT - Feature file with only functions

```javascript
// feature-name.js - GOOD: Only functions

/**
 * Description of what this feature does
 */

// Configuration
const FEATURE_CONFIG = {
    enabled: true,
    debugMode: true
};

/**
 * Main feature function
 */
function myFeatureFunction() {
    console.log('Feature executing...');

    const element = document.getElementById('target');
    if (!element) {
        console.warn('Element not found');
        return false;
    }

    // Do work here
    console.log('✅ Feature completed');
    return true;
}

/**
 * Helper function
 */
function myHelperFunction() {
    // Helper logic
}

// Expose global API (optional)
if (typeof window !== 'undefined') {
    window.myFeature = {
        config: FEATURE_CONFIG,
        execute: myFeatureFunction,
        helper: myHelperFunction
    };

    console.log('✅ Feature functions loaded');
}
```

### ✅ CORRECT - Router handles setup

```javascript
// router.js - GOOD: Router sets up observers

{
    name: 'My Feature Route',
    pattern: /^#my-route$/i,
    action: () => {
        console.log('🚀 Matched my-route');

        if (typeof myFeatureFunction === 'function') {
            // Set up MutationObserver
            const observer = new MutationObserver((mutations) => {
                const hasChanges = mutations.some(mutation => {
                    return Array.from(mutation.addedNodes).some(node => {
                        return node.nodeType === 1 &&
                               node.matches?.('.target-element');
                    });
                });

                if (hasChanges) {
                    console.log('🔄 Changes detected');
                    myFeatureFunction();
                }
            });

            observer.observe(document.body, {
                childList: true,
                subtree: true
            });

            // Try immediately
            setTimeout(() => {
                myFeatureFunction();
            }, 500);

            console.log('✅ Feature observer set up');
        } else {
            console.warn('⚠️ myFeatureFunction not loaded');
        }
    },
    description: 'Description of what this route does'
}
```

## File Naming Convention

- Feature files: `feature-name.js` (lowercase, hyphenated)
- Router file: `router.js` (single, main router)
- Documentation: `FEATURE-NAME-README.md` (uppercase, hyphenated)

## Function Naming Convention

- Main functions: `doSomething()`, `handleSomething()`, `addSomething()`
- Helper functions: `clickButton()`, `findElement()`, `checkCondition()`
- Config objects: `FEATURE_CONFIG` (uppercase, snake_case)

## Examples from This Codebase

### ✅ GOOD: auto-print-buttons.js

```javascript
// Pure functions only
function clickPackingSlipButton() { /* ... */ }
function clickCartonLabelButton() { /* ... */ }
function printAllButtons() { /* ... */ }
function addPrintAllButton() { /* ... */ }
function handleShipmentModalAppearance() { /* ... */ }

// Global API exposure
window.autoPrintButtons = {
    config: AUTO_PRINT_CONFIG,
    printAll: printAllButtons,
    // ...
};
```

### ✅ GOOD: router.js handles setup

```javascript
{
    name: 'Outbound Packing Route',
    pattern: /^#outbound\/packing(\?.*)?$/i,
    action: () => {
        if (typeof handleShipmentModalAppearance === 'function') {
            // Router sets up MutationObserver
            const modalObserver = new MutationObserver((mutations) => {
                // Detection logic
                handleShipmentModalAppearance();
            });

            modalObserver.observe(document.body, { /* ... */ });
        }
    }
}
```

## Why This Pattern?

### Benefits

1. **Reusability**: Functions can be called from multiple places
2. **Testability**: Pure functions are easier to test
3. **Maintainability**: Clear separation of concerns
4. **Debugging**: Easy to call functions manually from console
5. **Performance**: Router controls when observers run

### Problems with IIFE Pattern

```javascript
// ❌ PROBLEMS with this approach:
(function() {
    // 1. Can't access functions from outside
    // 2. Always runs, even on wrong pages
    // 3. Hard to debug
    // 4. Can't turn off or reconfigure
    // 5. Multiple observers on same elements
})();
```

## Checklist for New Features

When creating a new feature:

- [ ] Create feature file with only functions
- [ ] No MutationObserver setup in feature file
- [ ] No DOMContentLoaded listeners in feature file
- [ ] No IIFE auto-execution pattern
- [ ] Expose global API for debugging (optional)
- [ ] Add route to router.js
- [ ] Router handles MutationObserver setup
- [ ] Router checks if function exists before calling
- [ ] Add console.log statements for debugging
- [ ] Create documentation file

## Global API Pattern

### Recommended Global API Structure

```javascript
if (typeof window !== 'undefined') {
    window.featureName = {
        // Configuration
        config: FEATURE_CONFIG,

        // Main functions
        mainFunction: mainFunction,
        helperFunction: helperFunction,

        // Convenience methods
        enable: () => { FEATURE_CONFIG.enabled = true; },
        disable: () => { FEATURE_CONFIG.enabled = false; }
    };

    console.log('✅ Feature functions loaded');
    console.log('🔧 Debug with: window.featureName');
}
```

## Summary

**Remember:**
- Feature files = Functions only
- Router = Setup and orchestration
- This keeps code clean, testable, and maintainable

**Golden Rule:**
> If it's a MutationObserver, it belongs in router.js, not the feature file.
