// utils.js - Utility functions for the extension

/**
 * Safely executes a function with error handling
 * @param {Function} fn - Function to execute
 * @param {string} context - Context for error logging
 * @returns {*} Result of the function or null on error
 */
function safeExecute(fn, context = 'unknown') {
  try {
    return fn();
  } catch (error) {
    console.error(`Error in ${context}:`, error);
    return null;
  }
}

/**
 * Debounces a function call
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} Debounced function
 */
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

/**
 * Validates a CSS selector
 * @param {string} selector - CSS selector to validate
 * @returns {boolean} True if valid
 */
function isValidSelector(selector) {
  try {
    document.querySelector(selector);
    return true;
  } catch (error) {
    return false;
  }
}

/**
 * Safely parses JSON with fallback
 * @param {string} jsonString - JSON string to parse
 * @param {*} fallback - Fallback value
 * @returns {*} Parsed object or fallback
 */
function safeJsonParse(jsonString, fallback = {}) {
  try {
    return JSON.parse(jsonString);
  } catch (error) {
    console.warn('Failed to parse JSON:', error);
    return fallback;
  }
}

/**
 * Creates a promise that resolves after a delay
 * @param {number} ms - Milliseconds to delay
 * @returns {Promise}
 */
function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

/**
 * Checks if an element is visible in the viewport
 * @param {Element} element - Element to check
 * @returns {boolean} True if visible
 */
function isElementVisible(element) {
  if (!element) return false;
  const rect = element.getBoundingClientRect();
  return rect.top >= 0 && rect.left >= 0 &&
         rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
         rect.right <= (window.innerWidth || document.documentElement.clientWidth);
}

// Export utilities
window.ScanOverlayUtils = {
  safeExecute,
  debounce,
  isValidSelector,
  safeJsonParse,
  delay,
  isElementVisible
};