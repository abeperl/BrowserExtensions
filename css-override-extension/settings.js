// Settings page script for CSS Override Extension
class SettingsManager {
  constructor() {
    this.uploadedFiles = new Map();
    this.init();
  }

  async init() {
    this.bindEvents();
    await this.loadSettings();
    await this.loadUploadedFiles();
    this.displayUploadedFiles();
  }

  bindEvents() {
    // Extension enable/disable toggle
    document.getElementById('extensionEnabled').addEventListener('change', (e) => {
      this.saveSetting('extensionEnabled', e.target.checked);
    });

    // Notifications toggle
    document.getElementById('showNotifications').addEventListener('change', (e) => {
      this.saveSetting('showNotifications', e.target.checked);
    });

    // Export rules
    document.getElementById('exportRules').addEventListener('click', () => {
      this.exportRules();
    });

    // Import rules
    document.getElementById('importRules').addEventListener('click', () => {
      document.getElementById('importFile').click();
    });

    document.getElementById('importFile').addEventListener('change', (e) => {
      this.importRules(e.target.files[0]);
    });

    // Clear all rules
    document.getElementById('clearAllRules').addEventListener('click', () => {
      this.clearAllRules();
    });

    // CSS file upload
    document.getElementById('uploadCssBtn').addEventListener('click', () => {
      document.getElementById('cssFileInput').click();
    });

    document.getElementById('cssFileInput').addEventListener('change', (e) => {
      this.handleCssFileUpload(e.target.files);
    });
  }

  async loadSettings() {
    try {
      const settings = await chrome.storage.local.get([
        'extensionEnabled',
        'showNotifications'
      ]);

      // Set default values
      const extensionEnabled = settings.extensionEnabled !== false;
      const showNotifications = settings.showNotifications !== false;

      document.getElementById('extensionEnabled').checked = extensionEnabled;
      document.getElementById('showNotifications').checked = showNotifications;

    } catch (error) {
      console.error('Failed to load settings:', error);
    }
  }

  async saveSetting(key, value) {
    try {
      await chrome.storage.local.set({ [key]: value });

      if (key === 'extensionEnabled') {
        // Notify background script of extension state change
        chrome.runtime.sendMessage({
          type: 'EXTENSION_STATE_CHANGED',
          enabled: value
        });
      }

      this.showNotification('Setting saved successfully!', 'success');
    } catch (error) {
      console.error('Failed to save setting:', error);
      this.showNotification('Failed to save setting', 'error');
    }
  }

