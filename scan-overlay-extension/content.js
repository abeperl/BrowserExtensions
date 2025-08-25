// content.js
// Main content script for Scan Overlay Extension
// Handles overlay display, scan monitoring, API interception, and feedback

// === CURRENT SITE CONFIG ===
let currentSiteConfig = null;
let settings = {
  audioEnabled: true,
  overlayDuration: 2000,
  debugMode: false,
  autoFocusAfterScan: true,
  interceptFormSubmit: true
};

// === URL MATCHING AND INITIALIZATION ===
function findMatchingSiteConfig(url, siteConfigs) {
  if (!siteConfigs || !Array.isArray(siteConfigs)) {
    return null;
  }
  
  return siteConfigs.find(config => {
    if (!config.enabled) return false;
    
    try {
      const pattern = new RegExp(config.urlPattern);
      return pattern.test(url);
    } catch (e) {
      if (settings.debugMode) {
        console.warn('Invalid regex pattern in site config:', config.urlPattern, e);
      }
      return false;
    }
  });
}

function shouldInitializeOnCurrentPage() {
  const currentUrl = window.location.href;
  
  // Check if we have a matching site configuration
  if (!currentSiteConfig) {
    if (settings.debugMode) {
      console.log('No matching site configuration for URL:', currentUrl);
    }
    return false;
  }
  
  if (settings.debugMode) {
    console.log('Site config found for URL:', currentUrl, currentSiteConfig.name);
  }
  
  return true;
}

// === SETTINGS MANAGEMENT ===
async function loadSettings() {
  try {
    const result = await chrome.storage.sync.get('scanOverlaySettings');
    const savedSettings = result.scanOverlaySettings || {};
    
    // Update global settings
    settings.audioEnabled = savedSettings.audioEnabled !== false;
    settings.overlayDuration = savedSettings.overlayDuration || 2000;
    settings.debugMode = savedSettings.debugMode || false;
    settings.autoFocusAfterScan = savedSettings.autoFocusAfterScan !== false;
    settings.interceptFormSubmit = savedSettings.interceptFormSubmit !== false;
    
    // Find matching site configuration for current URL
    const currentUrl = window.location.href;
    currentSiteConfig = findMatchingSiteConfig(currentUrl, savedSettings.siteConfigs);
    
    if (settings.debugMode) {
      console.log('Scan overlay settings loaded:', settings);
      if (currentSiteConfig) {
        console.log('Using site config:', currentSiteConfig.name);
        console.log('Selectors - Item:', currentSiteConfig.itemIdSelector, 'Status:', currentSiteConfig.statusIdSelector);
      } else {
        console.log('No matching site configuration found for URL:', currentUrl);
      }
    }
    
    // Update overlay settings
    if (window.soeOverlay) {
      window.soeOverlay.setDuration(settings.overlayDuration);
      window.soeOverlay.setAudio(settings.audioEnabled);
    }
    
    return currentSiteConfig !== null; // Return whether we should be active on this page
    
  } catch (error) {
    console.error('Error loading scan overlay settings:', error);
    return false;
  }
}

