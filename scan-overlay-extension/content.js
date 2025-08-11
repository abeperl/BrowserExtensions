
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

// === State tracking to prevent duplicate events ===
let lastProcessedInput = { itemId: '', statusId: '', timestamp: 0 };
const DEBOUNCE_TIME = 100; // ms to prevent duplicate processing

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
		// Remove all existing event listeners by cloning the element
		const newItemInput = itemInput.cloneNode(true);
		itemInput.parentNode.replaceChild(newItemInput, itemInput);
		
		// Add our controlled event listeners with capture=true to intercept before other handlers
		newItemInput.addEventListener('input', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('itemId', e.target.value);
		}, { capture: true });
		
		newItemInput.addEventListener('change', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('itemId', e.target.value);
		}, { capture: true });
		
		newItemInput.addEventListener('blur', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('itemId', e.target.value);
		}, { capture: true });
		
		// Only handle keydown for Enter, remove keypress to prevent duplicate handling
		newItemInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') {
				e.preventDefault();
				e.stopImmediatePropagation();
				handleScanInput('itemId', e.target.value);
			}
		}, { capture: true });
		
		newItemInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached controlled listeners to', ITEM_ID_SELECTOR);
		}
	}
	
	if (!statusInput.dataset.soeListener) {
		// Remove all existing event listeners by cloning the element
		const newStatusInput = statusInput.cloneNode(true);
		statusInput.parentNode.replaceChild(newStatusInput, statusInput);
		
		// Add our controlled event listeners with capture=true to intercept before other handlers
		newStatusInput.addEventListener('input', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('statusId', e.target.value);
		}, { capture: true });
		
		newStatusInput.addEventListener('change', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('statusId', e.target.value);
		}, { capture: true });
		
		newStatusInput.addEventListener('blur', (e) => {
			e.stopImmediatePropagation();
			handleScanInput('statusId', e.target.value);
		}, { capture: true });
		
		// Only handle keydown for Enter, remove keypress to prevent duplicate handling
		newStatusInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') {
				e.preventDefault();
				e.stopImmediatePropagation();
				handleScanInput('statusId', e.target.value);
			}
		}, { capture: true });
		
		newStatusInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached controlled listeners to', STATUS_ID_SELECTOR);
		}
	}
	return true;
}

