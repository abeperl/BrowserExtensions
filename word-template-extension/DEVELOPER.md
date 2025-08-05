# Developer Guide

**Technical documentation for developing, extending, and contributing to the Word Template Extension**

This guide provides comprehensive technical information for developers who want to understand, modify, or extend the Word Template Extension functionality.

## 🏗️ Architecture Overview

The Word Template Extension consists of three main components that work together to provide seamless web-to-document automation:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Browser        │    │  Native          │    │  Word           │
│  Extension      │◄──►│  Messaging       │◄──►│  Document       │
│                 │    │  Host            │    │  Processing     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
│                      │                      │
│ • Content Scripts    │ • Python App        │ • python-docx
│ • Popup Interface    │ • JSON Protocol     │ • Template Engine
│ • Data Extraction    │ • File Management   │ • Placeholder Replace
│ • User Interface     │ • Error Handling    │ • Document Generation
└─────────────────     └──────────────────     └─────────────────
```

### Component Responsibilities

1. **Browser Extension**: User interface, web data extraction, native messaging client
2. **Native Host**: Template processing, Word document manipulation, file system operations
3. **Template System**: Document templates with placeholder variables for dynamic content

## 📁 Project Structure

```
word-template-extension/
├── extension/                    # Browser extension files
│   ├── manifest.json            # Extension manifest (Manifest V3)
│   ├── popup.html               # Extension popup interface
│   ├── popup.js                 # Popup logic and UI handling
│   ├── content.js               # Content script for data extraction
│   ├── background.js            # Service worker for native messaging
│   ├── styles.css               # Extension styling
│   └── icons/                   # Extension icons (16, 32, 48, 128px)
├── native-host/                 # Native messaging host
│   ├── word_updater.py          # Main Python application
│   ├── requirements.txt         # Python dependencies
│   ├── native-messaging-host-manifest.json  # Native host manifest
│   ├── install.bat              # Windows installation script
│   └── README.md               # Native host documentation
├── templates/                   # Template system
│   ├── sample-template.docx     # Example Word template
│   └── template-guide.md        # Template creation guide
├── README.md                    # Main project documentation
├── INSTALLATION.md              # Setup instructions
├── USER-GUIDE.md               # User documentation
└── DEVELOPER.md                # This file
```

## 🔧 Development Environment Setup

### Prerequisites

- **Python 3.7+**: For native host development
- **Node.js 16+**: For browser extension development (if using build tools)
- **Git**: For version control
- **Code Editor**: VS Code recommended with extensions:
  - Python extension
  - JavaScript/TypeScript support
  - Chrome extension development tools

### Initial Setup

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd word-template-extension
   ```

2. **Set Up Python Environment**
   ```bash
   cd native-host
   python -m venv venv
   
   # Windows
   venv\Scripts\activate
   
   # macOS/Linux
   source venv/bin/activate
   
   pip install -r requirements.txt
   ```

3. **Install Development Dependencies**
   ```bash
   # Optional: For extension development
   npm install -g web-ext
   pip install pyinstaller  # For building executables
   ```

## 🌐 Browser Extension Development

> **Note**: The browser extension files are currently not present in the repository. This section provides the expected structure and implementation details for when they are added.

### Expected Extension Structure

#### manifest.json (Manifest V3)
```json
{
  "manifest_version": 3,
  "name": "Word Template Extension",
  "version": "1.0.0",
  "description": "Extract web data and populate Word templates",
  "permissions": [
    "activeTab",
    "storage",
    "nativeMessaging"
  ],
  "host_permissions": [
    "http://*/*",
    "https://*/*"
  ],
  "action": {
    "default_popup": "popup.html",
    "default_title": "Word Template Extension"
  },
  "content_scripts": [
    {
      "matches": ["<all_urls>"],
      "js": ["content.js"]
    }
  ],
  "background": {
    "service_worker": "background.js"
  },
  "icons": {
    "16": "icons/icon16.png",
    "32": "icons/icon32.png",
    "48": "icons/icon48.png",
    "128": "icons/icon128.png"
  }
}
```

#### Key Extension Components

**popup.html**: Extension popup interface
- Template selection dropdown
- Data preview area
- Generate button
- Settings access

**popup.js**: Popup logic
- Template management
- Data extraction coordination
- Native messaging communication
- User interface updates

