# Audio Files Folder

This folder should contain MP3 audio files for the overlay notification system.

## Required Files

Place the following MP3 files in this folder:

- **success.mp3** - Success notification sound
- **error.mp3** - Error alert sound
- **warning.mp3** - Warning notification sound
- **info.mp3** - Information notification sound

## Where to Get Free Sounds

See the main [AUDIO-SETUP.md](../AUDIO-SETUP.md) file for detailed download links and instructions.

### Quick Start (Recommended)

1. Go to https://mixkit.co/free-sound-effects/notification/
2. Download 4 short notification sounds (< 1 second each)
3. Rename them to match the names above
4. Place them in this folder
5. Reload your page - sounds will auto-load!

## File Requirements

- **Format**: MP3 (WAV also supported)
- **Duration**: 0.1s - 1.0s (shorter is better)
- **Size**: Keep under 50KB per file
- **Bitrate**: 128 kbps or higher

## No Files? No Problem!

If you don't place any MP3 files here, the system will **automatically fallback** to enhanced synthesized sounds using Web Audio API. The sounds are pleasant and work great without any external files!

## Testing

Open `audio-test-demo.html` in your browser to test the audio system and verify files are loading correctly.
