// Word Template Extension - Settings Page
class SettingsManager {
  constructor() {
    this.settings = {
      // Default field mappings
      fieldMappings: [
        { sourceField: 'emails[0]', placeholder: 'PRIMARY_EMAIL', transform: 'none' },
        { sourceField: 'phones[0]', placeholder: 'PRIMARY_PHONE', transform: 'none' },
        { sourceField: 'currencies[0]', placeholder: 'AMOUNT', transform: 'none' },
        { sourceField: 'pageTitle', placeholder: 'TITLE', transform: 'none' },
        { sourceField: 'pageUrl', placeholder: 'URL', transform: 'none' },
        { sourceField: 'selectedText', placeholder: 'SELECTED_TEXT', transform: 'none' }
      ],
      
      // Array handling options
      arrayHandling: 'first', // first, join_comma, join_space, join_newline, count
      textTransform: 'none', // none, uppercase, lowercase, capitalize, sentence
      preserveEmptyPlaceholders: false,
      
      // Existing settings
      templatePath: '',
      outputPath: '',
      defaultTemplate: '',
      autoOpen: true,
      includeMetadata: true,
      maxFileSize: 50,
      
      // Data extraction settings
      extractEmails: true,
      extractPhones: true,
      extractCurrency: true,
      extractDates: true,
      customPatterns: '',
      
      // Advanced settings
      logLevel: 'INFO',
      dateFormat: 'YYYY-MM-DD',
      debugMode: false
    };
    
    this.nativeHostManager = new NativeHostManager();
    this.init();
  }

  async init() {
    await this.loadSettings();
    this.setupEventListeners();
    this.renderMappings();
    this.updateUI();
    this.checkConnection();
  }

  setupEventListeners() {
    // Field mapping events
    document.getElementById('addMapping').addEventListener('click', () => this.addMapping());
    
    // Settings form events
    document.getElementById('arrayHandling').addEventListener('change', (e) => {
      this.settings.arrayHandling = e.target.value;
      this.saveSettings();
    });
    
    document.getElementById('textTransform').addEventListener('change', (e) => {
      this.settings.textTransform = e.target.value;
      this.saveSettings();
    });
    
    document.getElementById('preserveEmptyPlaceholders').addEventListener('change', (e) => {
      this.settings.preserveEmptyPlaceholders = e.target.checked;
      this.saveSettings();
    });
    
    // Other setting controls
    const settingControls = [
      'autoOpen', 'includeMetadata', 'maxFileSize', 'extractEmails', 
      'extractPhones', 'extractCurrency', 'extractDates', 'customPatterns',
      'logLevel', 'dateFormat', 'debugMode'
    ];
    
    settingControls.forEach(id => {
      const element = document.getElementById(id);
      if (element) {
        const eventType = element.type === 'checkbox' ? 'change' : 'input';
        element.addEventListener(eventType, (e) => {
          const value = element.type === 'checkbox' ? e.target.checked : e.target.value;
          this.settings[id] = value;
          this.saveSettings();
        });
      }
    });
    
    // Action buttons
    document.getElementById('saveSettings').addEventListener('click', () => this.saveSettings());
    document.getElementById('resetSettings').addEventListener('click', () => this.resetSettings());
    document.getElementById('exportSettings').addEventListener('click', () => this.exportSettings());
    document.getElementById('importSettings').addEventListener('click', () => this.importSettings());
    
    // Template management
    document.getElementById('refreshTemplates').addEventListener('click', () => this.loadTemplates());
    document.getElementById('openTemplateFolder').addEventListener('click', () => this.openTemplateFolder());
  }

  addMapping(sourceField = '', placeholder = '', transform = 'none') {
    const mapping = { sourceField, placeholder, transform };
    this.settings.fieldMappings.push(mapping);
    this.renderMappings();
    this.saveSettings();
  }

  removeMapping(index) {
    this.settings.fieldMappings.splice(index, 1);
    this.renderMappings();
    this.saveSettings();
  }