// === Handle scan input ===
function handleScanInput(type, value) {
	const now = Date.now();
	const currentState = { itemId: scanState.itemId, statusId: scanState.statusId };
	currentState[type] = value;
	
	// Prevent duplicate processing within debounce time
	if (now - lastProcessedInput.timestamp < DEBOUNCE_TIME && 
	    lastProcessedInput.itemId === currentState.itemId && 
	    lastProcessedInput.statusId === currentState.statusId) {
		if (settings.debugMode) {
			console.log('handleScanInput debounced', type, value);
		}
		return;
	}
	
	lastProcessedInput = { ...currentState, timestamp: now };
	
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
	// Intercept ALL form submissions with capture=true to prevent them
	document.addEventListener('submit', (e) => {
		const form = e.target;
		const itemInput = document.querySelector(ITEM_ID_SELECTOR);
		const statusInput = document.querySelector(STATUS_ID_SELECTOR);
		
		if (form && (form.contains(itemInput) || form.contains(statusInput))) {
			e.preventDefault();
			e.stopImmediatePropagation();
			if (settings.debugMode) {
				console.log('Form submission blocked and intercepted');
			}
			// Show pre-submit overlay and handle submission ourselves
			handleControlledSubmission();
		}
	}, { capture: true });

	// Intercept fetch calls completely
	const origFetch = window.fetch;
	window.fetch = async function(...args) {
		const url = args[0];
		const isRelevantCall = typeof url === 'string' && url.includes(API_URL_PATTERN);
		
		if (isRelevantCall) {
			if (settings.debugMode) {
				console.log('Fetch API call completely intercepted:', url);
			}
			
			// Show pre-submit overlay
			window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
			
			try {
				// Make the actual request but control the response handling
				const resp = await origFetch.apply(this, args);
				
				if (resp.ok) {
					// Parse response to prevent page from handling it
					const respData = await resp.clone().text();
					
					// Show success overlay instead of letting page show its bubble
					window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'success',
						responseData: respData
					});
					clearScanFields();
					
					// Return a modified response that won't trigger page's success handlers
					return new Response('{"intercepted": true, "success": true}', {
						status: 200,
						statusText: 'OK',
						headers: resp.headers
					});
				} else {
					window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'API Error', status: resp.status, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'error',
					});
					clearScanFields();
					
					// Return a modified error response
					return new Response('{"intercepted": true, "error": true}', {
						status: resp.status,
						statusText: resp.statusText,
						headers: resp.headers
					});
				}
			} catch (err) {
				window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: err.message, progress: scanState.scanHistory.length }, '*');
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'error',
				});
				clearScanFields();
				throw new Error('Request intercepted - ' + err.message);
			}
		} else {
			// Let non-relevant calls pass through normally
			return origFetch.apply(this, args);
		}
	};

	// Intercept XMLHttpRequest calls
	const origXHR = window.XMLHttpRequest;
	window.XMLHttpRequest = function() {
		const xhr = new origXHR();
		const origOpen = xhr.open;
		const origSend = xhr.send;
		
		let isRelevantCall = false;
		
		xhr.open = function(method, url, ...args) {
			isRelevantCall = typeof url === 'string' && url.includes(API_URL_PATTERN);
			if (isRelevantCall && settings.debugMode) {
				console.log('XHR call intercepted:', method, url);
			}
			return origOpen.apply(this, [method, url, ...args]);
		};
		
		xhr.send = function(data) {
			if (isRelevantCall) {
				// Show pre-submit overlay
				window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
				
				// Override the response handlers
				const origOnLoad = xhr.onload;
				const origOnError = xhr.onerror;
				const origOnReadyStateChange = xhr.onreadystatechange;
				
				xhr.onload = function() {
					if (xhr.status >= 200 && xhr.status < 300) {
						window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
						chrome.runtime.sendMessage({
							type: 'LOG_SCAN',
							itemId: scanState.itemId,
							statusId: scanState.statusId,
							result: 'success',
							responseData: xhr.responseText
						});
					} else {
						window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'API Error', status: xhr.status, progress: scanState.scanHistory.length }, '*');
						chrome.runtime.sendMessage({
							type: 'LOG_SCAN',
							itemId: scanState.itemId,
							statusId: scanState.statusId,
							result: 'error',
						});
					}
					clearScanFields();
					// Don't call original onload to prevent page's success bubble
				};
				
				xhr.onerror = function() {
					window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'Network Error', progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'error',
					});
					clearScanFields();
					// Don't call original onerror
				};
				
				xhr.onreadystatechange = function() {
					// Only handle final state, and don't call original handler
					if (xhr.readyState === 4) {
						if (xhr.status >= 200 && xhr.status < 300) {
							// Success already handled in onload
						} else if (xhr.status !== 0) {
							// Error already handled in onload
						}
					}
				};
			}
			
			return origSend.apply(this, [data]);
		};
		
		return xhr;
	};
}

// Handle controlled form submission
function handleControlledSubmission() {
	if (!scanState.itemId || !scanState.statusId) {
		window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'Both fields required', progress: scanState.scanHistory.length }, '*');
		return;
	}
	
	// Show pre-submit overlay
	window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
	
	// Simulate API call or trigger controlled submission
	// This would typically make the API call using our controlled fetch/xhr
	setTimeout(() => {
		// Simulate success for now - in real implementation this would be the actual API call
		window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
		chrome.runtime.sendMessage({
			type: 'LOG_SCAN',
			itemId: scanState.itemId,
			statusId: scanState.statusId,
			result: 'success',
		});
		clearScanFields();
	}, 1000);
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

