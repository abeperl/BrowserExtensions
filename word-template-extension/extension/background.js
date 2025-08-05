// Background Service Worker for Word Template Extension
// Handles native messaging, context menus, and extension lifecycle

// Import NativeHostManager
importScripts('native-host-manager.js');

class BackgroundService {
  constructor() {
    this.init();
  }

  init() {
    this.setupEventListeners();
    this.createContextMenus();
  }

  setupEventListeners() {
    // Extension installation/startup
    chrome.runtime.onInstalled.addListener((details) => {
      this.handleInstallation(details);
    });

    chrome.runtime.onStartup.addListener(() => {
      console.log('Word Template Extension started');
    });

    // Handle messages from popup and content scripts
    chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
      this.handleMessage(message, sender, sendResponse);
      return true; // Keep message channel open for async responses
    });

    // Handle native messaging responses
    chrome.runtime.onMessageExternal.addListener((message, sender, sendResponse) => {
      console.log('External message received:', message);
      sendResponse({ received: true });
    });

    // Context menu clicks
    chrome.contextMenus.onClicked.addListener((info, tab) => {
      this.handleContextMenuClick(info, tab);
    });

    // Tab updates for dynamic behavior
    chrome.tabs.onActivated.addListener((activeInfo) => {
      this.handleTabActivated(activeInfo);
    });

    chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
      this.handleTabUpdated(tabId, changeInfo, tab);
    });
  }

  handleInstallation(details) {
    console.log('Word Template Extension installed:', details.reason);
    
    if (details.reason === 'install') {
      // First time installation
      this.setDefaultSettings();
      this.showWelcomePage();
    } else if (details.reason === 'update') {
      // Extension updated
      this.handleUpdate(details.previousVersion);
    }
  }

  async setDefaultSettings() {
    const defaultSettings = {
      autoOpen: true,
      includeMetadata: true,
      customFilename: '',
      templatePath: '',
      outputPath: '',
      autoExtract: false
    };

    await chrome.storage.local.set({ 
      extensionSettings: defaultSettings,
      usageStats: {},
      lastUsed: Date.now()
    });
  }

  showWelcomePage() {
    chrome.tabs.create({
      url: chrome.runtime.getURL('welcome.html')
    });
  }

  handleUpdate(previousVersion) {
    console.log(`Updated from version ${previousVersion}`);
    // Handle any migration logic here
  }

  async handleMessage(message, sender, sendResponse) {
    try {
      switch (message.action) {
        case 'sendNativeMessage':
          await this.forwardToNativeHost(message.data, sendResponse);
          break;
          
        case 'extractData':
          await this.handleDataExtraction(sender.tab, sendResponse);
          break;
          
        case 'getSettings':
          await this.getSettings(sendResponse);
          break;
          
        case 'saveSettings':
          await this.saveSettings(message.data, sendResponse);
          break;
          
        case 'getUsageStats':
          await this.getUsageStats(sendResponse);
          break;
          
        default:
          sendResponse({ error: 'Unknown action: ' + message.action });
      }
    } catch (error) {
      console.error('Error handling message:', error);
      sendResponse({ error: error.message });
    }
  }

  async forwardToNativeHost(data, sendResponse) {
    try {
      // Initialize native host manager if not already done
      if (!this.nativeHostManager) {
        this.nativeHostManager = new NativeHostManager();
      }

      const response = await this.nativeHostManager.sendMessageWithRetry(data);
      sendResponse(response);
    } catch (error) {
      console.error('Native host communication failed:', error);
      sendResponse({ 
        success: false, 
        error: error.message,
        errorType: error.type || 'COMMUNICATION_ERROR'
      });
    }
  }

  async handleDataExtraction(tab, sendResponse) {
    try {
      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        function: this.extractPageData
      });

      if (results && results[0]) {
        sendResponse({ 
          success: true, 
          data: results[0].result 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: 'No data extracted' 
        });
      }
    } catch (error) {
      sendResponse({ 
        success: false, 
        error: error.message 
      });
    }
  }

  // Data extraction function (runs in page context)
  extractPageData() {
    const patterns = {
      emails: /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g,
      phones: /(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})/g,
      currencies: /\$[\d,]+\.?\d*/g,
      dates: /\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b/g,
      urls: /https?:\/\/[^\s]+/g
    };

    const data = {};
    const pageText = document.body.innerText;

    Object.entries(patterns).forEach(([key, pattern]) => {
      const matches = pageText.match(pattern);
      if (matches) {
        data[key] = [...new Set(matches)].slice(0, 10);
      }
    });

    return data;
  }

  async getSettings(sendResponse) {
    try {
      const result = await chrome.storage.local.get(['extensionSettings']);
      sendResponse({ 
        success: true, 
        settings: result.extensionSettings || {} 
      });
    } catch (error) {
      sendResponse({ 
        success: false, 
        error: error.message 
      });
    }
  }

  async saveSettings(settings, sendResponse) {
    try {
      await chrome.storage.local.set({ extensionSettings: settings });
      sendResponse({ success: true });
    } catch (error) {
      sendResponse({ 
        success: false, 
        error: error.message 
      });
    }
  }

  async getUsageStats(sendResponse) {
    try {
      const result = await chrome.storage.local.get(['usageStats']);
      sendResponse({ 
        success: true, 
        stats: result.usageStats || {} 
      });
    } catch (error) {
      sendResponse({ 
        success: false, 
        error: error.message 
      });
    }
  }

  createContextMenus() {
    // Remove existing menus first
    chrome.contextMenus.removeAll(() => {
      // Create context menu for selected text
      chrome.contextMenus.create({
        id: 'extractSelection',
        title: 'Extract to Word Template',
        contexts: ['selection']
      });

      // Create context menu for entire page
      chrome.contextMenus.create({
        id: 'extractPage',
        title: 'Extract Page Data',
        contexts: ['page']
      });

      // Separator
      chrome.contextMenus.create({
        id: 'separator1',
        type: 'separator',
        contexts: ['page', 'selection']
      });

      // Quick actions submenu
      chrome.contextMenus.create({
        id: 'quickActions',
        title: 'Quick Actions',
        contexts: ['page']
      });

      chrome.contextMenus.create({
        id: 'openTemplateFolder',
        parentId: 'quickActions',
        title: 'Open Template Folder',
        contexts: ['page']
      });

      chrome.contextMenus.create({
        id: 'refreshTemplates',
        parentId: 'quickActions',
        title: 'Refresh Templates',
        contexts: ['page']
      });
    });
  }

  async handleContextMenuClick(info, tab) {
    try {
      switch (info.menuItemId) {
        case 'extractSelection':
          await this.extractSelection(info, tab);
          break;
          
        case 'extractPage':
          await this.extractPageFromContext(tab);
          break;
          
        case 'openTemplateFolder':
          await this.openTemplateFolder();
          break;
          
        case 'refreshTemplates':
          await this.refreshTemplates();
          break;
      }
    } catch (error) {
      console.error('Context menu action failed:', error);
    }
  }

  async extractSelection(info, tab) {
    const selectedText = info.selectionText;
    
    // Store selected text and open popup
    await chrome.storage.local.set({ 
      selectedText: selectedText,
      extractionMode: 'selection'
    });
    
    // Open extension popup
    chrome.action.openPopup();
  }

  async extractPageFromContext(tab) {
    await chrome.storage.local.set({ 
      extractionMode: 'page'
    });
    
    chrome.action.openPopup();
  }

  async openTemplateFolder() {
    try {
      const response = await this.sendNativeMessagePromise({
        action: 'open_template_folder'
      });
      
      if (!response.success) {
        this.showNotification('Error', 'Could not open template folder');
      }
    } catch (error) {
      this.showNotification('Error', 'Failed to open template folder');
    }
  }

  async refreshTemplates() {
    try {
      const response = await this.sendNativeMessagePromise({
        action: 'refresh_templates'
      });
      
      if (response.success) {
        this.showNotification('Success', 'Templates refreshed');
      } else {
        this.showNotification('Error', 'Failed to refresh templates');
      }
    } catch (error) {
      this.showNotification('Error', 'Failed to refresh templates');
    }
  }

  sendNativeMessagePromise(message) {
    return new Promise((resolve, reject) => {
      chrome.runtime.sendNativeMessage(
        'com.wordtemplateextension.nativehost',
        message,
        (response) => {
          if (chrome.runtime.lastError) {
            reject(new Error(chrome.runtime.lastError.message));
          } else {
            resolve(response);
          }
        }
      );
    });
  }

  showNotification(title, message) {
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.png',
      title: title,
      message: message
    });
  }

  handleTabActivated(activeInfo) {
    // Could be used for automatic data extraction if enabled
  }

  handleTabUpdated(tabId, changeInfo, tab) {
    if (changeInfo.status === 'complete' && tab.url) {
      // Page finished loading - could trigger auto-extraction
      this.checkAutoExtraction(tab);
    }
  }

  async checkAutoExtraction(tab) {
    try {
      const settings = await chrome.storage.local.get(['extensionSettings']);
      if (settings.extensionSettings?.autoExtract) {
        // Auto-extract if enabled in settings
        this.handleDataExtraction(tab, () => {});
      }
    } catch (error) {
      console.error('Auto-extraction check failed:', error);
    }
  }
}

// Initialize background service
new BackgroundService();
