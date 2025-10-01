// overlay-manager.js
// Condensed overlay management for css-js-toinject project
// Handles creation and display of feedback overlays

const OverlayManager = (() => {
  const OVERLAY_ID = 'cji-overlay';
  const DEFAULT_DURATION = 2000;

  let overlayTimeout = null;
  let settings = {
    duration: DEFAULT_DURATION,
    audioEnabled: true,
    dismissKey: 'Escape'
  };

  // Overlay HTML templates
  const templates = {
    success: (data) => `
      <div class="cji-overlay cji-overlay-success">
        <div class="cji-overlay-content">
          <div class="cji-overlay-icon">✓</div>
          <div class="cji-overlay-title">Success</div>
          <div class="cji-overlay-message">${data.message || 'Operation completed'}</div>
        </div>
      </div>`,

    error: (data) => `
      <div class="cji-overlay cji-overlay-error">
        <div class="cji-overlay-content">
          <div class="cji-overlay-icon">✕</div>
          <div class="cji-overlay-title">Error</div>
          <div class="cji-overlay-message">${data.message || 'Operation failed'}</div>
        </div>
      </div>`,

    info: (data) => `
      <div class="cji-overlay cji-overlay-info">
        <div class="cji-overlay-content">
          <div class="cji-overlay-icon">ℹ</div>
          <div class="cji-overlay-title">${data.title || 'Info'}</div>
          <div class="cji-overlay-message">${data.message || ''}</div>
        </div>
      </div>`,

    warning: (data) => `
      <div class="cji-overlay cji-overlay-warning">
        <div class="cji-overlay-content">
          <div class="cji-overlay-icon">⚠</div>
          <div class="cji-overlay-title">Warning</div>
          <div class="cji-overlay-message">${data.message || 'Please review'}</div>
        </div>
      </div>`
  };

  // Show overlay
  function show(type, data = {}) {
    remove();

    const overlay = document.createElement('div');
    overlay.id = OVERLAY_ID;
    overlay.className = 'cji-overlay-container';
    overlay.innerHTML = templates[type] ? templates[type](data) : templates.info(data);
    overlay.tabIndex = 0;
    overlay.setAttribute('role', 'alertdialog');
    overlay.setAttribute('aria-live', 'assertive');

    document.body.appendChild(overlay);
    overlay.focus();

    // Play audio if enabled
    if (settings.audioEnabled && data.playSound !== false) {
      playAudio(type);
    }

    // Auto-dismiss
    const duration = data.duration || settings.duration;
    overlayTimeout = setTimeout(remove, duration);
  }

  // Remove overlay
  function remove() {
    const overlay = document.getElementById(OVERLAY_ID);
    if (overlay) overlay.remove();
    if (overlayTimeout) {
      clearTimeout(overlayTimeout);
      overlayTimeout = null;
    }
  }

  // Play audio feedback
  function playAudio(type) {
    try {
      // Simple beep using Web Audio API (no external files needed)
      const audioContext = new (window.AudioContext || window.webkitAudioContext)();
      const oscillator = audioContext.createOscillator();
      const gainNode = audioContext.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(audioContext.destination);

      // Different frequencies for different types
      const frequencies = {
        success: 800,
        error: 300,
        warning: 600,
        info: 500
      };

      oscillator.frequency.value = frequencies[type] || 500;
      oscillator.type = 'sine';

      gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.2);

      oscillator.start(audioContext.currentTime);
      oscillator.stop(audioContext.currentTime + 0.2);

    } catch (error) {
      console.warn('Audio playback failed:', error);
    }
  }

  // Keyboard handler
  window.addEventListener('keydown', (e) => {
    if (e.key === settings.dismissKey) remove();
  });

  // Public API
  return {
    show,
    remove,
    success: (data) => show('success', data),
    error: (data) => show('error', data),
    info: (data) => show('info', data),
    warning: (data) => show('warning', data),
    configure: (options) => Object.assign(settings, options)
  };
})();

// Add overlay styles
if (!document.getElementById('cji-overlay-styles')) {
  const style = document.createElement('style');
  style.id = 'cji-overlay-styles';
  style.textContent = `
    .cji-overlay-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(0, 0, 0, 0.5);
      z-index: 999999;
      animation: cji-fade-in 0.2s ease-in;
    }

    .cji-overlay {
      background: white;
      border-radius: 12px;
      padding: 40px;
      min-width: 320px;
      max-width: 500px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
      animation: cji-scale-in 0.3s ease-out;
    }

    .cji-overlay-content {
      text-align: center;
    }

    .cji-overlay-icon {
      font-size: 64px;
      margin-bottom: 16px;
    }

    .cji-overlay-title {
      font-size: 24px;
      font-weight: bold;
      margin-bottom: 12px;
    }

    .cji-overlay-message {
      font-size: 16px;
      color: #666;
    }

    .cji-overlay-success {
      border-top: 4px solid #28a745;
    }

    .cji-overlay-success .cji-overlay-icon {
      color: #28a745;
    }

    .cji-overlay-error {
      border-top: 4px solid #dc3545;
    }

    .cji-overlay-error .cji-overlay-icon {
      color: #dc3545;
    }

    .cji-overlay-warning {
      border-top: 4px solid #ffc107;
    }

    .cji-overlay-warning .cji-overlay-icon {
      color: #ffc107;
    }

    .cji-overlay-info {
      border-top: 4px solid #17a2b8;
    }

    .cji-overlay-info .cji-overlay-icon {
      color: #17a2b8;
    }

    @keyframes cji-fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes cji-scale-in {
      from { transform: scale(0.8); opacity: 0; }
      to { transform: scale(1); opacity: 1; }
    }
  `;
  document.head.appendChild(style);
}
