using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

sealed class PublishMarkerAppResource : HostAppResource<HostApp, ParameterResource>
{
	public override string Name { get; } = "publish-marker";

	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<ParameterResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
