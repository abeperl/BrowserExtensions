using System.Diagnostics;
using Microsoft.Extensions.Logging;
using DataFlow.Mobile.Services.Interfaces;

namespace DataFlow.Mobile.Services;

public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;

    public NetworkService(ILogger<NetworkService> logger)
    {
        _logger = logger;

        // Subscribe to connectivity changes
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsConnected => Connectivity.NetworkAccess == NetworkAccess.Internet;

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var current = Connectivity.NetworkAccess;
            return await Task.FromResult(current == NetworkAccess.Internet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connectivity status");
            return false;
        }
    }

    public async Task<bool> CanReachHostAsync(string host, int timeoutSeconds = 5)
    {
        try
        {
            if (!IsConnected)
                return false;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var response = await client.GetAsync($"https://{host}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reaching host: {Host}", host);
            return false;
        }
    }

    public async Task<NetworkQuality> GetNetworkQualityAsync()
    {
        try
        {
            if (!IsConnected)
                return NetworkQuality.Unknown;

            var profiles = Connectivity.ConnectionProfiles;

            if (profiles.Contains(ConnectionProfile.WiFi))
            {
                return await TestNetworkSpeed() switch
                {
                    > 25 => NetworkQuality.Excellent,
                    > 10 => NetworkQuality.Good,
                    > 1 => NetworkQuality.Fair,
                    _ => NetworkQuality.Poor
                };
            }
            else if (profiles.Contains(ConnectionProfile.Cellular))
            {
                return await TestNetworkSpeed() switch
                {
                    > 10 => NetworkQuality.Good,
                    > 1 => NetworkQuality.Fair,
                    _ => NetworkQuality.Poor
                };
            }

            return NetworkQuality.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining network quality");
            return NetworkQuality.Unknown;
        }
    }

    public event EventHandler<bool>? ConnectivityChanged;

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isConnected = e.NetworkAccess == NetworkAccess.Internet;
        _logger.LogInformation("Network connectivity changed: {IsConnected}", isConnected);
        ConnectivityChanged?.Invoke(this, isConnected);
    }

    private async Task<double> TestNetworkSpeed()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Download a small test file to measure speed
            var testUrl = "https://httpbin.org/bytes/1024"; // 1KB test download
            await client.GetByteArrayAsync(testUrl);

            stopwatch.Stop();

            // Calculate speed in Mbps (rough estimate)
            var speedMbps = (1.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) * 8;
            return speedMbps;
        }
        catch
        {
            return 0;
        }
    }
}