using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;

namespace ScheduledPrintService.Services;

/// <summary>
/// Prints PDF files using Windows shell (default PDF viewer) or Adobe Reader.
/// This approach properly handles PDF rendering and printer communication.
/// </summary>
public class WindowsPdfPrinter : IPdfPrinter
{
    private readonly ILogger<WindowsPdfPrinter> _logger;
    private readonly PrinterConfig _config;

    public WindowsPdfPrinter(ILogger<WindowsPdfPrinter> logger, IOptions<PrinterConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public Task PrintAsync(byte[] pdfBytes, string jobName, CancellationToken ct)
        => PrintAsync(pdfBytes, jobName, null, ct);

    public async Task PrintAsync(byte[] pdfBytes, string jobName, string? printerName, CancellationToken ct)
    {
        var printer = printerName ?? _config.PrinterName ?? _config.FallbackPrinterName;
        if (string.IsNullOrWhiteSpace(printer))
        {
            throw new InvalidOperationException("PrinterName is not configured.");
        }

        // Validate printer exists
        if (!PrinterExists(printer!))
        {
            throw new InvalidOperationException($"Printer '{printer}' not found on this system.");
        }

        // Create temporary file for printing
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        try
        {
            // Write PDF to temporary file
            await File.WriteAllBytesAsync(tempFile, pdfBytes, ct);
            _logger.LogInformation("Created temporary PDF file: {TempFile} ({Bytes} bytes)", tempFile, pdfBytes.Length);

            // Try multiple printing methods in order of preference
            bool printed = false;

            // Method 1: Try SumatraPDF if available (best for silent printing, no UI, very reliable)
            if (!printed && TryFindSumatraPDF(out var sumatraPath))
            {
                _logger.LogInformation("Attempting to print using SumatraPDF: {SumatraPath}", sumatraPath);
                printed = await PrintWithSumatraPDFAsync(tempFile, printer!, sumatraPath, ct);
            }

            // Method 2: Try Ghostscript if available (excellent for server-side PDF printing)
            if (!printed && TryFindGhostscript(out var gsPath))
            {
                _logger.LogInformation("Attempting to print using Ghostscript: {GsPath}", gsPath);
                printed = await PrintWithGhostscriptAsync(tempFile, printer!, gsPath, ct);
            }

            // Method 3: Try Adobe Reader if available (may hang if another instance is running)
            if (!printed && TryFindAdobeReader(out var adobePath))
            {
                _logger.LogInformation("Attempting to print using Adobe Reader: {AdobePath}", adobePath);
                printed = await PrintWithAdobeReaderAsync(tempFile, printer!, adobePath, ct);
            }

            // Method 4: Use Windows shell default PDF handler (least reliable)
            if (!printed)
            {
                _logger.LogInformation("Attempting to print using Windows shell (default PDF viewer)");
                printed = await PrintWithShellAsync(tempFile, printer!, ct);
            }

            if (!printed)
            {
                throw new InvalidOperationException($"Failed to print PDF '{jobName}' to printer '{printer}' using all available methods.");
            }

            _logger.LogInformation("Successfully spooled job '{Job}' to printer '{Printer}'. Bytes={Length}", jobName, printer, pdfBytes.Length);
        }
        finally
        {
            // Clean up temporary file after a delay (allow time for spooler to read it)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000, CancellationToken.None); // Wait 5 seconds
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                        _logger.LogDebug("Deleted temporary PDF file: {TempFile}", tempFile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary PDF file: {TempFile}", tempFile);
                }
            }, CancellationToken.None);
        }
    }

    private bool PrinterExists(string printerName)
    {
        try
        {
            var printers = PrinterSettings.InstalledPrinters;
            foreach (string printer in printers)
            {
                if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate installed printers");
            return true; // Assume printer exists if we can't check
        }
    }

    private bool TryFindAdobeReader(out string adobePath)
    {
        // Common Adobe Reader installation paths
        var possiblePaths = new[]
        {
            @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
            @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
            @"C:\Program Files\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe",
            @"C:\Program Files (x86)\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe",
            @"C:\Program Files\Adobe\Reader 11.0\Reader\AcroRd32.exe",
            @"C:\Program Files (x86)\Adobe\Reader 11.0\Reader\AcroRd32.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                adobePath = path;
                return true;
            }
        }

        adobePath = string.Empty;
        return false;
    }

    private bool TryFindSumatraPDF(out string sumatraPath)
    {
        // Common SumatraPDF installation paths
        var possiblePaths = new[]
        {
            @"C:\Program Files\SumatraPDF\SumatraPDF.exe",
            @"C:\Program Files (x86)\SumatraPDF\SumatraPDF.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"SumatraPDF\SumatraPDF.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                sumatraPath = path;
                return true;
            }
        }

        sumatraPath = string.Empty;
        return false;
    }

    private bool TryFindGhostscript(out string gsPath)
    {
        // Common Ghostscript installation paths
        var possiblePaths = new List<string>
        {
            @"C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.03.1\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.03.0\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.02.1\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.02.0\bin\gswin64c.exe",
            @"C:\Program Files (x86)\gs\gs10.04.0\bin\gswin32c.exe",
            @"C:\Program Files (x86)\gs\gs10.03.1\bin\gswin32c.exe",
            @"C:\Program Files (x86)\gs\gs10.03.0\bin\gswin32c.exe"
        };

        // Also check for any gs installation by scanning directories
        var gsDirs = new[] { @"C:\Program Files\gs", @"C:\Program Files (x86)\gs" };
        foreach (var gsDir in gsDirs)
        {
            if (Directory.Exists(gsDir))
            {
                try
                {
                    var versionDirs = Directory.GetDirectories(gsDir, "gs*");
                    foreach (var versionDir in versionDirs)
                    {
                        var gs64 = Path.Combine(versionDir, @"bin\gswin64c.exe");
                        var gs32 = Path.Combine(versionDir, @"bin\gswin32c.exe");
                        if (File.Exists(gs64)) possiblePaths.Add(gs64);
                        if (File.Exists(gs32)) possiblePaths.Add(gs32);
                    }
                }
                catch
                {
                    // Ignore directory enumeration errors
                }
            }
        }

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                gsPath = path;
                return true;
            }
        }

        gsPath = string.Empty;
        return false;
    }

    private async Task<bool> PrintWithGhostscriptAsync(string pdfPath, string printerName, string gsPath, CancellationToken ct)
    {
        try
        {
            // Ghostscript command line for printing with optimizations:
            // -dPrinted = mark as printed
            // -dBATCH = exit after processing
            // -dNOPAUSE = don't pause between pages
            // -dNOSAFER = allow file access (needed for Windows printer)
            // -q = quiet mode
            // -dNumCopies=1 = print one copy
            // -r600 = 600 DPI (reasonable quality without bloat)
            // -sDEVICE=mswinpr2 = Windows printer device
            // -dPDFFitPage = fit to page
            var startInfo = new ProcessStartInfo
            {
                FileName = gsPath,
                Arguments = $"-dPrinted -dBATCH -dNOPAUSE -dNOSAFER -q -dNumCopies=1 -r600 -dPDFFitPage -sDEVICE=mswinpr2 -sOutputFile=\"%printer%{printerName}\" \"{pdfPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start Ghostscript process");
                return false;
            }

            // Wait for process to complete with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Ghostscript printed successfully");
                    return true;
                }
                else
                {
                    var stderr = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("Ghostscript exited with code {ExitCode}. Error: {Error}", process.ExitCode, stderr);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                // Kill the process if it times out
                if (!process.HasExited)
                {
                    _logger.LogWarning("Ghostscript timed out after 30 seconds, killing process");
                    process.Kill(true);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to print using Ghostscript");
            return false;
        }
    }

    private async Task<bool> PrintWithAdobeReaderAsync(string pdfPath, string printerName, string adobePath, CancellationToken ct)
    {
        try
        {
            // Adobe Reader command line: /t (print to default) or /p (print dialog)
            // /t <file> <printername> - print to specific printer and close
            var startInfo = new ProcessStartInfo
            {
                FileName = adobePath,
                Arguments = $"/t \"{pdfPath}\" \"{printerName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start Adobe Reader process");
                return false;
            }

            // Wait for process to complete with timeout (Adobe Reader should exit after spooling)
            // Use a reasonable timeout instead of relying on the cancellation token
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Adobe Reader printed successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Adobe Reader exited with code {ExitCode}", process.ExitCode);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                // Kill the process if it times out
                if (!process.HasExited)
                {
                    _logger.LogWarning("Adobe Reader timed out after 30 seconds, killing process");
                    process.Kill(true);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to print using Adobe Reader");
            return false;
        }
    }

    private async Task<bool> PrintWithSumatraPDFAsync(string pdfPath, string printerName, string sumatraPath, CancellationToken ct)
    {
        try
        {
            // SumatraPDF command line: -print-to <printername> <file>
            // Add -print-settings to control print quality and avoid excessive rasterization
            // noscale = don't scale the page
            // paper=letter = use letter size paper
            // bin=auto = auto-select paper tray
            var startInfo = new ProcessStartInfo
            {
                FileName = sumatraPath,
                Arguments = $"-print-to \"{printerName}\" -print-settings \"noscale,paper=letter,bin=auto\" \"{pdfPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start SumatraPDF process");
                return false;
            }

            // Wait for process to complete with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("SumatraPDF printed successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("SumatraPDF exited with code {ExitCode}", process.ExitCode);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                // Kill the process if it times out
                if (!process.HasExited)
                {
                    _logger.LogWarning("SumatraPDF timed out after 20 seconds, killing process");
                    process.Kill(true);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to print using SumatraPDF");
            return false;
        }
    }

    private async Task<bool> PrintWithShellAsync(string pdfPath, string printerName, CancellationToken ct)
    {
        try
        {
            // Use shell verb "printto" to print using default PDF handler
            var startInfo = new ProcessStartInfo
            {
                FileName = pdfPath,
                Verb = "printto",
                Arguments = $"\"{printerName}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start shell print process");
                return false;
            }

            // Give the process time to spool the document with timeout
            // Shell printing doesn't always wait for completion, so we use a reasonable delay
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await Task.Delay(3000, linkedCts.Token);
                _logger.LogInformation("Shell print command executed (using default PDF viewer)");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Shell print operation was canceled");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to print using Windows shell");
            return false;
        }
    }
}