// Suppress page notifications and bubbles
function suppressPageNotifications() {
	// Hide common notification/bubble selectors
	const commonNotificationSelectors = [
		'.notification', '.toast', '.alert', '.message', '.popup',
		'.bubble', '.status-message', '.success-message', '.error-message',
		'[role="alert"]', '[role="status"]', '[aria-live]',
		'.scan-result', '.result-popup', '.feedback-bubble'
	];
	
	// Observer to hide notifications as they appear
	const notificationObserver = new MutationObserver((mutations) => {
		mutations.forEach((mutation) => {
			mutation.addedNodes.forEach((node) => {
				if (node.nodeType === Node.ELEMENT_NODE) {
					// Check if the added node matches notification patterns
					commonNotificationSelectors.forEach(selector => {
						try {
							if (node.matches && node.matches(selector)) {
								node.style.display = 'none';
								if (settings.debugMode) {
									console.log('Suppressed page notification:', selector);
								}
							}
							// Also check child elements
							const childNotifications = node.querySelectorAll(selector);
							childNotifications.forEach(el => {
								el.style.display = 'none';
								if (settings.debugMode) {
									console.log('Suppressed child notification:', selector);
								}
							});
						} catch (e) {
							// Ignore selector errors
						}
					});
				}
			});
		});
	});
	
	notificationObserver.observe(document.body, {
		childList: true,
		subtree: true
	});
	
	// Also hide any existing notifications
	commonNotificationSelectors.forEach(selector => {
		try {
			document.querySelectorAll(selector).forEach(el => {
				el.style.display = 'none';
				if (settings.debugMode) {
					console.log('Suppressed existing notification:', selector);
				}
			});
		} catch (e) {
			// Ignore selector errors
		}
	});
}

// Override common notification methods
function overridePageNotificationMethods() {
	// Override alert, confirm, prompt
	const originalAlert = window.alert;
	const originalConfirm = window.confirm;
	const originalPrompt = window.prompt;
	
	window.alert = function(message) {
		if (settings.debugMode) {
			console.log('Alert suppressed:', message);
		}
		// Show our overlay instead if it's scan-related
		if (message && (message.includes('scan') || message.includes('success') || message.includes('error'))) {
			window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
		}
	};
	
	window.confirm = function(message) {
		if (settings.debugMode) {
			console.log('Confirm suppressed:', message);
		}
		return true; // Default to true for scan operations
	};
	
	window.prompt = function(message) {
		if (settings.debugMode) {
			console.log('Prompt suppressed:', message);
		}
		return null;
	};
	
	// Override common toast/notification libraries
	if (window.toastr) {
		const originalToastr = window.toastr;
		window.toastr = {
			success: () => {},
			error: () => {},
			warning: () => {},
			info: () => {}
		};
	}
	
	// Override other common notification patterns
	const originalConsoleLog = console.log;
	const originalConsoleError = console.error;
	
	// Don't completely disable console, but filter scan-related messages if needed
	console.log = function(...args) {
		const message = args.join(' ');
		if (!message.includes('scan-related-suppression-keyword')) {
			originalConsoleLog.apply(console, args);
		}
	};
}


// === Initialize content script ===
async function init() {
	// Load settings first
	await loadSettings();
	
	// Initialize all interception and suppression mechanisms
	suppressPageNotifications();
	overridePageNotificationMethods();
	interceptApiSubmission();
	
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
	
	// Listen for settings changes
	chrome.storage.onChanged.addListener((changes, namespace) => {
		if (namespace === 'sync' && changes.scanOverlaySettings) {
			loadSettings();
		}
	});
	
	if (settings.debugMode) {
		console.log('Scan overlay extension fully initialized with complete interception');
	}
}

// Wait for DOM ready
if (document.readyState === 'loading') {
	document.addEventListener('DOMContentLoaded', init);
} else {
	init();
}