// Expose settings update function for popup
window.scanOverlayExtension = {
  updateSettings: function(newSettings) {
    // Update global settings
    Object.assign(settings, newSettings);
    
    // Find new matching site configuration
    const currentUrl = window.location.href;
    currentSiteConfig = findMatchingSiteConfig(currentUrl, newSettings.siteConfigs);
    
    if (settings.debugMode) {
      console.log('Settings updated:', settings);
      if (currentSiteConfig) {
        console.log('New site config:', currentSiteConfig.name);
      }
    }
    
    // Re-initialize with new settings if we have a matching config
    if (currentSiteConfig) {
      initializeExtensionFeatures();
    } else {
      // Clean up if no longer matching
      cleanupExtensionFeatures();
    }
    
    // Update overlay settings
    if (window.soeOverlay) {
      window.soeOverlay.setDuration(settings.overlayDuration);
      window.soeOverlay.setAudio(settings.audioEnabled);
    }
  },
  
  getCurrentSiteConfig: function() {
    return currentSiteConfig;
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

// === Utility: Find input fields using current site config ===
function getInputFields() {
  if (!currentSiteConfig) return { itemInput: null, statusInput: null };
  
	const itemInput = document.querySelector(currentSiteConfig.itemIdSelector);
	const statusInput = document.querySelector(currentSiteConfig.statusIdSelector);
	return { itemInput, statusInput };
}

// === State tracking to prevent duplicate events ===
let lastProcessedInput = { itemId: '', statusId: '', timestamp: 0 };
const DEBOUNCE_TIME = 100; // ms to prevent duplicate processing

// === Monitor input fields for scan events ===
function monitorScanFields() {
  if (!currentSiteConfig) return false;
  
	const { itemInput, statusInput } = getInputFields();
	if (!itemInput || !statusInput) {
		if (settings.debugMode) {
			console.log('Scan fields not found, will observe DOM for changes...');
			console.log('Looking for:', currentSiteConfig.itemIdSelector, 'and', currentSiteConfig.statusIdSelector);
		}
		return false;
	}
	
	// Also monitor the save button
	monitorSaveButton();
	if (!itemInput.dataset.soeListener) {
		// Add our monitoring listeners without removing existing ones
		// Use passive listeners that don't interfere with original functionality
		itemInput.addEventListener('input', (e) => {
			handleScanInput('itemId', e.target.value, false);
		}, { passive: true });
		
		itemInput.addEventListener('change', (e) => {
			handleScanInput('itemId', e.target.value, false);
		}, { passive: true });
		
		itemInput.addEventListener('blur', (e) => {
			handleScanInput('itemId', e.target.value, false);
		}, { passive: true });
		
		// For Enter key, intercept only for our overlay logic but don't prevent default
		itemInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') {
				handleScanInput('itemId', e.target.value, true);
				// Don't prevent default - let original handlers run
			}
		}, { passive: true });
		
		itemInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached monitoring listeners to', currentSiteConfig.itemIdSelector);
		}
	}
	
	if (!statusInput.dataset.soeListener) {
		// Add our monitoring listeners without removing existing ones
		// Use passive listeners that don't interfere with original functionality
		statusInput.addEventListener('input', (e) => {
			handleScanInput('statusId', e.target.value, false);
		}, { passive: true });
		
		statusInput.addEventListener('change', (e) => {
			handleScanInput('statusId', e.target.value, false);
		}, { passive: true });
		
		statusInput.addEventListener('blur', (e) => {
			handleScanInput('statusId', e.target.value, false);
		}, { passive: true });
		
		// For Enter key, intercept only for our overlay logic but don't prevent default
		statusInput.addEventListener('keydown', (e) => {
			if (e.key === 'Enter') {
				handleScanInput('statusId', e.target.value, true);
				// Don't prevent default - let original handlers run
			}
		}, { passive: true });
		
		statusInput.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached monitoring listeners to', currentSiteConfig.statusIdSelector);
		}
	}
	return true;
}

// === Monitor save button ===
function monitorSaveButton() {
	const saveButton = document.querySelector('#scan-status-save');
	if (saveButton && !saveButton.dataset.soeListener) {
		// Add our monitoring listener without interfering with original handlers
		saveButton.addEventListener('click', (e) => {
			const { itemInput, statusInput } = getInputFields();
			const itemId = itemInput ? itemInput.value.trim() : '';
			const statusId = statusInput ? statusInput.value.trim().toLowerCase() : '';
			
			if (settings.debugMode) {
				console.log('Save button clicked - monitoring', { itemId, statusId });
			}
			
			// Update scan state for monitoring
			scanState.itemId = itemId;
			scanState.statusId = statusId;
			scanState.lastScan = itemId;
			
			// Add to scan history if new
			if (itemId && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== itemId)) {
				scanState.scanHistory.push(itemId);
			}
			
			// Show pre-submit overlay alongside original functionality
			if (itemId && statusId) {
				window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
				
				// Log scan event to background for history
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'scanned',
				});
			}
			
			if (settings.debugMode) {
				console.log('Save button monitoring complete - allowing original execution');
			}
			
			// Don't prevent default - let original website handle validation and submission
		}, { passive: true });
		
		saveButton.dataset.soeListener = '1';
		if (settings.debugMode) {
			console.log('Attached validation listener to save button');
		}
	}
}

// === Client-side validation (mimics page's validation logic) ===
function validateScanData(itemId, statusId) {
	try {
		// Check if item exists in the page (like the original: $(".item-row .product-info[data-lineitemid='" + skuVal + "']"))
		const itemRow = document.querySelector(`.item-row .product-info[data-lineitemid='${itemId}']`);
		
		if (!itemRow || itemRow.length === 0) {
			return { isValid: false, error: 'No Record Found' };
		}
		
		// Check if status exists in dropdown (like the original: ".item-status-dropdown option[data-text='"+statusVal+"']")
		const itemStatus = itemRow.parentElement.querySelector(`.item-status-dropdown option[data-text='${statusId.toLowerCase()}']`);
		
		if (!itemStatus || itemStatus.length === 0) {
			return { isValid: false, error: 'No Status Found' };
		}
		
		return { isValid: true };
		
	} catch (error) {
		if (settings.debugMode) {
			console.log('Validation error:', error);
		}
		// If validation fails due to DOM structure differences, assume it's valid and let API handle it
		return { isValid: true };
	}
}

