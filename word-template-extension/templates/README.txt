TEMPLATES FOLDER

This folder should contain .docx Word template files with placeholder text like:
- {{ORDERID}}
- {{PNAME}}
- {{PRODUCTID}}
- {{SELECTEDTEXT}}
- {{EXTRACTIONDATE}}

To create templates:
1. Create a Word document with your desired layout
2. Insert placeholder text using double braces: {{FIELDNAME}}
3. Save as .docx format in this templates folder
4. The extension will find and use these templates

Example template content:
===========================
Order Processing Document

Order ID: {{ORDERID}}
Product Name: {{PNAME}}
Product ID: {{PRODUCTID}}
Selected Content: {{SELECTEDTEXT}}
Processed on: {{EXTRACTIONDATE}}

Notes:
{{NOTES}}
===========================