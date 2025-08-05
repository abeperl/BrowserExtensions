# Extension Development and Testing Guide

## Quick Development Setup

### 1. Load Extension for Testing
```powershell
# Open Chrome/Edge and navigate to:
# chrome://extensions/ or edge://extensions/

# Enable Developer mode (toggle in top right)
# Click "Load unpacked" 
# Select: c:\Users\User\source\repos\MalchutFiles\word-template-extension\extension
```

### 2. Test Native Host Communication
```powershell
cd "c:\Users\User\source\repos\MalchutFiles\word-template-extension\native-host"

# Install dependencies
pip install python-docx pywin32

# Test the native host manually
python word_updater.py
```

### 3. Build and Package
```powershell
cd "c:\Users\User\source\repos\MalchutFiles\word-template-extension"

# Build packages for store submission
.\build-packages.ps1

# Build with source code included
.\build-packages.ps1 -IncludeSource

# Skip native host build
.\build-packages.ps1 -BuildNativeHost:$false
```

## Testing Checklist

### ✅ Extension Loading
- [ ] Extension loads without errors in Chrome
- [ ] Extension loads without errors in Edge  
- [ ] All icons appear correctly
- [ ] Popup opens and displays properly

### ✅ Data Extraction
- [ ] Email extraction works on test pages
- [ ] Phone number extraction works
- [ ] Currency amount extraction works
- [ ] Date extraction works
- [ ] Selected text extraction works
- [ ] Table data extraction works

### ✅ Native Host Integration
- [ ] Native host executable exists
- [ ] Manifest file is properly configured
- [ ] Extension can communicate with native host
- [ ] Template listing works
- [ ] Document generation works

### ✅ Template Processing
- [ ] Templates load from default directory
- [ ] Placeholder replacement works
- [ ] Generated documents open correctly
- [ ] Output directory creation works

### ✅ User Interface
- [ ] Popup design is responsive
- [ ] All buttons function correctly
- [ ] Status messages display properly
- [ ] Advanced options work
- [ ] Settings save and load

### ✅ Error Handling
- [ ] Graceful handling of missing templates
- [ ] Proper error messages for failed extraction
- [ ] Network/communication error handling
- [ ] Invalid template handling

## Store Submission Preparation

### Chrome Web Store Requirements
1. **Developer Account**: $5 one-time fee
2. **Extension Package**: ZIP file (created by build script)
3. **Store Listing**:
   - Title: "Word Template Extension"
   - Summary: "Extract web data and populate Word templates seamlessly"
   - Detailed description: See STORE_SUBMISSION_GUIDE.md
   - Category: Productivity
   - Language: English

4. **Assets Required**:
   - Screenshots: 5 images, 1280x800px each
   - Small promotional tile: 440x280px
   - Large promotional tile: 920x680px (optional)
   - Marquee: 1400x560px (optional)

5. **Screenshots to Create**:
   - Extension popup with extracted data
   - Webpage showing extraction in action
   - Generated Word document example
   - Template selection interface
   - Settings/configuration screen

### Microsoft Edge Add-ons
1. **Partner Center Account**: Free
2. **Same package and assets** as Chrome
3. **Submission process**: Similar to Chrome Web Store

### Extension ID Generation
- **Development**: Generated when loading unpacked extension
- **Store**: Automatically assigned when published

## File Structure Summary

```
word-template-extension/
├── extension/                 # Main extension files
│   ├── manifest.json         # Extension manifest (V3)
│   ├── popup.html            # Extension popup interface
│   ├── popup.js              # Popup functionality
│   ├── background.js         # Service worker
│   ├── content.js            # Content script for extraction
│   ├── styles.css            # Popup styling
│   ├── welcome.html          # Welcome page
│   └── icons/                # Extension icons (16,32,48,128px)
├── native-host/              # Native messaging host
│   ├── word_updater.py       # Python native host
│   ├── word_updater.exe      # Compiled executable
│   ├── requirements.txt      # Python dependencies
│   └── install.bat           # Installation script
├── templates/                # Sample templates
├── build-packages.ps1        # Build script
├── STORE_SUBMISSION_GUIDE.md # Store submission guide
├── PRIVACY_POLICY.md         # Privacy policy
└── dist/                     # Generated packages
```

## Extension Permissions Explained

- **activeTab**: Read current webpage content when user clicks extract
- **storage**: Save user preferences and settings
- **nativeMessaging**: Communicate with Word processing application
- **contextMenus**: Add right-click menu options
- **host_permissions**: Work on any website (for data extraction)

## Development Tips

### Debugging
```javascript
// In popup.js or content.js
console.log('Debug info:', data);

// Check background service worker
// Go to chrome://extensions/ → Extension details → service worker → Console
```

### Testing Different Websites
Test data extraction on various sites:
- Contact pages (emails, phones)
- E-commerce sites (prices, dates)
- News articles (dates, URLs)
- Forms and tables
- Social media profiles

### Performance Optimization
- Limit extraction results (max 10 items per category)
- Use efficient regex patterns
- Implement proper error boundaries
- Add loading states for user feedback

## Deployment Workflow

1. **Development** → Test locally with unpacked extension
2. **Testing** → Comprehensive testing across scenarios
3. **Building** → Run build-packages.ps1 to create store packages
4. **Assets** → Create required screenshots and promotional images
5. **Submission** → Upload to Chrome Web Store and Edge Add-ons
6. **Review** → Wait for store review (1-3 days Chrome, 7-10 days Edge)
7. **Publication** → Extension goes live in stores
8. **Monitoring** → Track reviews, usage, and feedback

## Post-Launch Considerations

- Monitor user reviews and feedback
- Track usage analytics (if implemented)
- Plan feature updates and improvements
- Maintain compatibility with browser updates
- Update native host for new Word versions
- Respond to user support requests

---

**Ready to deploy? Run the build script and follow the store submission guide!**
