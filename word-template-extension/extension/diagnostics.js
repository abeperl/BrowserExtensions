// Diagnostics page functionality
class DiagnosticsManager {
    constructor() {
        this.nativeHostManager = new NativeHostManager();
        this.init();
    }

    async init() {
        await this.loadSystemInfo();
        await this.checkConnection();
        this.bindEvents();
    }

    async loadSystemInfo() {
        // Browser info
        const browserInfo = this.getBrowserInfo();
        document.getElementById('browserInfo').textContent = browserInfo;

        // Extension info
        const manifest = chrome.runtime.getManifest();
        document.getElementById('extensionVersion').textContent = manifest.version;
        document.getElementById('extensionId').textContent = chrome.runtime.id;
    }

    getBrowserInfo() {
        const userAgent = navigator.userAgent;
        if (userAgent.includes('Chrome')) {
            const match = userAgent.match(/Chrome\/(\d+)/);
            return `Chrome ${match ? match[1] : 'Unknown'}`;
        } else if (userAgent.includes('Edge')) {
            const match = userAgent.match(/Edge\/(\d+)/);
            return `Edge ${match ? match[1] : 'Unknown'}`;
        }
        return 'Unknown Browser';
    }

    async checkConnection() {
        const statusDiv = document.getElementById('connectionStatus');
        
        try {
            const status = this.nativeHostManager.getConnectionStatus();
            
            if (status.connected) {
                statusDiv.innerHTML = `
                    <div><span class="status-indicator status-success"></span>Connected to native host</div>
                    <div style="margin-top: 8px; color: #6c757d;">Host: ${status.hostName}</div>
                `;
            } else {
                statusDiv.innerHTML = `
                    <div><span class="status-indicator status-error"></span>Not connected to native host</div>
                    <div style="margin-top: 8px; color: #dc3545;">Error: ${status.lastError || 'Unknown error'}</div>
                    <div style="margin-top: 8px; color: #6c757d;">Host: ${status.hostName}</div>
                `;
            }
        } catch (error) {
            statusDiv.innerHTML = `
                <div><span class="status-indicator status-error"></span>Connection test failed</div>
                <div style="margin-top: 8px; color: #dc3545;">Error: ${error.message}</div>
            `;
        }
    }

    async runAllDiagnostics() {
        const resultsDiv = document.getElementById('testResults');
        resultsDiv.innerHTML = '<div class="loading">Running diagnostics...</div>';

        try {
            const diagnostics = await this.nativeHostManager.runDiagnostics();
            this.displayDiagnosticResults(diagnostics);
            
            // Load additional info
            await this.loadNativeHostInfo();
            await this.loadTemplatesInfo();
            await this.loadConfigInfo();
            
        } catch (error) {
            resultsDiv.innerHTML = `
                <div class="test-result test-error">
                    <strong>Diagnostics Failed</strong><br>
                    ${error.message}
                </div>
            `;
        }
    }

    displayDiagnosticResults(diagnostics) {
        const resultsDiv = document.getElementById('testResults');
        let html = '';

        diagnostics.tests.forEach(test => {
            const statusClass = test.status === 'success' ? 'test-success' : 'test-error';
            const statusIcon = test.status === 'success' ? '✅' : '❌';
            
            html += `
                <div class="test-result ${statusClass}">
                    <strong>${statusIcon} ${test.name.replace(/_/g, ' ').toUpperCase()}</strong><br>
                    ${test.message}
                    ${test.error ? `<div class="error-details">Error Type: ${test.error}</div>` : ''}
                </div>
            `;
        });

        resultsDiv.innerHTML = html;
    }

    async loadNativeHostInfo() {
        const infoDiv = document.getElementById('nativeHostInfo');
        
        try {
            const response = await this.nativeHostManager.sendMessage({ action: 'ping' });
            
            if (response && response.success) {
                infoDiv.innerHTML = `
                    <div class="info-grid">
                        <div class="info-item">
                            <div class="info-label">Status</div>
                            <div class="info-value">${response.status || 'Unknown'}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Version</div>
                            <div class="info-value">${response.version || 'Unknown'}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Template Path</div>
                            <div class="info-value">${response.config?.template_path || 'Unknown'}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Output Path</div>
                            <div class="info-value">${response.config?.output_path || 'Unknown'}</div>
                        </div>
                    </div>
                `;
            } else {
                infoDiv.innerHTML = '<div class="test-result test-error">Native host not responding</div>';
            }
        } catch (error) {
            infoDiv.innerHTML = `<div class="test-result test-error">Error: ${error.message}</div>`;
        }
    }

