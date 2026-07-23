using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PublishMarkerAppResource() : HostResourceBase<ExampleHostApp, ParameterResource>("publish-maker")
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