// === Handle scan input ===
function handleScanInput(type, value, isSubmit = false) {
	const now = Date.now();
	const currentState = { itemId: scanState.itemId, statusId: scanState.statusId };
	
	// Handle save button click differently - don't update scan state
	if (type !== 'save') {
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
		
		scanState[type] = value;
		scanState.lastScan = value;
		// Add to scan history if new
		if (value && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== value)) {
			scanState.scanHistory.push(value);
		}
	}
	
	lastProcessedInput = { ...currentState, timestamp: now };
	
	if (settings.debugMode) {
		console.log('handleScanInput', type, value, 'isSubmit:', isSubmit);
	}
	
	// Only show pre-submit overlay when user indicates they're ready to submit (Enter key or Save button)
	// or when there's an error
	if (isSubmit && scanState.itemId && scanState.statusId) {
		// Perform client-side validation like the page does
		const validationResult = validateScanData(scanState.itemId, scanState.statusId);
		
		if (validationResult.isValid) {
			window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
			// Log scan event to background for history
			chrome.runtime.sendMessage({
				type: 'LOG_SCAN',
				itemId: scanState.itemId,
				statusId: scanState.statusId,
				result: 'scanned',
			});
		} else {
			// Show validation error overlay instead of letting page show snackbar
			window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: validationResult.error, progress: scanState.scanHistory.length }, '*');
		}
	} else if (isSubmit && (!scanState.itemId || !scanState.statusId)) {
		// Show error overlay if user tries to submit with missing fields
		window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'Both fields required', progress: scanState.scanHistory.length }, '*');
	}
}

