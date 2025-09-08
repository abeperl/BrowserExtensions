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
        console.log('Input selector:', currentSiteConfig.inputSelector);
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

// Temporary debug mode toggle for testing - remove this after debugging
window.toggleSOEDebug = () => {
  settings.debugMode = !settings.debugMode;
  console.log('SOE Debug mode:', settings.debugMode ? 'ENABLED' : 'DISABLED');
  console.log('To toggle debug mode, run: toggleSOEDebug() in console');
};
console.log('SOE Debug toggle available. Run toggleSOEDebug() to enable/disable debug logging');

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
  if (!currentSiteConfig || !currentSiteConfig.inputSelector) return [];
  
	const inputs = document.querySelectorAll(currentSiteConfig.inputSelector);
	return Array.from(inputs);
}

// === State tracking to prevent duplicate events ===
let lastProcessedInput = { itemId: '', statusId: '', timestamp: 0, eventType: '' };
const DEBOUNCE_TIME = 100; // ms to prevent duplicate processing

// === Modal close tracking ===
let isModalClosing = false;
const MODAL_CLOSE_DEBOUNCE = 500; // ms to suppress overlays after close button click

// === Document-level event delegation to avoid interfering with modal ===
let documentListenersAttached = false;

function attachDocumentLevelListeners() {
	if (documentListenersAttached) return;
	
	// Use event delegation from document level to avoid interfering with modal event handling
	document.addEventListener('input', (e) => {
		if (!currentSiteConfig) return;
		
		// Check if the input matches our selector
		if (e.target.matches && e.target.matches(currentSiteConfig.inputSelector)) {
			const inputFields = getInputFields();
			const index = inputFields.indexOf(e.target);
			if (index >= 0) {
				// Only update scan state without showing overlay
				updateScanState('input', e.target.value, index);
			}
		}
	}, { passive: true });
	
	document.addEventListener('change', (e) => {
		if (!currentSiteConfig) return;
		
		if (e.target.matches && e.target.matches(currentSiteConfig.inputSelector)) {
			const inputFields = getInputFields();
			const index = inputFields.indexOf(e.target);
			if (index >= 0) {
				handleScanInput('input', e.target.value, false, index, false);
			}
		}
	}, { passive: true });
	
	document.addEventListener('blur', (e) => {
		if (!currentSiteConfig) return;

		if (e.target.matches && e.target.matches(currentSiteConfig.inputSelector)) {
			const inputFields = getInputFields();
			const index = inputFields.indexOf(e.target);
			if (index >= 0) {
				if (settings.debugMode) {
					console.log(`[${Date.now()}] Blur event on input ${index}, value: "${e.target.value}", scheduling overlay check in 200ms`);
				}

				// Longer delay to ensure modal close detection happens first
				setTimeout(() => {
					const timestamp = Date.now();
					const modalStillExists = document.getElementById('_modal_block_ui');

					if (settings.debugMode) {
						console.log(`[${timestamp}] Blur timeout fired:`, {
							modalStillExists: !!modalStillExists,
							isModalClosing,
							modalDisplay: modalStillExists ? modalStillExists.style.display : 'N/A',
							willShowOverlay: !isModalClosing && modalStillExists
						});
					}

					if (!isModalClosing && modalStillExists) {
						if (settings.debugMode) {
							console.log(`[${timestamp}] Showing blur overlay for input ${index}`);
						}
						handleScanInput('input', e.target.value, false, index, true);
					} else if (settings.debugMode) {
						console.log(`[${timestamp}] Blur overlay suppressed - modal is closing or closed`);
					}
				}, 200); // Even longer delay
			}
		}
	}, { passive: true });
	
	document.addEventListener('keydown', (e) => {
		if (!currentSiteConfig) return;
		
		if (e.key === 'Enter' && e.target.matches && e.target.matches(currentSiteConfig.inputSelector)) {
			const inputFields = getInputFields();
			const index = inputFields.indexOf(e.target);
			if (index >= 0) {
				handleScanInput('input', e.target.value, true, index);
				// Don't prevent default - let original handlers run
			}
		}
	}, { passive: true });
	
	documentListenersAttached = true;
	
	if (settings.debugMode) {
		console.log('Attached document-level event delegation for scan inputs');
	}
}

