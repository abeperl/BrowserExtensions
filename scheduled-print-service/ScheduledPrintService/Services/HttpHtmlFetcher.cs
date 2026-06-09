using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ScheduledPrintService.Services;

public class HttpHtmlFetcher : IHtmlFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHtmlFetcher> _logger;

    public HttpHtmlFetcher(HttpClient httpClient, ILogger<HttpHtmlFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> FetchAsync(string url, CancellationToken ct)
    {
        _logger.LogInformation("Fetching HTML from {Url}", url);
        using var resp = await _httpClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogInformation("Fetched {Length} chars from {Url}", html.Length, url);
        return html;
    }
}
