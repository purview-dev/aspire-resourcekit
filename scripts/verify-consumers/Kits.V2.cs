using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dotnet;
using Purview.Aspire.ResourceKit;

// Aspire 17 Project V2, packaged-consumer proof: no base types and no generated members are
// hand-written, so this compiles only when the shipped analyzer understands
// [ResourceDefinition<DotnetProjectResource>] and the ProjectPath declared on it.
[HostKit]
partial class ConsumerHostKit;

[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
partial class ApiKit
{
	protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddDotnetProject(Name, "../Api/Api.csproj");
}
