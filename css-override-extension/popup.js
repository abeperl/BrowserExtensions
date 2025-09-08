// Popup script for CSS Override Extension
class PopupManager {
  constructor() {
    this.rules = new Map();
    this.uploadedFiles = new Map();
    this.currentRuleId = null;
    this.init();
  }

  async init() {
    this.bindEvents();
    await this.loadRules();
    await this.loadUploadedFiles();
    this.renderRules();
  }

  bindEvents() {
    // Add rule button
    document.getElementById('addRuleBtn').addEventListener('click', () => {
      this.openModal();
    });

    // Modal events
    document.getElementById('closeModal').addEventListener('click', () => {
      this.closeModal();
    });

    document.getElementById('cancelBtn').addEventListener('click', () => {
      this.closeModal();
    });

    // Form submission
    document.getElementById('ruleForm').addEventListener('submit', (e) => {
      e.preventDefault();
      this.saveRule();
    });

    // Click outside modal to close
    document.getElementById('ruleModal').addEventListener('click', (e) => {
      if (e.target.id === 'ruleModal') {
        this.closeModal();
      }
    });

    // CSS source selection
    document.getElementById('cssSource').addEventListener('change', (e) => {
      this.handleCssSourceChange(e.target.value);
    });

    // Uploaded file selection
    document.getElementById('uploadedFileSelect').addEventListener('change', (e) => {
      this.handleUploadedFileChange(e.target.value);
    });
  }

  async loadRules() {
    try {
      const response = await chrome.runtime.sendMessage({ type: 'GET_RULES' });
      if (response.rules) {
        this.rules = new Map(Object.entries(response.rules));
      }
    } catch (error) {
      console.error('Failed to load rules:', error);
    }
  }

  renderRules() {
    const rulesList = document.getElementById('rulesList');
    const noRules = document.getElementById('noRules');

    if (this.rules.size === 0) {
      rulesList.innerHTML = '';
      noRules.style.display = 'block';
      return;
    }

    noRules.style.display = 'none';
    rulesList.innerHTML = '';

    for (const [ruleId, rule] of this.rules) {
      const ruleElement = this.createRuleElement(ruleId, rule);
      rulesList.appendChild(ruleElement);
    }
  }

  createRuleElement(ruleId, rule) {
    const ruleDiv = document.createElement('div');
    ruleDiv.className = 'rule-item';
    ruleDiv.setAttribute('data-rule-id', ruleId);

    ruleDiv.innerHTML = `
      <div class="rule-header">
        <div class="rule-info">
          <h3>${this.escapeHtml(rule.name)}</h3>
          <p class="url-pattern">${this.escapeHtml(rule.urlPattern)}</p>
        </div>
        <div class="rule-controls">
          <label class="toggle">
            <input type="checkbox" ${rule.enabled !== false ? 'checked' : ''} data-rule-id="${ruleId}">
            <span class="toggle-slider"></span>
          </label>
          <button class="btn btn-small btn-edit" data-rule-id="${ruleId}">Edit</button>
          <button class="btn btn-small btn-delete" data-rule-id="${ruleId}">Delete</button>
        </div>
      </div>
      <div class="rule-details">
        <span class="rule-mode">${rule.mode === 'replace' ? 'Replace' : 'Override'}</span>
        <span class="css-preview">${this.escapeHtml(rule.css.substring(0, 100))}${rule.css.length > 100 ? '...' : ''}</span>
      </div>
    `;

    // Bind events
    const toggle = ruleDiv.querySelector('input[type="checkbox"]');
    const editBtn = ruleDiv.querySelector('.btn-edit');
    const deleteBtn = ruleDiv.querySelector('.btn-delete');

    toggle.addEventListener('change', (e) => {
      this.toggleRule(ruleId, e.target.checked);
    });

    editBtn.addEventListener('click', () => {
      this.editRule(ruleId);
    });

    deleteBtn.addEventListener('click', () => {
      this.deleteRule(ruleId);
    });

    return ruleDiv;
  }

  openModal(ruleId = null) {
    this.currentRuleId = ruleId;
    const modal = document.getElementById('ruleModal');
    const modalTitle = document.getElementById('modalTitle');

    if (ruleId) {
      modalTitle.textContent = 'Edit CSS Rule';
      this.populateForm(ruleId);
    } else {
      modalTitle.textContent = 'Add CSS Rule';
      this.clearForm();
    }

    modal.style.display = 'block';
  }

  closeModal() {
    const modal = document.getElementById('ruleModal');
    modal.style.display = 'none';
    this.currentRuleId = null;
    this.clearForm();
  }

