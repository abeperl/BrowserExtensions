namespace ScheduledPrintService.Models;

public class ApiConfig
{
    // Number identifier from PrimaryApi.ApiNumber
    public int ApiNumber { get; set; }
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://mj.3plnext.com";
    public string BearerToken { get; set; } = string.Empty;
    public int WarehouseId { get; set; } = 1;
    public Dictionary<string, string> Cookies { get; set; } = new();

    // Custom HTTP headers for this API (loaded from ApiHeaders table)
    // Replaces hardcoded logic for ClientId, StoreId, WarehouseId, etc.
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    // Primary API request definition (from database PrimaryApi row)
    public string PrimaryEndpoint { get; set; } = "/api/order/GetOrdersList";
    public string PrimaryHttpMethod { get; set; } = "POST";
    // Optional raw JSON payload to send for primary request. When null/empty, DefaultRequest is serialized instead.
    public string? PrimaryPayload { get; set; }

    // Login credentials for token renewal
    public string UserEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public OrdersListRequest DefaultRequest { get; set; } = new();
    public System.Text.Json.JsonElement? OrdersListColumns { get; set; }

    // Retry settings for HTTP calls
    public int RetryMaxAttempts { get; set; } = 5;
    public int RetryBaseDelayMs { get; set; } = 300;
    public int RetryMaxDelayMs { get; set; } = 3000;

    // JSON path to extract ID from each record (e.g., "[0]" for first element in array)
    public string IdJsonPath { get; set; } = "[0]";

    // Track processed IDs to avoid duplicates
    public string ProcessedIdsPath { get; set; } = "processed-orders.txt";

    // Manual mode: run once and exit (for debugging)
    public bool ManualMode { get; set; } = false;

    // Printer name for this API (optional, uses default if not specified)
    public string? PrinterName { get; set; }

    // Primary API response filtering (applied before calling sub-actions)
    public string? PrimaryFilterArrayJsonPath { get; set; }
    public int? PrimaryFilterArrayIndex { get; set; }
    public string? PrimaryFilterType { get; set; }
    public string? PrimaryFilterValue { get; set; }
    public string? PrimaryFilterField { get; set; }

    // Primary API field mapping for placeholder replacement
    // Maps dictionary keys to JSON paths or field names in the API response
    // Example: { "customerName": "Customers", "orderNumber": "[0]" }
    public Dictionary<string, string>? PrimaryApiFieldMap { get; set; }

    // Sub-actions to perform for each order
    public List<SubAction> SubActions { get; set; } = new();
}

public class OrdersListRequest
{
    public int Draw { get; set; } = 1;
    public int Start { get; set; } = 0;
    public int Length { get; set; } = 25;
    public string ClientID { get; set; } = "0";
    public string StatusName { get; set; } = "1,2,3,4,5,6,7,8,9,10";
    public int ChannelId { get; set; } = 0;
    public int CreatedBy { get; set; } = 0;
    public int PaymentMethod { get; set; } = 0;
    public string PaymentMethodString { get; set; } = "0";
    public string Customer { get; set; } = string.Empty;
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public bool IsDropship { get; set; } = false;
    public int CarrierId { get; set; } = 0;
    public string PickupDate { get; set; } = string.Empty;
    public bool NotSchedule { get; set; } = false;
    public bool IsQuickOrder { get; set; } = false;
    public int BackOrders { get; set; } = 0;
    public int IsPersonalized { get; set; } = 0;
    public string OrderTypeFulfillment { get; set; } = "0";
    public string CutOffOrders { get; set; } = "0";
    public string InStock { get; set; } = "0";
    public int ClientOrderStatusId { get; set; } = 0;
    public int ClientOrderStatusDetailId { get; set; } = 0;
}

public class SubAction
{
    // Action type: "CallApi", "GetHtmlAndPrint", "Delay"
    public string Type { get; set; } = string.Empty;

    // Display name for logging
    public string Name { get; set; } = string.Empty;

    // Enable/disable this action
    public bool Enabled { get; set; } = true;

    // API endpoint (relative to BaseUrl) - supports {id} placeholder
    public string Endpoint { get; set; } = string.Empty;

    // HTTP method: GET, POST, PUT, DELETE
    public string Method { get; set; } = "GET";

    // Request body template (JSON) - supports {id} placeholder
    public string? RequestBody { get; set; }

    // Custom headers for this action
    public Dictionary<string, string> Headers { get; set; } = new();

