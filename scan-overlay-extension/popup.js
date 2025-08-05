// popup.js
// Enhanced popup interface for Scan Overlay Extension

class PopupManager {
  constructor() {
    this.scanCount = 0;
    this.init();
  }

  async init() {
    await this.loadSettings();
    await this.loadScanCount();
    this.bindEvents();
    this.checkFieldStatus();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get('scanOverlaySettings');
      const settings = result.scanOverlaySettings || {};
      
      // Update quick settings toggles
      const audioToggle = document.getElementById('quickAudioToggle');
      const overlayToggle = document.getElementById('quickOverlayToggle');
      
      if (audioToggle) audioToggle.checked = settings.audioEnabled !== false;
      if (overlayToggle) overlayToggle.checked = true; // Always show overlays in popup context
      
    } catch (error) {
      console.error('Error loading settings:', error);
    }
  }

  async loadScanCount() {
    try {
      // Get scan history to count session scans
      const response = await this.sendMessage({ type: 'GET_SCAN_HISTORY' });
      if (response && response.history) {
        this.scanCount = response.history.length;
        this.updateScanCount();
      }
    } catch (error) {
      console.error('Error loading scan count:', error);
    }
  }

  updateScanCount() {
    const scanCountEl = document.getElementById('scanCount');
    if (scanCountEl) {
      scanCountEl.textContent = this.scanCount.toString();
    }
  }

  async checkFieldStatus() {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (!tab) return;

      const result = await chrome.storage.sync.get('scanOverlaySettings');
      const settings = result.scanOverlaySettings || {};
      
      const itemSelector = settings.itemIdSelector || '#product-scan';
      const statusSelector = settings.statusIdSelector || '#status-scan';

      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: (item, status) => {
          try {
            const itemEl = document.querySelector(item);
            const statusEl = document.querySelector(status);
            return {
              itemFound: !!itemEl,
              statusFound: !!statusEl
            };
          } catch (error) {
            return { itemFound: false, statusFound: false, error: error.message };
          }
        },
        args: [itemSelector, statusSelector]
      });

      const fieldResult = results[0].result;
      this.updateFieldStatus('itemFieldStatus', fieldResult.itemFound);
      this.updateFieldStatus('statusFieldStatus', fieldResult.statusFound);
      this.updateConnectionStatus(fieldResult.itemFound || fieldResult.statusFound);

    } catch (error) {
      console.error('Error checking field status:', error);
      this.updateConnectionStatus(false);
    }
  }

  updateFieldStatus(elementId, found) {
    const element = document.getElementById(elementId);
    if (element) {
      element.textContent = found ? 'Found' : 'Not detected';
      element.className = `field-status-indicator ${found ? 'found' : 'not-found'}`;
    }
  }

  updateConnectionStatus(connected) {
    const statusDot = document.getElementById('connectionStatus');
    const statusText = document.getElementById('statusText');
    
    if (statusDot) {
      statusDot.className = `status-dot ${connected ? 'connected' : 'disconnected'}`;
    }
    
    if (statusText) {
      statusText.textContent = connected ? 'Active' : 'No fields detected';
    }
  }

  async saveQuickSettings() {
    try {
      const audioEnabled = document.getElementById('quickAudioToggle').checked;
      
      // Get current settings and update
      const result = await chrome.storage.sync.get('scanOverlaySettings');
      const settings = result.scanOverlaySettings || {};
      settings.audioEnabled = audioEnabled;
      
      await chrome.storage.sync.set({ scanOverlaySettings: settings });
      
      // Notify content scripts
      await this.notifySettingsChange(settings);
      
    } catch (error) {
      console.error('Error saving quick settings:', error);
    }
  }

  async notifySettingsChange(settings) {
    try {
      const tabs = await chrome.tabs.query({});
      for (const tab of tabs) {
        try {
          await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: (newSettings) => {
              if (window.scanOverlayExtension) {
                window.scanOverlayExtension.updateSettings(newSettings);
              }
            },
            args: [settings]
          });
        } catch (error) {
          // Ignore errors for tabs that can't be scripted
        }
      }
    } catch (error) {
      console.error('Error notifying settings change:', error);
    }
  }

  async testOverlay() {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (!tab) return;

      await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: () => {
          window.postMessage({
            type: 'SHOW_SUCCESS_OVERLAY',
            itemId: 'TEST-123',
            statusId: 'DEMO-456',
            progress: 1
          }, '*');
        }
      });

    } catch (error) {
      console.error('Error testing overlay:', error);
    }
  }

  async clearHistory() {
    try {
      await this.sendMessage({ type: 'CLEAR_SCAN_HISTORY' });
      this.scanCount = 0;
      this.updateScanCount();
      
      // Update history display
      const historyList = document.getElementById('history-list');
      if (historyList) {
        historyList.innerHTML = '<div class="no-history">No scans in this session</div>';
      }
      
    } catch (error) {
      console.error('Error clearing history:', error);
    }
  }

  openSettings() {
    chrome.tabs.create({
      url: chrome.runtime.getURL('settings.html')
    });
  }

  sendMessage(message) {
    return new Promise((resolve, reject) => {
      chrome.runtime.sendMessage(message, (response) => {
        if (chrome.runtime.lastError) {
          reject(new Error(chrome.runtime.lastError.message));
        } else {
          resolve(response);
        }
      });
    });
  }

  bindEvents() {
    // Settings button
    const settingsBtn = document.getElementById('settingsBtn');
    if (settingsBtn) {
      settingsBtn.addEventListener('click', () => this.openSettings());
    }

    // Full settings button
    const fullSettingsBtn = document.getElementById('fullSettingsBtn');
    if (fullSettingsBtn) {
      fullSettingsBtn.addEventListener('click', () => this.openSettings());
    }

    // Quick toggles
    const audioToggle = document.getElementById('quickAudioToggle');
    const overlayToggle = document.getElementById('quickOverlayToggle');
    
    if (audioToggle) {
      audioToggle.addEventListener('change', () => this.saveQuickSettings());
    }

    // Field detection
    const detectFieldsBtn = document.getElementById('detectFieldsBtn');
    if (detectFieldsBtn) {
      detectFieldsBtn.addEventListener('click', () => this.checkFieldStatus());
    }

    // Test overlay
    const testOverlayBtn = document.getElementById('testOverlayBtn');
    if (testOverlayBtn) {
      testOverlayBtn.addEventListener('click', () => this.testOverlay());
    }

    // Clear history
    const clearHistoryBtn = document.getElementById('clearHistoryBtn');
    if (clearHistoryBtn) {
      clearHistoryBtn.addEventListener('click', () => {
        if (confirm('Clear scan history?')) {
          this.clearHistory();
        }
      });
    }

    // Refresh field status periodically
    setInterval(() => {
      this.checkFieldStatus();
    }, 5000);
  }
}

// Initialize popup manager when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  new PopupManager();
});
