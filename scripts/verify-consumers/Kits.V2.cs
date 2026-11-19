using Aspire.Hosting.Dotnet;
using ConsumerV2;
using Purview.Aspire.ResourceKit;

// Aspire 17 Project V2, packaged-consumer proof: no base types and no generated members are
// hand-written, so this compiles only when the shipped analyzer understands
// [ResourceDefinition<DotnetProjectResource>] and the ProjectPath declared on it.
//
// The path comes from AppHostProjects, which the packaged buildTransitive targets generate from the
// ResourceKitProject item in ConsumerV2.csproj. BuildResource is not written either: ApiKit compiles
// only when the shipped generator emits AddDotnetProject(Name, path) from ProjectPath.
[HostKit]
partial class ConsumerHostKit;

[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = AppHostProjects.Self.Path)]
partial class ApiKit;
