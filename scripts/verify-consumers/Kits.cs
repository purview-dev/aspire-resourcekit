using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

// Deliberately carries no base types and no members beyond the two overrides the generator's own
// contracts demand: if the packaged analyzer did not run, this file does not compile.
[HostKit]
partial class ConsumerHostKit;

[ResourceDefinition<ParameterResource>("p")]
partial class ParameterKit
{
	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "value");
}

// Proves the packaged analyzer emits the typed Parent/child surface (issue #57) in a real
// PackageReference consumer, on both target frameworks.
[ResourceDefinition<ParameterResource>("c", Parent = typeof(ParameterKit))]
partial class ChildParameterKit
{
	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		_ = Parent.ResourceBuilder;
		return builder.AddParameter(Name, "child-value");
	}
}
