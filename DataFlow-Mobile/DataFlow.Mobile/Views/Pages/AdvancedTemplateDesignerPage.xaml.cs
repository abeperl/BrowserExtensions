using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class AdvancedTemplateDesignerPage : ContentPage
{
    private readonly AdvancedTemplateDesignerViewModel _viewModel;

    public AdvancedTemplateDesignerPage(AdvancedTemplateDesignerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTemplateCommand.ExecuteAsync(null);
    }
}