using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class PageWizardPage : ContentPage
{
    private readonly PageWizardViewModel _viewModel;

    public PageWizardPage(PageWizardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load page data if in edit mode - this will be triggered by QueryProperty
    }
}