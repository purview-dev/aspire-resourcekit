using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "publish-marker")]
sealed partial class PublishMarkerAppResource : ExampleHostAppResourceBase<ParameterResource>
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
