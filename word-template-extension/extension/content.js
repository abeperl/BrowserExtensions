// Content Script for Word Template Extension
// Handles advanced data extraction and page interaction

// Prevent redeclaration if script is loaded multiple times
if (typeof ContentScriptExtractor === 'undefined') {

class ContentScriptExtractor {
  constructor() {
    this.isInitialized = false;
    this.defaultPatterns = {
      emails: /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g,
      phones: /(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})/g,
      currencies: /\$[\d,]+\.?\d*/g,
      dates: /\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b/g,
      urls: /https?:\/\/[^\s]+/g,
      zipCodes: /\b\d{5}(-\d{4})?\b/g,
      socialSecurity: /\b\d{3}-\d{2}-\d{4}\b/g,
      creditCards: /\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b/g
    };
    this.extractionPatterns = {};
    this.init();
  }

  async init() {
    if (this.isInitialized) return;
    
    await this.loadExtractionPatterns();
    this.setupMessageListener();
    this.setupSelectionHandler();
    this.injectStyles();
    
    this.isInitialized = true;
  }

  async loadExtractionPatterns() {
    try {
      // Use callback-style chrome.storage.local.get for better compatibility
      const result = await new Promise((resolve) => {
        chrome.storage.local.get(['wordTemplateSettings'], resolve);
      });
      
      const settings = result.wordTemplateSettings || {};
      
      // Parse custom patterns if they exist
      if (settings.customPatterns && settings.customPatterns.trim()) {
        this.extractionPatterns = this.parseCustomPatterns(settings.customPatterns);
        console.log('Using custom extraction patterns:', Object.keys(this.extractionPatterns));
        console.log('Custom patterns loaded:', this.extractionPatterns);
      } else {
        // Use default patterns if no custom patterns
        this.extractionPatterns = { ...this.defaultPatterns };
        console.log('Using default extraction patterns:', Object.keys(this.extractionPatterns));
        console.log('No custom patterns found in settings:', settings);
      }
    } catch (error) {
      console.error('Error loading extraction patterns:', error);
      this.extractionPatterns = { ...this.defaultPatterns };
    }
  }

  parseCustomPatterns(customPatternsText) {
    const patterns = {};
    const lines = customPatternsText.split('\n').filter(line => line.trim());
    
    lines.forEach(line => {
      const colonIndex = line.indexOf(':');
      if (colonIndex > 0) {
        const fieldName = line.substring(0, colonIndex).trim().toLowerCase();
        const patternText = line.substring(colonIndex + 1).trim();
        
        try {
          // Parse regex pattern with flags
          const lastSlash = patternText.lastIndexOf('/');
          if (patternText.startsWith('/') && lastSlash > 0) {
            const pattern = patternText.substring(1, lastSlash);
            const flags = patternText.substring(lastSlash + 1);
            patterns[fieldName] = new RegExp(pattern, flags);
          } else {
            // Treat as literal pattern with global flag
            patterns[fieldName] = new RegExp(patternText, 'g');
          }
        } catch (error) {
          console.warn(`Invalid regex pattern for ${fieldName}:`, error);
        }
      }
    });
    
    return patterns;
  }

  setupMessageListener() {
    chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
      switch (message.action) {
        case 'extractData':
          this.performExtraction().then(data => {
            sendResponse({ success: true, data });
          }).catch(error => {
            sendResponse({ success: false, error: error.message });
          });
          return true; // Keep channel open for async response
          
        case 'highlightData':
          this.highlightExtractableData();
          sendResponse({ success: true });
          break;
          
        case 'clearHighlights':
          this.clearHighlights();
          sendResponse({ success: true });
          break;
          
        case 'extractSelection':
          const selection = this.getSelectedText();
          sendResponse({ success: true, data: { selectedText: selection } });
          break;
          
        case 'reloadPatterns':
          this.loadExtractionPatterns().then(() => {
            sendResponse({ success: true });
          }).catch(error => {
            sendResponse({ success: false, error: error.message });
          });
          return true;
          
        case 'ping':
          sendResponse({ success: true, message: 'Content script is alive' });
          break;
      }
    });
  }

  setupSelectionHandler() {
    document.addEventListener('mouseup', () => {
      const selection = this.getSelectedText();
      if (selection && selection.length > 10) {
        this.showSelectionTooltip(selection);
      } else {
        this.hideSelectionTooltip();
      }
    });

    document.addEventListener('mousedown', () => {
      this.hideSelectionTooltip();
    });
  }

  async performExtraction() {
    const data = {
      pageInfo: this.extractPageInfo(),
      textData: this.extractTextPatterns(),
      structuredData: this.extractStructuredData(),
      metadata: this.extractMetadata()
    };

    // Flatten the data structure for easier template replacement
    const flattenedData = {
      ...data.pageInfo,
      ...data.textData,
      ...data.structuredData,
      ...data.metadata
    };

    return flattenedData;
  }

  extractPageInfo() {
    return {
      title: document.title,
      url: window.location.href,
      domain: window.location.hostname,
      timestamp: new Date().toISOString(),
      extractionDate: new Date().toLocaleDateString(),
      extractionTime: new Date().toLocaleTimeString()
    };
  }

  extractTextPatterns() {
    const pageText = this.getCleanPageText();
    const data = {};

    Object.entries(this.extractionPatterns).forEach(([key, pattern]) => {
      const matches = pageText.match(pattern);
      if (matches) {
        // Remove duplicates and clean up
        const cleanMatches = [...new Set(matches)]
          .map(match => match.trim())
          .filter(match => match.length > 0)
          .slice(0, 10); // Limit to 10 items
        
        if (cleanMatches.length > 0) {
          data[key] = cleanMatches;
        }
      }
    });

    // Extract selected text if any
    const selection = this.getSelectedText();
    if (selection) {
      data.selectedText = selection;
    }

    return data;
  }

  extractStructuredData() {
    const data = {};

    // Extract tables
    const tables = document.querySelectorAll('table');
    if (tables.length > 0) {
      data.tables = this.extractTableData(tables);
    }

    // Extract lists
    const lists = document.querySelectorAll('ul, ol');
    if (lists.length > 0) {
      data.lists = this.extractListData(lists);
    }

    // Extract headings
    const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
    if (headings.length > 0) {
      data.headings = Array.from(headings)
        .map(h => ({
          level: h.tagName.toLowerCase(),
          text: h.textContent.trim()
        }))
        .filter(h => h.text.length > 0)
        .slice(0, 10);
    }

    // Extract forms
    const forms = document.querySelectorAll('form');
    if (forms.length > 0) {
      data.forms = this.extractFormData(forms);
    }

    return data;
  }

  extractMetadata() {
    const data = {};

    // Meta tags
    const metaTags = document.querySelectorAll('meta[name], meta[property]');
    metaTags.forEach(meta => {
      const name = meta.getAttribute('name') || meta.getAttribute('property');
      const content = meta.getAttribute('content');
      if (name && content) {
        data[`meta_${name}`] = content;
      }
    });

    // Open Graph data
    const ogTags = document.querySelectorAll('meta[property^="og:"]');
    ogTags.forEach(meta => {
      const property = meta.getAttribute('property').replace('og:', '');
      const content = meta.getAttribute('content');
      if (content) {
        data[`og_${property}`] = content;
      }
    });

    // JSON-LD structured data
    const jsonLdScripts = document.querySelectorAll('script[type="application/ld+json"]');
    if (jsonLdScripts.length > 0) {
      data.structuredData = [];
      jsonLdScripts.forEach((script, index) => {
        try {
          const jsonData = JSON.parse(script.textContent);
          data.structuredData.push(jsonData);
        } catch (e) {
          console.warn('Invalid JSON-LD data found:', e);
        }
      });
    }

    return data;
  }

  extractTableData(tables) {
    return Array.from(tables).slice(0, 3).map((table, index) => {
      const headers = Array.from(table.querySelectorAll('th')).map(th => th.textContent.trim());
      const rows = Array.from(table.querySelectorAll('tr')).slice(headers.length > 0 ? 1 : 0, 6).map(row =>
        Array.from(row.querySelectorAll('td, th')).map(cell => cell.textContent.trim())
      );
      
      return {
        tableIndex: index + 1,
        headers: headers,
        rows: rows,
        rowCount: rows.length
      };
    });
  }

  extractListData(lists) {
    return Array.from(lists).slice(0, 3).map((list, index) => {
      const items = Array.from(list.querySelectorAll('li')).map(li => li.textContent.trim());
      return {
        listIndex: index + 1,
        type: list.tagName.toLowerCase(),
        items: items.slice(0, 10),
        itemCount: items.length
      };
    });
  }

  extractFormData(forms) {
    return Array.from(forms).slice(0, 2).map((form, index) => {
      const fields = Array.from(form.querySelectorAll('input, select, textarea')).map(field => ({
        type: field.type || field.tagName.toLowerCase(),
        name: field.name || field.id,
        label: this.getFieldLabel(field),
        value: field.value,
        placeholder: field.placeholder
      }));

      return {
        formIndex: index + 1,
        action: form.action,
        method: form.method,
        fields: fields
      };
    });
  }

  getFieldLabel(field) {
    // Try to find associated label
    const id = field.id;
    if (id) {
      const label = document.querySelector(`label[for="${id}"]`);
      if (label) return label.textContent.trim();
    }

    // Look for parent label
    const parentLabel = field.closest('label');
    if (parentLabel) {
      return parentLabel.textContent.replace(field.value, '').trim();
    }

    // Look for preceding text
    const previousElement = field.previousElementSibling;
    if (previousElement && previousElement.textContent) {
      return previousElement.textContent.trim();
    }

    return field.name || field.placeholder || '';
  }

  getCleanPageText() {
    // Create a clone to avoid modifying the original document
    const clone = document.body.cloneNode(true);
    
    // Remove script and style elements
    const scripts = clone.querySelectorAll('script, style, noscript');
    scripts.forEach(el => el.remove());
    
    // Remove hidden elements
    const hiddenElements = clone.querySelectorAll('[style*="display: none"], [style*="visibility: hidden"]');
    hiddenElements.forEach(el => el.remove());

    return clone.innerText || clone.textContent || '';
  }

  getSelectedText() {
    const selection = window.getSelection();
    return selection.toString().trim();
  }

  highlightExtractableData() {
    this.clearHighlights();
    
    const pageText = document.body.innerHTML;
    let highlightedText = pageText;

    // Highlight emails
    highlightedText = highlightedText.replace(
      this.extractionPatterns.emails,
      '<span class="word-ext-highlight word-ext-email">$&</span>'
    );

    // Highlight phones
    highlightedText = highlightedText.replace(
      this.extractionPatterns.phones,
      '<span class="word-ext-highlight word-ext-phone">$&</span>'
    );

    // Highlight currencies
    highlightedText = highlightedText.replace(
      this.extractionPatterns.currencies,
      '<span class="word-ext-highlight word-ext-currency">$&</span>'
    );

    document.body.innerHTML = highlightedText;
  }

  clearHighlights() {
    const highlights = document.querySelectorAll('.word-ext-highlight');
    highlights.forEach(highlight => {
      const parent = highlight.parentNode;
      parent.replaceChild(document.createTextNode(highlight.textContent), highlight);
      parent.normalize();
    });
  }

  showSelectionTooltip(selectedText) {
    this.hideSelectionTooltip();

    const tooltip = document.createElement('div');
    tooltip.id = 'word-ext-selection-tooltip';
    tooltip.innerHTML = `
      <div class="word-ext-tooltip-content">
        <span>Extract selected text to Word template</span>
        <button id="word-ext-extract-selection">Extract</button>
      </div>
    `;

    document.body.appendChild(tooltip);

    // Position tooltip
    const selection = window.getSelection();
    if (selection.rangeCount > 0) {
      const range = selection.getRangeAt(0);
      const rect = range.getBoundingClientRect();
      tooltip.style.left = `${rect.left + window.scrollX}px`;
      tooltip.style.top = `${rect.bottom + window.scrollY + 5}px`;
    }

    // Add click handler
    document.getElementById('word-ext-extract-selection').addEventListener('click', () => {
      chrome.runtime.sendMessage({
        action: 'extractSelection',
        data: { selectedText }
      });
      this.hideSelectionTooltip();
    });

    // Auto-hide after 5 seconds
    setTimeout(() => this.hideSelectionTooltip(), 5000);
  }

  hideSelectionTooltip() {
    const tooltip = document.getElementById('word-ext-selection-tooltip');
    if (tooltip) {
      tooltip.remove();
    }
  }

  injectStyles() {
    if (document.getElementById('word-ext-styles')) return;

    const style = document.createElement('style');
    style.id = 'word-ext-styles';
    style.textContent = `
      .word-ext-highlight {
        background-color: rgba(255, 255, 0, 0.3) !important;
        border-radius: 2px !important;
        padding: 1px 2px !important;
      }
      
      .word-ext-email {
        background-color: rgba(0, 123, 255, 0.2) !important;
      }
      
      .word-ext-phone {
        background-color: rgba(40, 167, 69, 0.2) !important;
      }
      
      .word-ext-currency {
        background-color: rgba(255, 193, 7, 0.2) !important;
      }
      
      #word-ext-selection-tooltip {
        position: absolute;
        z-index: 10000;
        background: #333;
        color: white;
        padding: 8px 12px;
        border-radius: 4px;
        font-size: 12px;
        font-family: Arial, sans-serif;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        max-width: 200px;
      }
      
      .word-ext-tooltip-content {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      
      #word-ext-extract-selection {
        background: #007bff;
        color: white;
        border: none;
        padding: 4px 8px;
        border-radius: 2px;
        cursor: pointer;
        font-size: 11px;
      }
      
      #word-ext-extract-selection:hover {
        background: #0056b3;
      }
    `;

    document.head.appendChild(style);
  }
}

// Initialize content script only if not already initialized
if (!window.wordExtContentScript) {
  try {
    window.wordExtContentScript = new ContentScriptExtractor();
    console.log('Word Template Extension content script initialized');
  } catch (error) {
    console.error('Failed to initialize content script:', error);
  }
}

} // End of ContentScriptExtractor declaration guard
