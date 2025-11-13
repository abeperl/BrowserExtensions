using System.Threading;
using System.Threading.Tasks;

namespace ScheduledPrintService.Services;

public interface IHtmlFetcher
{
    Task<string> FetchAsync(string url, CancellationToken ct);
}
