#!/usr/bin/env python3
"""
Generate JavaScript with base64-embedded audio for notification sounds
Reads MP3 files and generates JavaScript constant declarations
"""

import base64
import os

def read_base64(filename):
    """Read file and return base64 encoded string"""
    with open(filename, 'rb') as f:
        return base64.b64encode(f.read()).decode('utf-8')

def generate_sound_constants():
    """Generate JavaScript constant declarations for embedded sounds"""

    sounds = {
        'success': 'success.mp3',
        'error': 'error.mp3',
        'warning': 'warning.mp3',
        'info': 'info.mp3'
    }

    cdn_urls = {
        'success': 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/button_click.mp3',
        'error': 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/computer_error.mp3',
        'warning': 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/door_bell.mp3',
        'info': 'https://cdnjs.cloudflare.com/ajax/libs/ion-sound/3.0.7/sounds/bell_ring.mp3'
    }

    print("// Base64-embedded notification sounds")
    print("// Generated from ion-sound CDN (https://cdnjs.com/libraries/ion-sound)")
    print("// File sizes: success=13KB, error=11KB, warning=18KB, info=31KB")
    print()
    print("const SOUND_CDN_URLS = {")
    for sound_type, url in cdn_urls.items():
        print(f"  {sound_type}: '{url}',")
    print("};")
    print()
    print("const SOUND_BASE64_DATA = {")

    for sound_type, filename in sounds.items():
        if os.path.exists(filename):
            b64_data = read_base64(filename)
            file_size = len(b64_data)
            print(f"  // {sound_type}.mp3 - {file_size} bytes base64")
            print(f"  {sound_type}: 'data:audio/mp3;base64,{b64_data}',")
            print()
        else:
            print(f"  // {filename} not found - skipping")
            print(f"  {sound_type}: null,")
            print()

    print("};")

if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    generate_sound_constants()