// history.js
// Provides scan history viewing in the popup (optional, for advanced users)

(function() {
  function fetchScanHistory(callback) {
    chrome.runtime.sendMessage({ type: 'GET_SCAN_HISTORY' }, (response) => {
      if (response && response.history) {
        callback(response.history);
      }
    });
  }

  // Example: render history in a div with id 'history-list'
  function renderHistory() {
    fetchScanHistory((history) => {
      const list = document.getElementById('history-list');
      if (!list) return;
      list.innerHTML = '';
      if (!history.length) {
        list.textContent = 'No scans this session.';
        return;
      }
      for (const entry of history) {
        const li = document.createElement('div');
        li.className = 'soe-history-entry';
        li.textContent = `#${history.indexOf(entry)+1}: Item ${entry.itemId}, Status ${entry.statusId}, ${entry.result} @ ${new Date(entry.timestamp).toLocaleTimeString()}`;
        list.appendChild(li);
      }
    });
  }

  // Optionally call renderHistory() on popup load
  if (document.getElementById('history-list')) {
    renderHistory();
  }
})();