**content.js**: Content script for data extraction
- DOM parsing and data extraction
- Text pattern recognition
- User selection handling
- Data formatting and validation

**background.js**: Service worker
- Native messaging host communication
- Extension lifecycle management
- Cross-tab communication
- Error handling and logging

### Data Extraction Engine

The content script implements intelligent data extraction:

```javascript
// Example data extraction patterns
const extractionPatterns = {
  email: /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g,
  phone: /(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})/g,
  currency: /\$[\d,]+\.?\d*/g,
  date: /\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b/g
};

function extractPageData() {
  const data = {};
  
  // Extract structured data
  data.title = document.title;
  data.url = window.location.href;
  
  // Extract contact information
  data.emails = extractPattern(extractionPatterns.email);
  data.phones = extractPattern(extractionPatterns.phone);
  
  // Extract business data
  data.amounts = extractPattern(extractionPatterns.currency);
  data.dates = extractPattern(extractionPatterns.date);
  
  return data;
}
```

### Native Messaging Protocol

Communication between extension and native host uses JSON messages:

```javascript
// Extension to Native Host
const message = {
  action: "update_template",
  data: {
    template: "invoice-template.docx",
    extractedData: {
      title: "Sample Company",
      amount: "$1,234.56",
      date: "2024-01-15",
      email: "contact@company.com"
    }
  }
};

// Native Host Response
const response = {
  success: true,
  message: "Document generated successfully",
  outputPath: "/path/to/generated/document.docx"
};
```

## 🐍 Native Host Development

### Core Architecture

The native host (`word_updater.py`) is built with a modular architecture:

```python
class WordTemplateUpdater:
    def __init__(self):
        self.config_dir = Path.home() / "AppData" / "Local" / "WordTemplateExtension"
        self.load_config()
    
    def run(self):
        """Main message processing loop"""
        while True:
            message = self.read_message()
            response = self.process_message(message)
            self.send_message(response)
    
    def process_message(self, message):
        """Route messages to appropriate handlers"""
        action = message.get('action')
        
        if action == 'update_template':
            return self.update_template(message['data'])
        elif action == 'list_templates':
            return self.list_templates()
        elif action == 'get_config':
            return self.get_config()
        # ... other actions
```

### Key Features

#### Template Processing Engine
```python
def replace_placeholders(self, doc, data):
    """Replace placeholders in Word document with extracted data"""
    for paragraph in doc.paragraphs:
        for placeholder, value in data.items():
            placeholder_text = f"{{{{{placeholder}}}}}"
            if placeholder_text in paragraph.text:
                paragraph.text = paragraph.text.replace(placeholder_text, str(value))
    
    # Process tables
    for table in doc.tables:
        for row in table.rows:
            for cell in row.cells:
                for placeholder, value in data.items():
                    placeholder_text = f"{{{{{placeholder}}}}}"
                    if placeholder_text in cell.text:
                        cell.text = cell.text.replace(placeholder_text, str(value))
```

#### Configuration Management
```python
def load_config(self):
    """Load user configuration with defaults"""
    default_config = {
        "template_path": str(Path.home() / "Documents" / "Templates"),
        "output_path": str(Path.home() / "Documents" / "Generated"),
        "auto_open": True,
        "default_template": "template.docx"
    }
    
    if self.config_file.exists():
        with open(self.config_file, 'r') as f:
            user_config = json.load(f)
            default_config.update(user_config)
    
    self.config = default_config
```

#### Error Handling and Logging
```python
import logging

# Configure comprehensive logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(log_file),
        logging.StreamHandler(sys.stderr)
    ]
)

def safe_process_template(self, template_path, data):
    """Process template with comprehensive error handling"""
    try:
        doc = Document(template_path)
        self.replace_placeholders(doc, data)
        return doc
    except FileNotFoundError:
        logger.error(f"Template not found: {template_path}")
        raise TemplateNotFoundError(f"Template {template_path} does not exist")
    except Exception as e:
        logger.error(f"Template processing failed: {str(e)}")
        raise TemplateProcessingError(f"Failed to process template: {str(e)}")
```

### Native Messaging Protocol Implementation

