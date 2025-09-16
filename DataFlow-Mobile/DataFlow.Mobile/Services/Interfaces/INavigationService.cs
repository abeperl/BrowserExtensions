namespace DataFlow.Mobile.Services.Interfaces;

public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
    Task GoBackAsync(IDictionary<string, object> parameters);
    Task PopToRootAsync();
    string GetCurrentRoute();
}