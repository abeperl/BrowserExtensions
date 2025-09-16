using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class ApiConfigurationPage : ContentPage
{
    private readonly ApiConfigurationViewModel _viewModel;

    public ApiConfigurationPage(ApiConfigurationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPageCommand.ExecuteAsync(null);
    }
}