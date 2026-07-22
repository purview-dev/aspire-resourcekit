using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

[AppResource(Name = "publish-marker")]
sealed partial class PublishMarkerAppResource : ExampleHostAppAppResourceBase<ParameterResource>
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
