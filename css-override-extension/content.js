// Content script for CSS Override Extension
class ContentScriptManager {
  constructor() {
    this.injectedStyles = new Map();
    this.init();
  }

  init() {
    // Listen for messages from background script
    chrome.runtime.onMessage.addListener(this.handleMessage.bind(this));

    // Notify background script that content script is ready
    chrome.runtime.sendMessage({ type: 'CONTENT_SCRIPT_READY' });
  }

  handleMessage(message, sender, sendResponse) {
    try {
      switch (message.type) {
        case 'INJECT_CSS':
          this.injectCSS(message.css, message.ruleId);
          sendResponse({ success: true });
          break;

        case 'REMOVE_CSS':
          this.removeCSS(message.ruleId);
          sendResponse({ success: true });
          break;

        case 'UPDATE_CSS':
          this.updateCSS(message.ruleId, message.css);
          sendResponse({ success: true });
          break;

        default:
          sendResponse({ error: 'Unknown message type' });
      }
    } catch (error) {
      console.error('Content script error:', error);
      sendResponse({ error: error.message });
    }
  }

  injectCSS(css, ruleId) {
    try {
      // Create a new style element
      const styleElement = document.createElement('style');
      styleElement.setAttribute('data-css-override-rule', ruleId);
      styleElement.textContent = css;

      // Add to document head
      document.head.appendChild(styleElement);

      // Track the injected style
      this.injectedStyles.set(ruleId, styleElement);

      console.log(`CSS rule ${ruleId} injected successfully`);
    } catch (error) {
      console.error(`Failed to inject CSS for rule ${ruleId}:`, error);
    }
  }

  removeCSS(ruleId) {
    try {
      const styleElement = this.injectedStyles.get(ruleId);
      if (styleElement) {
        styleElement.remove();
        this.injectedStyles.delete(ruleId);
        console.log(`CSS rule ${ruleId} removed successfully`);
      }
    } catch (error) {
      console.error(`Failed to remove CSS for rule ${ruleId}:`, error);
    }
  }

  updateCSS(ruleId, newCss) {
    try {
      const styleElement = this.injectedStyles.get(ruleId);
      if (styleElement) {
        styleElement.textContent = newCss;
        console.log(`CSS rule ${ruleId} updated successfully`);
      } else {
        // If style element doesn't exist, inject it
        this.injectCSS(newCss, ruleId);
      }
    } catch (error) {
      console.error(`Failed to update CSS for rule ${ruleId}:`, error);
    }
  }

  // Utility method to check if CSS is valid
  isValidCSS(css) {
    try {
      const style = document.createElement('style');
      style.textContent = css;
      return true;
    } catch (error) {
      return false;
    }
  }
}

// Initialize content script manager when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    new ContentScriptManager();
  });
} else {
  new ContentScriptManager();
}