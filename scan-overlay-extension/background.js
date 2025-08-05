
// background.js
// Handles tab focus, session scan history, and messaging

let scanHistory = [];

// Listen for scan events from content scripts (if needed for logging/history)
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
	if (msg.type === 'LOG_SCAN') {
		scanHistory.push({
			itemId: msg.itemId,
			statusId: msg.statusId,
			timestamp: Date.now(),
			result: msg.result || 'unknown',
		});
		sendResponse({ ok: true });
	} else if (msg.type === 'GET_SCAN_HISTORY') {
		sendResponse({ history: scanHistory });
	} else if (msg.type === 'CLEAR_SCAN_HISTORY') {
		scanHistory = [];
		sendResponse({ ok: true });
	}
});

// Page focus management: keep scanning tab active
chrome.tabs.onActivated.addListener((activeInfo) => {
	chrome.tabs.get(activeInfo.tabId, (tab) => {
		if (tab && tab.url && tab.url.includes('scan')) {
			chrome.windows.update(tab.windowId, { focused: true });
		}
	});
});