  renderMappings() {
    const container = document.getElementById('mappingList');
    container.innerHTML = '';

    // Add default message if no mappings
    if (this.settings.fieldMappings.length === 0) {
      container.innerHTML = '<div class="text-muted text-center py-lg">No custom mappings configured. Click "Add Mapping" to create one.</div>';
      return;
    }

    this.settings.fieldMappings.forEach((mapping, index) => {
      const mappingElement = document.createElement('div');
      mappingElement.className = 'card p-md';
      mappingElement.innerHTML = `
        <div class="grid grid-cols-4 gap-md items-end">
          <div class="form-group mb-0">
            <label class="form-label">Source Field</label>
            <select class="form-control mapping-source" data-index="${index}">
              <option value="">Select field...</option>
              <optgroup label="Basic Fields">
                <option value="pageTitle" ${mapping.sourceField === 'pageTitle' ? 'selected' : ''}>Page Title</option>
                <option value="pageUrl" ${mapping.sourceField === 'pageUrl' ? 'selected' : ''}>Page URL</option>
                <option value="selectedText" ${mapping.sourceField === 'selectedText' ? 'selected' : ''}>Selected Text</option>
                <option value="extractionDate" ${mapping.sourceField === 'extractionDate' ? 'selected' : ''}>Extraction Date</option>
              </optgroup>
              <optgroup label="Extracted Arrays">
                <option value="emails" ${mapping.sourceField === 'emails' ? 'selected' : ''}>All Emails</option>
                <option value="emails[0]" ${mapping.sourceField === 'emails[0]' ? 'selected' : ''}>First Email</option>
                <option value="phones" ${mapping.sourceField === 'phones' ? 'selected' : ''}>All Phones</option>
                <option value="phones[0]" ${mapping.sourceField === 'phones[0]' ? 'selected' : ''}>First Phone</option>
                <option value="currencies" ${mapping.sourceField === 'currencies' ? 'selected' : ''}>All Currencies</option>
                <option value="currencies[0]" ${mapping.sourceField === 'currencies[0]' ? 'selected' : ''}>First Currency</option>
                <option value="dates" ${mapping.sourceField === 'dates' ? 'selected' : ''}>All Dates</option>
                <option value="dates[0]" ${mapping.sourceField === 'dates[0]' ? 'selected' : ''}>First Date</option>
                <option value="urls" ${mapping.sourceField === 'urls' ? 'selected' : ''}>All URLs</option>
                <option value="urls[0]" ${mapping.sourceField === 'urls[0]' ? 'selected' : ''}>First URL</option>
                <option value="headings" ${mapping.sourceField === 'headings' ? 'selected' : ''}>All Headings</option>
                <option value="headings[0]" ${mapping.sourceField === 'headings[0]' ? 'selected' : ''}>First Heading</option>
                <option value="zipcodes" ${mapping.sourceField === 'zipcodes' ? 'selected' : ''}>All ZIP Codes</option>
                <option value="zipcodes[0]" ${mapping.sourceField === 'zipcodes[0]' ? 'selected' : ''}>First ZIP Code</option>
              </optgroup>
              ${this.getCustomPatternOptions(mapping.sourceField)}
            </select>
          </div>
          <div class="form-group mb-0">
            <label class="form-label">Placeholder Name</label>
            <input type="text" class="form-control mapping-placeholder" data-index="${index}" 
                   value="${mapping.placeholder}" placeholder="e.g., PRIMARY_EMAIL">
          </div>
          <div class="form-group mb-0">
            <label class="form-label">Transform</label>
            <select class="form-control mapping-transform" data-index="${index}">
              <option value="none" ${mapping.transform === 'none' ? 'selected' : ''}>None</option>
              <option value="uppercase" ${mapping.transform === 'uppercase' ? 'selected' : ''}>UPPERCASE</option>
              <option value="lowercase" ${mapping.transform === 'lowercase' ? 'selected' : ''}>lowercase</option>
              <option value="capitalize" ${mapping.transform === 'capitalize' ? 'selected' : ''}>Capitalize</option>
            </select>
          </div>
          <div class="form-group mb-0">
            <button class="btn btn-outline btn-sm remove-mapping" data-index="${index}">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
              Remove
            </button>
          </div>
        </div>
        <div class="mt-sm">
          <small class="text-muted">
            Template placeholder: <code>{{${mapping.placeholder || 'PLACEHOLDER_NAME'}}}</code>
          </small>
        </div>
      `;
      
      container.appendChild(mappingElement);
    });

    // Add event listeners for mapping controls
    container.querySelectorAll('.mapping-source').forEach(select => {
      select.addEventListener('change', (e) => {
        const index = parseInt(e.target.dataset.index);
        this.settings.fieldMappings[index].sourceField = e.target.value;
        this.saveSettings();
      });
    });

    container.querySelectorAll('.mapping-placeholder').forEach(input => {
      input.addEventListener('input', (e) => {
        const index = parseInt(e.target.dataset.index);
        this.settings.fieldMappings[index].placeholder = e.target.value.toUpperCase();
        this.saveSettings();
        
        // Update the preview
        const preview = e.target.closest('.card').querySelector('code');
        if (preview) {
          preview.textContent = `{{${e.target.value.toUpperCase() || 'PLACEHOLDER_NAME'}}}`;
        }
      });
    });

    container.querySelectorAll('.mapping-transform').forEach(select => {
      select.addEventListener('change', (e) => {
        const index = parseInt(e.target.dataset.index);
        this.settings.fieldMappings[index].transform = e.target.value;
        this.saveSettings();
      });
    });

    container.querySelectorAll('.remove-mapping').forEach(button => {
      button.addEventListener('click', (e) => {
        const index = parseInt(e.target.dataset.index);
        this.removeMapping(index);
      });
    });
  }

  async loadSettings() {
    try {
      const stored = await chrome.storage.local.get(['wordTemplateSettings']);
      if (stored.wordTemplateSettings) {
        this.settings = { ...this.settings, ...stored.wordTemplateSettings };
      }
    } catch (error) {
      console.error('Error loading settings:', error);
    }
  }

