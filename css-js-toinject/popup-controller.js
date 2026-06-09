// popup-controller.js
// Condensed popup control interface for css-js-toinject project
// Manages popup UI state and interactions

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

// Add popup styles when DOM is ready
function injectPopupStyles() {
  if (!document.getElementById('cji-popup-styles')) {
    const style = document.createElement('style');
    style.id = 'cji-popup-styles';
    style.textContent = `
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
    (document.head || document.documentElement).appendChild(style);
  }
}

// Inject styles when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', injectPopupStyles);
} else {
  injectPopupStyles();
}

// Keyboard shortcut to toggle popup (Ctrl+Shift+P)
window.addEventListener('keydown', (e) => {
  if (e.ctrlKey && e.shiftKey && e.key === 'P') {
    e.preventDefault();
    PopupController.toggle();
  }
});
