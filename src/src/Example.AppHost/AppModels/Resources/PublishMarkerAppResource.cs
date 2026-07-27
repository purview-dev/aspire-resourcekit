using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<ParameterResource>(Platform.ResourceKits.PublishMarker)]
sealed partial class PublishMarkerAppResource
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddParameter(Name, "enabled", secret: false);
}
