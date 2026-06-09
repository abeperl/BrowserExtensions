// sound-cdn-config.js
// CDN-hosted notification sounds configuration
// Uses ion-sound library from Cloudflare CDN (free, fast, reliable)
// Source: https://cdnjs.com/libraries/ion-sound

const SOUND_CDN_URLS = {
  // Success/Confirmation - button click sound (13.7 KB)
  success: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/button_click.mp3',

  // Error/Alert - computer error sound (10.9 KB)
  error: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/computer_error.mp3',

  // Warning/Attention - door bell sound (17.9 KB)
  warning: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/door_bell.mp3',

  // Info/Notification - bell ring sound (30.8 KB)
  info: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/bell_ring.mp3'
};

// Alternative sounds from same CDN (in case you want to swap)
const SOUND_CDN_ALTERNATIVES = {
  successAlt: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/snap.mp3',
  warningAlt: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/button_push.mp3',
  infoAlt: 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/water_droplet.mp3'
};

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { SOUND_CDN_URLS, SOUND_CDN_ALTERNATIVES };
}