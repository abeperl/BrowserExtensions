namespace ScheduledPrintService.Models;

public class PdfConfig
{
    public string ChromiumDownloadMode { get; set; } = "Auto"; // Auto | External | Disabled
    public string? ChromiumExecutablePath { get; set; }
    public string CacheDirectory { get; set; } = "chromium-cache";
    public int NavigationTimeoutSeconds { get; set; } = 30;
    public double? PageWidthInches { get; set; } = 8.27; // Default A4 width
    public double? PageHeightInches { get; set; } = 11.69; // Default A4 height
    public double MarginMillimeters { get; set; } = 10; // uniform margin
    public bool Landscape { get; set; } = false;
    public bool PrintBackground { get; set; } = true;

    /// <summary>
    /// Optional additional delay (milliseconds) after initial HTML load to allow XHR/SPA rendering to complete.
    /// Set to 0 to disable.
    /// </summary>
    public int WaitForNetworkIdleMs { get; set; } = 0;

    /// <summary>
    /// Optional CSS selector that must appear before printing. Useful for waiting until a specific element
    /// (e.g. a data table populated by XHR) is present. Leave null/empty to skip.
    /// </summary>
    public string? WaitForSelector { get; set; }

    /// <summary>
    /// Additional selectors that should exist before printing. Each is waited for presence only (not text content).
    /// Empty list skips.
    /// </summary>
    public List<string> AdditionalWaitSelectors { get; set; } = new();

    /// <summary>
    /// Milliseconds to wait after all selectors/data-row conditions are met to allow final dynamic bindings to settle.
    /// Set to 0 to disable.
    /// </summary>
    public int PostSelectorStableMs { get; set; } = 800;

    /// <summary>
    /// CSS selector targeting data rows whose presence indicates XHR-loaded data is rendered (e.g., table tbody rows).
    /// Leave null/empty to skip row count waiting.
    /// </summary>
    public string? DataReadyRowSelector { get; set; }

    /// <summary>
    /// Minimum number of rows (matching DataReadyRowSelector) required before proceeding. Ignored if selector is empty.
    /// </summary>
    public int MinimumDataRows { get; set; } = 1;

    /// <summary>
    /// When true, capture a full-page diagnostic PNG screenshot at key points (after navigation & before print).
    /// </summary>
    public bool CaptureDiagnosticScreenshot { get; set; } = true;

    /// <summary>
    /// When true, log selected network responses (filtered by NetworkLogUrlPattern) for debugging data load issues.
    /// </summary>
    public bool DebugLogNetworkResponses { get; set; } = true;

    /// <summary>
    /// Simple substring or regex (if enclosed by /.../) used to filter which response URLs to log.
    /// Default targets ManualPicking related requests.
    /// </summary>
    public string NetworkLogUrlPattern { get; set; } = "GetPickListDetails";

    /// <summary>
    /// Total milliseconds to continue polling for data rows after initial wait stages (progressive polling).
    /// </summary>
    public int DataLoadRetryMs { get; set; } = 5000;

    /// <summary>
    /// When true, each navigation (capture-only phase) will launch a completely isolated Chromium instance
    /// instead of reusing a shared browser. This mitigates state leakage and timing races in complex SPAs
    /// at the cost of performance. Use only if shared-browser mode fails to consistently trigger required XHRs.
    /// </summary>
    public bool IsolateBrowserPerPicklist { get; set; } = false;

    /// <summary>
    /// Optional CSS selector for rows/items that must be clicked to trigger detail XHR (e.g. table row in Manual Picking).
    /// If set, after hash navigation & SPA ready, code will attempt to find a row whose innerText contains the current order/picklist id
    /// and dispatch a click before waiting for data readiness.
    /// </summary>
    public string? AutoClickRowSelector { get; set; }
}
