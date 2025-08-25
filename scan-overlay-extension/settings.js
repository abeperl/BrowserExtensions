// settings.js
// Handles the settings page functionality

class SettingsManager {
  constructor() {
    this.defaultSettings = {
      // Site-specific configurations
      siteConfigs: [
        {
          id: 'default',
          name: 'Default Configuration',
          urlPattern: '.*', // Regex pattern for matching URLs
          enabled: true,
          itemIdSelector: '#product-scan',
          statusIdSelector: '#status-scan',
          apiUrlPattern: '/api/scan'
        }
      ],
      
      // Global overlay settings
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
    
    this.currentSettings = {};
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
      
      // Migrate old settings format if needed
      if (settings.itemIdSelector && !settings.siteConfigs) {
        settings.siteConfigs = [{
          id: 'migrated',
          name: 'Migrated Configuration',
          urlPattern: '.*',
          enabled: true,
          itemIdSelector: settings.itemIdSelector,
          statusIdSelector: settings.statusIdSelector,
          apiUrlPattern: settings.apiUrlPattern
        }];
        delete settings.itemIdSelector;
        delete settings.statusIdSelector;
        delete settings.apiUrlPattern;
      }
      
      // Ensure siteConfigs exists
      if (!settings.siteConfigs) {
        settings.siteConfigs = this.defaultSettings.siteConfigs;
      }
      
      this.currentSettings = settings;
      
      // Populate form fields (non-site-config fields)
      Object.keys(settings).forEach(key => {
        if (key !== 'siteConfigs') {
          const element = document.getElementById(key);
          if (element) {
            if (element.type === 'checkbox') {
              element.checked = settings[key];
            } else {
              element.value = settings[key];
            }
          }
        }
      });
      
      // Render site configurations
      this.renderSiteConfigs();
      
      console.log('Settings loaded:', settings);
    } catch (error) {
      console.error('Error loading settings:', error);
      this.showStatus('Error loading settings', 'error');
    }
  }