// === Monitor input fields for scan events ===
function monitorScanFields() {
  if (!currentSiteConfig) return false;

	const inputFields = getInputFields();
	if (inputFields.length === 0) {
		if (settings.debugMode) {
			console.log('Scan fields not found, will observe DOM for changes...');
			console.log('Looking for selector:', currentSiteConfig.inputSelector);
		}
		return false;
	}

	// No need to mark inputs for enlargement - using simple CSS approach

	// Also monitor the save button (only once)
	monitorSaveButton();

	// Hook into existing modal close function (only once)
	hookIntoModalClose();

	// Use document-level event delegation instead of direct listeners
	attachDocumentLevelListeners();

	if (settings.debugMode) {
		console.log(`Found ${inputFields.length} input fields, using document-level event delegation`);
	}

	return inputFields.length > 0;
}

// === Monitor save button ===
function monitorSaveButton() {
	const saveButton = document.querySelector('#scan-status-save');
	if (saveButton && !saveButton.dataset.soeListener) {
		// Add our monitoring listener without interfering with original handlers
		const saveClickHandler = (e) => {
			const inputFields = getInputFields();
			const values = inputFields.map(input => input.value.trim());
			
			if (settings.debugMode) {
				console.log('Save button clicked - monitoring', { values });
			}
			
			// Update scan state for monitoring (use first two values as itemId and statusId for compatibility)
			scanState.itemId = values[0] || '';
			scanState.statusId = values[1] || '';
			scanState.lastScan = values[0] || '';
			
			// Add to scan history if new
			if (values[0] && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== values[0])) {
				scanState.scanHistory.push(values[0]);
			}
			
			// Show pre-submit overlay alongside original functionality
			// But not if modal is closing
			if (values.some(v => v) && !isModalClosing) {
				window.postMessage({ 
					type: 'SHOW_PRESUBMIT_OVERLAY', 
					itemId: scanState.itemId, 
					statusId: scanState.statusId, 
					progress: scanState.scanHistory.length 
				}, '*');
				
				// Log scan event to background for history
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'ready_to_submit',
				});
			} else if (values.some(v => v) && isModalClosing && settings.debugMode) {
				console.log('Save button overlay suppressed - modal is closing');
			}
			
			if (settings.debugMode) {
				console.log('Save button monitoring complete - allowing original execution');
			}
			
			// Don't prevent default - let original website handle validation and submission
		};
		
		saveButton.addEventListener('click', saveClickHandler, { passive: true });
		saveButton.dataset.soeListener = '1';
		
		if (settings.debugMode) {
			console.log('Attached validation listener to save button');
		}
	}
}

// === Monitor modal removal from DOM ===
let modalRemovalObserver = null;
let modalCloseHooked = false;