#### Message Reading
```python
def read_message(self):
    """Read message from browser extension"""
    raw_length = sys.stdin.buffer.read(4)
    if len(raw_length) == 0:
        sys.exit(0)
    
    message_length = struct.unpack('=I', raw_length)[0]
    message = sys.stdin.buffer.read(message_length).decode('utf-8')
    return json.loads(message)
```

#### Message Sending
```python
def send_message(self, message):
    """Send response back to browser extension"""
    encoded_message = json.dumps(message).encode('utf-8')
    encoded_length = struct.pack('=I', len(encoded_message))
    
    sys.stdout.buffer.write(encoded_length)
    sys.stdout.buffer.write(encoded_message)
    sys.stdout.buffer.flush()
```

## 🧪 Testing

### Unit Testing

Create comprehensive tests for the native host:

```python
# test_word_updater.py
import unittest
from unittest.mock import patch, MagicMock
from word_updater import WordTemplateUpdater

class TestWordTemplateUpdater(unittest.TestCase):
    def setUp(self):
        self.updater = WordTemplateUpdater()
    
    def test_placeholder_replacement(self):
        """Test placeholder replacement functionality"""
        mock_doc = MagicMock()
        mock_paragraph = MagicMock()
        mock_paragraph.text = "Hello {{NAME}}, your amount is {{AMOUNT}}"
        mock_doc.paragraphs = [mock_paragraph]
        mock_doc.tables = []
        
        data = {"NAME": "John", "AMOUNT": "$100"}
        self.updater.replace_placeholders(mock_doc, data)
        
        expected = "Hello John, your amount is $100"
        self.assertEqual(mock_paragraph.text, expected)
    
    def test_config_loading(self):
        """Test configuration loading with defaults"""
        self.assertIn("template_path", self.updater.config)
        self.assertIn("output_path", self.updater.config)
        self.assertTrue(isinstance(self.updater.config["auto_open"], bool))
```

### Integration Testing

Test the complete workflow:

```python
def test_end_to_end_processing(self):
    """Test complete message processing workflow"""
    message = {
        "action": "update_template",
        "data": {
            "template": "test-template.docx",
            "title": "Test Document",
            "amount": "$500.00",
            "date": "2024-01-15"
        }
    }
    
    with patch('word_updater.Document') as mock_doc:
        response = self.updater.process_message(message)
        self.assertTrue(response["success"])
        self.assertIn("outputPath", response)
```

### Browser Extension Testing

```javascript
// Test data extraction
describe('Data Extraction', () => {
  test('should extract email addresses', () => {
    document.body.innerHTML = '<p>Contact us at test@example.com</p>';
    const data = extractPageData();
    expect(data.emails).toContain('test@example.com');
  });
  
  test('should extract currency amounts', () => {
    document.body.innerHTML = '<span>Price: $1,234.56</span>';
    const data = extractPageData();
    expect(data.amounts).toContain('$1,234.56');
  });
});
```

## 🔨 Building and Packaging

### Native Host Executable

Create standalone executables for distribution:

```bash
# Install PyInstaller
pip install pyinstaller

# Build executable
pyinstaller --onefile --console --name word_updater word_updater.py

# The executable will be in dist/word_updater.exe
```

### Browser Extension Packaging

```bash
# For Chrome Web Store
zip -r word-template-extension.zip extension/

# For development testing
web-ext build --source-dir=extension/
```

### Cross-Platform Builds

```bash
# Windows
pyinstaller --onefile --console word_updater.py

# macOS (requires macOS machine)
pyinstaller --onefile --console word_updater.py

# Linux
pyinstaller --onefile --console word_updater.py
```

## 🚀 Adding New Features

### Extending Data Extraction

Add new extraction patterns:

```javascript
// In content.js
const newPatterns = {
  socialSecurity: /\b\d{3}-\d{2}-\d{4}\b/g,
  zipCode: /\b\d{5}(-\d{4})?\b/g,
  url: /https?:\/\/[^\s]+/g
};

function extractCustomData() {
  // Implement custom extraction logic
  return extractedData;
}
```

### Adding Template Features

Extend the template processing engine:

