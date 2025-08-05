// settings.js
// Handles the settings page functionality

class SettingsManager {
  constructor() {
    this.defaultSettings = {
      // Field selectors
      itemIdSelector: '#product-scan',
      statusIdSelector: '#status-scan',
      apiUrlPattern: '/api/scan',
      
      // Overlay settings
      overlayDuration: 2000,
      colorScheme: 'default',
      overlayPosition: 'center',
      
      // Audio settings
      audioEnabled: true,
      audioVolume: 50,
      
      // Keyboard shortcuts
      dismissKey: 'Escape',
      autoFocusAfterScan: true,
      
      // Advanced settings
      debugMode: false,
      maxHistoryEntries: 100,
      interceptFormSubmit: true
    };
    
    this.init();
  }

  async init() {
    await this.loadSettings();
    this.bindEvents();
    this.updateVolumeDisplay();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get('scanOverlaySettings');
      const settings = result.scanOverlaySettings || this.defaultSettings;
      
      // Populate form fields
      Object.keys(settings).forEach(key => {
        const element = document.getElementById(key);
        if (element) {
          if (element.type === 'checkbox') {
            element.checked = settings[key];
          } else {
            element.value = settings[key];
          }
        }
      });
      
      console.log('Settings loaded:', settings);
    } catch (error) {
      console.error('Error loading settings:', error);
      this.showStatus('Error loading settings', 'error');
    }
  }

  async saveSettings() {
    try {
      const settings = {};
      
      // Collect all form values
      Object.keys(this.defaultSettings).forEach(key => {
        const element = document.getElementById(key);
        if (element) {
          if (element.type === 'checkbox') {
            settings[key] = element.checked;
          } else if (element.type === 'number' || element.type === 'range') {
            settings[key] = parseInt(element.value, 10) || this.defaultSettings[key];
          } else {
            settings[key] = element.value || this.defaultSettings[key];
          }
        }
      });

      await chrome.storage.sync.set({ scanOverlaySettings: settings });
      
      // Notify all tabs about settings change
      await this.notifySettingsChange(settings);
      
      this.showStatus('Settings saved successfully!', 'success');
      console.log('Settings saved:', settings);
    } catch (error) {
      console.error('Error saving settings:', error);
      this.showStatus('Error saving settings', 'error');
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
      console.error('Error notifying tabs of settings change:', error);
    }
  }

  async testSelectors() {
    try {
      const itemSelector = document.getElementById('itemIdSelector').value;
      const statusSelector = document.getElementById('statusIdSelector').value;
      
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      
      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: (item, status) => {
          const itemEl = document.querySelector(item);
          const statusEl = document.querySelector(status);
          
          return {
            itemFound: !!itemEl,
            statusFound: !!statusEl,
            itemEl: itemEl ? itemEl.tagName + (itemEl.id ? '#' + itemEl.id : '') : null,
            statusEl: statusEl ? statusEl.tagName + (statusEl.id ? '#' + statusEl.id : '') : null
          };
        },
        args: [itemSelector, statusSelector]
      });

      const result = results[0].result;
      const resultDiv = document.getElementById('selectorTestResult');
      
      let html = '<div class="test-results">';
      html += `<div class="test-item ${result.itemFound ? 'success' : 'error'}">`;
      html += `Item Selector: ${result.itemFound ? '✓ Found' : '✗ Not found'} ${result.itemEl || ''}`;
      html += '</div>';
      html += `<div class="test-item ${result.statusFound ? 'success' : 'error'}">`;
      html += `Status Selector: ${result.statusFound ? '✓ Found' : '✗ Not found'} ${result.statusEl || ''}`;
      html += '</div>';
      html += '</div>';
      
      resultDiv.innerHTML = html;
      
    } catch (error) {
      console.error('Error testing selectors:', error);
      document.getElementById('selectorTestResult').innerHTML = 
        '<div class="test-results error">Error testing selectors. Make sure you\'re on a valid web page.</div>';
    }
  }

  resetSettings() {
    if (confirm('Are you sure you want to reset all settings to defaults? This cannot be undone.')) {
      Object.keys(this.defaultSettings).forEach(key => {
        const element = document.getElementById(key);
        if (element) {
          if (element.type === 'checkbox') {
            element.checked = this.defaultSettings[key];
          } else {
            element.value = this.defaultSettings[key];
          }
        }
      });
      
      this.updateVolumeDisplay();
      this.showStatus('Settings reset to defaults', 'info');
    }
  }

  exportSettings() {
    chrome.storage.sync.get('scanOverlaySettings', (result) => {
      const settings = result.scanOverlaySettings || this.defaultSettings;
      const dataStr = JSON.stringify(settings, null, 2);
      const dataBlob = new Blob([dataStr], { type: 'application/json' });
      
      const link = document.createElement('a');
      link.href = URL.createObjectURL(dataBlob);
      link.download = 'scan-overlay-settings.json';
      link.click();
      
      this.showStatus('Settings exported', 'success');
    });
  }

  importSettings() {
    document.getElementById('importFile').click();
  }

  handleImportFile(event) {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const settings = JSON.parse(e.target.result);
        
        // Validate settings
        const validSettings = {};
        Object.keys(this.defaultSettings).forEach(key => {
          if (settings.hasOwnProperty(key)) {
            validSettings[key] = settings[key];
          } else {
            validSettings[key] = this.defaultSettings[key];
          }
        });
        
        // Populate form
        Object.keys(validSettings).forEach(key => {
          const element = document.getElementById(key);
          if (element) {
            if (element.type === 'checkbox') {
              element.checked = validSettings[key];
            } else {
              element.value = validSettings[key];
            }
          }
        });
        
        this.updateVolumeDisplay();
        this.showStatus('Settings imported successfully', 'success');
        
      } catch (error) {
        console.error('Error importing settings:', error);
        this.showStatus('Error importing settings file', 'error');
      }
    };
    
    reader.readAsText(file);
  }

  updateVolumeDisplay() {
    const volumeSlider = document.getElementById('audioVolume');
    const volumeDisplay = document.querySelector('.volume-display');
    if (volumeSlider && volumeDisplay) {
      volumeDisplay.textContent = volumeSlider.value + '%';
    }
  }

  testAudio(audioType) {
    const volume = document.getElementById('audioVolume').value / 100;
    const audioEnabled = document.getElementById('audioEnabled').checked;
    
    if (!audioEnabled) {
      this.showStatus('Audio is disabled', 'info');
      return;
    }
    
    let audioFile = '';
    switch (audioType) {
      case 'scan': audioFile = 'audio/scan.mp3'; break;
      case 'success': audioFile = 'audio/taskCompleted.mp3'; break;
      case 'error': audioFile = 'audio/error.mp3'; break;
      case 'warning': audioFile = 'audio/warning.mp3'; break;
      default: return;
    }
    
    try {
      const audio = new Audio(chrome.runtime.getURL(audioFile));
      audio.volume = volume;
      audio.play().catch(error => {
        console.error('Error playing audio:', error);
        this.showStatus('Error playing audio file', 'error');
      });
    } catch (error) {
      console.error('Error creating audio:', error);
      this.showStatus('Error loading audio file', 'error');
    }
  }

  showStatus(message, type = 'info') {
    const statusDiv = document.getElementById('saveStatus');
    statusDiv.textContent = message;
    statusDiv.className = `save-status ${type}`;
    
    setTimeout(() => {
      statusDiv.textContent = '';
      statusDiv.className = 'save-status';
    }, 3000);
  }

  bindEvents() {
    // Save settings
    document.getElementById('saveSettings').addEventListener('click', () => {
      this.saveSettings();
    });

    // Test selectors
    document.getElementById('testSelectors').addEventListener('click', () => {
      this.testSelectors();
    });

    // Reset settings
    document.getElementById('resetSettings').addEventListener('click', () => {
      this.resetSettings();
    });

    // Export settings
    document.getElementById('exportSettings').addEventListener('click', () => {
      this.exportSettings();
    });

    // Import settings
    document.getElementById('importSettings').addEventListener('click', () => {
      this.importSettings();
    });

    // Handle import file
    document.getElementById('importFile').addEventListener('change', (e) => {
      this.handleImportFile(e);
    });

    // Volume slider
    document.getElementById('audioVolume').addEventListener('input', () => {
      this.updateVolumeDisplay();
    });

    // Audio test buttons
    document.querySelectorAll('[data-audio]').forEach(button => {
      button.addEventListener('click', (e) => {
        this.testAudio(e.target.dataset.audio);
      });
    });

    // Auto-save on certain changes
    const autoSaveElements = ['overlayDuration', 'audioVolume', 'colorScheme', 'overlayPosition'];
    autoSaveElements.forEach(id => {
      const element = document.getElementById(id);
      if (element) {
        element.addEventListener('change', () => {
          setTimeout(() => this.saveSettings(), 500);
        });
      }
    });
  }
}

// Initialize settings manager when page loads
document.addEventListener('DOMContentLoaded', () => {
  new SettingsManager();
});