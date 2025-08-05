#!/usr/bin/env python3
import sys
import json
import struct
import subprocess

def send_message(message):
    """Send a message in Chrome native messaging format"""
    message_json = json.dumps(message)
    message_bytes = message_json.encode('utf-8')
    length_bytes = struct.pack('<I', len(message_bytes))
    
    return length_bytes + message_bytes

def test_native_host():
    """Test the native host with a list_templates message"""
    ping_message = {"action": "list_templates"}
    message_data = send_message(ping_message)
    
    # Run the native host and send the message
    process = subprocess.Popen(
        [sys.executable, 'word_updater.py'],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE
    )
    
    stdout, stderr = process.communicate(input=message_data)
    
    try:
        print("STDOUT:", stdout.decode('utf-8'))
    except UnicodeDecodeError:
        print("STDOUT (hex):", stdout.hex())
    
    try:
        print("STDERR:", stderr.decode('utf-8'))
    except UnicodeDecodeError:
        print("STDERR (hex):", stderr.hex())
        
    print("Return code:", process.returncode)

if __name__ == "__main__":
    test_native_host()