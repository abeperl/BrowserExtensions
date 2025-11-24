using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public class PdfPrintService
{
    private readonly ILogger<PdfPrintService> _logger;
    private readonly PdfBrowserManager _browserManager;
    private readonly IPdfPrinter _printer;
    private readonly PdfConfig _pdfConfig;

    public PdfPrintService(
        ILogger<PdfPrintService> logger,
        PdfBrowserManager browserManager,
        IPdfPrinter printer,
        IOptions<PdfConfig> pdfOptions)
    {
        _logger = logger;
        _browserManager = browserManager;
        _printer = printer;
        _pdfConfig = pdfOptions.Value;
    }

    public async Task PrintUrlAsync(string url, string jobName, CancellationToken ct)
    {
        var pdfBytes = await RenderUrlToPdfAsync(url, ct);
        await _printer.PrintAsync(pdfBytes, jobName, ct);
    }

    public async Task PrintHtmlAsync(string html, string jobName, CancellationToken ct)
    {
        var pdfBytes = await RenderHtmlToPdfAsync(html, ct);
        await _printer.PrintAsync(pdfBytes, jobName, ct);
    }

    /// <summary>
    /// Directly print already-generated PDF bytes. Useful when a caller has performed advanced navigation
    /// and produced a PDF via Puppeteer and wants to reuse existing PdfPrintService plumbing (tracking, naming, etc.).
    /// </summary>
    public Task PrintPdfBytesAsync(byte[] pdfBytes, string jobName, CancellationToken ct)
        => _printer.PrintAsync(pdfBytes, jobName, ct);

    public async Task<byte[]> RenderUrlToPdfAsync(string url, CancellationToken ct)
    {
        await using var page = await _browserManager.NewPageAsync(ct);
        _logger.LogInformation("Navigating to {Url}", url);
        await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);
        return await CreatePdfAsync(page, ct);
    }

    public async Task<byte[]> RenderHtmlToPdfAsync(string html, CancellationToken ct)
    {
        await using var page = await _browserManager.NewPageAsync(ct);
        // Set initial HTML content. Dynamic scripts may still be loading data.
        await page.SetContentAsync(html);

        // Optional selector wait (for SPA/XHR rendered elements)
        if (!string.IsNullOrWhiteSpace(_pdfConfig.WaitForSelector))
        {
            try
            {
                _logger.LogDebug("Waiting for selector '{Selector}' before PDF capture", _pdfConfig.WaitForSelector);
                await page.WaitForSelectorAsync(_pdfConfig.WaitForSelector, new WaitForSelectorOptions
                {
                    Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
                });

                // Additionally wait until the element has non-empty text content (value must load)
                var escaped = _pdfConfig.WaitForSelector.Replace("'", "\\'");
                _logger.LogDebug("Waiting for non-empty text content in '{Selector}'", _pdfConfig.WaitForSelector);
                try
                {
                    await page.WaitForFunctionAsync($"() => (document.querySelector('{escaped}')?.textContent || '').trim().length > 0", new WaitForFunctionOptions
                    {
                        Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Element '{Selector}' did not acquire non-empty text before timeout; proceeding anyway.", _pdfConfig.WaitForSelector);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Selector '{Selector}' not found before timeout; continuing to print anyway.", _pdfConfig.WaitForSelector);
            }
        }

        // Try network idle wait (non-fatal) to allow outstanding XHRs to finish
        try
        {
            _logger.LogDebug("Attempting network idle wait for dynamic content");
            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions
            {
                IdleTime = 1000,
                Timeout = _pdfConfig.NavigationTimeoutSeconds * 1000
            });
        }
        catch (Exception ex) when (ex is WaitTaskTimeoutException or TaskCanceledException)
        {
            _logger.LogDebug("Network idle wait skipped: {Message}", ex.Message);
        }

        // Configurable extra delay as a last resort for late rendering frameworks (React hydration etc.)
        if (_pdfConfig.WaitForNetworkIdleMs > 0)
        {
            _logger.LogDebug("Extra post-load delay {Ms}ms for dynamic rendering", _pdfConfig.WaitForNetworkIdleMs);
            try { await Task.Delay(_pdfConfig.WaitForNetworkIdleMs, ct); } catch (TaskCanceledException) { }
        }
        return await CreatePdfAsync(page, ct);
    }

    private async Task<byte[]> CreatePdfAsync(IPage page, CancellationToken ct)
    {
        var marginInches = _pdfConfig.MarginMillimeters / 25.4;

        var pdfOptions = new PdfOptions
        {
            PrintBackground = _pdfConfig.PrintBackground,
            Landscape = _pdfConfig.Landscape,
            MarginOptions = new MarginOptions
            {
                Top = $"{marginInches}in",
                Right = $"{marginInches}in",
                Bottom = $"{marginInches}in",
                Left = $"{marginInches}in"
            }
        };

        if (_pdfConfig.PageWidthInches.HasValue && _pdfConfig.PageHeightInches.HasValue)
        {
            pdfOptions.Width = $"{_pdfConfig.PageWidthInches.Value}in";
            pdfOptions.Height = $"{_pdfConfig.PageHeightInches.Value}in";
        }

        _logger.LogInformation("Generating PDF (Landscape={Landscape}, Bg={Bg})", pdfOptions.Landscape, pdfOptions.PrintBackground);
        var bytes = await page.PdfDataAsync(pdfOptions);
        _logger.LogInformation("Generated PDF with {Length} bytes", bytes.Length);
        return bytes;
    }
}
