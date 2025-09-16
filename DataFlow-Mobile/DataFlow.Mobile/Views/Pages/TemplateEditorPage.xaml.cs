using DataFlow.Mobile.ViewModels;

namespace DataFlow.Mobile.Views.Pages;

public partial class TemplateEditorPage : ContentPage
{
    private readonly TemplateEditorViewModel _viewModel;

    public TemplateEditorPage(TemplateEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Template loading will be triggered by the QueryProperty when TemplateId is set
    }
}