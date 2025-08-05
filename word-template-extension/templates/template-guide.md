# Word Template Guide

## Overview
This guide explains how to create and use custom Word templates with the Word Template Extension. The extension allows you to replace placeholders in Word documents with dynamic content.

## Placeholder Format

### Basic Syntax
Placeholders use double curly braces: `{{PLACEHOLDER_NAME}}`

### Supported Placeholder Formats
- `{{TITLE}}` - Document title
- `{{DATE}}` - Current date or specified date
- `{{AMOUNT}}` - Monetary amounts or totals
- `{{DESCRIPTION}}` - Text descriptions
- `{{COMPANY}}` - Company name
- `{{ADDRESS}}` - Company or customer address
- `{{PHONE}}` - Phone numbers
- `{{EMAIL}}` - Email addresses

### Advanced Placeholders
- `{{ITEM_1}}`, `{{ITEM_2}}`, etc. - Numbered items
- `{{ITEM_1_DESC}}`, `{{ITEM_1_QTY}}`, `{{ITEM_1_AMOUNT}}` - Item details
- `{{PROJECT_MANAGER}}` - Project manager name
- `{{REFERENCE_NUMBER}}` - Reference or invoice numbers
- `{{DUE_DATE}}` - Due dates
- `{{PAYMENT_TERMS}}` - Payment terms and conditions
- `{{SIGNATURE}}` - Signature fields
- `{{NOTES}}` - Additional notes or comments

## Creating Custom Templates

### Step 1: Design Your Document
1. Open Microsoft Word
2. Create your document layout with headers, footers, tables, and formatting
3. Use professional styling and consistent formatting

### Step 2: Add Placeholders
1. Insert placeholders using the `{{PLACEHOLDER_NAME}}` format
2. Place placeholders where you want dynamic content
3. Ensure placeholder names are descriptive and unique

### Step 3: Test Your Template
1. Save the document as a `.docx` file
2. Place it in the `templates/` directory
3. Use the browser extension to test placeholder replacement

## Best Practices

### Naming Conventions
- Use UPPERCASE for placeholder names
- Use underscores to separate words: `{{FIRST_NAME}}`
- Be descriptive: `{{CUSTOMER_ADDRESS}}` instead of `{{ADDR}}`

### Placement Guidelines
- **Headers/Footers**: Company info, page numbers, contact details
- **Document Body**: Main content, descriptions, dynamic text
- **Tables**: Itemized data, structured information
- **Signature Areas**: Names, dates, approval fields

### Formatting Tips
- Apply formatting to the placeholder text (bold, italic, font size)
- The formatting will be preserved when content is replaced
- Use tables for structured data with multiple placeholders
- Consider using styles for consistent formatting

## Template Locations

### Supported Locations
Placeholders can be placed in:
- Document headers
- Document footers
- Main document body
- Table cells
- Text boxes
- Footnotes and endnotes

### Not Supported
- Images (alt text only)
- Charts and graphs
- Embedded objects
- Form fields

## Example Templates

### Invoice Template
```
{{COMPANY}}
{{ADDRESS}}
Phone: {{PHONE}} | Email: {{EMAIL}}

INVOICE

Date: {{DATE}}
Invoice #: {{INVOICE_NUMBER}}

Bill To:
{{CUSTOMER_NAME}}
{{CUSTOMER_ADDRESS}}

| Item | Description | Qty | Amount |
|------|-------------|-----|--------|
| {{ITEM_1}} | {{ITEM_1_DESC}} | {{ITEM_1_QTY}} | {{ITEM_1_AMOUNT}} |
| {{ITEM_2}} | {{ITEM_2_DESC}} | {{ITEM_2_QTY}} | {{ITEM_2_AMOUNT}} |

Total: {{TOTAL_AMOUNT}}
```

### Business Letter Template
```
{{COMPANY}}
{{COMPANY_ADDRESS}}

{{DATE}}

{{RECIPIENT_NAME}}
{{RECIPIENT_ADDRESS}}

Dear {{RECIPIENT_NAME}},

{{LETTER_BODY}}

Sincerely,

{{SENDER_NAME}}
{{SENDER_TITLE}}
```

### Report Template
```
{{REPORT_TITLE}}

Prepared by: {{AUTHOR}}
Date: {{DATE}}
Department: {{DEPARTMENT}}

Executive Summary:
{{EXECUTIVE_SUMMARY}}

Findings:
{{FINDINGS}}

Recommendations:
{{RECOMMENDATIONS}}

Conclusion:
{{CONCLUSION}}
```

## Troubleshooting

### Common Issues
1. **Placeholder not replaced**: Check spelling and ensure exact match
2. **Formatting lost**: Apply formatting to the placeholder text, not just the braces
3. **Table issues**: Ensure placeholders are in separate cells
4. **Special characters**: Avoid special characters in placeholder names

### Error Prevention
- Always use double curly braces: `{{}}` not `{}`
- Don't include spaces inside braces: `{{NAME}}` not `{{ NAME }}`
- Use consistent naming throughout the document
- Test templates with sample data before production use

## Advanced Features

### Conditional Content
While not directly supported, you can create multiple template versions for different scenarios.

### Dynamic Tables
For tables with variable rows, create templates with maximum expected rows and leave unused placeholders empty.

### Multi-language Support
Create separate templates for different languages with appropriate placeholders.

## Template Management

### Organization
- Use descriptive filenames: `invoice-template.docx`, `letter-template.docx`
- Group related templates in subdirectories
- Maintain a template inventory with descriptions

### Version Control
- Include version numbers in template names when needed
- Keep backup copies of working templates
- Document changes and updates

### Sharing Templates
- Templates can be shared across teams
- Ensure all users understand placeholder conventions
- Provide documentation for custom placeholders

## Security Considerations

### Data Protection
- Avoid including sensitive data in template files
- Use placeholders for all variable content
- Review templates before sharing

### Access Control
- Limit template modification to authorized users
- Use read-only permissions for production templates
- Maintain audit trails for template changes

## Support and Resources

### Getting Help
- Check this guide for common questions
- Review the sample template for examples
- Test with simple placeholders first

### Additional Resources
- Microsoft Word documentation for advanced formatting
- Template design best practices
- Accessibility guidelines for document creation

---

*Last updated: July 2025*
*Version: 1.0*