using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class AudioSettingsPage : ContentPage
{
    public AudioSettingsPage(AudioSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}