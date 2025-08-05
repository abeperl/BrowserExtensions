
// content.js
// Main content script for Scan Overlay Extension
// Handles overlay display, scan monitoring, API interception, and feedback

// === CONFIGURABLE SELECTORS (loaded from settings) ===
let ITEM_ID_SELECTOR = '#product-scan';
let STATUS_ID_SELECTOR = '#status-scan';
let API_URL_PATTERN = '/api/scan';
let settings = {
  audioEnabled: true,
  overlayDuration: 2000,
  debugMode: false,
  autoFocusAfterScan: true,
  interceptFormSubmit: true
};

// === SETTINGS MANAGEMENT ===
async function loadSettings() {
  try {
    const result = await chrome.storage.sync.get('scanOverlaySettings');
    const savedSettings = result.scanOverlaySettings || {};
    
    // Update selectors
    ITEM_ID_SELECTOR = savedSettings.itemIdSelector || '#product-scan';
    STATUS_ID_SELECTOR = savedSettings.statusIdSelector || '#status-scan';
    API_URL_PATTERN = savedSettings.apiUrlPattern || '/api/scan';
    
    // Update settings
    settings.audioEnabled = savedSettings.audioEnabled !== false;
    settings.overlayDuration = savedSettings.overlayDuration || 2000;
    settings.debugMode = savedSettings.debugMode || false;
    settings.autoFocusAfterScan = savedSettings.autoFocusAfterScan !== false;
    settings.interceptFormSubmit = savedSettings.interceptFormSubmit !== false;
    
    if (settings.debugMode) {
      console.log('Scan overlay settings loaded:', settings);
      console.log('Selectors - Item:', ITEM_ID_SELECTOR, 'Status:', STATUS_ID_SELECTOR);
    }
    
    // Update overlay settings
    if (window.soeOverlay) {
      window.soeOverlay.setDuration(settings.overlayDuration);
      window.soeOverlay.setAudio(settings.audioEnabled);
    }
    
  } catch (error) {
    console.error('Error loading scan overlay settings:', error);
  }
}

// Expose settings update function for popup
window.scanOverlayExtension = {
  updateSettings: function(newSettings) {
    ITEM_ID_SELECTOR = newSettings.itemIdSelector || ITEM_ID_SELECTOR;
    STATUS_ID_SELECTOR = newSettings.statusIdSelector || STATUS_ID_SELECTOR;
    API_URL_PATTERN = newSettings.apiUrlPattern || API_URL_PATTERN;
    
    Object.assign(settings, newSettings);
    
    if (settings.debugMode) {
      console.log('Settings updated:', settings);
    }
    
    // Re-initialize with new settings
    monitorScanFields();
    
    // Update overlay settings
    if (window.soeOverlay) {
      window.soeOverlay.setDuration(settings.overlayDuration);
      window.soeOverlay.setAudio(settings.audioEnabled);
    }
  }
};

if (settings.debugMode) {
  console.log('Scan overlay content script loaded');
}

// === State ===
let scanState = {
	itemId: '',
	statusId: '',
	lastScan: '',
	scanHistory: [],
	overlayActive: false,
};

// === Utility: Find input fields ===
function getInputFields() {
	const itemInput = document.querySelector(ITEM_ID_SELECTOR);
	const statusInput = document.querySelector(STATUS_ID_SELECTOR);
	return { itemInput, statusInput };
}

// === Monitor input fields for scan events ===
function monitorScanFields() {
	const { itemInput, statusInput } = getInputFields();
	if (!itemInput || !statusInput) {
		if (settings.debugMode) {
			console.log('Scan fields not found, will observe DOM for changes...');
			console.log('Looking for:', ITEM_ID_SELECTOR, 'and', STATUS_ID_SELECTOR);
		}
		return false;
	}
	if (!itemInput.dataset.soeListener) {
		// Blur event
		itemInput.addEventListener('blur', (e) => handleScanInput('itemId', e.target.value));
		// Enter key event
		itemInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') handleScanInput('itemId', e.target.value);
		});
		itemInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached blur/enter listener to', ITEM_ID_SELECTOR);
		}
	}
	if (!statusInput.dataset.soeListener) {
		statusInput.addEventListener('blur', (e) => handleScanInput('statusId', e.target.value));
		statusInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') handleScanInput('statusId', e.target.value);
		});
		statusInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached blur/enter listener to', STATUS_ID_SELECTOR);
		}
	}
	return true;
}

