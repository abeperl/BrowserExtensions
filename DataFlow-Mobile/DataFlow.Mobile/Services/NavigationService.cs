using DataFlow.Mobile.Services.Interfaces;

namespace DataFlow.Mobile.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(route, parameters);
    }

    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task GoBackAsync(IDictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync("..", parameters);
    }

    public async Task PopToRootAsync()
    {
        await Shell.Current.GoToAsync("///");
    }

    public string GetCurrentRoute()
    {
        return Shell.Current.CurrentState.Location.ToString();
    }
}