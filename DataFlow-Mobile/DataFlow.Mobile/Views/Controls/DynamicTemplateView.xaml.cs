using DataFlow.Mobile.Services.Interfaces;
using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Views.Controls;

public partial class DynamicTemplateView : ContentView
{
    public static readonly BindableProperty ProcessedDataProperty =
        BindableProperty.Create(nameof(ProcessedData), typeof(ProcessedTemplateData), typeof(DynamicTemplateView), null);

    public static readonly BindableProperty TemplateProperty =
        BindableProperty.Create(nameof(Template), typeof(Models.Template), typeof(DynamicTemplateView), null);

    public static readonly BindableProperty LayoutTypeProperty =
        BindableProperty.Create(nameof(LayoutType), typeof(string), typeof(DynamicTemplateView), "List");

    public ProcessedTemplateData ProcessedData
    {
        get => (ProcessedTemplateData)GetValue(ProcessedDataProperty);
        set => SetValue(ProcessedDataProperty, value);
    }

    public Models.Template Template
    {
        get => (Models.Template)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public string LayoutType
    {
        get => (string)GetValue(LayoutTypeProperty);
        set => SetValue(LayoutTypeProperty, value);
    }

    public DynamicTemplateView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(ProcessedData) && ProcessedData != null)
        {
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        // Update layout based on template settings
        if (Template?.LayoutTemplate != null)
        {
            LayoutType = Template.LayoutTemplate.LayoutType;

            if (LayoutType == "Grid" && Template.LayoutTemplate.ColumnsPerRow > 1)
            {
                var gridLayout = new GridItemsLayout(Template.LayoutTemplate.ColumnsPerRow, ItemsLayoutOrientation.Vertical)
                {
                    HorizontalItemSpacing = Template.LayoutTemplate.ItemSpacing,
                    VerticalItemSpacing = Template.LayoutTemplate.ItemSpacing
                };
                GridView.ItemsLayout = gridLayout;
            }
        }
    }
}