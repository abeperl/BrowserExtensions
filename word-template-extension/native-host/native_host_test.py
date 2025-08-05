import sys
import struct
import datetime

with open("native_host_test.log", "a") as log:
    log.write(f"Started at {datetime.datetime.now()}\n")
    try:
        # Try to read a native message (4-byte length + JSON)
        raw_length = sys.stdin.buffer.read(4)
        if raw_length:
            message_length = struct.unpack('<I', raw_length)[0]
            message = sys.stdin.buffer.read(message_length).decode('utf-8')
            log.write(f"Received message: {message}\n")
        else:
            log.write("No message received.\n")
    except Exception as e:
        log.write(f"Exception: {e}\n")