function hookIntoModalClose() {
	// Don't hook multiple times
	if (modalCloseHooked) return;
	
	// Monitor for modal removal from DOM (most reliable approach)
	if (modalRemovalObserver) {
		modalRemovalObserver.disconnect();
	}
	
	modalRemovalObserver = new MutationObserver((mutations) => {
		const timestamp = Date.now();
		if (settings.debugMode) {
			console.log(`[${timestamp}] MutationObserver fired with ${mutations.length} mutations`);
		}

		mutations.forEach((mutation, mutationIndex) => {
			if (settings.debugMode) {
				console.log(`[${timestamp}] Processing mutation ${mutationIndex}:`, {
					type: mutation.type,
					target: mutation.target,
					removedNodes: mutation.removedNodes.length,
					addedNodes: mutation.addedNodes.length,
					attributeName: mutation.attributeName
				});
			}

			mutation.removedNodes.forEach((removedNode) => {
				// Check if the removed node is the modal block
				if (removedNode.nodeType === Node.ELEMENT_NODE &&
				    (removedNode.id === '_modal_block_ui' || removedNode.classList?.contains('modal-box'))) {

					if (settings.debugMode) {
						console.log(`[${timestamp}] Modal removed from DOM - suppressing overlays temporarily`, {
							id: removedNode.id,
							classes: Array.from(removedNode.classList || []),
							isModalClosing: isModalClosing
						});
					}

					// Set flag to suppress overlays briefly
					isModalClosing = true;

					// Clear flag after brief period
					setTimeout(() => {
						if (settings.debugMode) {
							console.log(`[${Date.now()}] Modal removal close period ended - overlays re-enabled`);
						}
						isModalClosing = false;
					}, MODAL_CLOSE_DEBOUNCE);
				}
			});
			
			// Also check if any modals are being hidden instead of removed
			if (mutation.type === 'attributes' && (mutation.attributeName === 'style' || mutation.attributeName === 'class')) {
				const target = mutation.target;
				if (target.id === '_modal_block_ui' || target.classList?.contains('modal-box')) {
					const timestamp = Date.now();
					const hiddenFlag = target.dataset.soeModalHidden;

					if (settings.debugMode) {
						console.log(`[${timestamp}] Modal attributes changed:`, {
							id: target.id,
							classes: Array.from(target.classList || []),
							style: target.style.cssText,
							display: target.style.display,
							visibility: target.style.visibility,
							offsetParent: !!target.offsetParent,
							hiddenFlag: hiddenFlag,
							isModalClosing: isModalClosing
						});
					}

					// Check if modal is being hidden
					const isHidden = target.style.display === 'none' ||
					                 target.style.visibility === 'hidden' ||
					                 target.classList.contains('hidden') ||
					                 target.offsetParent === null;

					// Check if modal is becoming visible
					const isVisible = !isHidden && target.style.display !== 'none' && target.style.visibility !== 'hidden';

					if (settings.debugMode) {
						console.log(`[${timestamp}] Modal state analysis:`, {
							isHidden,
							isVisible,
							willProcess: isHidden && hiddenFlag !== 'true'
						});
					}

					if (isHidden && hiddenFlag !== 'true') {
						if (settings.debugMode) {
							console.log(`[${timestamp}] Processing modal hide - removing enlargement class and suppressing overlays`);
						}

						// Mark this modal as processed to prevent loops
						target.dataset.soeModalHidden = 'true';

						// Remove our enlargement class to ensure it can hide properly
						target.classList.remove('soe-enlarged-modal');
						enlargedModals.delete(target);

						// Suppress overlays
						isModalClosing = true;
						setTimeout(() => {
							if (settings.debugMode) {
								console.log(`[${Date.now()}] Modal close debounce ended, clearing flags`);
							}
							isModalClosing = false;
							// Clear the flag after debounce period in case modal is shown again
							if (target.dataset) {
								delete target.dataset.soeModalHidden;
							}
						}, MODAL_CLOSE_DEBOUNCE);
					} else if (isVisible && hiddenFlag === 'true') {
						// Modal is becoming visible again, clear the flag
						if (settings.debugMode) {
							console.log(`[${timestamp}] Modal became visible again - clearing hidden flag`);
						}
						delete target.dataset.soeModalHidden;
					} else if (isHidden && hiddenFlag === 'true') {
						if (settings.debugMode) {
							console.log(`[${timestamp}] Modal hide already processed, skipping`);
						}
					}
				}
			}
		});
	});
	
	// Observe document body for modal removal and style changes
	modalRemovalObserver.observe(document.body, {
		childList: true,
		subtree: true,
		attributes: true,
		attributeFilter: ['style', 'class']
	});
	
	modalCloseHooked = true;
	
	if (settings.debugMode) {
		console.log('Monitoring modal DOM removal for overlay suppression');
	}
}

// === Escape key monitoring is now handled by hookIntoModalClose() ===

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

// === Update scan state without showing overlays (for input events) ===
function updateScanState(type, value, fieldIndex = 0) {
	// Store value in appropriate scan state based on field index
	if (fieldIndex === 0) {
		scanState.itemId = value;
	} else if (fieldIndex === 1) {
		scanState.statusId = value;
	}
	
	// Always update lastScan to the current value
	scanState.lastScan = value;
	
	if (settings.debugMode) {
		console.log('updateScanState', type, value, 'fieldIndex:', fieldIndex, '(no overlay)');
	}
}

