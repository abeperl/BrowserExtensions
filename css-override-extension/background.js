// Background service worker for CSS Override Extension
class CSSOverrideManager {
  constructor() {
    this.rules = new Map();
    this.activeInjections = new Map();
    this.uploadedCssFiles = new Map();
    this.init();
  }

  async init() {
    // Load saved rules and CSS files from storage
    await this.loadRules();
    await this.loadUploadedCssFiles();

    // Set up listeners
    chrome.tabs.onUpdated.addListener(this.handleTabUpdate.bind(this));
    chrome.tabs.onRemoved.addListener(this.handleTabRemoved.bind(this));
    chrome.runtime.onMessage.addListener(this.handleMessage.bind(this));
  }

  async loadRules() {
    try {
      const result = await chrome.storage.local.get(['cssRules']);
      if (result.cssRules) {
        this.rules = new Map(Object.entries(result.cssRules));
      }
    } catch (error) {
      console.error('Failed to load CSS rules:', error);
    }
  }

  async loadUploadedCssFiles() {
    try {
      const result = await chrome.storage.local.get(['uploadedCssFiles']);
      if (result.uploadedCssFiles) {
        this.uploadedCssFiles = new Map(Object.entries(result.uploadedCssFiles));
      }
    } catch (error) {
      console.error('Failed to load uploaded CSS files:', error);
    }
  }

  async saveRules() {
    try {
      const rulesObject = Object.fromEntries(this.rules);
      await chrome.storage.local.set({ cssRules: rulesObject });
    } catch (error) {
      console.error('Failed to save CSS rules:', error);
    }
  }

  matchesUrlPattern(url, pattern) {
    try {
      // Support for wildcards (*)
      if (pattern.includes('*')) {
        const regex = new RegExp(pattern.replace(/\*/g, '.*').replace(/\?/g, '.'));
        return regex.test(url);
      }
      // Exact match
      return url === pattern;
    } catch (error) {
      console.error('Error matching URL pattern:', error);
      return false;
    }
  }

  async handleTabUpdate(tabId, changeInfo, tab) {
    if (changeInfo.status === 'loading' && tab.url) {
      // Inject print interceptor script first
      await this.injectPrintInterceptor(tabId);
      // Then apply CSS rules
      await this.applyCSSRules(tabId, tab.url);
    }
  }

  async handleTabRemoved(tabId) {
    // Clean up active injections
    if (this.activeInjections.has(tabId)) {
      this.activeInjections.delete(tabId);
    }
  }

  async injectPrintInterceptor(tabId) {
    try {
      await chrome.scripting.executeScript({
        target: { tabId, allFrames: true },
        files: ['print-interceptor.js'],
        world: 'MAIN'
      });
      console.log('Print interceptor injected into tab:', tabId);
    } catch (error) {
      console.error('Failed to inject print interceptor:', error);
    }
  }

  async applyCSSRules(tabId, url) {
    try {
      // Remove existing injections for this tab
      await this.removeCSSInjections(tabId);

      const applicableRules = [];

      // Find all rules that match the current URL
      for (const [ruleId, rule] of this.rules) {
        if (rule.enabled !== false && this.matchesUrlPattern(url, rule.urlPattern)) {
          applicableRules.push({ ruleId, rule });
        }
      }

      if (applicableRules.length === 0) {
        return;
      }

      // Apply CSS rules
      const injections = [];
      for (const { ruleId, rule } of applicableRules) {
        const injection = await this.injectCSS(tabId, rule);
        if (injection) {
          injections.push({ ruleId, injectionId: injection });
        }
      }

      // Track active injections
      this.activeInjections.set(tabId, injections);

    } catch (error) {
      console.error('Failed to apply CSS rules:', error);
    }
  }

  async injectCSS(tabId, rule) {
    try {
      const injection = {
        target: { tabId },
        css: rule.css
      };

      // Handle different injection modes
      if (rule.mode === 'replace') {
        // For replace mode, we'll need to handle this differently
        // This is a simplified version - in practice, we'd need more complex logic
        injection.css = rule.css;
      } else {
        // Override mode (default)
        injection.css = rule.css;
      }

      const result = await chrome.scripting.insertCSS(injection);
      return result;
    } catch (error) {
      console.error('Failed to inject CSS:', error);
      return null;
    }
  }

  async removeCSSInjections(tabId) {
    const activeInjections = this.activeInjections.get(tabId);
    if (!activeInjections) return;

    for (const injection of activeInjections) {
      try {
        await chrome.scripting.removeCSS({
          target: { tabId },
          css: injection.injectionId
        });
      } catch (error) {
        console.error('Failed to remove CSS injection:', error);
      }
    }

    this.activeInjections.delete(tabId);
  }

  async handleMessage(message, sender, sendResponse) {
    try {
      switch (message.type) {
        case 'ADD_RULE':
          await this.addRule(message.rule);
          sendResponse({ success: true });
          break;

        case 'UPDATE_RULE':
          await this.updateRule(message.ruleId, message.rule);
          sendResponse({ success: true });
          break;

        case 'DELETE_RULE':
          await this.deleteRule(message.ruleId);
          sendResponse({ success: true });
          break;

        case 'GET_RULES':
          sendResponse({ rules: Object.fromEntries(this.rules) });
          break;

        case 'REFRESH_TAB':
          if (sender.tab) {
            await this.applyCSSRules(sender.tab.id, sender.tab.url);
          }
          sendResponse({ success: true });
          break;

        case 'GET_CSS_FILES':
          sendResponse({ files: Object.fromEntries(this.uploadedCssFiles) });
          break;

        case 'CSS_FILES_UPDATED':
          await this.loadUploadedCssFiles();
          sendResponse({ success: true });
          break;

        default:
          sendResponse({ error: 'Unknown message type' });
      }
    } catch (error) {
      console.error('Error handling message:', error);
      sendResponse({ error: error.message });
    }
  }

  async addRule(rule) {
    const ruleId = Date.now().toString();
    this.rules.set(ruleId, {
      ...rule,
      id: ruleId,
      created: new Date().toISOString(),
      enabled: rule.enabled !== false
    });
    await this.saveRules();
  }

  async updateRule(ruleId, updates) {
    if (!this.rules.has(ruleId)) {
      throw new Error('Rule not found');
    }

    const existingRule = this.rules.get(ruleId);
    this.rules.set(ruleId, { ...existingRule, ...updates });
    await this.saveRules();
  }

  async deleteRule(ruleId) {
    if (!this.rules.has(ruleId)) {
      throw new Error('Rule not found');
    }

    this.rules.delete(ruleId);
    await this.saveRules();
  }
}

// Initialize the CSS override manager
const cssManager = new CSSOverrideManager();