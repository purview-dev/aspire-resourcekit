namespace Purview.Aspire.ResourceKit;

public interface IIsolatedResource
{
	string ResourceKey { get; }

	bool IsEnabled { get; }

	void BuildResource(IDistributedApplicationBuilder builder, AppIsolationContext context);

	void ConfigureResource(IsolatedResourceCollection app, AppIsolationContext context);
}
