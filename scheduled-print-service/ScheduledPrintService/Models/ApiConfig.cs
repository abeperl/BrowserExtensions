namespace ScheduledPrintService.Models;

public class ApiConfig
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://mj.3plnext.com";
    public string BearerToken { get; set; } = string.Empty;
    public int WarehouseId { get; set; } = 1;
    public Dictionary<string, string> Cookies { get; set; } = new();
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
    public int DelayMilliseconds { get; set; }

    // Continue to next action on error?
    public bool ContinueOnError { get; set; } = true;

    // For batch actions (e.g., CreatePicklistBatch): number of IDs per request
    public int BatchSize { get; set; } = 10;

    // For CreatePicklistBatch: whether to QuickShip
    public bool QuickShip { get; set; } = false;
}
