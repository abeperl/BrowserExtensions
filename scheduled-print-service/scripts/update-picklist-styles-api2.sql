-- Update Picklist Styles for API 2 (Picklist Datatable API)
-- Date: 2025-12-15
--
-- Style Changes:
-- 1. Make customer box wide enough to show full name on one line
-- 2. Remove bold from SKU (make it same font size as other text)
-- 3. Make Barcode header same size as other headers
-- 4. Populate Barcode column with SKU barcode (using Libre Barcode 128 font)

UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.HtmlInjections[0].HtmlTemplate',
    '<style>
/* CUSTOMER BOX: Make wide enough for full name on one line */
.form-group[column-permission=CustomerMaster] .form-control[data-bind=Customers] {
  background-color: #000 !important;
  color: #fff !important;
  font-size: 2.55em !important;
  font-weight: bold !important;
  padding: 20px !important;
  text-align: center !important;
  display: block !important;
  margin-top: 10px !important;
  min-width: 100% !important;
  width: 100% !important;
  white-space: nowrap !important;
  overflow: visible !important;
}

/* Make "Pick List Detail" title bigger */
.page-title[locale-res="PickListDetail"] {
  font-weight: bold !important;
  padding: 15px 0 !important;
}

/* Hide specific form groups */
.form-group[column-permission="Seller"],
.form-group[column-permission="TotalQty"],
.form-group[column-permission="TotalCases"],
.form-group[column-permission="PalletCount"],
.form-group[column-permission="AssignedCart"],
.form-group[column-permission="TotalAmount"] {
  display: none !important;
}

/* BARCODE HEADER: Change Order# header to "Barcode" - same size as other headers */
th.numberid_width.skip-extend[locale-res="OrderNo"] {
  font-size: 0 !important;
}
th.numberid_width.skip-extend[locale-res="OrderNo"]::before {
  content: "Barcode" !important;
  font-weight: bold !important;
  font-size: inherit !important; /* Same size as other headers */
}

/* BARCODE DATA: Show SKU as barcode in Order# column */
td[data-repeat-item="OrderNumber"] {
  font-size: 0 !important;
}
td[data-repeat-item="OrderNumber"]::after {
  content: attr(data-barcode-value) !important;
  font-size: 32px !important;
  font-family: "Libre Barcode 128", monospace !important;
  font-weight: normal !important;
  display: block !important;
  line-height: 1.2 !important;
}

/* SKU COLUMN: Remove bold, use normal font like other text */
.show_thermal .sku[data-repeat-item="SKU"] {
  font-family: inherit !important;
  font-size: inherit !important;
  font-weight: normal !important;
  display: inline-block !important;
}
</style>
<script>
// JavaScript to copy SKU value to Order# column as barcode
(function() {
  // Wait for DOM to be fully loaded
  function updateBarcodes() {
    const rows = document.querySelectorAll("tbody tr");
    rows.forEach(row => {
      const skuCell = row.querySelector(".sku[data-repeat-item=\"SKU\"]");
      const orderCell = row.querySelector("td[data-repeat-item=\"OrderNumber\"]");
      if (skuCell && orderCell) {
        const skuValue = skuCell.textContent.trim();
        orderCell.setAttribute("data-barcode-value", skuValue);
      }
    });
  }

  // Run on load
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", updateBarcodes);
  } else {
    updateBarcodes();
  }

  // Also run after delays to catch dynamic content
  setTimeout(updateBarcodes, 500);
  setTimeout(updateBarcodes, 1500);
  setTimeout(updateBarcodes, 3000);
})();
</script>'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 1;

-- Verify the update
SELECT
  ActionNumber,
  ActionName,
  ActionType,
  substr(json_extract(Configuration, '$.HtmlInjections[0].HtmlTemplate'), 1, 100) as StylePreview
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 1;