// === Handle scan input ===
function handleScanInput(type, value, isSubmit = false, fieldIndex = 0, isBlur = false) {
	const now = Date.now();
	const eventKey = `${type}_${value}_${fieldIndex}_${isSubmit}_${isBlur}`;
	
	// Update scan state (in case this wasn't called from input event)
	updateScanState(type, value, fieldIndex);
	
	// Prevent duplicate processing within debounce time with same event
	if (now - lastProcessedInput.timestamp < DEBOUNCE_TIME && 
	    lastProcessedInput.itemId === scanState.itemId && 
	    lastProcessedInput.statusId === scanState.statusId &&
	    lastProcessedInput.eventType === eventKey) {
		if (settings.debugMode) {
			console.log('handleScanInput debounced', type, value);
		}
		return;
	}
	
	// Add to scan history if new and meaningful
	if (value && value.trim() && (!scanState.scanHistory.length || scanState.scanHistory[scanState.scanHistory.length-1] !== value)) {
		scanState.scanHistory.push(value);
	}
	
	lastProcessedInput = { 
		itemId: scanState.itemId, 
		statusId: scanState.statusId, 
		timestamp: now, 
		eventType: eventKey 
	};
	
	if (settings.debugMode) {
		console.log('handleScanInput', type, value, 'fieldIndex:', fieldIndex, 'isSubmit:', isSubmit, 'isBlur:', isBlur, 'modalClosing:', isModalClosing);
	}
	
	// Only show scan overlay when user finishes typing (Enter key, blur/exit field, or submit)
	// Don't show on regular typing (input events)
	// Also don't show if modal is closing
	if (value && value.trim() && (isSubmit || isBlur) && !isModalClosing) {
		window.postMessage({ type: 'SHOW_SCAN_OVERLAY', value: value, progress: scanState.scanHistory.length }, '*');
		// Log scan event to background for history
		chrome.runtime.sendMessage({
			type: 'LOG_SCAN',
			itemId: scanState.itemId,
			statusId: scanState.statusId,
			result: 'scanned',
		});
	} else if ((isModalClosing || isSubmit) && settings.debugMode) {
		console.log('Scan overlay suppressed - modal is closing:', isModalClosing, 'isSubmit:', isSubmit);
	}
	
	// Show pre-submit overlay when user indicates they're ready to submit (Enter key)
	// But not if modal is closing
	if (isSubmit && value && value.trim() && !isModalClosing) {
		window.postMessage({ 
			type: 'SHOW_PRESUBMIT_OVERLAY', 
			itemId: scanState.itemId || value, 
			statusId: scanState.statusId || '', 
			progress: scanState.scanHistory.length 
		}, '*');
		
		// Log scan event to background for history
		chrome.runtime.sendMessage({
			type: 'LOG_SCAN',
			itemId: scanState.itemId || value,
			statusId: scanState.statusId || '',
			result: 'ready_to_submit',
		});
	} else if (isSubmit && isModalClosing && settings.debugMode) {
		console.log('Pre-submit overlay suppressed - modal is closing');
	}
}

