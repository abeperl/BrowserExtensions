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
        await page.SetContentAsync(html);
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
