// ui-feedback.js
// Combined overlay and popup management for css-js-toinject project
// Handles creation and display of feedback overlays and control popups

// ==================== OVERLAY MANAGER ====================

const OverlayManager = (() => {
  const OVERLAY_ID = 'cji-overlay';
  const DEFAULT_DURATION = 2000;

  let overlayTimeout = null;
  let settings = {
    duration: DEFAULT_DURATION,
    audioEnabled: true,
    volume: 0.3, // Volume level (0.0 to 1.0)
    dismissKey: 'Escape',
    useMp3Files: true // Try CDN MP3 files first, fallback to synthesized
  };

  // Audio cache for MP3 files
  const audioCache = {
    success: null,
    error: null,
    warning: null,
    info: null
  };

  // CDN-hosted sounds (ion-sound library from Cloudflare CDN)
  // Source: https://cdnjs.com/libraries/ion-sound
  const soundCdnUrls = {
    success: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/button_click.mp3',
    error: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/computer_error.mp3',
    warning: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/door_bell.mp3',
    info: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/bell_ring.mp3'
  };

  // Preload MP3 files from CDN
  function preloadAudioFiles() {
    if (!settings.useMp3Files) return;

    Object.keys(soundCdnUrls).forEach(type => {
      const audio = new Audio();
      audio.volume = settings.volume;
      audio.preload = 'auto';
      audio.src = soundCdnUrls[type];

      // Handle load success
      audio.addEventListener('canplaythrough', () => {
        audioCache[type] = audio;
        console.log(`Loaded ${type} sound from CDN`);
      }, { once: true });

      // Handle load failure - fallback to synthesized
      audio.addEventListener('error', () => {
        console.warn(`Failed to load ${type} sound from CDN, will use synthesized fallback`);
        audioCache[type] = null;
      }, { once: true });
    });
  }

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
      // Try MP3 file first if enabled and cached
      if (settings.useMp3Files && audioCache[type]) {
        playMp3Audio(type);
      } else {
        // Fallback to enhanced synthesized sound
        playSynthesizedAudio(type);
      }
    } catch (error) {
      console.warn('Audio playback failed:', error);
    }
  }

  // Play MP3 audio file
  function playMp3Audio(type) {
    try {
      const audio = audioCache[type];
      if (audio) {
        // Clone audio to allow overlapping plays
        const clone = audio.cloneNode();
        clone.volume = settings.volume;
        clone.play().catch(err => {
          console.warn('MP3 playback failed, using synthesized fallback:', err);
          playSynthesizedAudio(type);
        });
      } else {
        playSynthesizedAudio(type);
      }
    } catch (error) {
      console.warn('MP3 playback error:', error);
      playSynthesizedAudio(type);
    }
  }

  // Enhanced synthesized audio using Web Audio API
  function playSynthesizedAudio(type) {
    try {
      const audioContext = new (window.AudioContext || window.webkitAudioContext)();
      const now = audioContext.currentTime;
      const volume = settings.volume;

      switch (type) {
        case 'success':
          playSuccessSound(audioContext, now, volume);
          break;
        case 'error':
          playErrorSound(audioContext, now, volume);
          break;
        case 'warning':
          playWarningSound(audioContext, now, volume);
          break;
        case 'info':
          playInfoSound(audioContext, now, volume);
          break;
        default:
          playInfoSound(audioContext, now, volume);
      }
    } catch (error) {
      console.warn('Synthesized audio failed:', error);
    }
  }

  // Success: Rising two-tone chime (pleasant)
  function playSuccessSound(ctx, now, volume) {
    // First tone
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.frequency.value = 800;
    osc1.type = 'sine';
    gain1.gain.setValueAtTime(volume * 0.3, now);
    gain1.gain.exponentialRampToValueAtTime(0.01, now + 0.15);
    osc1.start(now);
    osc1.stop(now + 0.15);

    // Second tone (higher)
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.frequency.value = 1000;
    osc2.type = 'sine';
    gain2.gain.setValueAtTime(volume * 0.3, now + 0.08);
    gain2.gain.exponentialRampToValueAtTime(0.01, now + 0.25);
    osc2.start(now + 0.08);
    osc2.stop(now + 0.25);
  }

  // Error: Descending alert tone (attention-grabbing but not harsh)
  function playErrorSound(ctx, now, volume) {
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.frequency.setValueAtTime(400, now);
    osc.frequency.exponentialRampToValueAtTime(250, now + 0.3);
    osc.type = 'sine';

    gain.gain.setValueAtTime(volume * 0.4, now);
    gain.gain.exponentialRampToValueAtTime(0.01, now + 0.3);

    osc.start(now);
    osc.stop(now + 0.3);
  }

  // Warning: Alternating two-tone (non-intrusive)
  function playWarningSound(ctx, now, volume) {
    // First tone
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.frequency.value = 600;
    osc1.type = 'sine';
    gain1.gain.setValueAtTime(volume * 0.25, now);
    gain1.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
    osc1.start(now);
    osc1.stop(now + 0.1);

    // Second tone
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.frequency.value = 700;
    osc2.type = 'sine';
    gain2.gain.setValueAtTime(volume * 0.25, now + 0.12);
    gain2.gain.exponentialRampToValueAtTime(0.01, now + 0.22);
    osc2.start(now + 0.12);
    osc2.stop(now + 0.22);
  }

  // Info: Single soft tone (neutral)
  function playInfoSound(ctx, now, volume) {
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.frequency.value = 500;
    osc.type = 'sine';

    gain.gain.setValueAtTime(volume * 0.2, now);
    gain.gain.exponentialRampToValueAtTime(0.01, now + 0.15);

    osc.start(now);
    osc.stop(now + 0.15);
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
    configure: (options) => {
      Object.assign(settings, options);
      // Update cached audio volumes if volume changed
      if (options.volume !== undefined) {
        Object.values(audioCache).forEach(audio => {
          if (audio) audio.volume = settings.volume;
        });
      }
      // Reload audio files if path changed
      if (options.soundsPath !== undefined || options.useMp3Files !== undefined) {
        preloadAudioFiles();
      }
      return settings;
    },
    // Test audio playback
    testAudio: (type = 'success') => playAudio(type),
    // Get current settings
    getSettings: () => ({ ...settings }),
    // Manually preload audio files
    preloadAudio: preloadAudioFiles
  };
})();

