using System.Threading;
using System.Threading.Tasks;

namespace ScheduledPrintService.Services;

public interface IPdfPrinter
{
    Task PrintAsync(byte[] pdfBytes, string jobName, CancellationToken ct);
}