  async saveSettings() {
    try {
      const settings = { ...this.currentSettings };
      
      // Collect non-site-config form values
      Object.keys(this.defaultSettings).forEach(key => {
        if (key !== 'siteConfigs') {
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
        }
      });
      
      // Collect site configurations
      settings.siteConfigs = this.collectSiteConfigs();

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

  async testCurrentSite() {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      const currentUrl = tab.url;
      
      // Find matching site config
      const matchingConfig = this.currentSettings.siteConfigs.find(config => {
        try {
          const pattern = new RegExp(config.urlPattern);
          return pattern.test(currentUrl) && config.enabled;
        } catch (e) {
          return false;
        }
      });
      
      const resultDiv = document.getElementById('siteTestResult');
      
      if (!matchingConfig) {
        resultDiv.innerHTML = '<div class="test-results error">No matching site configuration found for current URL</div>';
        return;
      }
      
      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: (item, status, api, url, configName) => {
          const itemEl = document.querySelector(item);
          const statusEl = document.querySelector(status);
          
          return {
            url: url,
            itemFound: !!itemEl,
            statusFound: !!statusEl,
            itemEl: itemEl ? itemEl.tagName + (itemEl.id ? '#' + itemEl.id : '') : null,
            statusEl: statusEl ? statusEl.tagName + (statusEl.id ? '#' + statusEl.id : '') : null,
            configName: configName
          };
        },
        args: [matchingConfig.itemIdSelector, matchingConfig.statusIdSelector, matchingConfig.apiUrlPattern, currentUrl, matchingConfig.name]
      });

      const result = results[0].result;
      
      let html = '<div class="test-results">';
      html += `<div class="test-item info">Config: ${result.configName}</div>`;
      html += `<div class="test-item info">URL: ${currentUrl}</div>`;
      html += `<div class="test-item ${result.itemFound ? 'success' : 'error'}">`;  
      html += `Item Selector: ${result.itemFound ? '✓ Found' : '✗ Not found'} ${result.itemEl || ''}`;
      html += '</div>';
      html += `<div class="test-item ${result.statusFound ? 'success' : 'error'}">`;  
      html += `Status Selector: ${result.statusFound ? '✓ Found' : '✗ Not found'} ${result.statusEl || ''}`;
      html += '</div>';
      html += '</div>';
      
      resultDiv.innerHTML = html;
      
    } catch (error) {
      console.error('Error testing current site:', error);
      document.getElementById('siteTestResult').innerHTML = 
        '<div class="test-results error">Error testing current site. Make sure you\'re on a valid web page.</div>';
    }
  }

  resetSettings() {
    if (confirm('Are you sure you want to reset all settings to defaults? This cannot be undone.')) {
      Object.keys(this.defaultSettings).forEach(key => {
        if (key !== 'siteConfigs') {
          const element = document.getElementById(key);
          if (element) {
            if (element.type === 'checkbox') {
              element.checked = this.defaultSettings[key];
            } else {
              element.value = this.defaultSettings[key];
            }
          }
        }
      });
      
      this.currentSettings.siteConfigs = [...this.defaultSettings.siteConfigs];
      this.renderSiteConfigs();
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
        
        // Validate and merge settings
        const validSettings = { ...this.defaultSettings };
        Object.keys(this.defaultSettings).forEach(key => {
          if (settings.hasOwnProperty(key)) {
            validSettings[key] = settings[key];
          }
        });
        
        this.currentSettings = validSettings;
        
        // Populate form
        Object.keys(validSettings).forEach(key => {
          if (key !== 'siteConfigs') {
            const element = document.getElementById(key);
            if (element) {
              if (element.type === 'checkbox') {
                element.checked = validSettings[key];
              } else {
                element.value = validSettings[key];
              }
            }
          }
        });
        
        this.renderSiteConfigs();
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
      case 'scan': audioFile = 'audio/scan.wav'; break;
      case 'success': audioFile = 'audio/taskCompleted.wav'; break;
      case 'error': audioFile = 'audio/error.wav'; break;
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

  renderSiteConfigs() {
    const container = document.getElementById('siteConfigsList');
    container.innerHTML = '';
    
    this.currentSettings.siteConfigs.forEach((config, index) => {
      const configDiv = this.createSiteConfigElement(config, index);
      container.appendChild(configDiv);
    });
  }
  
  createSiteConfigElement(config, index) {
    const div = document.createElement('div');
    div.className = 'site-config-item';
    div.innerHTML = `
      <div class="site-config-header">
        <div>
          <h4 class="site-config-title">${config.name}<span class="site-config-status ${config.enabled ? 'active' : 'inactive'}">${config.enabled ? 'Active' : 'Inactive'}</span></h4>
          <p class="site-config-url">${config.urlPattern}</p>
        </div>
        <div class="site-config-actions">
          <button class="btn-small toggle-collapse" data-index="${index}">Edit</button>
          <button class="btn-small btn-danger" onclick="settingsManager.removeSiteConfig(${index})">Delete</button>
        </div>
      </div>
      <div class="site-config-form">
        <div class="form-row">
          <div class="input-group">
            <label>Configuration Name:</label>
            <input type="text" data-config="name" data-index="${index}" value="${config.name}">
          </div>
          <div class="input-group">
            <label class="checkbox-label">
              <input type="checkbox" data-config="enabled" data-index="${index}" ${config.enabled ? 'checked' : ''}>
              <span class="checkmark"></span>
              Enabled
            </label>
          </div>
        </div>
        <div class="form-row full-width">
          <div class="input-group">
            <label>URL Pattern (Regex):</label>
            <input type="text" data-config="urlPattern" data-index="${index}" value="${config.urlPattern}" placeholder="https://example\\.com/.*">
            <span class="input-help">Regular expression to match URLs where this config should be active</span>
          </div>
        </div>
        <div class="form-row">
          <div class="input-group">
            <label>Item ID Selector:</label>
            <input type="text" data-config="itemIdSelector" data-index="${index}" value="${config.itemIdSelector}" placeholder="#product-scan">
          </div>
          <div class="input-group">
            <label>Status ID Selector:</label>
            <input type="text" data-config="statusIdSelector" data-index="${index}" value="${config.statusIdSelector}" placeholder="#status-scan">
          </div>
        </div>
        <div class="form-row full-width">
          <div class="input-group">
            <label>API URL Pattern:</label>
            <input type="text" data-config="apiUrlPattern" data-index="${index}" value="${config.apiUrlPattern}" placeholder="/api/scan">
            <span class="input-help">URL pattern to intercept for API calls</span>
          </div>
        </div>
      </div>
    `;
    
    // Add event listeners
    const toggleButton = div.querySelector('.toggle-collapse');
    toggleButton.addEventListener('click', () => {
      div.classList.toggle('site-config-collapsed');
      toggleButton.textContent = div.classList.contains('site-config-collapsed') ? 'Edit' : 'Collapse';
    });
    
    // Add input change listeners
    div.querySelectorAll('input').forEach(input => {
      input.addEventListener('change', () => {
        this.updateSiteConfigPreview(index);
      });
    });
    
    // Start collapsed
    div.classList.add('site-config-collapsed');
    
    return div;
  }
  
  addSiteConfig() {
    const newConfig = {
      id: 'config_' + Date.now(),
      name: 'New Site Configuration',
      urlPattern: 'https://example\\.com/.*',
      enabled: true,
      itemIdSelector: '#product-scan',
      statusIdSelector: '#status-scan',
      apiUrlPattern: '/api/scan'
    };
    
    this.currentSettings.siteConfigs.push(newConfig);
    this.renderSiteConfigs();
    
    // Expand the newly added config
    const configs = document.querySelectorAll('.site-config-item');
    const newConfigDiv = configs[configs.length - 1];
    newConfigDiv.classList.remove('site-config-collapsed');
    newConfigDiv.querySelector('.toggle-collapse').textContent = 'Collapse';
  }
  
  removeSiteConfig(index) {
    if (confirm('Are you sure you want to delete this site configuration?')) {
      this.currentSettings.siteConfigs.splice(index, 1);
      this.renderSiteConfigs();
    }
  }
  
  updateSiteConfigPreview(index) {
    const configDiv = document.querySelectorAll('.site-config-item')[index];
    const nameInput = configDiv.querySelector('[data-config="name"]');
    const urlInput = configDiv.querySelector('[data-config="urlPattern"]');
    const enabledInput = configDiv.querySelector('[data-config="enabled"]');
    
    const title = configDiv.querySelector('.site-config-title');
    const url = configDiv.querySelector('.site-config-url');
    
    title.innerHTML = `${nameInput.value}<span class="site-config-status ${enabledInput.checked ? 'active' : 'inactive'}">${enabledInput.checked ? 'Active' : 'Inactive'}</span>`;
    url.textContent = urlInput.value;
  }
  
  collectSiteConfigs() {
    const configs = [];
    document.querySelectorAll('.site-config-item').forEach((div, index) => {
      const config = {
        id: this.currentSettings.siteConfigs[index]?.id || 'config_' + Date.now(),
        name: div.querySelector('[data-config="name"]').value,
        urlPattern: div.querySelector('[data-config="urlPattern"]').value,
        enabled: div.querySelector('[data-config="enabled"]').checked,
        itemIdSelector: div.querySelector('[data-config="itemIdSelector"]').value,
        statusIdSelector: div.querySelector('[data-config="statusIdSelector"]').value,
        apiUrlPattern: div.querySelector('[data-config="apiUrlPattern"]').value
      };
      configs.push(config);
    });
    return configs;
  }

  bindEvents() {
    // Save settings
    document.getElementById('saveSettings').addEventListener('click', () => {
      this.saveSettings();
    });

    // Test current site
    document.getElementById('testCurrentSite').addEventListener('click', () => {
      this.testCurrentSite();
    });
    
    // Add site configuration
    document.getElementById('addSiteConfig').addEventListener('click', () => {
      this.addSiteConfig();
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

// Make settingsManager available globally for onclick handlers
let settingsManager;

// Initialize settings manager when page loads
document.addEventListener('DOMContentLoaded', () => {
  settingsManager = new SettingsManager();
});