  populateForm(ruleId) {
    const rule = this.rules.get(ruleId);
    if (!rule) return;

    document.getElementById('urlPattern').value = rule.urlPattern;
    document.getElementById('ruleName').value = rule.name;
    document.getElementById('cssMode').value = rule.mode || 'override';
    document.getElementById('cssCode').value = rule.css;
    document.getElementById('ruleEnabled').checked = rule.enabled !== false;

    // Reset CSS source to custom by default
    document.getElementById('cssSource').value = 'custom';
    this.handleCssSourceChange('custom');
  }

  clearForm() {
    document.getElementById('ruleForm').reset();
    document.getElementById('ruleEnabled').checked = true;
    document.getElementById('uploadedFileGroup').style.display = 'none';
    document.getElementById('cssCode').style.display = 'block';
    document.getElementById('cssCode').required = true;
    document.getElementById('uploadedFileSelect').innerHTML = '<option value="">Choose a file...</option>';
  }

  async saveRule() {
    const ruleData = {
      urlPattern: document.getElementById('urlPattern').value.trim(),
      name: document.getElementById('ruleName').value.trim(),
      mode: document.getElementById('cssMode').value,
      css: document.getElementById('cssCode').value.trim(),
      enabled: document.getElementById('ruleEnabled').checked
    };

    try {
      if (this.currentRuleId) {
        await chrome.runtime.sendMessage({
          type: 'UPDATE_RULE',
          ruleId: this.currentRuleId,
          rule: ruleData
        });
      } else {
        await chrome.runtime.sendMessage({
          type: 'ADD_RULE',
          rule: ruleData
        });
      }

      await this.loadRules();
      this.renderRules();
      this.closeModal();

      // Refresh current tab to apply changes
      await chrome.runtime.sendMessage({ type: 'REFRESH_TAB' });

    } catch (error) {
      console.error('Failed to save rule:', error);
      alert('Failed to save rule: ' + error.message);
    }
  }

  async toggleRule(ruleId, enabled) {
    try {
      await chrome.runtime.sendMessage({
        type: 'UPDATE_RULE',
        ruleId: ruleId,
        rule: { enabled }
      });

      await this.loadRules();
      this.renderRules();

      // Refresh current tab to apply changes
      await chrome.runtime.sendMessage({ type: 'REFRESH_TAB' });

    } catch (error) {
      console.error('Failed to toggle rule:', error);
    }
  }

  editRule(ruleId) {
    this.openModal(ruleId);
  }

  async deleteRule(ruleId) {
    if (!confirm('Are you sure you want to delete this rule?')) {
      return;
    }

    try {
      await chrome.runtime.sendMessage({
        type: 'DELETE_RULE',
        ruleId: ruleId
      });

      await this.loadRules();
      this.renderRules();

      // Refresh current tab to apply changes
      await chrome.runtime.sendMessage({ type: 'REFRESH_TAB' });

    } catch (error) {
      console.error('Failed to delete rule:', error);
      alert('Failed to delete rule: ' + error.message);
    }
  }

  async loadUploadedFiles() {
    try {
      const response = await chrome.runtime.sendMessage({ type: 'GET_CSS_FILES' });
      if (response.files) {
        this.uploadedFiles = new Map(Object.entries(response.files));
      }
    } catch (error) {
      console.error('Failed to load uploaded files:', error);
    }
  }

  handleCssSourceChange(source) {
    const uploadedFileGroup = document.getElementById('uploadedFileGroup');
    const cssCodeTextarea = document.getElementById('cssCode');

    if (source === 'uploaded') {
      uploadedFileGroup.style.display = 'block';
      cssCodeTextarea.required = false;
      cssCodeTextarea.style.display = 'none';
      this.populateUploadedFiles();
    } else {
      uploadedFileGroup.style.display = 'none';
      cssCodeTextarea.required = true;
      cssCodeTextarea.style.display = 'block';
      cssCodeTextarea.value = '';
    }
  }

  populateUploadedFiles() {
    const select = document.getElementById('uploadedFileSelect');
    select.innerHTML = '<option value="">Choose a file...</option>';

    for (const [fileId, fileData] of this.uploadedFiles) {
      const option = document.createElement('option');
      option.value = fileId;
      option.textContent = fileData.name;
      select.appendChild(option);
    }
  }

  handleUploadedFileChange(fileId) {
    if (!fileId) {
      document.getElementById('cssCode').value = '';
      return;
    }

    const fileData = this.uploadedFiles.get(fileId);
    if (fileData) {
      document.getElementById('cssCode').value = fileData.content;
    }
  }

  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}

// Initialize popup when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new PopupManager();
});