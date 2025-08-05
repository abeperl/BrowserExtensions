# Word Template Extension - Native Host

This is the native messaging host component for the Word Template Extension. It receives data from the browser extension and updates Word document templates using the extracted information.

## Features

- **Native Messaging**: Communicates with browser extension via Chrome's native messaging API
- **Word Document Processing**: Uses python-docx to manipulate Word documents
- **Template Placeholders**: Supports dynamic replacement of placeholders like {{TITLE}}, {{DATE}}, {{AMOUNT}}, etc.
- **Auto-Open Documents**: Automatically opens generated documents after processing
- **Configurable Paths**: Customizable template and output directories
- **Error Handling**: Comprehensive logging and error reporting
- **Cross-Platform**: Works on Windows, macOS, and Linux

## Prerequisites

- **Python 3.7 or later**: Download from [python.org](https://python.org)
- **Microsoft Word**: For viewing/editing generated documents (optional)
- **Chrome or Edge**: For the browser extension

## Installation

### Windows (Automated)

1. **Run the installer**:
   ```cmd
   install.bat
   ```

2. **Update Extension ID**:
   - Open `native-messaging-host-manifest.json`
   - Replace `YOUR_EXTENSION_ID_HERE` with your actual extension ID
   - Save the file

### Manual Installation

1. **Install Python dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

2. **Register the native host**:
   
   **Windows (Chrome)**:
   ```cmd
   reg add "HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.wordtemplateextension.nativehost" /ve /t REG_SZ /d "C:\path\to\native-messaging-host-manifest.json" /f
   ```
   
   **Windows (Edge)**:
   ```cmd
   reg add "HKEY_CURRENT_USER\Software\Microsoft\Edge\NativeMessagingHosts\com.wordtemplateextension.nativehost" /ve /t REG_SZ /d "C:\path\to\native-messaging-host-manifest.json" /f
   ```
   
   **macOS/Linux**:
   - Chrome: `~/.config/google-chrome/NativeMessagingHosts/`
   - Edge: `~/.config/microsoft-edge/NativeMessagingHosts/`

3. **Update manifest paths**:
   - Edit `native-messaging-host-manifest.json`
   - Set correct path to `word_updater.py` or executable
   - Update extension ID

## Configuration

The native host creates a configuration file at:
- **Windows**: `%USERPROFILE%\AppData\Local\WordTemplateExtension\config.json`
- **macOS/Linux**: `~/.local/share/WordTemplateExtension/config.json`

### Default Configuration

```json
{
  "template_path": "~/Documents/Templates",
  "output_path": "~/Documents/Generated",
  "auto_open": true,
  "default_template": "template.docx"
}
```

### Configuration Options

- **template_path**: Directory containing Word templates
- **output_path**: Directory for generated documents
- **auto_open**: Whether to automatically open generated documents
- **default_template**: Default template filename

## Template Creation

### Supported Placeholders

Create Word templates with these placeholders:

- `{{TITLE}}` - Document title
- `{{DATE}}` - Current date or extracted date
- `{{AMOUNT}}` - Monetary amount
- `{{DESCRIPTION}}` - Description text
- `{{URL}}` - Source URL
- `{{TIMESTAMP}}` - Current timestamp
- `{{CUSTOM_FIELD}}` - Any custom field from extracted data

### Example Template

```
Invoice Template

Title: {{TITLE}}
Date: {{DATE}}
Amount: {{AMOUNT}}
Description: {{DESCRIPTION}}
Source: {{URL}}
Generated: {{TIMESTAMP}}
```

### Template Guidelines

1. **File Format**: Use `.docx` format
2. **Placeholders**: Use double curly braces `{{FIELD_NAME}}`
3. **Case Sensitive**: Placeholders are case-sensitive
4. **Location**: Place templates in the configured template directory
5. **Naming**: Use descriptive filenames (e.g., `invoice_template.docx`)

## Usage

### From Browser Extension

The native host is automatically invoked by the browser extension. No manual interaction required.

### Message Format

The native host accepts JSON messages with this structure:

```json
{
  "action": "update_template",
  "data": {
    "template": "invoice_template.docx",
    "title": "Sample Invoice",
    "date": "2024-01-15",
    "amount": "$1,234.56",
    "description": "Professional services",
    "url": "https://example.com"
  }
}
```

### Supported Actions

- **update_template**: Process a template with data
- **get_config**: Retrieve current configuration
- **update_config**: Update configuration settings
- **list_templates**: List available templates
- **ping**: Health check

## Troubleshooting

### Common Issues

1. **"Native host not found"**:
   - Verify registry entries (Windows) or manifest location
   - Check file paths in manifest
   - Restart browser after installation

2. **"Python not found"**:
   - Install Python 3.7+
   - Add Python to system PATH
   - Verify with `python --version`

3. **"Template not found"**:
   - Check template directory path
   - Verify template filename
   - Ensure templates are in .docx format

4. **"Permission denied"**:
   - Check file/directory permissions
   - Run as administrator (Windows)
   - Verify output directory exists

### Logging

Logs are written to:
- **Windows**: `%USERPROFILE%\AppData\Local\WordTemplateExtension\word_updater.log`
- **macOS/Linux**: `~/.local/share/WordTemplateExtension/word_updater.log`

### Testing

Test the native host manually:

```bash
# Test with sample message
echo '{"action":"ping"}' | python word_updater.py
```

Expected response:
```json
{"success": true, "message": "pong"}
```

## File Structure

```
native-host/
├── word_updater.py              # Main application
├── requirements.txt             # Python dependencies
├── native-messaging-host-manifest.json  # Chrome manifest
├── install.bat                  # Windows installer
└── README.md                   # This file
```

## Security Considerations

- **Extension ID Validation**: Only specified extension IDs can communicate
- **File Path Validation**: Paths are validated to prevent directory traversal
- **Input Sanitization**: All input data is sanitized before processing
- **Logging**: Sensitive data is not logged

## Development

### Building Executable

To create a standalone executable:

```bash
pip install pyinstaller
pyinstaller --onefile --console word_updater.py
```

### Testing Changes

1. Modify `word_updater.py`
2. Restart the browser
3. Test with browser extension

### Adding Features

The application is modular and extensible:

- Add new actions in the `run()` method
- Implement new placeholder types in `replace_placeholders()`
- Extend configuration options in `load_config()`

## Support

For issues and questions:

1. Check the log files for error details
2. Verify installation steps
3. Test with manual JSON input
4. Review browser extension console for errors

## License

This project is part of the Word Template Extension suite.