    // For GetHtmlAndPrint: JSON path to extract HTML from response
    public string? HtmlJsonPath { get; set; }

    // For Delay: milliseconds to wait
    public int? DelayMilliseconds { get; set; }

    // Continue to next action on error?
    public bool? ContinueOnError { get; set; } = true;

    // For batch actions (e.g., CreatePicklistBatch): number of IDs per request
    public int? BatchSize { get; set; } = 10;

    // For CreatePicklistBatch: whether to QuickShip
    public bool? QuickShip { get; set; } = false;

    // For CreatePicklistBatch: whether to force create picklist
    public bool? ForceCreatePicklist { get; set; } = false;

    // Chaining support: store response data in context with this key
    public string? OutputVariableName { get; set; }

    // Chaining support: use data from previous action stored in context
    public bool? UseChainedInput { get; set; } = false;

    // Chaining support: map context variable to action parameter (e.g., {picklistId} -> context["picklistIds"])
    public Dictionary<string, string> ChainedInputMapping { get; set; } = new();

    // For GetUrlAndPrint: wait for network idle before capturing
    public int? WaitForNetworkIdleMs { get; set; } = 3000;

    // For GetUrlAndPrint: make all hidden inputs visible
    public bool? MakeHiddenVisible { get; set; } = false;

    // Chaining support: JSON path to extract array data for iteration (e.g., "data" to get data array)
    public string? ChainedArrayJsonPath { get; set; }

    // Chaining support: JSON path to extract specific field from each item (e.g., "pickListId")
    public string? ChainedItemFieldPath { get; set; }

    // Chaining filter: field name to filter on (e.g., "itemNotes")
    public string? ChainedFilterField { get; set; }

    // Chaining filter: filter type - "NotEmpty", "Contains", "NotStartsWith", "StartsWithAny", "NotStartsWithAny", "IsFilePath"
    public string? ChainedFilterType { get; set; }

    // Chaining filter: value to compare against (optional, depends on filter type)
    public string? ChainedFilterValue { get; set; }

    // Chaining filter: multiple values for StartsWithAny/NotStartsWithAny filters (JSON array)
    public List<string>? ChainedFilterValues { get; set; }

    // Chaining filter: array index to filter on (e.g., 17 to check data[x][17])
    public int? ChainedFilterArrayIndex { get; set; }

    // Additional filter: field name for second-level filtering (e.g., "orderId" for API 3)
    public string? AdditionalFilterField { get; set; }

    // Additional filter: filter type (same types as ChainedFilterType)
    public string? AdditionalFilterType { get; set; }

    // Additional filter: single value to compare against
    public string? AdditionalFilterValue { get; set; }

    // Additional filter: multiple values for StartsWithAny/NotStartsWithAny filters
    public List<string>? AdditionalFilterValues { get; set; }

    // Action chaining: action number to chain from (receives output from that action)
    public int? ChainedFromActionNumber { get; set; }

    // Output file prefix for saved PDFs (used by SaveCapturedHtml and PrintSavedPdf)
    public string? OutputFilePrefix { get; set; }

    // Printer name override for this specific action
    public string? PrinterName { get; set; }

    // JSON path to extract ID field from item (e.g., "orderDetailsId")
    public string? IdJsonPath { get; set; }

    // HTML injection configurations for NavigateOnly actions
    // Injects dynamic HTML into the page with placeholder replacement
    public List<HtmlInjection>? HtmlInjections { get; set; }

    // Store name for SaveJsonToFile action - appends store name to output directory path
    // e.g., if StoreName = "Boro Park", output becomes: basePath/sales-orders/Boro Park/order-123.json
    public string? StoreName { get; set; }
}

/// <summary>
/// Configuration for injecting HTML into a page during NavigateOnly actions
/// </summary>
public class HtmlInjection
{
    /// <summary>
    /// CSS selector to target the element where HTML will be injected
    /// </summary>
    public string TargetSelector { get; set; } = string.Empty;

    /// <summary>
    /// Where to insert the HTML relative to the target element
    /// Options: "append", "prepend", "after", "before", "replace"
    /// </summary>
    public string InsertPosition { get; set; } = "append";

    /// <summary>
    /// HTML template with {placeholders} that will be replaced with values from the primary API response dictionary
    /// Example: "<h4 class=\"title\">{customerName}</h4>"
    /// </summary>
    public string HtmlTemplate { get; set; } = string.Empty;
}