  async saveSettings() {
    try {
      await chrome.storage.local.set({ wordTemplateSettings: this.settings });
      
      // Notify all content scripts to reload their patterns
      const tabs = await chrome.tabs.query({});
      tabs.forEach(tab => {
        chrome.tabs.sendMessage(tab.id, { action: 'reloadPatterns' }).catch(() => {
          // Ignore errors for tabs that don't have content scripts
        });
      });
      
      this.showStatus('Settings saved successfully', 'success');
    } catch (error) {
      console.error('Error saving settings:', error);
      this.showStatus('Error saving settings: ' + error.message, 'error');
    }
  }

  updateUI() {
    // Update form controls with current settings
    Object.keys(this.settings).forEach(key => {
      const element = document.getElementById(key);
      if (element) {
        if (element.type === 'checkbox') {
          element.checked = this.settings[key];
        } else {
          element.value = this.settings[key];
        }
      }
    });
  }

  async checkConnection() {
    try {
      const response = await this.nativeHostManager.sendMessage({ action: 'ping' });
      const statusElement = document.getElementById('connectionStatus');
      
      if (response && response.success) {
        statusElement.innerHTML = `
          <div class="flex items-center gap-sm text-success">
            <div class="status-dot success"></div>
            <span>Connected - Native host is running</span>
          </div>`;
      } else {
        statusElement.innerHTML = `
          <div class="flex items-center gap-sm text-error">
            <div class="status-dot error"></div>
            <span>Disconnected - Check native host installation</span>
          </div>`;
      }
    } catch (error) {
      document.getElementById('connectionStatus').innerHTML = `
        <div class="flex items-center gap-sm text-error">
          <div class="status-dot error"></div>
          <span>Error: ${error.message}</span>
        </div>`;
    }
  }

  async loadTemplates() {
    try {
      const response = await this.nativeHostManager.sendMessage({ action: 'list_templates' });
      const templateList = document.getElementById('templateList');
      
      if (response && response.success && response.templates) {
        templateList.innerHTML = '';
        response.templates.forEach(template => {
          const templateElement = document.createElement('div');
          templateElement.className = 'flex items-center justify-between p-md border rounded';
          templateElement.innerHTML = `
            <div>
              <div class="font-medium">${template.name}</div>
              <div class="text-small text-muted">Size: ${Math.round(template.size / 1024)} KB</div>
            </div>
            <div class="flex gap-sm">
              <button class="btn btn-ghost btn-sm" onclick="window.open('${template.path}')">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M19 19H5V5h7V3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.11 0 2-.9 2-2v-7h-2v7zM14 3v2h3.59l-9.83 9.83 1.41 1.41L19 6.41V10h2V3h-7z"/>
                </svg>
                Open
              </button>
            </div>
          `;
          templateList.appendChild(templateElement);
        });
      } else {
        templateList.innerHTML = '<div class="text-muted text-center py-lg">No templates found</div>';
      }
    } catch (error) {
      document.getElementById('templateList').innerHTML = 
        `<div class="text-error text-center py-lg">Error loading templates: ${error.message}</div>`;
    }
  }

  resetSettings() {
    if (confirm('Are you sure you want to reset all settings to defaults? This cannot be undone.')) {
      chrome.storage.local.remove(['wordTemplateSettings']);
      location.reload();
    }
  }

  exportSettings() {
    const dataStr = JSON.stringify(this.settings, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'word-template-settings.json';
    link.click();
    URL.revokeObjectURL(url);
  }

  importSettings() {
    document.getElementById('importFile').click();
    document.getElementById('importFile').addEventListener('change', (e) => {
      const file = e.target.files[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = (e) => {
          try {
            const imported = JSON.parse(e.target.result);
            this.settings = { ...this.settings, ...imported };
            this.saveSettings();
            this.updateUI();
            this.renderMappings();
            this.showStatus('Settings imported successfully', 'success');
          } catch (error) {
            this.showStatus('Error importing settings: Invalid JSON file', 'error');
          }
        };
        reader.readAsText(file);
      }
    });
  }

  openTemplateFolder() {
    // This would need to be implemented in the native host
    this.nativeHostManager.sendMessage({ action: 'open_template_folder' });
  }

  getCustomPatternOptions(selectedField) {
    if (!this.settings.customPatterns) return '';
    
    const customPatterns = this.settings.customPatterns.split('\n').filter(line => line.trim());
    if (customPatterns.length === 0) return '';
    
    let options = '<optgroup label="Custom Patterns">';
    customPatterns.forEach(line => {
      const [fieldName] = line.split(':');
      if (fieldName) {
        const field = fieldName.toLowerCase();
        const selected = selectedField === field ? 'selected' : '';
        const selected0 = selectedField === `${field}[0]` ? 'selected' : '';
        options += `
          <option value="${field}" ${selected}>All ${fieldName}</option>
          <option value="${field}[0]" ${selected0}>First ${fieldName}</option>
        `;
      }
    });
    options += '</optgroup>';
    return options;
  }

  showStatus(message, type = 'info') {
    const status = document.getElementById('saveStatus');
    status.textContent = message;
    status.className = `status-indicator ${type}`;
    status.style.display = 'block';
    
    setTimeout(() => {
      status.style.display = 'none';
    }, 3000);
  }
}

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
  new SettingsManager();
});