// === Handle scan input ===
function handleScanInput(type, value) {
	if (settings.debugMode) {
		console.log('handleScanInput', type, value);
	}
	scanState[type] = value;
	scanState.lastScan = value;
	// Add to scan history if new
	if (value && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== value)) {
		scanState.scanHistory.push(value);
	}
	// Only show pre-submit overlay if both fields are filled, and this is the second field to be completed
	const otherType = type === 'itemId' ? 'statusId' : 'itemId';
	if (scanState.itemId && scanState.statusId && value && scanState[otherType]) {
		window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
		// Log scan event to background for history
		chrome.runtime.sendMessage({
			type: 'LOG_SCAN',
			itemId: scanState.itemId,
			statusId: scanState.statusId,
			result: 'scanned',
		});
	}
}

// === Intercept API submission (form submit or XHR/fetch) ===
function interceptApiSubmission() {
	// Intercept form submit
	if (settings.interceptFormSubmit) {
		document.addEventListener('submit', (e) => {
			const form = e.target;
			const itemInput = document.querySelector(ITEM_ID_SELECTOR);
			const statusInput = document.querySelector(STATUS_ID_SELECTOR);
			
			if (form && (form.contains(itemInput) || form.contains(statusInput))) {
				if (settings.debugMode) {
					console.log('Form submission intercepted');
				}
				// Show pre-submit overlay
				window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId }, '*');
				// Optionally, delay actual submission until overlay confirmation (to be implemented)
			}
		}, true);
	}

	// Intercept fetch/XHR (to be implemented in detail later)
	// Example: monkey-patch fetch
	const origFetch = window.fetch;
	window.fetch = async function(...args) {
		// Only intercept relevant API calls (using configurable pattern)
		const url = args[0];
		if (typeof url === 'string' && url.includes(API_URL_PATTERN)) {
			if (settings.debugMode) {
				console.log('API call intercepted:', url);
			}
			window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId }, '*');
			try {
				const resp = await origFetch.apply(this, args);
				if (resp.ok) {
					window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'success',
					});
					clearScanFields();
				} else {
					window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'API Error', status: resp.status, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'error',
					});
					clearScanFields();
				}
				return resp;
			} catch (err) {
				window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: err.message, progress: scanState.scanHistory.length }, '*');
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'error',
				});
				clearScanFields();
				throw err;
			}
		} else {
			return origFetch.apply(this, args);
		}
	};
}

// Clear both scan fields and reset scan state
function clearScanFields() {
	const { itemInput, statusInput } = getInputFields();
	if (itemInput) itemInput.value = '';
	if (statusInput) statusInput.value = '';
	scanState.itemId = '';
	scanState.statusId = '';
	scanState.lastScan = '';
	scanState.overlayActive = false;
}


// === Initialize content script ===
async function init() {
	// Load settings first
	await loadSettings();
	
	// Always observe for scan fields, as dialogs may open/close repeatedly
	const observer = new MutationObserver(() => {
		const attached = monitorScanFields();
		if (attached && settings.debugMode) {
			console.log('Scan fields found and listeners attached via MutationObserver');
		}
	});
	observer.observe(document.body, { childList: true, subtree: true });
	
	// Also try once at init in case fields are already present
	monitorScanFields();
	interceptApiSubmission();
	
	// Listen for settings changes
	chrome.storage.onChanged.addListener((changes, namespace) => {
		if (namespace === 'sync' && changes.scanOverlaySettings) {
			loadSettings();
		}
	});
}

// Wait for DOM ready
if (document.readyState === 'loading') {
	document.addEventListener('DOMContentLoaded', init);
} else {
	init();
}
