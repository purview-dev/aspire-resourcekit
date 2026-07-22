namespace Purview.Aspire.ResourceIsolation;

public interface IHostAppResource<in THostApp>
	where THostApp : class
{
	bool IsEnabled { get; }

	void BuildResource(IDistributedApplicationBuilder builder, IServiceProvider services);

	void ConfigureResource(THostApp app, IServiceProvider services);
}
