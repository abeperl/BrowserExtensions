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
    volume: 0.3, // Volume level (0.0 to 1.0)
    dismissKey: 'Escape',
    useMp3Files: true, // Try MP3 files first, fallback to synthesized
    soundsPath: './audio/' // Path to MP3 files
  };

  // Audio cache for MP3 files
  const audioCache = {
    success: null,
    error: null,
    warning: null,
    info: null
  };

  // Preload MP3 files
  function preloadAudioFiles() {
    if (!settings.useMp3Files) return;

    const soundFiles = {
      success: 'success.mp3',
      error: 'error.mp3',
      warning: 'warning.mp3',
      info: 'info.mp3'
    };

    Object.keys(soundFiles).forEach(type => {
      const audio = new Audio();
      audio.volume = settings.volume;
      audio.preload = 'auto';
      audio.src = settings.soundsPath + soundFiles[type];

      // Handle load success
      audio.addEventListener('canplaythrough', () => {
        audioCache[type] = audio;
      }, { once: true });

      // Handle load failure silently
      audio.addEventListener('error', () => {
        console.warn(`Failed to load ${type} sound, will use synthesized fallback`);
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

// Add overlay styles when DOM is ready
function injectOverlayStyles() {
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
    (document.head || document.documentElement).appendChild(style);
  }
}

// Inject styles when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    injectOverlayStyles();
    OverlayManager.preloadAudio();
  });
} else {
  injectOverlayStyles();
  OverlayManager.preloadAudio();
}
