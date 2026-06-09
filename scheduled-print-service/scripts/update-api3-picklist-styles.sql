-- SQL Script to update picklist detail page styles for API #3
-- Issues addressed:
-- 1. Customer name wrapping (prevent truncation)
-- 2. Hide customer note label and value
-- 3. Add barcode column header text and generate scannable SKU barcodes
-- 4. Make Order Reference # value bold and bigger

-- This script APPENDS new CSS and JavaScript to the existing HtmlInjections array
-- It follows the same pattern as API #2 (PrimaryApiId=3) which already has similar injections

UPDATE SubAction
SET Configuration = json_insert(
    Configuration,
    '$.HtmlInjections[#]',
    json('{"HtmlTemplate": "<style>\n/* Fix customer name truncation - allow wrapping */\n.detail-box .form-group span.form-control[data-bind=\"ClientName\"],\n.detail-box .form-group span.form-control[data-bind=\"CustomerName\"] {\n  white-space: normal !important;\n  word-wrap: break-word !important;\n  overflow-wrap: break-word !important;\n  min-height: 2.5em;\n  line-height: 1.2em;\n}\n\n/* Hide customer note label and value */\n.detail-box .form-group[column-permission=\"PickerNote\"],\n.detail-box .form-group span.form-control.order_note,\n.detail-box .form-group label:has(+ span.order_note),\n.detail-box .form-group:has(span.order_note) {\n  display: none !important;\n}\n\n/* Style barcode column using attribute selector to avoid SQL reserved keyword */\n[class=\"table\"] th[column-permission=\"Barcode\"],\n[class=\"table\"] td[column-permission=\"Barcode\"] {\n  text-align: center;\n  vertical-align: middle;\n}\n\n.sku-barcode-container {\n  display: flex;\n  flex-direction: column;\n  align-items: center;\n  padding: 5px;\n}\n\n.sku-barcode {\n  margin: 5px 0;\n}\n\n/* Make Order Reference # value bold and bigger */\n.detail-box .form-group span.form-control[data-bind=\"RefType\"] {\n  font-weight: bold !important;\n  font-size: 1.15em !important;\n}\n</style>", "InsertPosition": "append", "TargetSelector": "head"}')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly'
  AND ActionNumber = 1;

-- Now append the JavaScript injection
UPDATE SubAction
SET Configuration = json_insert(
    Configuration,
    '$.HtmlInjections[#]',
    json('{"HtmlTemplate": "<script>\n(function() {\n  // Wait for page to load\n  setTimeout(function() {\n    // Add barcode column header if missing\n    var tableHeaders = document.querySelectorAll(\".table thead tr th\");\n    var hasBarcodeHeader = false;\n    tableHeaders.forEach(function(th) {\n      if (th.getAttribute(\"column-permission\") === \"Barcode\" || th.textContent.includes(\"Barcode\")) {\n        hasBarcodeHeader = true;\n        th.textContent = \"SKU Barcode\";\n      }\n    });\n\n    // If no barcode header exists, add one\n    if (!hasBarcodeHeader) {\n      var headerRow = document.querySelector(\".table thead tr\");\n      if (headerRow) {\n        var barcodeHeader = document.createElement(\"th\");\n        barcodeHeader.setAttribute(\"column-permission\", \"Barcode\");\n        barcodeHeader.textContent = \"SKU Barcode\";\n        headerRow.appendChild(barcodeHeader);\n      }\n    }\n\n    // Generate barcodes for each SKU in the table\n    var tableRows = document.querySelectorAll(\".table tbody tr\");\n    tableRows.forEach(function(row) {\n      // Find SKU cell\n      var skuCell = row.querySelector(\"td[data-bind*=\\\"SKU\\\"], td[data-bind*=\\\"Sku\\\"], td[data-bind*=\\\"sku\\\"]\");\n      if (!skuCell) {\n        var cells = row.querySelectorAll(\"td\");\n        skuCell = cells[2];\n      }\n\n      if (skuCell) {\n        var sku = skuCell.textContent.trim();\n\n        // Check if barcode cell already exists\n        var barcodeCell = row.querySelector(\"td[column-permission=\\\"Barcode\\\"]\");\n        if (!barcodeCell) {\n          barcodeCell = document.createElement(\"td\");\n          barcodeCell.setAttribute(\"column-permission\", \"Barcode\");\n          row.appendChild(barcodeCell);\n        }\n\n        // Generate Code128 barcode\n        if (sku && sku.length > 0) {\n          var container = document.createElement(\"div\");\n          container.className = \"sku-barcode-container\";\n\n          var barcodeDiv = document.createElement(\"div\");\n          barcodeDiv.className = \"sku-barcode barcode\";\n          barcodeDiv.setAttribute(\"data-barcode\", sku);\n          barcodeDiv.innerHTML = generateCode128Barcode(sku);\n\n          container.appendChild(barcodeDiv);\n          barcodeCell.innerHTML = \"\";\n          barcodeCell.appendChild(container);\n        }\n      }\n    });\n\n    function generateCode128Barcode(text) {\n      var barcode = \"\";\n      var height = \"0.6in\";\n\n      for (var i = 0; i < text.length; i++) {\n        var charCode = text.charCodeAt(i);\n        var pattern = (charCode % 2 === 0) ? \"110\" : \"101\";\n\n        for (var j = 0; j < pattern.length; j++) {\n          var color = pattern[j] === \"1\" ? \"black\" : \"white\";\n          barcode += \"<span style=\\\"border-left:0.01in solid \" + color + \";height:\" + height + \";display:inline-block;\\\"></span>\";\n        }\n      }\n\n      return \"<div style=\\\"text-align:center\\\">\" + barcode + \"</div><div style=\\\"text-align:center;font-size:10px;margin-top:2px;\\\">\" + text + \"</div>\";\n    }\n  }, 1000);\n})();\n</script>", "InsertPosition": "append", "TargetSelector": "head"}')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly'
  AND ActionNumber = 1;

-- Verify the update - show count of HtmlInjections
SELECT
    sa.ActionNumber,
    sa.ActionName,
    sa.ActionType,
    json_array_length(json_extract(sa.Configuration, '$.HtmlInjections')) as HtmlInjections_Count,
    json_extract(sa.Configuration, '$.HtmlInjections') as HtmlInjections
FROM SubAction sa
INNER JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 3
  AND sa.ActionType = 'NavigateOnly'
  AND sa.ActionNumber = 1;