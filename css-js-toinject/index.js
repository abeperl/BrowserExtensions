// Simple table customization script
// Features:
// 1. Toggle button for column visibility
// 2. Natural row heights
// 3. Horizontal checkbox layout

// Wait for the table to be ready
function waitForTable() {
    const table = document.getElementById('so-items-table');
    if (table) {
        initializeTableFeatures(table);
    } else {
        // Retry after a short delay
        setTimeout(waitForTable, 100);
    }
}

// Initialize all table features
function initializeTableFeatures(table) {
    applyBaseStyles();
    addToggleButton(table);
    setupColumnToggle(table);
}

// Apply base CSS styles for natural heights and horizontal checkboxes
function applyBaseStyles() {
    const styleId = 'table-custom-styles';
    if (document.getElementById(styleId)) return;

    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = `
        /* Basic table structure - ensure full width with no empty space */
        #so-items-table {
            width: 100% !important;
            table-layout: fixed !important;
            border-collapse: collapse !important;
            margin: 0 !important;
            border-spacing: 0 !important;
        }

        /* Ensure table container takes full width without empty space */
        #so-items-scroll {
            width: 100% !important;
            overflow-x: auto !important;
            margin: 0 !important;
            padding: 0 !important;
            position: relative !important;
        }

        /* Compact row heights - minimal padding for tight layout */
        #so-items-table thead tr,
        #so-items-table tbody tr {
            height: auto !important;
            min-height: auto !important;
            max-height: none !important;
            margin: 0 !important;
            padding: 0 !important;
            line-height: 1.2 !important;
        }

        #so-items-table thead th,
        #so-items-table tbody td {
            height: auto !important;
            min-height: auto !important;
            max-height: none !important;
            padding: 4px 6px !important;
            margin: 0 !important;
            vertical-align: middle !important;
            overflow: hidden !important;
            text-overflow: ellipsis !important;
            white-space: nowrap !important;
            line-height: 1.2 !important;
        }

        /* Header styling - compact */
        #so-items-table thead th {
            font-weight: bold !important;
            font-size: 11px !important;
            text-align: center !important;
            border-bottom: 1px solid #ddd !important;
            padding: 4px 6px !important;
        }

        /* Horizontal checkbox layout */
        #so-items-table thead th[class="text-center"],
        #so-items-table tbody td.handed-over-column {
            display: flex !important;
            flex-direction: row !important;
            flex-wrap: nowrap !important;
            align-items: center !important;
            justify-content: center !important;
            gap: 8px !important;
        }

        #so-items-table thead th[class="text-center"] .checkbox-cs,
        #so-items-table tbody td.handed-over-column .checkbox-cs {
            display: inline-flex !important;
            align-items: center !important;
            justify-content: center !important;
            margin: 0 !important;
            padding: 0 !important;
        }

        /* Hide columns when table has columns-hidden class */
        #so-items-table.columns-hidden thead th:nth-child(5),
        #so-items-table.columns-hidden tbody td:nth-child(5),
        #so-items-table.columns-hidden thead th:nth-child(8),
        #so-items-table.columns-hidden tbody td:nth-child(8),
        #so-items-table.columns-hidden thead th:nth-child(9),
        #so-items-table.columns-hidden tbody td:nth-child(9),
        #so-items-table.columns-hidden thead th:nth-child(10),
        #so-items-table.columns-hidden tbody td:nth-child(10) {
            display: none !important;
        }

        /* Toggle button styles */
        #column-toggle-btn {
            position: fixed;
            top: 10px;
            right: 10px;
            z-index: 2147483647;
            padding: 8px 16px;
            background: #007bff;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            pointer-events: auto;
        }

        #column-toggle-btn:hover {
            background: #0056b3;
        }
    `;
    document.head.appendChild(style);
}

// Add toggle button to the table
function addToggleButton(table) {
    // Create the button
    const button = document.createElement('button');
    button.id = 'column-toggle-btn';
    button.textContent = 'Hide Details'; // Start with "Hide Details" since columns are initially visible
    button.title = 'Toggle visibility of List Price, Unit Disc, Disc %, and Unit Tax columns';

    // Position the button relative to the table
    function positionButton() {
        const container = document.getElementById('so-items-scroll') || table.parentElement;

        if (container) {
            const containerRect = container.getBoundingClientRect();
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const scrollLeft = window.pageXOffset || document.documentElement.scrollLeft;

            // Position relative to container for stability
            button.style.position = 'fixed'; // Use fixed positioning for stability
            button.style.top = '10px';
            button.style.right = '10px';
            button.style.zIndex = '2147483647';
            button.style.pointerEvents = 'auto';
        }
    }

    // Initial positioning - button is now fixed position so no need for repositioning
    positionButton();

    // No need for scroll/resize listeners since button is fixed position
    // The button will stay in the top-right corner of the viewport

    // Add click handler
    button.addEventListener('click', () => {
        const isHidden = table.classList.toggle('columns-hidden');
        button.textContent = isHidden ? 'Show Details' : 'Hide Details';
    });

    // Add to page
    document.body.appendChild(button);
}

// Setup column toggle functionality
function setupColumnToggle(table) {
    // Initially show all columns (don't hide any)
    // Columns will be hidden only when user clicks the toggle button
}

// Start when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', waitForTable);
} else {
    waitForTable();
}