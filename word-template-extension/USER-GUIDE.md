# User Guide

**Complete guide to using the Word Template Extension for business document automation**

This guide will teach you everything you need to know to effectively use the Word Template Extension for your business needs. From creating your first template to advanced automation techniques.

## 🎯 Getting Started

### Your First Document in 5 Minutes

Let's create your first automated document to see how the extension works:

1. **Create a Simple Template**
   - Open Microsoft Word
   - Type: "Invoice for {{COMPANY}} - Amount: {{AMOUNT}} - Date: {{DATE}}"
   - Save as `simple-invoice.docx` in your `Documents/Templates/` folder

2. **Visit a Website**
   - Go to any business website (e.g., a company's contact page)
   - Look for company name, amounts, or dates on the page

3. **Use the Extension**
   - Click the Word Template Extension icon in your browser
   - Select "simple-invoice.docx" from the template dropdown
   - Click "Extract and Generate"

4. **View Your Document**
   - The extension automatically opens the completed document
   - Check the `Documents/Generated/` folder for the saved file

## 📝 Understanding Templates

### What Are Templates?

Templates are Word documents with special placeholders that get replaced with real data. Think of them as forms that fill themselves out automatically.

**Example Template:**
```
BUSINESS PROPOSAL

Client: {{COMPANY}}
Contact: {{CONTACT_NAME}}
Email: {{EMAIL}}
Phone: {{PHONE}}

Project: {{PROJECT_NAME}}
Estimated Cost: {{AMOUNT}}
Start Date: {{START_DATE}}
Completion: {{END_DATE}}

Description:
{{DESCRIPTION}}
```

### Placeholder Rules

- **Format**: Always use double curly braces `{{FIELD_NAME}}`
- **Naming**: Use UPPERCASE with underscores (e.g., `{{FIRST_NAME}}`)
- **Spacing**: No spaces inside braces: `{{NAME}}` not `{{ NAME }}`
- **Case Sensitive**: `{{EMAIL}}` is different from `{{email}}`

### Common Placeholders

| Placeholder | What It Captures | Example |
|-------------|------------------|---------|
| `{{TITLE}}` | Page titles, headings | "About Our Company" |
| `{{COMPANY}}` | Company names | "Acme Corporation" |
| `{{DATE}}` | Dates in various formats | "January 15, 2024" |
| `{{AMOUNT}}` | Prices, costs, numbers | "$1,234.56" |
| `{{EMAIL}}` | Email addresses | "contact@company.com" |
| `{{PHONE}}` | Phone numbers | "(555) 123-4567" |
| `{{ADDRESS}}` | Street addresses | "123 Main St, City, State" |
| `{{DESCRIPTION}}` | Longer text content | Product descriptions, etc. |

## 🏗️ Creating Professional Templates

### Template Design Best Practices

1. **Start with Layout**
   - Design your document structure first
   - Add headers, footers, and formatting
   - Use tables for organized data

2. **Add Placeholders Strategically**
   - Place them where variable content belongs
   - Keep placeholder names descriptive
   - Group related placeholders together

3. **Format Placeholders**
   - Apply formatting (bold, italic, font size) to placeholders
   - The formatting will be preserved when replaced
   - Use styles for consistent appearance

### Business Template Examples

#### Invoice Template
```
[Your Company Logo]

INVOICE

Invoice #: {{INVOICE_NUMBER}}
Date: {{DATE}}
Due Date: {{DUE_DATE}}

Bill To:
{{CLIENT_COMPANY}}
{{CLIENT_ADDRESS}}
{{CLIENT_CITY}}, {{CLIENT_STATE}} {{CLIENT_ZIP}}

Description: {{SERVICE_DESCRIPTION}}
Amount: {{AMOUNT}}
Tax: {{TAX_AMOUNT}}
Total: {{TOTAL_AMOUNT}}

Payment Terms: {{PAYMENT_TERMS}}
```

#### Business Letter Template
```
[Your Company Letterhead]

{{DATE}}

{{RECIPIENT_NAME}}
{{RECIPIENT_TITLE}}
{{RECIPIENT_COMPANY}}
{{RECIPIENT_ADDRESS}}

Dear {{RECIPIENT_NAME}},

{{LETTER_BODY}}

{{CLOSING_PARAGRAPH}}

Sincerely,

{{SENDER_NAME}}
{{SENDER_TITLE}}
{{SENDER_COMPANY}}
```

#### Project Report Template
```
PROJECT STATUS REPORT

Project: {{PROJECT_NAME}}
Manager: {{PROJECT_MANAGER}}
Date: {{REPORT_DATE}}
Status: {{PROJECT_STATUS}}

EXECUTIVE SUMMARY
{{EXECUTIVE_SUMMARY}}

PROGRESS UPDATE
{{PROGRESS_DETAILS}}

BUDGET STATUS
Allocated: {{BUDGET_ALLOCATED}}
Spent: {{BUDGET_SPENT}}
Remaining: {{BUDGET_REMAINING}}

NEXT STEPS
{{NEXT_STEPS}}

ISSUES & RISKS
{{ISSUES_RISKS}}
```

## 🌐 Website Data Extraction

### How the Extension Finds Data

The extension automatically scans web pages for common data patterns:

- **Text in headings** (H1, H2, H3 tags)
- **Contact information** (emails, phones, addresses)
- **Monetary amounts** (prices, costs, totals)
- **Dates** (various formats and contexts)
- **Company names** (from titles, headers, contact sections)
- **Form data** (visible form fields and labels)

### Optimizing Data Extraction

#### Best Websites for Extraction
- **Company websites**: Contact pages, about pages, service pages
- **E-commerce sites**: Product pages, pricing pages
- **Business directories**: Listings with contact information
- **News articles**: Headlines, dates, content
- **Social media profiles**: Business information

#### Tips for Better Results
1. **Visit specific pages**: Go to contact pages for contact info, pricing pages for amounts
2. **Look for structured data**: Tables, lists, and forms work best
3. **Check page completeness**: Ensure all needed data is visible on the page
4. **Use clean pages**: Avoid pages with excessive ads or clutter

### Manual Data Selection

If automatic extraction doesn't capture what you need:

1. **Highlight text** on the webpage before clicking the extension
2. **Use the custom field option** in the extension popup
3. **Map specific page elements** to template placeholders
4. **Save extraction patterns** for frequently used websites

## 📋 Using the Extension

### Step-by-Step Workflow

#### 1. Prepare Your Template
- Create or select a Word template
- Ensure it's saved in the `Documents/Templates/` folder
- Verify placeholder names match your data needs

#### 2. Navigate to Data Source
- Visit the website containing your data
- Ensure all needed information is visible on the page
- Note any specific data you want to capture

#### 3. Extract Data
- Click the Word Template Extension icon
- Select your template from the dropdown
- Review the data preview (if available)
- Click "Extract and Generate"

#### 4. Review and Use Document
- The completed document opens automatically
- Review for accuracy and completeness
- Save with a specific filename if needed
- Use the document for your business needs

### Extension Interface Guide

#### Main Popup
- **Template Selector**: Choose from available templates
- **Data Preview**: See what data will be extracted
- **Generate Button**: Start the document creation process
- **Settings**: Access configuration options

#### Settings Panel
- **Template Folder**: Change where templates are stored
- **Output Folder**: Change where generated documents are saved
- **Auto-Open**: Toggle automatic document opening
- **Data Mapping**: Configure custom field mappings

## 📁 File Management

### Organizing Templates

#### Folder Structure
```
Documents/
├── Templates/
│   ├── Invoices/
│   │   ├── standard-invoice.docx
│   │   ├── service-invoice.docx
│   │   └── product-invoice.docx
│   ├── Letters/
│   │   ├── business-letter.docx
│   │   ├── follow-up-letter.docx
│   │   └── proposal-letter.docx
│   └── Reports/
│       ├── project-report.docx
│       ├── status-update.docx
│       └── meeting-notes.docx
```

#### Template Naming Conventions
- Use descriptive names: `client-proposal-template.docx`
- Include version numbers: `invoice-v2.docx`
- Group by purpose: `sales-`, `hr-`, `finance-`
- Avoid spaces: Use hyphens or underscores

### Managing Generated Documents

#### Automatic Naming
Generated documents are automatically named with:
- Template name
- Current date and time
- Source website (if applicable)

Example: `invoice-template_2024-01-15_14-30-25.docx`

#### Custom Naming
To use custom names:
1. Include `{{FILENAME}}` placeholder in your template
2. The extension will use extracted title or company name
3. Manually rename after generation if needed

#### File Organization
- Create folders by date: `2024/January/`
- Organize by client: `ClientName/`
- Sort by document type: `Invoices/`, `Proposals/`

## 🔧 Advanced Features

### Custom Field Mapping

Map specific webpage elements to template placeholders:

1. **Right-click** on webpage text
2. Select "Map to Template Field"
3. Choose the placeholder from your template
4. Save the mapping for future use

### Batch Processing

Process multiple templates at once:

1. Select multiple templates in the extension
2. The extension extracts data once
3. Generates multiple documents with the same data
4. Useful for creating invoice + cover letter combinations

### Template Variables

Use dynamic content in templates:

- `{{TODAY}}`: Current date
- `{{TIMESTAMP}}`: Current date and time
- `{{USER_NAME}}`: Your name from settings
- `{{COMPANY_NAME}}`: Your company from settings

### Conditional Content

Create templates with optional sections:

```
{{#IF_AMOUNT}}
Total Amount: {{AMOUNT}}
{{/IF_AMOUNT}}

{{#IF_DISCOUNT}}
Discount Applied: {{DISCOUNT}}
{{/IF_DISCOUNT}}
```

## 🎯 Business Use Cases

### Sales and Marketing

#### Lead Generation
- Extract contact information from business directories
- Create personalized outreach letters
- Generate prospect profiles and notes

#### Proposal Creation
- Capture client requirements from RFPs
- Generate customized proposals
- Create follow-up documentation

#### Invoice Processing
- Extract billing information from project management tools
- Generate invoices with client details
- Create payment tracking documents

### Project Management

#### Status Reporting
- Extract project data from management platforms
- Generate standardized status reports
- Create client update documents

#### Meeting Documentation
- Capture meeting details from calendar invites
- Generate meeting agendas and notes
- Create action item tracking documents

### Administrative Tasks

#### Contact Management
- Extract contact details from business cards (digital)
- Generate contact sheets and directories
- Create mailing lists and labels

#### Document Processing
- Convert web content to formatted documents
- Generate reports from online data
- Create documentation from web resources

## ❓ Frequently Asked Questions

### General Usage

**Q: Can I use the extension with any website?**
A: Yes, the extension works with any website. However, results are best with sites that have structured data like contact pages, product listings, and business directories.

**Q: How many templates can I create?**
A: There's no limit to the number of templates you can create. Organize them in folders for easy management.

**Q: Can I edit the generated documents?**
A: Absolutely! Generated documents are standard Word files that you can edit, format, and modify as needed.

### Template Creation

**Q: What if a placeholder isn't replaced?**
A: This usually means the extension couldn't find matching data on the webpage. Check the placeholder spelling and ensure the data exists on the page.

**Q: Can I use the same placeholder multiple times?**
A: Yes, you can use the same placeholder multiple times in a template. All instances will be replaced with the same data.

**Q: How do I create multi-page templates?**
A: Create your template as you normally would in Word with multiple pages. All placeholders across all pages will be processed.

### Data Extraction

**Q: Why isn't the extension finding my data?**
A: The extension looks for common data patterns. If your data isn't being found, try:
- Highlighting the text before using the extension
- Using the manual mapping feature
- Ensuring the data is visible on the page (not hidden in dropdowns or tabs)

**Q: Can I extract data from password-protected sites?**
A: Yes, as long as you're logged in and can see the data in your browser, the extension can extract it.

**Q: How accurate is the data extraction?**
A: Accuracy depends on the website structure. Well-organized sites with clear data formatting typically yield 90%+ accuracy.

### Technical Issues

**Q: The extension isn't working. What should I check?**
A: Common solutions:
1. Restart your browser
2. Check that the native host is installed
3. Verify templates are in the correct folder
4. Look for error messages in the browser console

**Q: Can I use the extension offline?**
A: The extension requires an internet connection to access websites, but template processing happens locally on your computer.

**Q: Is my data secure?**
A: Yes, all processing happens on your computer. No data is sent to external servers.

## 🔍 Troubleshooting

### Common Issues and Solutions

#### Extension Not Appearing
- **Check installation**: Verify the extension is installed and enabled
- **Restart browser**: Close and reopen your browser completely
- **Check permissions**: Ensure the extension has necessary permissions

#### No Data Extracted
- **Verify page content**: Ensure the data you want is visible on the page
- **Check placeholder names**: Verify they match common data patterns
- **Try manual selection**: Highlight text before using the extension

#### Template Not Found
- **Check file location**: Ensure templates are in `Documents/Templates/`
- **Verify file format**: Templates must be `.docx` files
- **Check file permissions**: Ensure files aren't read-only or locked

#### Generated Document Issues
- **Check output folder**: Look in `Documents/Generated/`
- **Verify Word installation**: Ensure Microsoft Word is installed
- **Check file permissions**: Ensure you can write to the output folder

### Error Messages

#### "Native host not found"
1. Reinstall the native host component
2. Restart your browser
3. Check the installation guide for your operating system

#### "Template processing failed"
1. Check that the template file isn't corrupted
2. Verify placeholder syntax (double curly braces)
3. Ensure the template is a valid Word document

#### "Permission denied"
1. Check file and folder permissions
2. Run browser as administrator (Windows)
3. Verify antivirus isn't blocking the extension

## 📈 Tips for Maximum Efficiency

### Workflow Optimization

1. **Create template libraries** for different business functions
2. **Use consistent placeholder naming** across all templates
3. **Save frequently used websites** as bookmarks
4. **Batch similar tasks** together for efficiency

### Quality Assurance

1. **Always review generated documents** before sending
2. **Keep backup copies** of important templates
3. **Test templates** with sample data before production use
4. **Update templates** regularly to improve accuracy

### Best Practices

1. **Start simple** with basic templates and gradually add complexity
2. **Document your templates** with comments about their purpose
3. **Share templates** with team members for consistency
4. **Regular maintenance** - clean up old templates and generated files

---

**Need more help?** Check out our [Template Guide](templates/template-guide.md) for advanced template creation techniques, or see the [Installation Guide](INSTALLATION.md) if you're having setup issues.