// ==================== POPUP CONTROLLER ====================

const PopupController = (() => {
  let isVisible = false;
  let currentConfig = {
    position: 'bottom-right', // top-left, top-right, bottom-left, bottom-right
    theme: 'light', // light, dark
    buttons: []
  };

  // Create popup container
  function createPopup() {
    if (document.getElementById('cji-popup')) return;

    const popup = document.createElement('div');
    popup.id = 'cji-popup';
    popup.className = `cji-popup cji-popup-${currentConfig.position} cji-popup-${currentConfig.theme}`;
    popup.innerHTML = `
      <div class="cji-popup-header">
        <span class="cji-popup-title">Controls</span>
        <button class="cji-popup-close" aria-label="Close">×</button>
      </div>
      <div class="cji-popup-body">
        <div id="cji-popup-buttons"></div>
      </div>
    `;

    document.body.appendChild(popup);
    bindEvents();
    updateButtons();
  }

  // Update button list
  function updateButtons() {
    const container = document.getElementById('cji-popup-buttons');
    if (!container) return;

    container.innerHTML = currentConfig.buttons.map((btn, idx) => `
      <button class="cji-popup-btn ${btn.className || ''}" data-idx="${idx}">
        ${btn.icon ? `<span class="cji-btn-icon">${btn.icon}</span>` : ''}
        <span class="cji-btn-label">${btn.label}</span>
      </button>
    `).join('');

    // Bind button clicks
    container.querySelectorAll('.cji-popup-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const idx = parseInt(btn.dataset.idx);
        const config = currentConfig.buttons[idx];
        if (config && config.onClick) {
          config.onClick();
        }
      });
    });
  }

  // Bind events
  function bindEvents() {
    const popup = document.getElementById('cji-popup');
    if (!popup) return;

    const closeBtn = popup.querySelector('.cji-popup-close');
    if (closeBtn) {
      closeBtn.addEventListener('click', hide);
    }

    // Draggable functionality
    const header = popup.querySelector('.cji-popup-header');
    let isDragging = false;
    let currentX, currentY, initialX, initialY;

    header.addEventListener('mousedown', (e) => {
      if (e.target.closest('.cji-popup-close')) return;
      isDragging = true;
      initialX = e.clientX - popup.offsetLeft;
      initialY = e.clientY - popup.offsetTop;
      popup.style.position = 'fixed';
      popup.classList.remove(`cji-popup-${currentConfig.position}`);
    });

    document.addEventListener('mousemove', (e) => {
      if (!isDragging) return;
      e.preventDefault();
      currentX = e.clientX - initialX;
      currentY = e.clientY - initialY;
      popup.style.left = currentX + 'px';
      popup.style.top = currentY + 'px';
    });

    document.addEventListener('mouseup', () => {
      isDragging = false;
    });
  }

  // Show popup
  function show() {
    createPopup();
    const popup = document.getElementById('cji-popup');
    if (popup) {
      popup.classList.add('cji-popup-visible');
      isVisible = true;
    }
  }

  // Hide popup
  function hide() {
    const popup = document.getElementById('cji-popup');
    if (popup) {
      popup.classList.remove('cji-popup-visible');
      isVisible = false;
    }
  }

  // Toggle popup
  function toggle() {
    if (isVisible) hide();
    else show();
  }

  // Configure popup
  function configure(config) {
    Object.assign(currentConfig, config);
    if (isVisible) {
      const popup = document.getElementById('cji-popup');
      if (popup) {
        popup.className = `cji-popup cji-popup-${currentConfig.position} cji-popup-${currentConfig.theme}`;
        if (isVisible) popup.classList.add('cji-popup-visible');
      }
      updateButtons();
    }
  }

  // Add button
  function addButton(button) {
    currentConfig.buttons.push(button);
    if (isVisible) updateButtons();
  }

  // Remove button
  function removeButton(index) {
    currentConfig.buttons.splice(index, 1);
    if (isVisible) updateButtons();
  }

  // Public API
  return {
    show,
    hide,
    toggle,
    configure,
    addButton,
    removeButton,
    isVisible: () => isVisible
  };
})();

