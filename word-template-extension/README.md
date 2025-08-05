# Word Template Extension

**Automatically extract data from websites and populate Word document templates**

Transform your web browsing into professional documents with just a few clicks. The Word Template Extension extracts information from any website and automatically fills your custom Word templates, saving hours of manual data entry.

## 🚀 What It Does

- **Extract Data**: Automatically capture text, numbers, dates, and other information from websites
- **Fill Templates**: Populate your Word document templates with the extracted data
- **Save Time**: Convert web content to professional documents in seconds
- **Stay Organized**: Generate consistent, formatted documents for invoices, reports, letters, and more

## 📋 Quick Start

1. **Install the Extension**: Add to Chrome or Edge browser
2. **Set Up Templates**: Create Word documents with placeholders like `{{TITLE}}` and `{{DATE}}`
3. **Browse & Extract**: Visit any website and click the extension icon
4. **Generate Document**: Your Word template is automatically filled and saved

## 🔄 How It Works

```
Website Data → Browser Extension → Template Processing → Word Document
     ↓              ↓                    ↓                ↓
  Text, dates,   Extracts and      Replaces {{}}      Professional
  amounts, etc.   organizes        placeholders       formatted doc
```

## 💼 Perfect For

- **Invoicing**: Extract client details and project info to generate invoices
- **Reporting**: Convert web data into formatted business reports  
- **Documentation**: Create consistent project documentation from online sources
- **Data Collection**: Transform web research into organized documents
- **Administrative Tasks**: Automate repetitive document creation

## 🖥️ System Requirements

- **Operating System**: Windows 10/11, macOS 10.14+, or Linux
- **Browser**: Chrome 88+ or Edge 88+
- **Microsoft Word**: 2016 or later (for viewing generated documents)
- **Python**: 3.7 or later (automatically installed during setup)

## 📦 Installation

### Windows (Recommended)
1. Download the extension package
2. Run `install.bat` as Administrator
3. Follow the setup wizard
4. Install browser extension from Chrome Web Store

### Other Platforms
See our detailed [Installation Guide](INSTALLATION.md) for step-by-step instructions.

## 🎯 Basic Usage

1. **Create a Template**
   - Open Word and create your document layout
   - Add placeholders: `{{TITLE}}`, `{{DATE}}`, `{{AMOUNT}}`, etc.
   - Save as `.docx` in your Templates folder

2. **Extract Data**
   - Visit any website with the information you need
   - Click the Word Template Extension icon
   - Select your template from the dropdown

3. **Generate Document**
   - The extension extracts relevant data automatically
   - Your template is filled and saved to the Generated folder
   - The completed document opens in Word

## 📁 File Locations

- **Templates**: `Documents/Templates/` (your Word template files)
- **Generated**: `Documents/Generated/` (completed documents)
- **Configuration**: Automatically managed by the extension

## 🔧 Troubleshooting

### Extension Not Working
- Restart your browser after installation
- Check that the native host is properly registered
- Verify Python is installed and accessible

### Template Issues
- Ensure placeholders use double curly braces: `{{FIELD_NAME}}`
- Check that template files are in `.docx` format
- Verify templates are in the correct folder

### Permission Problems
- Run installation as Administrator (Windows)
- Check file permissions for Templates and Generated folders
- Ensure antivirus isn't blocking the extension

## 📚 Documentation

- **[Installation Guide](INSTALLATION.md)**: Complete setup instructions for all platforms
- **[User Guide](USER-GUIDE.md)**: Detailed usage instructions and template creation
- **[Developer Guide](DEVELOPER.md)**: Technical documentation for customization
- **[Template Guide](templates/template-guide.md)**: Comprehensive template creation reference

## 🆘 Getting Help

1. **Check the logs**: Look for error messages in the extension console
2. **Review documentation**: Most issues are covered in our guides
3. **Test with simple templates**: Start with basic placeholders
4. **Verify installation**: Ensure all components are properly installed

## 🔒 Security & Privacy

- **Local Processing**: All data processing happens on your computer
- **No Cloud Storage**: Templates and documents stay on your device
- **Minimal Permissions**: Extension only accesses data you explicitly extract
- **Open Source**: Code is available for security review

## 🏗️ Architecture

The extension consists of three main components:

1. **Browser Extension**: Captures data from websites and provides user interface
2. **Native Host**: Python application that processes templates and generates documents
3. **Template System**: Word documents with placeholder variables for dynamic content

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

We welcome contributions! See our [Developer Guide](DEVELOPER.md) for information on:
- Setting up a development environment
- Code structure and architecture
- Submitting bug reports and feature requests
- Contributing code improvements

---

**Ready to get started?** Check out our [Installation Guide](INSTALLATION.md) for detailed setup instructions.