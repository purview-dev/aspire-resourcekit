using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PublishMarkerAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, ParameterResource>(hostKit, Platform.ResourceKits.PublishMarker)
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddParameter(Name, "enabled", secret: false);
}