  async exportRules() {
    try {
      // Get all rules from background script
      const response = await chrome.runtime.sendMessage({ type: 'GET_RULES' });

      if (!response.rules || Object.keys(response.rules).length === 0) {
        this.showNotification('No rules to export', 'info');
        return;
      }

      // Create export data with metadata
      const exportData = {
        version: '1.0',
        exportDate: new Date().toISOString(),
        rules: response.rules
      };

      // Create and download file
      const dataStr = JSON.stringify(exportData, null, 2);
      const dataBlob = new Blob([dataStr], { type: 'application/json' });
      const url = URL.createObjectURL(dataBlob);

      const link = document.createElement('a');
      link.href = url;
      link.download = `css-override-rules-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      this.showNotification('Rules exported successfully!', 'success');

    } catch (error) {
      console.error('Failed to export rules:', error);
      this.showNotification('Failed to export rules', 'error');
    }
  }

  async importRules(file) {
    if (!file) return;

    try {
      const text = await file.text();
      const importData = JSON.parse(text);

      if (!importData.rules) {
        throw new Error('Invalid import file format');
      }

      // Validate import data
      if (!confirm(`Import ${Object.keys(importData.rules).length} CSS rules? This will merge with existing rules.`)) {
        return;
      }

      // Import rules one by one
      let importedCount = 0;
      for (const [ruleId, rule] of Object.entries(importData.rules)) {
        try {
          await chrome.runtime.sendMessage({
            type: 'ADD_RULE',
            rule: rule
          });
          importedCount++;
        } catch (error) {
          console.error(`Failed to import rule ${ruleId}:`, error);
        }
      }

      this.showNotification(`Successfully imported ${importedCount} rules!`, 'success');

      // Refresh the popup if it's open
      chrome.runtime.sendMessage({ type: 'REFRESH_POPUP' });

    } catch (error) {
      console.error('Failed to import rules:', error);
      this.showNotification('Failed to import rules: ' + error.message, 'error');
    }

    // Reset file input
    document.getElementById('importFile').value = '';
  }

  async clearAllRules() {
    if (!confirm('Are you sure you want to delete ALL CSS rules? This action cannot be undone!')) {
      return;
    }

    try {
      // Get all rules
      const response = await chrome.runtime.sendMessage({ type: 'GET_RULES' });

      if (!response.rules || Object.keys(response.rules).length === 0) {
        this.showNotification('No rules to delete', 'info');
        return;
      }

      // Delete all rules
      let deletedCount = 0;
      for (const ruleId of Object.keys(response.rules)) {
        try {
          await chrome.runtime.sendMessage({
            type: 'DELETE_RULE',
            ruleId: ruleId
          });
          deletedCount++;
        } catch (error) {
          console.error(`Failed to delete rule ${ruleId}:`, error);
        }
      }

      this.showNotification(`Deleted ${deletedCount} rules`, 'success');

      // Refresh the popup if it's open
      chrome.runtime.sendMessage({ type: 'REFRESH_POPUP' });

    } catch (error) {
      console.error('Failed to clear rules:', error);
      this.showNotification('Failed to clear rules', 'error');
    }
  }

  async loadUploadedFiles() {
    try {
      const result = await chrome.storage.local.get(['uploadedCssFiles']);
      if (result.uploadedCssFiles) {
        this.uploadedFiles = new Map(Object.entries(result.uploadedCssFiles));
      }
    } catch (error) {
      console.error('Failed to load uploaded files:', error);
    }
  }

  async saveUploadedFiles() {
    try {
      const filesObject = Object.fromEntries(this.uploadedFiles);
      await chrome.storage.local.set({ uploadedCssFiles: filesObject });
    } catch (error) {
      console.error('Failed to save uploaded files:', error);
    }
  }

  async handleCssFileUpload(files) {
    if (!files || files.length === 0) return;

    const fileList = document.getElementById('fileList');
    fileList.innerHTML = '';

    let uploadedCount = 0;

    for (const file of files) {
      if (file.type !== 'text/css' && !file.name.endsWith('.css')) {
        this.showNotification(`Skipping ${file.name}: Not a CSS file`, 'warning');
        continue;
      }

      try {
        const content = await file.text();
        const fileId = Date.now() + '_' + Math.random().toString(36).substr(2, 9);

        const fileData = {
          id: fileId,
          name: file.name,
          content: content,
          size: file.size,
          uploadedAt: new Date().toISOString()
        };

        this.uploadedFiles.set(fileId, fileData);
        uploadedCount++;

        // Show file in list
        const fileItem = document.createElement('div');
        fileItem.className = 'file-item';
        fileItem.innerHTML = `
          <span class="file-name">${file.name}</span>
          <span class="file-size">(${this.formatFileSize(file.size)})</span>
          <span class="file-status">✓ Uploaded</span>
        `;
        fileList.appendChild(fileItem);

      } catch (error) {
        console.error(`Failed to read file ${file.name}:`, error);
        this.showNotification(`Failed to upload ${file.name}`, 'error');
      }
    }

    if (uploadedCount > 0) {
      await this.saveUploadedFiles();
      this.displayUploadedFiles();
      this.showNotification(`Successfully uploaded ${uploadedCount} CSS file(s)!`, 'success');

      // Notify background script to refresh
      chrome.runtime.sendMessage({ type: 'CSS_FILES_UPDATED' });
    }

    // Reset file input
    document.getElementById('cssFileInput').value = '';
  }

  displayUploadedFiles() {
    const container = document.getElementById('uploadedFiles');
    container.innerHTML = '';

    if (this.uploadedFiles.size === 0) {
      container.innerHTML = '<p>No CSS files uploaded yet.</p>';
      return;
    }

    for (const [fileId, fileData] of this.uploadedFiles) {
      const fileElement = document.createElement('div');
      fileElement.className = 'uploaded-file-item';
      fileElement.innerHTML = `
        <div class="file-info">
          <strong>${fileData.name}</strong>
          <span class="file-size">${this.formatFileSize(fileData.size)}</span>
          <span class="file-date">${new Date(fileData.uploadedAt).toLocaleDateString()}</span>
        </div>
        <div class="file-actions">
          <button class="btn btn-small btn-preview" data-file-id="${fileId}">Preview</button>
          <button class="btn btn-small btn-delete" data-file-id="${fileId}">Delete</button>
        </div>
      `;

      // Add event listeners
      const previewBtn = fileElement.querySelector('.btn-preview');
      const deleteBtn = fileElement.querySelector('.btn-delete');

      previewBtn.addEventListener('click', () => {
        this.previewCssFile(fileId);
      });

      deleteBtn.addEventListener('click', () => {
        this.deleteCssFile(fileId);
      });

      container.appendChild(fileElement);
    }
  }

  previewCssFile(fileId) {
    const fileData = this.uploadedFiles.get(fileId);
    if (!fileData) return;

    // Create modal to show CSS content
    const modal = document.createElement('div');
    modal.className = 'css-preview-modal';
    modal.innerHTML = `
      <div class="modal-content">
        <div class="modal-header">
          <h3>CSS File Preview: ${fileData.name}</h3>
          <button class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <pre class="css-content">${this.escapeHtml(fileData.content)}</pre>
        </div>
      </div>
    `;

    // Add close functionality
    const closeBtn = modal.querySelector('.close-btn');
    closeBtn.addEventListener('click', () => {
      document.body.removeChild(modal);
    });

    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        document.body.removeChild(modal);
      }
    });

    document.body.appendChild(modal);
  }

  async deleteCssFile(fileId) {
    const fileData = this.uploadedFiles.get(fileId);
    if (!fileData) return;

    if (!confirm(`Delete CSS file "${fileData.name}"? This action cannot be undone.`)) {
      return;
    }

    this.uploadedFiles.delete(fileId);
    await this.saveUploadedFiles();
    this.displayUploadedFiles();
    this.showNotification('CSS file deleted successfully', 'success');

    // Notify background script
    chrome.runtime.sendMessage({ type: 'CSS_FILES_UPDATED' });
  }

  formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;

    // Add to page
    document.body.appendChild(notification);

    // Show notification
    setTimeout(() => {
      notification.classList.add('show');
    }, 100);

    // Hide after 3 seconds
    setTimeout(() => {
      notification.classList.remove('show');
      setTimeout(() => {
        document.body.removeChild(notification);
      }, 300);
    }, 3000);
  }
}

// Initialize settings when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new SettingsManager();
});