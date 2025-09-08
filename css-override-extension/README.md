# CSS Override Extension

A powerful browser extension that allows you to customize website appearance by injecting custom CSS rules based on URL patterns. Perfect for web developers, designers, and users who want to personalize their browsing experience.

## Features

- **URL Pattern Matching**: Apply CSS rules to specific websites using flexible URL patterns with wildcard support
- **Multiple Injection Modes**: Choose between "Override" (add additional styles) or "Replace" (replace existing styles)
- **Rule Management**: Easy-to-use interface for creating, editing, and organizing CSS rules
- **Live Preview**: See your changes applied immediately when navigating to matched URLs
- **Import/Export**: Backup and share your CSS rules with others
- **Cross-Browser Support**: Works on Chrome and Microsoft Edge

## Installation

### From Web Stores (Recommended)
1. **Chrome Web Store**: Visit [CSS Override Extension](https://chrome.google.com/webstore) and click "Add to Chrome"
2. **Microsoft Edge Add-ons**: Visit [CSS Override Extension](https://microsoftedge.microsoft.com/addons) and click "Get"

### Manual Installation (Development)
1. Download or clone this repository
2. Open your browser's extension management page:
   - Chrome: `chrome://extensions`
   - Edge: `edge://extensions`
3. Enable "Developer mode" in the top right corner
4. Click "Load unpacked" and select the `css-override-extension` folder
5. The extension should now appear in your browser toolbar

## How to Use

### Basic Usage
1. Click the CSS Override icon in your browser toolbar
2. Click "Add Rule" to create your first CSS rule
3. Enter a URL pattern (e.g., `*.example.com/*` for all pages on example.com)
4. Give your rule a descriptive name
5. Choose the injection mode:
   - **Override**: Adds your CSS on top of existing styles
   - **Replace**: Replaces the website's CSS with your custom rules
6. Enter your CSS code in the text area
7. Click "Save Rule"
8. Navigate to a matching URL to see your changes applied

### URL Pattern Examples
- `*.google.com/*` - All pages on google.com and its subdomains
- `https://example.com/page` - Exact URL match
- `https://example.com/*` - All pages under example.com
- `*` - All websites (use with caution)

### CSS Examples

#### Hide Annoying Elements
```css
/* Hide cookie banners */
.cookie-banner,
.gdpr-banner,
#cookie-consent {
  display: none !important;
}
```

#### Improve Readability
```css
/* Better font and spacing */
body {
  font-family: 'Georgia', serif;
  line-height: 1.6;
  color: #333;
}

/* Improve article readability */
.article-content p {
  margin-bottom: 1.5em;
  text-align: justify;
}
```

#### Dark Mode for Specific Sites
```css
/* Dark mode for a specific website */
body {
  background-color: #1a1a1a !important;
  color: #e0e0e0 !important;
}

a {
  color: #4fc3f7 !important;
}
```

## Advanced Features

### Settings Page
Access advanced settings by right-clicking the extension icon and selecting "Options" or by visiting the extension's options page.

- **Enable/Disable Extension**: Turn the extension on or off globally
- **Notification Settings**: Control when notifications are shown
- **Data Management**: Export/import your rules or clear all data

### Rule Organization
- Use descriptive names for your rules to keep them organized
- Enable/disable individual rules without deleting them
- Rules are applied in the order they appear (top to bottom)

## Permissions Explained

The extension requires the following permissions:

- **Storage**: Save your CSS rules and settings locally
- **Active Tab**: Access the current tab to apply CSS rules
- **Scripting**: Inject CSS into web pages
- **Tabs**: Monitor tab changes to apply rules automatically

## Development

### Project Structure
```
css-override-extension/
├── manifest.json          # Extension manifest
├── background.js          # Background service worker
├── content.js            # Content script for CSS injection
├── popup.html            # Extension popup interface
├── popup.js              # Popup functionality
├── settings.html         # Settings page
├── settings.js           # Settings functionality
├── styles.css            # Extension styles
├── icons/                # Extension icons
├── README.md             # This file
└── PRD.md               # Product Requirements Document
```

### Building for Production
1. Ensure all files are in place
2. Test the extension thoroughly
3. Create a ZIP file containing all extension files
4. Submit to Chrome Web Store and Microsoft Edge Add-ons

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Troubleshooting

### Rules Not Applying
- Check that the URL pattern matches the current page
- Ensure the rule is enabled (toggle switch is on)
- Try refreshing the page
- Check browser console for error messages

### CSS Not Working
- Verify your CSS syntax is valid
- Use browser developer tools to inspect elements
- Try using `!important` for overriding existing styles
- Check if the website uses Content Security Policy that blocks inline styles

### Extension Not Loading
- Ensure all required files are present
- Check browser console for errors
- Try reloading the extension in developer mode
- Restart your browser

## Privacy Policy

This extension:
- Stores all data locally on your device
- Does not collect or transmit any personal information
- Does not track your browsing history
- Only accesses websites you explicitly configure

## Support

If you encounter issues or have questions:
1. Check the troubleshooting section above
2. Review the browser console for error messages
3. Create an issue on GitHub with:
   - Browser version and extension version
   - Steps to reproduce the issue
   - Expected vs actual behavior

## Changelog

### Version 1.0.0
- Initial release
- URL pattern matching
- CSS override and replace modes
- Rule management interface
- Import/export functionality
- Cross-browser support (Chrome, Edge)

## License

This project is open source and available under the MIT License.