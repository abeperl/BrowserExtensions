namespace DataFlow.Mobile.Services.Interfaces;

public interface INetworkService
{
    bool IsConnected { get; }
    Task<bool> IsConnectedAsync();
    Task<bool> CanReachHostAsync(string host, int timeoutSeconds = 5);
    Task<NetworkQuality> GetNetworkQualityAsync();

    event EventHandler<bool> ConnectivityChanged;
}

public enum NetworkQuality
{
    Unknown = 0,
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}