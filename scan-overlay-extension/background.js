
// background.js
// Handles tab focus, persistent scan history, and messaging

const STORAGE_KEY = 'scanOverlayHistory';
const MAX_HISTORY_ENTRIES = 1000;

let scanHistory = [];
let isInitialized = false;

// Initialize history from storage
async function initializeHistory() {
  if (isInitialized) return;

  try {
    const result = await chrome.storage.local.get(STORAGE_KEY);
    scanHistory = result[STORAGE_KEY] || [];
    isInitialized = true;
  } catch (error) {
    console.error('Failed to load scan history:', error);
    scanHistory = [];
    isInitialized = true;
  }
}

// Save history to storage
async function saveHistory() {
  try {
    // Keep only the most recent entries
    if (scanHistory.length > MAX_HISTORY_ENTRIES) {
      scanHistory = scanHistory.slice(-MAX_HISTORY_ENTRIES);
    }

    await chrome.storage.local.set({ [STORAGE_KEY]: scanHistory });
  } catch (error) {
    console.error('Failed to save scan history:', error);
  }
}

// Listen for scan events from content scripts (if needed for logging/history)
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  (async () => {
    try {
      await initializeHistory();

      if (msg.type === 'LOG_SCAN') {
        const scanEntry = {
          itemId: msg.itemId,
          statusId: msg.statusId,
          timestamp: Date.now(),
          result: msg.result || 'unknown',
          url: sender.tab?.url || 'unknown'
        };

        scanHistory.push(scanEntry);
        await saveHistory();
        sendResponse({ ok: true });

      } else if (msg.type === 'GET_SCAN_HISTORY') {
        sendResponse({ history: scanHistory });

      } else if (msg.type === 'CLEAR_SCAN_HISTORY') {
        scanHistory = [];
        await chrome.storage.local.remove(STORAGE_KEY);
        sendResponse({ ok: true });

      } else if (msg.type === 'GET_HISTORY_STATS') {
        const stats = {
          total: scanHistory.length,
          today: scanHistory.filter(entry =>
            new Date(entry.timestamp).toDateString() === new Date().toDateString()
          ).length,
          success: scanHistory.filter(entry => entry.result === 'success').length,
          errors: scanHistory.filter(entry => entry.result === 'error').length
        };
        sendResponse({ stats });
      }

    } catch (error) {
      console.error('Background message handler error:', error);
      sendResponse({ error: error.message });
    }
  })();

  // Return true to indicate async response
  return true;
});

// Page focus management: keep scanning tab active
chrome.tabs.onActivated.addListener((activeInfo) => {
	chrome.tabs.get(activeInfo.tabId, (tab) => {
		if (tab && tab.url && tab.url.includes('scan')) {
			chrome.windows.update(tab.windowId, { focused: true });
		}
	});
});