    async loadTemplatesInfo() {
        const infoDiv = document.getElementById('templatesInfo');
        
        try {
            const response = await this.nativeHostManager.listTemplates();
            
            if (response && response.success && response.templates) {
                const templates = response.templates;
                
                if (templates.length === 0) {
                    infoDiv.innerHTML = `
                        <div class="test-result test-warning">
                            No templates found in: ${response.template_path}
                        </div>
                    `;
                } else {
                    let html = `<div class="test-result test-success">Found ${templates.length} template(s)</div>`;
                    
                    templates.forEach(template => {
                        const sizeKB = Math.round(template.size / 1024);
                        const modified = new Date(template.modified).toLocaleString();
                        
                        html += `
                            <div class="info-item">
                                <div class="info-label">${template.name}</div>
                                <div class="info-value">${sizeKB} KB • Modified: ${modified}</div>
                            </div>
                        `;
                    });
                    
                    infoDiv.innerHTML = html;
                }
            } else {
                infoDiv.innerHTML = '<div class="test-result test-error">Failed to load templates</div>';
            }
        } catch (error) {
            infoDiv.innerHTML = `<div class="test-result test-error">Error: ${error.message}</div>`;
        }
    }

    async loadConfigInfo() {
        const infoDiv = document.getElementById('configInfo');
        
        try {
            const response = await this.nativeHostManager.getConfig();
            
            if (response && response.success && response.config) {
                const config = response.config;
                
                infoDiv.innerHTML = `
                    <div class="info-grid">
                        <div class="info-item">
                            <div class="info-label">Template Path</div>
                            <div class="info-value">${config.template_path}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Output Path</div>
                            <div class="info-value">${config.output_path}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Auto Open</div>
                            <div class="info-value">${config.auto_open ? 'Yes' : 'No'}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Default Template</div>
                            <div class="info-value">${config.default_template}</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Max File Size</div>
                            <div class="info-value">${config.max_file_size_mb} MB</div>
                        </div>
                        <div class="info-item">
                            <div class="info-label">Log Level</div>
                            <div class="info-value">${config.log_level}</div>
                        </div>
                    </div>
                `;
            } else {
                infoDiv.innerHTML = '<div class="test-result test-error">Failed to load configuration</div>';
            }
        } catch (error) {
            infoDiv.innerHTML = `<div class="test-result test-error">Error: ${error.message}</div>`;
        }
    }

    async testPing() {
        try {
            const response = await this.nativeHostManager.sendMessage({ action: 'ping' });
            this.showTestResult('Ping Test', true, 'Native host responded successfully', response);
        } catch (error) {
            this.showTestResult('Ping Test', false, error.message);
        }
    }

    async testTemplates() {
        try {
            const response = await this.nativeHostManager.listTemplates();
            const count = response.templates ? response.templates.length : 0;
            this.showTestResult('Templates Test', true, `Found ${count} templates`, response);
        } catch (error) {
            this.showTestResult('Templates Test', false, error.message);
        }
    }

    async testConfig() {
        try {
            const response = await this.nativeHostManager.getConfig();
            this.showTestResult('Config Test', true, 'Configuration loaded successfully', response);
        } catch (error) {
            this.showTestResult('Config Test', false, error.message);
        }
    }

    showTestResult(testName, success, message, data = null) {
        const resultsDiv = document.getElementById('testResults');
        const statusClass = success ? 'test-success' : 'test-error';
        const statusIcon = success ? '✅' : '❌';
        
        const dataHtml = data ? `<details style="margin-top: 10px;"><summary>Response Data</summary><pre>${JSON.stringify(data, null, 2)}</pre></details>` : '';
        
        resultsDiv.innerHTML = `
            <div class="test-result ${statusClass}">
                <strong>${statusIcon} ${testName}</strong><br>
                ${message}
                ${dataHtml}
            </div>
        `;
    }

    async exportDiagnostics() {
        try {
            const diagnostics = await this.nativeHostManager.runDiagnostics();
            const systemInfo = {
                browser: this.getBrowserInfo(),
                extensionId: chrome.runtime.id,
                extensionVersion: chrome.runtime.getManifest().version,
                timestamp: new Date().toISOString()
            };

            const report = {
                system: systemInfo,
                diagnostics: diagnostics,
                userAgent: navigator.userAgent
            };

            const dataStr = JSON.stringify(report, null, 2);
            const dataBlob = new Blob([dataStr], { type: 'application/json' });

            const link = document.createElement('a');
            link.href = URL.createObjectURL(dataBlob);
            link.download = `word-template-extension-diagnostics-${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.json`;
            link.click();

        } catch (error) {
            alert(`Failed to export diagnostics: ${error.message}`);
        }
    }

    bindEvents() {
        document.getElementById('runDiagnosticsBtn').addEventListener('click', () => {
            this.runAllDiagnostics();
        });

        document.getElementById('testPingBtn').addEventListener('click', () => {
            this.testPing();
        });

        document.getElementById('testTemplatesBtn').addEventListener('click', () => {
            this.testTemplates();
        });

        document.getElementById('testConfigBtn').addEventListener('click', () => {
            this.testConfig();
        });

        document.getElementById('exportDiagnosticsBtn').addEventListener('click', () => {
            this.exportDiagnostics();
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new DiagnosticsManager();
});