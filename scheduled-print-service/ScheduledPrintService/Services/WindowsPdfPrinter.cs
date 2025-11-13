using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ScheduledPrintService.Services;

// Sends raw PDF bytes to the Windows print spooler.
// Note: This only works if the target printer supports PDF directly.
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
    {
        var printer = _config.PrinterName ?? _config.FallbackPrinterName;
        if (string.IsNullOrWhiteSpace(printer))
        {
            throw new InvalidOperationException("PrinterName is not configured.");
        }

        if (!RawPrinterHelper.SendBytesToPrinter(printer!, pdfBytes, jobName))
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            throw new InvalidOperationException($"Failed to spool to printer '{printer}': {error.Message}");
        }

        _logger.LogInformation("Spooling job '{Job}' to printer '{Printer}'. Bytes={Length}", jobName, printer, pdfBytes.Length);
        return Task.CompletedTask;
    }

    private static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOC_INFO_1
        {
            public string? pDocName;
            public string? pOutputFile;
            public string? pDatatype;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOC_INFO_1 di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string printerName, byte[] bytes, string documentName)
        {
            IntPtr hPrinter;
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return false;

            try
            {
                var di = new DOC_INFO_1
                {
                    pDocName = documentName,
                    pOutputFile = null,
                    pDatatype = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        return false;

                    try
                    {
                        int dwWritten;
                        IntPtr pUnmanagedBytes = Marshal.AllocHGlobal(bytes.Length);
                        try
                        {
                            Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                            if (!WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out dwWritten))
                                return false;
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pUnmanagedBytes);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }

                return true;
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
