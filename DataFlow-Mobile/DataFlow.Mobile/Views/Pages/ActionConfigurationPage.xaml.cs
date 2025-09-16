using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class ActionConfigurationPage : ContentPage
{
    private readonly ActionConfigurationViewModel _viewModel;

    public ActionConfigurationPage(ActionConfigurationViewModel viewModel)
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