using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class PageDetailView : ContentPage
{
    private readonly PageDetailViewModel _viewModel;

    public PageDetailView(PageDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // The PageId will be set via QueryProperty, triggering LoadPageDataAsync
        // No need to manually call it here
    }
}