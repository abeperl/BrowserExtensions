// overlay.js
// Handles creation and management of full-screen overlays for scan feedback

const OVERLAY_ID = 'scan-overlay-extension-overlay';
const OVERLAY_ANIMATION_CLASS = 'soe-fade-in';

// Overlay state
let overlayTimeout = null;
let overlayDuration = 2000; // Default, can be updated from settings
let overlayDismissKey = 'Escape';
let overlayAudioEnabled = true;
let overlayColorScheme = 'default'; // or 'high-contrast', etc.

// Overlay templates
function getOverlayHTML({ state, value, itemId, statusId, error, status }) {
  let progressHtml = '';
  if (typeof arguments[0].progress === 'number') {
    progressHtml = `<div class="soe-progress">Scan #${arguments[0].progress}</div>`;
  }
  switch (state) {
    case 'scan':
      return `<div class="soe-overlay soe-overlay-red"><div class="soe-overlay-content"><span class="soe-label">Scanned:</span><span class="soe-value">${value}</span>${progressHtml}</div></div>`;
    case 'presubmit':
      return `<div class="soe-overlay soe-overlay-amber"><div class="soe-overlay-content"><span class="soe-label">Item ID:</span><span class="soe-value">${itemId}</span><br><span class="soe-label">Status ID:</span><span class="soe-value">${statusId}</span><div class="soe-confirm">Ready to submit</div>${progressHtml}</div></div>`;
    case 'success':
      return `<div class="soe-overlay soe-overlay-green"><div class="soe-overlay-content"><span class="soe-label">Success!</span><br><span class="soe-label">Item ID:</span><span class="soe-value">${itemId}</span><br><span class="soe-label">Status ID:</span><span class="soe-value">${statusId}</span>${progressHtml}</div></div>`;
    case 'error':
      return `<div class="soe-overlay soe-overlay-red"><div class="soe-overlay-content"><span class="soe-label">Error:</span><span class="soe-value">${error || status || 'Unknown error'}</span>${progressHtml}</div></div>`;
    default:
      return '';
  }
}

// Overlay show/hide
function showOverlay(params) {
  removeOverlay();
  const overlay = document.createElement('div');
  overlay.id = OVERLAY_ID;
  overlay.className = OVERLAY_ANIMATION_CLASS + (overlayColorScheme === 'high-contrast' ? ' soe-high-contrast' : '');
  overlay.innerHTML = getOverlayHTML(params);
  overlay.tabIndex = 0;
  overlay.setAttribute('role', 'alertdialog');
  overlay.setAttribute('aria-live', 'assertive');
  document.body.appendChild(overlay);
  overlay.focus();
  // Play audio feedback
  if (overlayAudioEnabled) playAudio(params.state);
  // Dismiss overlay after duration (except for error overlays, which require manual dismissal)
  if (params.state !== 'error') {
    overlayTimeout = setTimeout(removeOverlay, overlayDuration);
  }
}

function removeOverlay() {
  const overlay = document.getElementById(OVERLAY_ID);
  if (overlay) overlay.remove();
  if (overlayTimeout) clearTimeout(overlayTimeout);
  // Focus and select the first scan input after overlay is dismissed
  try {
    // Use the configurable selector
    const settings = window.scanOverlayExtension?.settings || {};
    const selector = settings.itemIdSelector || '#product-scan';
    const input = document.querySelector(selector);
    if (input && (settings.autoFocusAfterScan !== false)) {
      input.focus();
      input.select && input.select();
    }
  } catch (e) { /* ignore */ }
}

// Audio feedback
function playAudio(state) {
  let audioFile = '';
  switch (state) {
    case 'scan': audioFile = 'audio/scan.mp3'; break;
    case 'presubmit': audioFile = 'audio/warning.mp3'; break;
    case 'success': audioFile = 'audio/success.mp3'; break;
    case 'error': audioFile = 'audio/error.mp3'; break;
    default: return;
  }
  const audio = new Audio(chrome.runtime.getURL(audioFile));
  audio.play();
}

// Keyboard shortcut for dismissal
window.addEventListener('keydown', (e) => {
  if (e.key === overlayDismissKey) removeOverlay();
});

// Listen for messages from content script
window.addEventListener('message', (event) => {
  if (!event.data || !event.data.type) return;
  switch (event.data.type) {
    case 'SHOW_SCAN_OVERLAY':
      showOverlay({ state: 'scan', value: event.data.value, progress: event.data.progress });
      break;
    case 'SHOW_PRESUBMIT_OVERLAY':
      showOverlay({ state: 'presubmit', itemId: event.data.itemId, statusId: event.data.statusId, progress: event.data.progress });
      break;
    case 'SHOW_SUCCESS_OVERLAY':
      showOverlay({ state: 'success', itemId: event.data.itemId, statusId: event.data.statusId, progress: event.data.progress });
      break;
    case 'SHOW_ERROR_OVERLAY':
      showOverlay({ state: 'error', error: event.data.error, status: event.data.status, progress: event.data.progress });
      break;
    default:
      break;
  }
});

// Expose for settings
window.soeOverlay = {
  setDuration: (ms) => { overlayDuration = ms; },
  setAudio: (enabled) => { overlayAudioEnabled = enabled; },
  setColorScheme: (scheme) => {
    overlayColorScheme = scheme;
    document.body.classList.toggle('soe-high-contrast', scheme === 'high-contrast');
  },
  remove: removeOverlay,
};
