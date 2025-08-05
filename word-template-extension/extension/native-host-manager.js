// Native Host Manager
// Handles native messaging with better error handling and diagnostics

class NativeHostManager {
  constructor() {
    this.hostName = 'com.wordtemplateextension.nativehost';
    this.isConnected = false;
    this.lastError = null;
    this.messageQueue = [];
    this.connectionPromise = null;
    
    // Initialize connection test
    this.testConnection();
  }

  async testConnection() {
    try {
      const response = await this.sendMessage({ action: 'ping' });
      this.isConnected = response && response.success;
      this.lastError = this.isConnected ? null : 'Ping failed';
      console.log('Native host connection test:', this.isConnected ? 'SUCCESS' : 'FAILED');
    } catch (error) {
      this.isConnected = false;
      this.lastError = error.message;
      console.error('Native host connection test failed:', error);
    }
  }

  sendMessage(message, timeout = 10000) {
    return new Promise((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        reject(new Error(`Native messaging timeout after ${timeout}ms`));
      }, timeout);

      try {
        console.log('Sending to native host:', message);
        
        chrome.runtime.sendNativeMessage(
          this.hostName,
          message,
          (response) => {
            clearTimeout(timeoutId);
            
            if (chrome.runtime.lastError) {
              const error = new Error(chrome.runtime.lastError.message);
              error.type = 'NATIVE_HOST_ERROR';
              this.lastError = error.message;
              this.isConnected = false;
              
              console.error('Native messaging error:', error);
              reject(error);
            } else {
              console.log('Native host response:', response);
              this.isConnected = true;
              this.lastError = null;
              resolve(response);
            }
          }
        );
      } catch (error) {
        clearTimeout(timeoutId);
        console.error('Error sending native message:', error);
        reject(error);
      }
    });
  }

  async sendMessageWithRetry(message, maxRetries = 2) {
    let lastError;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        const response = await this.sendMessage(message);
        return response;
      } catch (error) {
        lastError = error;
        console.warn(`Native messaging attempt ${attempt} failed:`, error.message);
        
        if (attempt < maxRetries) {
          // Wait before retry
          await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        }
      }
    }
    
    throw lastError;
  }

  async listTemplates() {
    try {
      const response = await this.sendMessageWithRetry({ action: 'list_templates' });
      return response;
    } catch (error) {
      console.error('Failed to list templates:', error);
      throw error;
    }
  }

  async updateTemplate(templateName, extractedData) {
    try {
      const message = {
        action: 'update_template',
        data: {
          template: templateName,
          extractedData: extractedData
        }
      };
      
      const response = await this.sendMessageWithRetry(message);
      return response;
    } catch (error) {
      console.error('Failed to update template:', error);
      throw error;
    }
  }

  async getConfig() {
    try {
      const response = await this.sendMessageWithRetry({ action: 'get_config' });
      return response;
    } catch (error) {
      console.error('Failed to get config:', error);
      throw error;
    }
  }

  async updateConfig(config) {
    try {
      const message = {
        action: 'update_config',
        config: config
      };
      
      const response = await this.sendMessageWithRetry(message);
      return response;
    } catch (error) {
      console.error('Failed to update config:', error);
      throw error;
    }
  }

  getConnectionStatus() {
    return {
      connected: this.isConnected,
      lastError: this.lastError,
      hostName: this.hostName
    };
  }

  getDiagnosticInfo() {
    return {
      hostName: this.hostName,
      connected: this.isConnected,
      lastError: this.lastError,
      browser: this.getBrowserInfo(),
      timestamp: new Date().toISOString()
    };
  }

  getBrowserInfo() {
    const userAgent = navigator.userAgent;
    if (userAgent.includes('Chrome')) {
      return 'Chrome';
    } else if (userAgent.includes('Edge')) {
      return 'Edge';
    } else {
      return 'Unknown';
    }
  }

  async runDiagnostics() {
    const diagnostics = {
      timestamp: new Date().toISOString(),
      browser: this.getBrowserInfo(),
      hostName: this.hostName,
      tests: []
    };

    // Test 1: Basic connectivity
    try {
      const pingResponse = await this.sendMessage({ action: 'ping' }, 5000);
      diagnostics.tests.push({
        name: 'ping_test',
        status: 'success',
        message: 'Native host responded to ping',
        data: pingResponse
      });
    } catch (error) {
      diagnostics.tests.push({
        name: 'ping_test',
        status: 'failed',
        message: error.message,
        error: error.type || 'UNKNOWN_ERROR'
      });
    }

    // Test 2: List templates
    try {
      const templatesResponse = await this.sendMessage({ action: 'list_templates' }, 5000);
      diagnostics.tests.push({
        name: 'list_templates_test',
        status: 'success',
        message: `Found ${templatesResponse.templates?.length || 0} templates`,
        data: templatesResponse
      });
    } catch (error) {
      diagnostics.tests.push({
        name: 'list_templates_test',
        status: 'failed',
        message: error.message,
        error: error.type || 'UNKNOWN_ERROR'
      });
    }

    // Test 3: Get configuration
    try {
      const configResponse = await this.sendMessage({ action: 'get_config' }, 5000);
      diagnostics.tests.push({
        name: 'get_config_test',
        status: 'success',
        message: 'Successfully retrieved configuration',
        data: configResponse
      });
    } catch (error) {
      diagnostics.tests.push({
        name: 'get_config_test',
        status: 'failed',
        message: error.message,
        error: error.type || 'UNKNOWN_ERROR'
      });
    }

    return diagnostics;
  }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
  module.exports = NativeHostManager;
} else if (typeof window !== 'undefined') {
  // Browser environment (popup, content script, etc.)
  window.NativeHostManager = NativeHostManager;
} else {
  // Service worker environment - make globally available
  self.NativeHostManager = NativeHostManager;
}