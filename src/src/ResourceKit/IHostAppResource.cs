namespace Purview.Aspire.ResourceKit;

public interface IHostAppResource<in THostApp>
	where THostApp : class
{
	bool IsEnabled { get; set; }

	void Build(IDistributedApplicationBuilder builder);

	void Configure(THostApp app);
}
