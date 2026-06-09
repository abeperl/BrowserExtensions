using DataFlow.Mobile.Services;

namespace DataFlow.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override async void OnStart()
	{
		base.OnStart();

		// Initialize database
		try
		{
			var dbContext = IPlatformApplication.Current?.Services?.GetService<DataFlowDbContext>();
			if (dbContext != null)
			{
				await dbContext.Database.EnsureCreatedAsync();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
		}
	}
}