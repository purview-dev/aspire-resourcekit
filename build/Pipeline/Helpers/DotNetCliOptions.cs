using ModularPipelines.Options;

namespace Purview.Aspire.ResourceKit.Pipeline.Helpers;

public sealed record DotNetCliOptions : CommandLineToolOptions
{
	public static DotNetCliOptions Create(params string[] commandParts) =>
		new() { Tool = "dotnet", CommandParts = commandParts };
}
