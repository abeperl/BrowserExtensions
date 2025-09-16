using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}