```python
def process_advanced_placeholders(self, doc, data):
    """Process advanced placeholder features"""
    
    # Conditional content
    for paragraph in doc.paragraphs:
        if "{{#IF_" in paragraph.text:
            self.process_conditional(paragraph, data)
    
    # Date formatting
    for placeholder, value in data.items():
        if placeholder.endswith("_DATE"):
            formatted_date = self.format_date(value)
            data[placeholder] = formatted_date
    
    # Mathematical operations
    if "{{TOTAL}}" in doc and "{{SUBTOTAL}}" in doc and "{{TAX}}" in doc:
        self.calculate_totals(data)
```

### Custom Actions

Add new native messaging actions:

```python
def process_message(self, message):
    """Extended message processing with new actions"""
    action = message.get('action')
    
    if action == 'validate_template':
        return self.validate_template(message['data'])
    elif action == 'backup_templates':
        return self.backup_templates()
    elif action == 'import_template':
        return self.import_template(message['data'])
    # ... existing actions
```

## 🔒 Security Considerations

### Input Validation

```python
def validate_input(self, data):
    """Validate all input data for security"""
    
    # Sanitize file paths
    if 'template' in data:
        template_path = Path(data['template'])
        if not template_path.is_relative_to(self.config['template_path']):
            raise SecurityError("Template path outside allowed directory")
    
    # Validate data types
    for key, value in data.items():
        if not isinstance(value, (str, int, float)):
            raise ValidationError(f"Invalid data type for {key}")
    
    # Sanitize content
    for key, value in data.items():
        if isinstance(value, str):
            data[key] = self.sanitize_string(value)
```

### File System Security

```python
def secure_file_operations(self, file_path):
    """Ensure secure file operations"""
    
    # Resolve path and check bounds
    resolved_path = Path(file_path).resolve()
    allowed_paths = [
        Path(self.config['template_path']).resolve(),
        Path(self.config['output_path']).resolve()
    ]
    
    if not any(resolved_path.is_relative_to(allowed) for allowed in allowed_paths):
        raise SecurityError("File access outside allowed directories")
    
    return resolved_path
```

### Extension Security

```javascript
// Content Security Policy in manifest.json
{
  "content_security_policy": {
    "extension_pages": "script-src 'self'; object-src 'self'"
  }
}

// Sanitize extracted data
function sanitizeData(data) {
  const sanitized = {};
  for (const [key, value] of Object.entries(data)) {
    if (typeof value === 'string') {
      sanitized[key] = value.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');
    } else {
      sanitized[key] = value;
    }
  }
  return sanitized;
}
```

## 📊 Performance Optimization

### Native Host Optimization

```python
# Lazy loading of dependencies
def get_document_processor():
    """Lazy load document processing to improve startup time"""
    if not hasattr(get_document_processor, '_processor'):
        from docx import Document
        get_document_processor._processor = Document
    return get_document_processor._processor

# Caching frequently used templates
from functools import lru_cache

@lru_cache(maxsize=10)
def load_template(template_path):
    """Cache frequently used templates"""
    return Document(template_path)
```

### Extension Performance

```javascript
// Debounce data extraction
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

const debouncedExtraction = debounce(extractPageData, 300);

// Efficient DOM querying
function optimizedDataExtraction() {
  const walker = document.createTreeWalker(
    document.body,
    NodeFilter.SHOW_TEXT,
    null,
    false
  );
  
  const textNodes = [];
  let node;
  while (node = walker.nextNode()) {
    textNodes.push(node.textContent);
  }
  
  return processTextNodes(textNodes);
}
```

## 🤝 Contributing Guidelines

### Code Standards

#### Python Code Style
- Follow PEP 8 style guidelines
- Use type hints for function parameters and return values
- Include comprehensive docstrings
- Maximum line length: 88 characters (Black formatter)

```python
def process_template(
    self, 
    template_path: Path, 
    data: Dict[str, Any]
) -> Tuple[bool, str]:
    """
    Process a Word template with provided data.
    
    Args:
        template_path: Path to the Word template file
        data: Dictionary containing placeholder data
        
    Returns:
        Tuple of (success: bool, message: str)
        
    Raises:
        TemplateNotFoundError: If template file doesn't exist
        ProcessingError: If template processing fails
    """
    pass
```

#### JavaScript Code Style
- Use ES6+ features
- Follow Airbnb JavaScript style guide
- Use meaningful variable names
- Include JSDoc comments for functions