// === Intercept API submission (form submit or XHR/fetch) ===
function interceptApiSubmission() {
  if (!currentSiteConfig) return;
  
	// Monitor form submissions without interfering with original functionality
	document.addEventListener('submit', (e) => {
		const form = e.target;
		const { itemInput, statusInput } = getInputFields();
		
		if (form && (form.contains(itemInput) || form.contains(statusInput))) {
			const itemId = itemInput ? itemInput.value.trim() : '';
			const statusId = statusInput ? statusInput.value.trim().toLowerCase() : '';
			
			if (settings.debugMode) {
				console.log('Form submission detected - monitoring', { itemId, statusId });
			}
			
			// Update scan state for monitoring
			scanState.itemId = itemId;
			scanState.statusId = statusId;
			scanState.lastScan = itemId;
			
			// Add to scan history if new
			if (itemId && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== itemId)) {
				scanState.scanHistory.push(itemId);
			}
			
			// Show pre-submit overlay alongside original form handling
			if (itemId && statusId) {
				window.postMessage({ type: 'SHOW_PRESUBMIT_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
				
				// Log scan event to background for history
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'scanned',
				});
			}
			
			if (settings.debugMode) {
				console.log('Form submission monitoring complete - allowing original handling');
			}
			
			// Don't prevent default - let original website handle the form
		}
	}, { passive: true });

	// Monitor fetch calls but only intercept if we need to show overlays
	const origFetch = window.fetch;
	window.fetch = async function(...args) {
		const url = args[0];
		const isRelevantCall = typeof url === 'string' && url.includes(currentSiteConfig.apiUrlPattern);
		
		if (isRelevantCall && scanState.itemId && scanState.statusId) {
			if (settings.debugMode) {
				console.log('Fetch API call monitored:', url);
			}
			
			try {
				// Make the actual request and let it proceed normally
				const resp = await origFetch.apply(this, args);
				
				if (resp.ok) {
					// Show success overlay in addition to page's success handling
					window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'success',
					});
					
					// Clear scan fields after showing overlay
					setTimeout(() => {
						clearScanFields();
					}, 100);
					
					if (settings.debugMode) {
						console.log('Fetch success - showing overlay alongside normal page behavior');
					}
				} else {
					// Show error overlay alongside page's error handling
					window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'API Error', status: resp.status, progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'error',
					});
					
					if (settings.debugMode) {
						console.log('Fetch error - showing overlay alongside normal page behavior');
					}
				}
				
				// Return the original response unchanged so page can handle it normally
				return resp;
				
			} catch (err) {
				// Show error overlay alongside page's error handling
				window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: err.message, progress: scanState.scanHistory.length }, '*');
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'error',
				});
				
				// Re-throw the error so page can handle it normally
				throw err;
			}
		} else {
			// Let all other calls pass through normally
			return origFetch.apply(this, args);
		}
	};

	// Monitor XMLHttpRequest calls but allow normal execution
	const origXHR = window.XMLHttpRequest;
	window.XMLHttpRequest = function() {
		const xhr = new origXHR();
		const origOpen = xhr.open;
		const origSend = xhr.send;
		
		let isRelevantCall = false;
		
		xhr.open = function(method, url, ...args) {
			isRelevantCall = typeof url === 'string' && url.includes(currentSiteConfig.apiUrlPattern);
			if (isRelevantCall && settings.debugMode) {
				console.log('XHR call monitored:', method, url);
			}
			return origOpen.apply(this, [method, url, ...args]);
		};
		
		xhr.send = function(data) {
			if (isRelevantCall && scanState.itemId && scanState.statusId) {
				// Store original handlers
				const origOnLoad = xhr.onload;
				const origOnError = xhr.onerror;
				const origOnReadyStateChange = xhr.onreadystatechange;
				
				// Enhance the response handlers to also show our overlays
				xhr.onload = function() {
					if (xhr.status >= 200 && xhr.status < 300) {
						// Show success overlay in addition to page's handling
						window.postMessage({ type: 'SHOW_SUCCESS_OVERLAY', itemId: scanState.itemId, statusId: scanState.statusId, progress: scanState.scanHistory.length }, '*');
						chrome.runtime.sendMessage({
							type: 'LOG_SCAN',
							itemId: scanState.itemId,
							statusId: scanState.statusId,
							result: 'success',
							responseData: xhr.responseText
						});
						
						// Clear scan fields after showing overlay
						setTimeout(() => {
							clearScanFields();
						}, 100);
						
						if (settings.debugMode) {
							console.log('XHR success - showing overlay alongside normal page behavior');
						}
					} else {
						// Show error overlay alongside page's error handling
						window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'API Error', status: xhr.status, progress: scanState.scanHistory.length }, '*');
						chrome.runtime.sendMessage({
							type: 'LOG_SCAN',
							itemId: scanState.itemId,
							statusId: scanState.statusId,
							result: 'error',
						});
						
						if (settings.debugMode) {
							console.log('XHR error - showing overlay alongside normal page behavior');
						}
					}
					
					// Call original onload handler if it exists
					if (origOnLoad) {
						origOnLoad.apply(this, arguments);
					}
				};
				
				xhr.onerror = function() {
					// Show error overlay alongside page's error handling
					window.postMessage({ type: 'SHOW_ERROR_OVERLAY', error: 'Network Error', progress: scanState.scanHistory.length }, '*');
					chrome.runtime.sendMessage({
						type: 'LOG_SCAN',
						itemId: scanState.itemId,
						statusId: scanState.statusId,
						result: 'error',
					});
					
					if (settings.debugMode) {
						console.log('XHR network error - showing overlay alongside normal page behavior');
					}
					
					// Call original onerror handler if it exists
					if (origOnError) {
						origOnError.apply(this, arguments);
					}
				};
				
				xhr.onreadystatechange = function() {
					// Call original handler to allow normal page behavior
					if (origOnReadyStateChange) {
						origOnReadyStateChange.apply(this, arguments);
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

// === Initialize extension features ===
function initializeExtensionFeatures() {
  if (!currentSiteConfig) return;
  
  // Initialize monitoring mechanisms (non-invasive)
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
  
  if (settings.debugMode) {
    console.log('Scan overlay extension features initialized for:', currentSiteConfig.name);
  }
}

// === Cleanup extension features ===
function cleanupExtensionFeatures() {
  // Remove event listeners and reset state
  scanState = {
    itemId: '',
    statusId: '',
    lastScan: '',
    scanHistory: [],
    overlayActive: false,
  };
  
  // Reset processed input tracking
  lastProcessedInput = { itemId: '', statusId: '', timestamp: 0 };
  
  if (settings.debugMode) {
    console.log('Scan overlay extension features cleaned up');
  }
}

// === Initialize content script ===
async function init() {
	// Load settings and check if we should be active on this page
	const shouldBeActive = await loadSettings();
  
  if (!shouldBeActive) {
    if (settings.debugMode) {
      console.log('Extension not active on this page - no matching site configuration');
    }
    return;
  }
  
  // Initialize extension features
  initializeExtensionFeatures();
	
	// Listen for settings changes
	chrome.storage.onChanged.addListener((changes, namespace) => {
		if (namespace === 'sync' && changes.scanOverlaySettings) {
			loadSettings().then(shouldBeActive => {
        if (shouldBeActive) {
          initializeExtensionFeatures();
        } else {
          cleanupExtensionFeatures();
        }
      });
		}
	});
	
	if (settings.debugMode) {
		console.log('Scan overlay extension fully initialized with URL-based filtering');
	}
}

// Wait for DOM ready
if (document.readyState === 'loading') {
	document.addEventListener('DOMContentLoaded', init);
} else {
	init();
}
