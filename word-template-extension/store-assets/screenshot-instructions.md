# Screenshot Instructions for Store Submission

## Overview
This document provides instructions for creating high-quality promotional screenshots for Chrome Web Store and Microsoft Edge Add-ons store submissions.

## Screenshot Requirements

### Chrome Web Store
- **Size**: 1280x800 or 640x400 pixels
- **Format**: PNG or JPEG
- **Count**: 1-5 screenshots
- **Quality**: High resolution, clear text, professional appearance

### Microsoft Edge Add-ons
- **Size**: 1366x768 pixels (recommended)
- **Format**: PNG (preferred)
- **Count**: 1-10 screenshots
- **Quality**: High resolution, clear and readable

## How to Create Screenshots

### Method 1: Using the Demo HTML File
1. Open `screenshot-demo.html` in your browser
2. Set browser zoom to 100%
3. Use browser developer tools to set viewport to 1280x800
4. Take full-page screenshots of each demo screen
5. Crop if necessary to exact dimensions

### Method 2: Using the Live Extension
1. Load the extension in Chrome/Edge developer mode
2. Navigate to a webpage with sample data
3. Open the extension popup
4. Take screenshots showing the extraction process
5. Capture settings page and welcome screen

## Required Screenshots

### Screenshot 1: Main Extension Interface (Primary)
- **File**: `01-main-interface.png`
- **Content**: Extension popup showing extracted data and template selection
- **Focus**: Demonstrate core functionality and professional UI
- **Size**: 1280x800px

### Screenshot 2: Feature Overview
- **File**: `02-feature-overview.png`  
- **Content**: Welcome screen with feature highlights
- **Focus**: Show key benefits and value proposition
- **Size**: 1280x800px

### Screenshot 3: Settings and Configuration
- **File**: `03-settings-configuration.png`
- **Content**: Settings page with various configuration options
- **Focus**: Demonstrate customization depth and advanced features
- **Size**: 1280x800px

### Screenshot 4: In Action (Optional)
- **File**: `04-extension-in-action.png`
- **Content**: Extension working on a real website
- **Focus**: Show practical usage and data extraction
- **Size**: 1280x800px

### Screenshot 5: Template Management (Optional)
- **File**: `05-template-management.png`
- **Content**: Template selection and management interface
- **Focus**: Show professional document templates
- **Size**: 1280x800px

## Promotional Assets

### Chrome Web Store Promotional Tile
- **Size**: 440x280px
- **File**: `promotional-tile-440x280.png`
- **Source**: Use `promotional-tile.html`
- **Content**: Branded tile with logo, title, and key features

### App Icon
- **Size**: 128x128px
- **File**: `app-icon-128x128.png`
- **Source**: Extract from `promotional-tile.html`
- **Content**: Clean app icon with "W" logo

### Square Promotional Image
- **Size**: 400x400px
- **File**: `promotional-square-400x400.png`
- **Source**: Use square version from `promotional-tile.html`
- **Content**: Square format for social media and featured listings

## Screenshot Guidelines

### Do's
✅ Use high-resolution displays (Retina/4K preferred)
✅ Ensure text is crisp and readable
✅ Show realistic data in the interface
✅ Maintain consistent branding and colors
✅ Include status indicators showing successful connections
✅ Show populated data extraction results
✅ Highlight key features and benefits
✅ Use professional sample data (no personal information)

### Don'ts
❌ Include any personal or sensitive information
❌ Use low-resolution or blurry images
❌ Show error states or broken functionality
❌ Include browser chrome unless specifically needed
❌ Use Lorem ipsum or obvious placeholder text
❌ Include debug information or developer tools
❌ Show empty or unpopulated interfaces

## Sample Data for Screenshots

Use this realistic but fictional data for screenshots:

```
EMAIL: sarah.johnson@techsolutions.com
PHONE: +1 (555) 123-4567
DATE: 2024-08-05
AMOUNT: $2,499.99
COMPANY: TechSolutions Inc.
ADDRESS: 1234 Innovation Drive, Suite 200
PRODUCT: Professional Software License
ORDER: ORD-2024-0805-001
```

## Browser Setup for Screenshots

### Chrome/Edge Settings
1. Set zoom to 100%
2. Hide bookmarks bar
3. Use clean profile (no extensions in toolbar)
4. Set viewport to required dimensions
5. Clear any notifications or popups

### Developer Tools Setup
1. Open DevTools (F12)
2. Click device toolbar icon
3. Set to "Responsive"
4. Enter custom dimensions (1280x800)
5. Set DPR to 1.0
6. Take full-page screenshots

## File Organization

Store all screenshots in the `screenshots/` directory:

```
store-assets/
├── screenshots/
│   ├── 01-main-interface.png
│   ├── 02-feature-overview.png
│   ├── 03-settings-configuration.png
│   ├── 04-extension-in-action.png
│   └── 05-template-management.png
├── promotional/
│   ├── promotional-tile-440x280.png
│   ├── app-icon-128x128.png
│   └── promotional-square-400x400.png
└── source/
    ├── screenshot-demo.html
    └── promotional-tile.html
```

## Quality Checklist

Before submitting screenshots, verify:

- [ ] Correct dimensions (1280x800px for Chrome, 1366x768px for Edge)
- [ ] High resolution and crisp text
- [ ] Professional appearance with consistent branding
- [ ] No personal or sensitive information visible
- [ ] Realistic sample data used throughout
- [ ] Key features and benefits clearly highlighted
- [ ] UI elements are properly aligned and styled
- [ ] Colors match the extension's brand palette
- [ ] File sizes are optimized (under 5MB each)
- [ ] PNG format used for best quality

## Tools Recommended

- **Browser**: Chrome or Edge (latest version)
- **Screenshot Tools**: 
  - Built-in browser screenshot (DevTools)
  - Snagit
  - LightShot
  - macOS Screenshot (Cmd+Shift+4)
  - Windows Snipping Tool
- **Image Editing**: 
  - Photoshop
  - GIMP
  - Figma
  - Canva Pro

## Notes

- Screenshots should tell a story about the extension's value
- First screenshot is most important - it's shown in search results
- Consider the target audience (business professionals, office workers)
- Emphasize productivity and professional document creation
- Show the extension solving real problems
- Maintain consistency across all promotional materials