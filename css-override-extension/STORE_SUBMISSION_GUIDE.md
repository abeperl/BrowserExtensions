# Store Submission Guide for CSS Override Extension

This guide provides instructions on how to package and submit the CSS Override Extension to browser web stores, such as the Chrome Web Store and Microsoft Edge Add-ons store.

## 1. Prepare Your Extension

Before submitting, ensure the following:

### Required Files
- **`manifest.json`** is complete with all required fields:
  - `name`, `version`, `description` filled out
  - All `permissions` properly justified
  - `icons` in required sizes (16x16, 32x32, 48x48, 128x128)
  - Valid `homepage_url` (optional but recommended)

### Icons
Create icons in the following sizes and place them in the `icons/` directory:
- `icon16.png` - 16x16 pixels
- `icon32.png` - 32x32 pixels
- `icon48.png` - 48x48 pixels
- `icon128.png` - 128x128 pixels

Icons should be:
- PNG format with transparent background
- Clean, professional design
- Clearly represent the extension's purpose (CSS/styling theme)

### Screenshots
Create high-quality screenshots showcasing the extension:
- **Main Interface**: Show the popup with CSS rules
- **Settings Page**: Demonstrate configuration options
- **In Action**: Show CSS being applied to a website
- **Add Rule Modal**: Display the rule creation interface

Screenshots should be:
- 1280x800 or 1366x768 resolution minimum
- PNG or JPG format
- Show the extension actually working
- Include browser UI to show it's a real extension

### Promotional Images
- **Small Promo Tile**: 440x280 pixels
- **Marquee Promo Tile**: 1400x560 pixels
- **Store Logo**: 96x96 pixels (for Edge)

### Privacy Policy
- Complete privacy policy included (see `PRIVACY.md`)
- Hosted on a publicly accessible URL
- Complies with store requirements

## 2. Test Your Extension

Before submitting, thoroughly test your extension:

### Functionality Testing
- [ ] CSS injection works on various websites
- [ ] URL pattern matching functions correctly
- [ ] Override and replace modes work as expected
- [ ] Settings page functions properly
- [ ] Import/export features work
- [ ] No console errors or warnings

### Cross-Browser Testing
- [ ] Test on Chrome (recommended version and latest)
- [ ] Test on Microsoft Edge (if submitting to both)
- [ ] Verify all features work consistently

### Performance Testing
- [ ] Extension doesn't significantly impact page load times
- [ ] Memory usage is reasonable
- [ ] No performance issues with multiple rules

## 3. Package Your Extension

### Chrome Web Store
1. Navigate to the `css-override-extension` directory
2. Select all files and folders **except**:
   - `.git` directory (if present)
   - Any development files
   - `node_modules` (if present)
   - Temporary files
3. Compress into a ZIP file: `css-override-extension-v1.0.0.zip`
4. **Important**: Do not include the ZIP inside another ZIP

### Microsoft Edge
1. Use the same ZIP file created for Chrome
2. Ensure manifest.json is compatible with Edge requirements

## 4. Submit to Chrome Web Store

### Account Setup
1. Go to [Chrome Developer Dashboard](https://chrome.google.com/webstore/developer/dashboard)
2. Pay the one-time $5 developer registration fee
3. Verify your account

### Submission Process
1. Click **"Add new item"**
2. Upload your ZIP file
3. Fill out the store listing:
   - **Title**: CSS Override Extension
   - **Description**: Detailed description (see below)
   - **Category**: Productivity or Developer Tools
   - **Screenshots**: Upload 3-5 high-quality screenshots
   - **Promotional Images**: Upload small and marquee promo tiles
   - **Privacy Policy**: Provide URL to your privacy policy

### Store Description
```
Customize website appearance with custom CSS rules! Apply styles to specific websites using URL patterns with wildcard support. Perfect for web developers, designers, and users who want to personalize their browsing experience.

Features:
• URL pattern matching with wildcards (*)
• Multiple CSS rules per website
• Override or replace existing styles
• Live preview of changes
• Import/export rules for backup
• Clean, intuitive interface

Use cases:
• Hide annoying elements (ads, banners)
• Improve readability and typography
• Apply dark mode to specific sites
• Test CSS changes on live websites
• Personalize website appearance
```

## 5. Submit to Microsoft Edge Add-ons

### Account Setup
1. Go to [Microsoft Partner Center](https://partner.microsoft.com/en-us/dashboard/microsoftedge/publishapi)
2. Create a developer account (may require verification)
3. Set up your Edge developer profile

### Submission Process
1. Click **"Create new extension"**
2. Upload your ZIP file
3. Complete the submission form:
   - **App name**: CSS Override Extension
   - **Description**: Same detailed description as Chrome
   - **Category**: Productivity
   - **Screenshots**: Upload required screenshots
   - **App icon**: 300x300 pixels required
   - **Promotional images**: Store logo and promotional images
   - **Privacy policy**: URL to privacy policy

## 6. After Submission

### Review Process
- **Chrome**: Review typically takes 1-3 business days
- **Edge**: Review can take 3-7 business days
- You'll receive email notifications about the review status

### Common Review Issues
- **Permissions**: Ensure all permissions are justified and necessary
- **Content Security**: No remote code execution or unsafe practices
- **Privacy**: Clear privacy policy and data handling
- **Functionality**: Extension must work as described
- **Store Policies**: Follow all store guidelines

### Making Updates
1. Increment version number in `manifest.json`
2. Create new ZIP file with updated version
3. Upload new package to store dashboard
4. Update store listing if needed

## 7. Marketing and Promotion

### Store Optimization
- Use relevant keywords in title and description
- Clear, professional screenshots
- Compelling feature list
- Target appropriate category

### Additional Promotion
- Create a dedicated website or landing page
- Write blog posts about the extension
- Share on social media and developer communities
- Engage with users who leave reviews

## 8. Maintenance

### Regular Updates
- Monitor user feedback and reviews
- Fix bugs and address feature requests
- Keep dependencies updated
- Follow browser API changes

### Analytics
- Monitor installation numbers
- Track user engagement
- Identify popular features
- Plan future enhancements

## 9. Troubleshooting Submission Issues

### Common Chrome Issues
- **Manifest Errors**: Validate manifest.json syntax
- **Permission Justifications**: Explain why each permission is needed
- **Content Policy**: Ensure no violation of content policies
- **Technical Issues**: Test extension thoroughly before submission

### Common Edge Issues
- **App Identity**: Ensure proper app identity setup
- **Store Policies**: Follow Microsoft store policies
- **Technical Compatibility**: Verify Edge compatibility

## 10. Support Resources

- [Chrome Web Store Developer Documentation](https://developer.chrome.com/docs/webstore/)
- [Microsoft Edge Add-ons Documentation](https://docs.microsoft.com/en-us/microsoft-edge/extensions-chromium/)
- [Extension Manifest Reference](https://developer.chrome.com/docs/extensions/mv3/manifest/)
- [Chrome Extension Samples](https://github.com/GoogleChrome/chrome-extensions-samples)

---

**Remember**: Always test your extension thoroughly before submission and keep your contact information up to date for review communications.