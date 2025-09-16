using DataFlow.Mobile.Views.Pages;

namespace DataFlow.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register navigation routes
		Routing.RegisterRoute("pagedetail", typeof(PageDetailView));
		Routing.RegisterRoute("templateeditor", typeof(TemplateEditorPage));
		Routing.RegisterRoute("pagewizard", typeof(PageWizardPage));
		Routing.RegisterRoute("apiconfiguration", typeof(ApiConfigurationPage));
		Routing.RegisterRoute("advancedtemplatedesigner", typeof(AdvancedTemplateDesignerPage));
		Routing.RegisterRoute("actionconfiguration", typeof(ActionConfigurationPage));
	}
}
