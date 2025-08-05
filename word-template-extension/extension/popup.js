// Word Template Extension - Popup Script
class WordTemplateExtension {
  constructor() {
    this.templates = [];
    this.extractedData = {};
    this.settings = {
      autoOpen: true,
      includeMetadata: true,
      customFilename: ''
    };
    this.init();
  }

  async init() {
    await this.loadSettings();
    await this.loadTemplates();
    this.setupEventListeners();
    this.updateUI();
  }

  setupEventListeners() {
    // Main action buttons
    const extractBtn = document.getElementById('extract-btn');
    const generateBtn = document.getElementById('generate-btn');
    if (extractBtn) extractBtn.addEventListener('click', () => this.extractData());
    if (generateBtn) generateBtn.addEventListener('click', () => this.generateDocument());
    
    // Template management
    const templateSelect = document.getElementById('template-select');
    const refreshBtn = document.getElementById('refreshBtn');
    if (templateSelect) templateSelect.addEventListener('change', () => this.updateGenerateButton());
    if (refreshBtn) refreshBtn.addEventListener('click', () => this.loadTemplates());
    
    // Advanced options
    const toggleAdvanced = document.getElementById('toggle-advanced');
    const autoOpenDoc = document.getElementById('auto-open-doc');
    const includeMetadata = document.getElementById('include-metadata');
    const outputName = document.getElementById('output-name');
    
    if (toggleAdvanced) toggleAdvanced.addEventListener('click', () => this.toggleAdvancedOptions());
    if (autoOpenDoc) autoOpenDoc.addEventListener('change', (e) => this.updateSetting('autoOpen', e.target.checked));
    if (includeMetadata) includeMetadata.addEventListener('change', (e) => this.updateSetting('includeMetadata', e.target.checked));
    if (outputName) outputName.addEventListener('input', (e) => this.updateSetting('customFilename', e.target.value));
    
    // Footer buttons
    const settingsBtn = document.getElementById('settings-btn');
    const diagnosticsBtn = document.getElementById('diagnostics-btn');
    if (settingsBtn) settingsBtn.addEventListener('click', () => this.openSettings());
    if (diagnosticsBtn) diagnosticsBtn.addEventListener('click', () => this.openDiagnostics());
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.local.get(['extensionSettings']);
      if (result.extensionSettings) {
        this.settings = { ...this.settings, ...result.extensionSettings };
      }
      this.applySettings();
    } catch (error) {
      console.error('Error loading settings:', error);
    }
  }

  async saveSettings() {
    try {
      await chrome.storage.local.set({ extensionSettings: this.settings });
    } catch (error) {
      console.error('Error saving settings:', error);
    }
  }

  applySettings() {
    const autoOpenDoc = document.getElementById('auto-open-doc');
    const includeMetadata = document.getElementById('include-metadata');
    const outputName = document.getElementById('output-name');
    
    if (autoOpenDoc) autoOpenDoc.checked = this.settings.autoOpen;
    if (includeMetadata) includeMetadata.checked = this.settings.includeMetadata;
    if (outputName) outputName.value = this.settings.customFilename;
  }

  updateSetting(key, value) {
    this.settings[key] = value;
    this.saveSettings();
  }

  async loadTemplates() {
    try {
      this.showStatus('Loading templates...', 'info');
      const response = await this.sendNativeMessage({ action: 'list_templates' });
      
      console.log('Native host response:', response); // Debug logging
      
      if (response && response.success) {
        this.templates = Array.isArray(response.templates) ? response.templates : [];
        console.log('Templates loaded:', this.templates); // Debug logging
        this.updateTemplateSelect();
        this.showStatus(`Templates loaded successfully (${this.templates.length} found)`, 'success');
      } else {
        this.templates = [];
        this.updateTemplateSelect();
        const errorMsg = response?.message || 'No templates found. Please add templates to your templates folder.';
        this.showStatus(errorMsg, 'info');
      }
    } catch (error) {
      console.error('Error loading templates:', error);
      this.templates = [];
      this.updateTemplateSelect();
      this.showStatus('Error loading templates: ' + error.message, 'error');
    }
  }

  updateTemplateSelect() {
    const select = document.getElementById('template-select');
    if (!select) return;
    
    select.innerHTML = '';
    
    if (this.templates.length === 0) {
      select.innerHTML = '<option value="">No templates found</option>';
    } else {
      select.innerHTML = '<option value="">Select a template...</option>';
      this.templates.forEach(template => {
        const option = document.createElement('option');
        
        // Handle both string templates and template objects
        let templateName, templateValue;
        if (typeof template === 'string') {
          templateName = template;
          templateValue = template;
        } else if (template && template.name) {
          templateName = template.name;
          templateValue = template.name; // Use name as value for compatibility
        } else {
          templateName = String(template);
          templateValue = String(template);
        }
        
        option.value = templateValue;
        option.textContent = templateName.replace(/\.[^/.]+$/, ""); // Remove file extension for display
        select.appendChild(option);
      });
    }
    this.updateGenerateButton();
  }

  async extractData() {
    try {
      console.log('Extract button clicked'); // Debug
      this.showStatus('Extracting data from page...', 'info');
      
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      console.log('Current tab:', tab); // Debug
      
      if (!tab || !tab.id) {
        throw new Error('No active tab found');
      }
      
      // Try to communicate with content script, fallback if it fails
      let response = null;
      try {
        // Give content script time to initialize (it's auto-injected via manifest)
        await new Promise(resolve => setTimeout(resolve, 300));
        
        // Test if content script is responsive
        try {
          const pingResponse = await chrome.tabs.sendMessage(tab.id, { action: 'ping' });
          console.log('Content script ping response:', pingResponse);
        } catch (error) {
          console.log('Content script not responding to ping:', error.message);
          throw error; // Go to fallback
        }
        
        // Try to reload patterns first
        try {
          await chrome.tabs.sendMessage(tab.id, { action: 'reloadPatterns' });
          await new Promise(resolve => setTimeout(resolve, 100));
        } catch (error) {
          console.log('Could not reload patterns:', error.message);
        }
        
        // Try to extract data via content script
        response = await chrome.tabs.sendMessage(tab.id, { action: 'extractData' });
        
        if (response && response.success) {
          this.extractedData = response.data || {};
        } else {
          throw new Error('Content script returned no data or failed');
        }
      } catch (error) {
        // Fallback to inline extraction if content script fails
        console.warn('Content script extraction failed, using fallback:', error.message);
        try {
          this.extractedData = await this.fallbackExtraction(tab.id);
          console.log('Fallback extraction completed:', this.extractedData);
        } catch (fallbackError) {
          console.error('Fallback extraction also failed:', fallbackError);
          this.extractedData = {};
        }
      }
      
      console.log('Extracted data:', this.extractedData); // Debug
      
      // Debug: show what patterns were used
      const currentSettings = await chrome.storage.local.get(['wordTemplateSettings']);
      const settings = currentSettings.wordTemplateSettings || {};
      console.log('Current custom patterns:', settings.customPatterns);
        
      // Add metadata if enabled
      if (this.settings.includeMetadata) {
        this.extractedData.pageTitle = tab.title;
        this.extractedData.pageUrl = tab.url;
        this.extractedData.extractionDate = new Date().toLocaleDateString();
        this.extractedData.extractionTime = new Date().toLocaleTimeString();
      }
      
      this.updateDataPreview();
      this.updateGenerateButton();
      
      const dataCount = Object.keys(this.extractedData).length;
      if (dataCount > 0) {
        this.showStatus(`Data extracted successfully! Found ${dataCount} data points.`, 'success');
      } else {
        this.showStatus('No data found with current extraction patterns', 'info');
      }
    } catch (error) {
      console.error('Error extracting data:', error);
      this.showStatus('Error extracting data: ' + error.message, 'error');
    }
  }

  async fallbackExtraction(tabId) {
    try {
      console.log('Starting fallback extraction...');
      // Load current settings including custom patterns
      const currentSettings = await chrome.storage.local.get(['wordTemplateSettings']);
      const allSettings = { ...this.settings, ...(currentSettings.wordTemplateSettings || {}) };
      console.log('Fallback extraction settings:', allSettings);
      
      const results = await chrome.scripting.executeScript({
        target: { tabId: tabId },
        args: [allSettings],
        func: (settings) => {
          const extractionPatterns = {};
          
          // Add custom patterns from settings
          if (settings && settings.customPatterns) {
            const customPatterns = settings.customPatterns.split('\n').filter(line => line.trim());
            customPatterns.forEach(line => {
              try {
                const colonIndex = line.indexOf(':');
                if (colonIndex > 0) {
                  const fieldName = line.substring(0, colonIndex).trim().toLowerCase();
                  const patternText = line.substring(colonIndex + 1).trim();
                  
                  // Parse regex pattern with flags
                  const lastSlash = patternText.lastIndexOf('/');
                  if (patternText.startsWith('/') && lastSlash > 0) {
                    const pattern = patternText.substring(1, lastSlash);
                    const flags = patternText.substring(lastSlash + 1);
                    extractionPatterns[fieldName] = new RegExp(pattern, flags);
                  } else {
                    // Treat as literal pattern with global flag
                    extractionPatterns[fieldName] = new RegExp(patternText, 'g');
                  }
                }
              } catch (error) {
                console.warn('Invalid custom pattern:', line, error);
              }
            });
          } else {
            // Use default patterns if no custom patterns
            extractionPatterns.emails = /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g;
            extractionPatterns.phones = /(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})/g;
            extractionPatterns.currencies = /\$[\d,]+\.?\d*/g;
            extractionPatterns.dates = /\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b/g;
            extractionPatterns.urls = /https?:\/\/[^\s]+/g;
          }

          const data = {};
          const pageText = document.body.innerText;
          const pageHTML = document.body.innerHTML;
          
          console.log('Page text sample:', pageText.substring(0, 500));
          console.log('Page HTML sample:', pageHTML.substring(0, 500));
          
          // Extract patterns and clean up results
          Object.entries(extractionPatterns).forEach(([key, pattern]) => {
            console.log(`Testing pattern for ${key}:`, pattern);
            
            // Try both text and HTML content, handling capture groups
            let matches = [];
            
            // Test HTML first (for patterns with HTML tags)
            let match;
            pattern.lastIndex = 0; // Reset regex
            while ((match = pattern.exec(pageHTML)) !== null) {
              // If there are capture groups, use the first capture group, otherwise use full match
              const result = match[1] !== undefined ? match[1] : match[0];
              matches.push(result);
              if (!pattern.global) break; // Prevent infinite loop for non-global patterns
            }
            
            // If no HTML matches, try text content
            if (matches.length === 0) {
              pattern.lastIndex = 0; // Reset regex
              while ((match = pattern.exec(pageText)) !== null) {
                const result = match[1] !== undefined ? match[1] : match[0];
                matches.push(result);
                if (!pattern.global) break;
              }
            }
            
            console.log(`Matches for ${key}:`, matches);
            
            if (matches.length > 0) {
              const uniqueMatches = [...new Set(matches)].slice(0, 10);
              data[key] = uniqueMatches;
              console.log(`Added ${key}:`, uniqueMatches);
            } else {
              console.log(`No matches found for ${key} pattern`);
            }
          });

          // Extract selected text if any
          const selection = window.getSelection().toString().trim();
          if (selection) {
            data.selectedText = selection;
          }

          // Extract headings
          const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
          if (headings.length > 0) {
            data.headings = Array.from(headings)
              .map(h => h.textContent.trim())
              .filter(text => text.length > 0)
              .slice(0, 5);
          }

          // Extract tables
          const tables = document.querySelectorAll('table');
          if (tables.length > 0) {
            data.tables = Array.from(tables).slice(0, 2).map((table, index) => {
              const rows = Array.from(table.rows).slice(0, 5).map(row => 
                Array.from(row.cells).map(cell => cell.textContent.trim())
              );
              return { tableIndex: index + 1, data: rows };
            });
          }

          return data;
        }
      });
      
      return results && results[0] ? results[0].result : {};
    } catch (error) {
      console.error('Fallback extraction failed:', error);
      return {};
    }
  }

  // This function runs in the context of the webpage
  extractPageData() {
    const extractionPatterns = {
      emails: /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g,
      phones: /(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})/g,
      currencies: /\$[\d,]+\.?\d*/g,
      dates: /\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b/g,
      urls: /https?:\/\/[^\s]+/g,
      zipCodes: /\b\d{5}(-\d{4})?\b/g
    };

    const data = {};

    // Get all text content from the page
    const pageText = document.body.innerText;
    
    // Extract patterns and clean up results
    Object.entries(extractionPatterns).forEach(([key, pattern]) => {
      const matches = pageText.match(pattern);
      if (matches) {
        // Remove duplicates and limit to 10 items
        const uniqueMatches = [...new Set(matches)].slice(0, 10);
        if (uniqueMatches.length > 0) {
          data[key] = uniqueMatches;
        }
      }
    });

    // Extract selected text if any
    const selection = window.getSelection().toString().trim();
    if (selection) {
      data.selectedText = selection;
    }

    // Extract structured data
    const tables = document.querySelectorAll('table');
    if (tables.length > 0) {
      data.tables = Array.from(tables).slice(0, 2).map((table, index) => {
        const rows = Array.from(table.rows).slice(0, 5).map(row => 
          Array.from(row.cells).map(cell => cell.textContent.trim())
        );
        return { tableIndex: index + 1, data: rows };
      });
    }

    // Extract headings
    const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
    if (headings.length > 0) {
      data.headings = Array.from(headings)
        .map(h => h.textContent.trim())
        .filter(text => text.length > 0)
        .slice(0, 5);
    }

    // Extract company/business information
    const businessKeywords = ['company', 'corporation', 'LLC', 'Inc', 'Ltd', 'business'];
    const businessRegex = new RegExp(`\\b\\w+\\s+(${businessKeywords.join('|')})\\b`, 'gi');
    const businessMatches = pageText.match(businessRegex);
    if (businessMatches) {
      data.businessNames = [...new Set(businessMatches)].slice(0, 5);
    }

    return data;
  }

  updateDataPreview() {
    const preview = document.getElementById('data-preview');
    
    if (Object.keys(this.extractedData).length === 0) {
      preview.innerHTML = '<div style="color: #95a5a6; font-style: italic;">No data extracted yet</div>';
      return;
    }

    let html = '';
    Object.entries(this.extractedData).forEach(([key, value]) => {
      let displayValue;
      
      if (Array.isArray(value)) {
        if (value.length === 0) return;
        displayValue = value.length > 3 ? 
          `${value.slice(0, 3).join(', ')} ... (+${value.length - 3} more)` : 
          value.join(', ');
      } else if (typeof value === 'object') {
        displayValue = JSON.stringify(value).substring(0, 100) + '...';
      } else {
        displayValue = String(value).substring(0, 100);
      }
      
      html += `<div class="data-item"><strong>${this.formatKey(key)}:</strong> ${displayValue}</div>`;
    });
    
    preview.innerHTML = html;
  }

  formatKey(key) {
    return key.replace(/([A-Z])/g, ' $1')
              .replace(/^./, str => str.toUpperCase())
              .replace(/\b\w/g, str => str.toUpperCase());
  }

  updateGenerateButton() {
    const templateSelect = document.getElementById('template-select');
    const generateBtn = document.getElementById('generate-btn');
    
    if (!templateSelect || !generateBtn) return;
    
    const templateSelected = templateSelect.value;
    const hasData = Object.keys(this.extractedData).length > 0;
    
    generateBtn.disabled = !(templateSelected && hasData);
    
    if (!templateSelected && hasData) {
      generateBtn.title = 'Please select a template';
    } else if (templateSelected && !hasData) {
      generateBtn.title = 'Please extract data first';
    } else if (templateSelected && hasData) {
      generateBtn.title = 'Generate Word document';
    } else {
      generateBtn.title = 'Extract data and select template';
    }
  }

  async generateDocument() {
    const templateSelect = document.getElementById('template-select');
    if (!templateSelect) {
      this.showStatus('Template selector not found', 'error');
      return;
    }
    
    const template = templateSelect.value;
    if (!template) {
      this.showStatus('Please select a template', 'error');
      return;
    }

    if (Object.keys(this.extractedData).length === 0) {
      this.showStatus('Please extract data first', 'error');
      return;
    }

    try {
      this.showStatus('Generating document...', 'info');
      
      // Get current settings including field mappings
      const currentSettings = await chrome.storage.local.get(['wordTemplateSettings']);
      const fieldMappingSettings = currentSettings.wordTemplateSettings || {};
      
      const message = {
        action: 'update_template',
        data: {
          template: template,
          extractedData: this.extractedData,
          settings: { 
            ...this.settings,
            ...fieldMappingSettings
          }
        }
      };
      
      console.log('Data being sent to native host:', message.data.extractedData);

      const response = await this.sendNativeMessage(message);

      if (response && response.success) {
        this.showStatus(`Document generated successfully! Saved to: ${response.outputPath}`, 'success');
        
        // Track usage
        this.trackUsage('document_generated', { template: template });
        
      } else {
        this.showStatus('Error: ' + (response?.message || 'Unknown error occurred'), 'error');
      }
    } catch (error) {
      console.error('Error generating document:', error);
      this.showStatus('Error generating document: ' + error.message, 'error');
    }
  }

  async sendNativeMessage(message) {
    return new Promise((resolve, reject) => {
      try {
        console.log('Sending native message:', message);
        chrome.runtime.sendNativeMessage('com.wordtemplateextension.nativehost', message, response => {
          console.log('Native host response:', response);
          console.log('Chrome runtime last error:', chrome.runtime.lastError);
          
          if (chrome.runtime.lastError) {
            reject(new Error(`Native host error: ${chrome.runtime.lastError.message}`));
          } else if (!response) {
            reject(new Error('Native host returned no response'));
          } else {
            resolve(response);
          }
        });
      } catch (error) {
        console.error('Native message error:', error);
        reject(error);
      }
    });
  }

  toggleAdvancedOptions() {
    const options = document.getElementById('advanced-options');
    const toggle = document.getElementById('toggle-advanced');
    
    if (options && toggle) {
      if (options.style.display === 'none') {
        options.style.display = 'block';
        toggle.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor"><path d="M7.41 15.41L12 10.83l4.59 4.58L18 14l-6-6-6 6z"/></svg>';
      } else {
        options.style.display = 'none';
        toggle.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor"><path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/></svg>';
      }
    }
  }

  openSettings() {
    // Open settings page or modal
    chrome.tabs.create({ url: chrome.runtime.getURL('settings.html') });
  }

  openDiagnostics() {
    // Open diagnostics page
    chrome.tabs.create({ url: chrome.runtime.getURL('diagnostics.html') });
  }

  openHelp() {
    // Open help documentation
    chrome.tabs.create({ url: 'https://github.com/your-repo/word-template-extension/wiki' });
  }

  showStatus(message, type = 'info') {
    const status = document.getElementById('status');
    if (!status) {
      console.log('Status message:', message, type); // Fallback logging
      return;
    }
    
    status.textContent = message;
    status.className = `status-indicator ${type}`;
    status.style.display = 'block';
    
    // Auto-hide after 5 seconds for success/info, 8 seconds for errors
    const hideDelay = type === 'error' ? 8000 : 5000;
    setTimeout(() => {
      status.style.display = 'none';
    }, hideDelay);
  }

  async trackUsage(action, data = {}) {
    try {
      const usage = await chrome.storage.local.get(['usageStats']) || { usageStats: {} };
      const stats = usage.usageStats || {};
      
      const today = new Date().toDateString();
      if (!stats[today]) {
        stats[today] = {};
      }
      
      stats[today][action] = (stats[today][action] || 0) + 1;
      
      await chrome.storage.local.set({ usageStats: stats });
    } catch (error) {
      console.error('Error tracking usage:', error);
    }
  }

  updateUI() {
    // Update UI based on current state
    this.updateGenerateButton();
  }
}

// Initialize extension when popup opens
document.addEventListener('DOMContentLoaded', () => {
  new WordTemplateExtension();
});