```javascript
/**
 * Extract data from the current webpage
 * @param {Object} options - Extraction options
 * @param {string[]} options.patterns - Patterns to extract
 * @param {boolean} options.includeMetadata - Include page metadata
 * @returns {Promise<Object>} Extracted data object
 */
async function extractPageData(options = {}) {
  // Implementation
}
```

### Commit Guidelines

Use conventional commit format:

```
feat: add support for table data extraction
fix: resolve template path validation issue
docs: update installation guide for macOS
test: add unit tests for data extraction
refactor: improve error handling in native host
```

### Pull Request Process

1. **Fork the repository** and create a feature branch
2. **Write tests** for new functionality
3. **Update documentation** as needed
4. **Ensure all tests pass** and code follows style guidelines
5. **Submit pull request** with clear description of changes

### Development Workflow

```bash
# 1. Set up development environment
git clone <your-fork>
cd word-template-extension
python -m venv venv
source venv/bin/activate  # or venv\Scripts\activate on Windows
pip install -r native-host/requirements.txt
pip install -r dev-requirements.txt  # Development dependencies

# 2. Create feature branch
git checkout -b feature/new-extraction-pattern

# 3. Make changes and test
python -m pytest tests/
npm test  # If using Node.js for extension testing

# 4. Format code
black native-host/
prettier --write extension/

# 5. Commit and push
git add .
git commit -m "feat: add new extraction pattern for addresses"
git push origin feature/new-extraction-pattern
```

## 📚 API Reference

### Native Messaging Protocol

#### Supported Actions

| Action | Description | Parameters | Response |
|--------|-------------|------------|----------|
| `ping` | Health check | None | `{success: true, message: "pong"}` |
| `update_template` | Process template | `{template, data}` | `{success, message, outputPath}` |
| `list_templates` | Get available templates | None | `{success, templates: []}` |
| `get_config` | Get current configuration | None | `{success, config: {}}` |
| `update_config` | Update configuration | `{config}` | `{success, message}` |

#### Message Format

```json
{
  "action": "update_template",
  "data": {
    "template": "invoice-template.docx",
    "extractedData": {
      "COMPANY": "Acme Corp",
      "AMOUNT": "$1,234.56",
      "DATE": "2024-01-15"
    }
  }
}
```

### Configuration Schema

```json
{
  "template_path": "string - Path to templates directory",
  "output_path": "string - Path to generated documents",
  "auto_open": "boolean - Auto-open generated documents",
  "default_template": "string - Default template filename",
  "date_format": "string - Date formatting pattern",
  "currency_format": "string - Currency formatting pattern"
}
```

## 🔍 Debugging

### Native Host Debugging

```python
# Enable debug logging
import logging
logging.getLogger().setLevel(logging.DEBUG)

# Add debug breakpoints
import pdb; pdb.set_trace()

# Test native host manually
echo '{"action":"ping"}' | python word_updater.py
```

### Extension Debugging

```javascript
// Browser console debugging
console.log('Extension data:', extractedData);

// Chrome DevTools
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  console.log('Received message:', message);
  debugger; // Breakpoint in DevTools
});
```

### Common Debug Scenarios

1. **Template not found**: Check file paths and permissions
2. **Data extraction failing**: Verify page structure and extraction patterns
3. **Native messaging errors**: Check manifest registration and permissions
4. **Document generation issues**: Verify Word template format and placeholders

---

## 📈 Future Improvements

### Scalability Enhancements
- **S1**: Implement template caching for improved performance
- **S2**: Add support for batch processing multiple documents
- **S3**: Create template marketplace for sharing common templates

### Security Improvements
- **S1**: Implement template signing and verification
- **S2**: Add sandboxed template processing environment
- **S3**: Enhanced input validation and sanitization

### Maintainability Improvements
- **S1**: Modularize codebase with clear separation of concerns
- **S2**: Implement comprehensive logging and monitoring
- **S3**: Add automated testing and continuous integration

### Areas for Further Investigation
- Machine learning for improved data extraction accuracy
- Support for additional document formats (PDF, Excel)
- Cloud-based template synchronization
- Advanced template features (charts, images, conditional formatting)
- Integration with popular business applications (CRM, ERP systems)

---

**Ready to contribute?** Start by setting up your development environment and exploring the codebase. Check out the [issues](../../issues) for good first contributions.