// === Intercept API submission (form submit or XHR/fetch) ===
function interceptApiSubmission() {
  if (!currentSiteConfig) return;
  
	// Monitor form submissions without interfering with original functionality
	document.addEventListener('submit', (e) => {
		const form = e.target;
		const inputFields = getInputFields();
		
		// Check if form contains any of our monitored inputs
		const hasMonitoredInputs = inputFields.some(input => form.contains(input));
		
		if (form && hasMonitoredInputs) {
			const values = inputFields.map(input => input.value.trim());
			const itemId = values[0] || '';
			const statusId = values[1] || '';
			
			if (settings.debugMode) {
				console.log('Form submission detected - monitoring', { values, itemId, statusId });
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
			// But not if modal is closing
			if (values.some(v => v) && !isModalClosing) {
				window.postMessage({ 
					type: 'SHOW_PRESUBMIT_OVERLAY', 
					itemId: scanState.itemId, 
					statusId: scanState.statusId, 
					progress: scanState.scanHistory.length 
				}, '*');
				
				// Log scan event to background for history
				chrome.runtime.sendMessage({
					type: 'LOG_SCAN',
					itemId: scanState.itemId,
					statusId: scanState.statusId,
					result: 'ready_to_submit',
				});
			} else if (values.some(v => v) && isModalClosing && settings.debugMode) {
				console.log('Form submission overlay suppressed - modal is closing');
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

// Clear all scan fields and reset scan state
function clearScanFields() {
	const inputFields = getInputFields();
	inputFields.forEach(input => {
		if (input) input.value = '';
	});
	
	scanState.itemId = '';
	scanState.statusId = '';
	scanState.lastScan = '';
	scanState.overlayActive = false;
}

// === Simple Modal Enhancement ===
// The modal is now enlarged via CSS only - no complex JavaScript needed

// === Error Snackbar Interception ===
// Intercept error snackbars and show them as overlays instead
let errorSnackbarObserver = null;
let isInterceptingError = false; // Prevent recursive triggering

function initializeErrorSnackbarInterception() {
  if (errorSnackbarObserver) {
    errorSnackbarObserver.disconnect();
  }

  errorSnackbarObserver = new MutationObserver((mutations) => {
    // Skip processing if we're currently intercepting an error
    if (isInterceptingError) {
      if (settings.debugMode) {
        console.log('Skipping mutation processing - currently intercepting error');
      }
      return;
    }

    mutations.forEach((mutation) => {
      // Skip mutations related to our overlay system
      if (mutation.target && (
          mutation.target.id === 'scan-overlay-extension-overlay' ||
          mutation.target.classList?.contains('soe-overlay') ||
          mutation.target.closest('#scan-overlay-extension-overlay')
      )) {
        if (settings.debugMode) {
          console.log('Skipping mutation from our overlay system');
        }
        return;
      }

      if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
        mutation.addedNodes.forEach((node) => {
          if (node.nodeType === Node.ELEMENT_NODE) {
            // Skip our own overlay elements
            if (node.id === 'scan-overlay-extension-overlay' ||
                node.classList?.contains('soe-overlay')) {
              return;
            }

            // Check for common error snackbar patterns
            const errorElement = checkForErrorSnackbar(node);
            if (errorElement) {
              interceptErrorSnackbar(errorElement);
            }
          }
        });
      }

      // Also check for attribute changes that might show errors
      if (mutation.type === 'attributes' &&
          (mutation.attributeName === 'class' || mutation.attributeName === 'style')) {
        const target = mutation.target;

        // Skip our overlay elements
        if (target.id === 'scan-overlay-extension-overlay' ||
            target.classList?.contains('soe-overlay') ||
            target.closest('#scan-overlay-extension-overlay')) {
          return;
        }

        if (target.nodeType === Node.ELEMENT_NODE) {
          const errorElement = checkForErrorSnackbar(target);
          if (errorElement) {
            interceptErrorSnackbar(errorElement);
          }
        }
      }
    });
  });

  errorSnackbarObserver.observe(document.body, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ['class', 'style']
  });

  if (settings.debugMode) {
    console.log('Error snackbar interception initialized');
  }
}

function checkForErrorSnackbar(element) {
  // Check for common error snackbar patterns
  const errorSelectors = [
    '.snackbar',
    '.snackbar-error',
    '.error-snackbar',
    '.toast-error',
    '.alert-error',
    '.notification-error',
    '.tf-validate-error-message',
    '[class*="error"]',
    '[class*="snackbar"]',
    '[class*="toast"]',
    '[class*="alert"]'
  ];

  // Check if element matches error patterns
  for (const selector of errorSelectors) {
    if (element.matches && element.matches(selector)) {
      return element;
    }
  }

  // Check if element contains error text
  const textContent = element.textContent || '';
  if (textContent.toLowerCase().includes('error') ||
      textContent.toLowerCase().includes('no record found') ||
      textContent.toLowerCase().includes('invalid') ||
      textContent.toLowerCase().includes('failed')) {
    return element;
  }

  return null;
}

function interceptErrorSnackbar(errorElement) {
  // Prevent recursive triggering
  if (isInterceptingError) {
    if (settings.debugMode) {
      console.log('Already intercepting an error, skipping');
    }
    return;
  }

  isInterceptingError = true;

  try {
    // Extract error message
    const errorMessage = errorElement.textContent || errorElement.innerText || 'Error occurred';

    if (settings.debugMode) {
      console.log('Intercepting error snackbar:', errorMessage);
    }

    // Hide the original error element
    errorElement.style.display = 'none !important';
    errorElement.style.visibility = 'hidden !important';
    errorElement.remove(); // Remove from DOM to prevent any interference

    // Show our overlay with the error message
    window.postMessage({
      type: 'SHOW_ERROR_OVERLAY',
      error: errorMessage,
      progress: scanState.scanHistory.length
    }, '*');

    // Log the error interception
    if (settings.debugMode) {
      console.log('Error snackbar intercepted and overlay shown:', errorMessage);
    }

    // Also log to background for history
    chrome.runtime.sendMessage({
      type: 'LOG_SCAN',
      itemId: scanState.itemId || 'error_intercepted',
      statusId: scanState.statusId || '',
      result: 'error_intercepted',
      error: errorMessage
    });

  } catch (error) {
    if (settings.debugMode) {
      console.error('Error intercepting snackbar:', error);
    }
  } finally {
    // Clear the flag after a short delay to allow any triggered mutations to complete
    setTimeout(() => {
      isInterceptingError = false;
      if (settings.debugMode) {
        console.log('Error interception flag cleared');
      }
    }, 100);
  }
}

function cleanupErrorSnackbarInterception() {
  if (errorSnackbarObserver) {
    errorSnackbarObserver.disconnect();
    errorSnackbarObserver = null;
  }
  // Clear the interception flag
  isInterceptingError = false;
}

// === Track DOM observation state ===
let domObserver = null;
let isCheckingFields = false;

// === Initialize extension features ===
function initializeExtensionFeatures() {
  if (!currentSiteConfig) return;

  // Initialize monitoring mechanisms (non-invasive)
  interceptApiSubmission();

  // Initialize error snackbar interception
  initializeErrorSnackbarInterception();

  // Clean up existing observer
  if (domObserver) {
    domObserver.disconnect();
  }
  
  // Always observe for scan fields, as dialogs may open/close repeatedly
  domObserver = new MutationObserver((mutations) => {
    // Prevent recursive calls
    if (isCheckingFields) return;
    
    let shouldCheck = false;
    
    // Only check if new nodes were added (not attributes or text changes)
    for (const mutation of mutations) {
      if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
        // Skip mutations caused by our own dataset modifications
        if (mutation.target && mutation.target.dataset && 
            (mutation.target.dataset.soeListener || mutation.target.dataset.soeCloseListener)) {
          continue;
        }
        
        // Check if any added nodes contain input fields or are input fields themselves
        for (const node of mutation.addedNodes) {
          if (node.nodeType === Node.ELEMENT_NODE) {
            // Skip our own modifications
            if (node.dataset && (node.dataset.soeListener || node.dataset.soeCloseListener)) {
              continue;
            }
            
            const hasInputs = node.matches && (
              node.matches(currentSiteConfig.inputSelector) ||
              (node.querySelector && node.querySelector(currentSiteConfig.inputSelector))
            );
            if (hasInputs) {
              shouldCheck = true;
              break;
            }

            // No need to mark inputs for enlargement - using simple CSS approach
          }
        }
        if (shouldCheck) break;
      }
    }
    
    if (shouldCheck) {
      // Use debounced field checking
      debounceFieldCheck();
    }
  });
  
  domObserver.observe(document.body, { 
    childList: true, 
    subtree: true,
    attributeFilter: [] // Don't watch attribute changes to prevent triggering on our dataset changes
  });
  
  // Also try once at init in case fields are already present
  monitorScanFields();
  
  if (settings.debugMode) {
    console.log('Scan overlay extension features initialized for:', currentSiteConfig.name);
  }
}

// === Debounced field checking ===
let fieldCheckTimeout = null;

function debounceFieldCheck() {
  // Cancel any pending check
  if (fieldCheckTimeout) {
    clearTimeout(fieldCheckTimeout);
  }
  
  // Schedule new check with debounce
  fieldCheckTimeout = setTimeout(() => {
    if (!isCheckingFields) {
      isCheckingFields = true;
      try {
        const attached = monitorScanFields();
        if (attached && settings.debugMode) {
          console.log('Scan fields found and listeners attached via debounced check');
        }
      } finally {
        isCheckingFields = false;
      }
    }
  }, 250); // 250ms debounce
}

// === Cleanup extension features ===
function cleanupExtensionFeatures() {
  // Clean up DOM observer
  if (domObserver) {
    domObserver.disconnect();
    domObserver = null;
  }
  
  // Clear any pending field checks
  if (fieldCheckTimeout) {
    clearTimeout(fieldCheckTimeout);
    fieldCheckTimeout = null;
  }
  
  // Remove event listeners and reset state
  scanState = {
    itemId: '',
    statusId: '',
    lastScan: '',
    scanHistory: [],
    overlayActive: false,
  };
  
  // Reset processed input tracking
  lastProcessedInput = { itemId: '', statusId: '', timestamp: 0, eventType: '' };
  
  // Reset modal close tracking
  isModalClosing = false;
  
  // Clean up modal removal observer
  if (modalRemovalObserver) {
    modalRemovalObserver.disconnect();
    modalRemovalObserver = null;
  }
  modalCloseHooked = false;
  
  // Reset field checking state
  isCheckingFields = false;
  
  // Reset document listeners
  documentListenersAttached = false;

  // Clean up error snackbar interception
  cleanupErrorSnackbarInterception();

  if (settings.debugMode) {
    console.log('Scan overlay extension features cleaned up');
  }
}

// === Cleanup mechanism to prevent memory leaks ===
function cleanupEventListeners() {
  // Remove all event listeners that were added with dataset markers
  const elements = document.querySelectorAll('[data-soe-listener]');
  elements.forEach(element => {
    // Note: In a real implementation, you'd need to store references to remove specific listeners
    // This is a simplified cleanup
    element.dataset.soeListener = '';
  });
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

	// Cleanup on page unload
	window.addEventListener('beforeunload', () => {
		cleanupExtensionFeatures();
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
