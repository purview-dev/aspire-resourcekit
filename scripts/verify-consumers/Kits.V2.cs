using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dotnet;
using ConsumerV2;
using Purview.Aspire.ResourceKit;

// Aspire 17 Project V2, packaged-consumer proof: no base types and no generated members are
// hand-written, so this compiles only when the shipped analyzer understands
// [ResourceDefinition<DotnetProjectResource>] and the ProjectPath declared on it.
//
// The path comes from AppHostProjects, which the packaged buildTransitive targets generate from the
// ResourceKitProject item in ConsumerV2.csproj. SG0018 still resolves it, because it is a const.
[HostKit]
partial class ConsumerHostKit;

[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = AppHostProjects.Self.Path)]
partial class ApiKit
{
	protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddDotnetProject(Name, AppHostProjects.Self.Path);
}