// ==================== STYLES INJECTION ====================

function injectStyles() {
  // Inject overlay styles
  if (!document.getElementById('cji-overlay-styles')) {
    const overlayStyle = document.createElement('style');
    overlayStyle.id = 'cji-overlay-styles';
    overlayStyle.textContent = `
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
    (document.head || document.documentElement).appendChild(overlayStyle);
  }

  // Inject popup styles
  if (!document.getElementById('cji-popup-styles')) {
    const popupStyle = document.createElement('style');
    popupStyle.id = 'cji-popup-styles';
    popupStyle.textContent = `
      .cji-popup {
        position: fixed;
        width: 280px;
        background: white;
        border-radius: 8px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
        z-index: 999998;
        opacity: 0;
        transform: scale(0.9);
        transition: opacity 0.2s, transform 0.2s;
        pointer-events: none;
      }

      .cji-popup-visible {
        opacity: 1;
        transform: scale(1);
        pointer-events: auto;
      }

      .cji-popup-top-left {
        top: 20px;
        left: 20px;
      }

      .cji-popup-top-right {
        top: 20px;
        right: 20px;
      }

      .cji-popup-bottom-left {
        bottom: 20px;
        left: 20px;
      }

      .cji-popup-bottom-right {
        bottom: 20px;
        right: 20px;
      }

      .cji-popup-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px 16px;
        border-bottom: 1px solid #e0e0e0;
        cursor: move;
        user-select: none;
      }

      .cji-popup-title {
        font-weight: 600;
        font-size: 14px;
      }

      .cji-popup-close {
        background: none;
        border: none;
        font-size: 24px;
        line-height: 1;
        color: #999;
        cursor: pointer;
        padding: 0;
        width: 24px;
        height: 24px;
      }

      .cji-popup-close:hover {
        color: #333;
      }

      .cji-popup-body {
        padding: 12px;
      }

      #cji-popup-buttons {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }

      .cji-popup-btn {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 12px;
        background: #f5f5f5;
        border: 1px solid #ddd;
        border-radius: 6px;
        cursor: pointer;
        font-size: 14px;
        transition: background 0.2s;
      }

      .cji-popup-btn:hover {
        background: #e8e8e8;
      }

      .cji-popup-btn:active {
        background: #d8d8d8;
      }

      .cji-btn-icon {
        font-size: 18px;
      }

      .cji-btn-label {
        flex: 1;
      }

      .cji-popup-dark {
        background: #2d2d2d;
        color: #fff;
      }

      .cji-popup-dark .cji-popup-header {
        border-bottom-color: #444;
      }

      .cji-popup-dark .cji-popup-close {
        color: #aaa;
      }

      .cji-popup-dark .cji-popup-close:hover {
        color: #fff;
      }

      .cji-popup-dark .cji-popup-btn {
        background: #3d3d3d;
        border-color: #555;
        color: #fff;
      }

      .cji-popup-dark .cji-popup-btn:hover {
        background: #4d4d4d;
      }

      .cji-popup-dark .cji-popup-btn:active {
        background: #5d5d5d;
      }
    `;
    (document.head || document.documentElement).appendChild(popupStyle);
  }
}

// Inject styles when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    injectStyles();
    OverlayManager.preloadAudio();
  });
} else {
  injectStyles();
  OverlayManager.preloadAudio();
}

// Keyboard shortcut to toggle popup (Ctrl+Shift+P)
window.addEventListener('keydown', (e) => {
  if (e.ctrlKey && e.shiftKey && e.key === 'P') {
    e.preventDefault();
    PopupController.toggle();
  }
});

console.log('✅ UI Feedback module loaded (OverlayManager + PopupController)');
