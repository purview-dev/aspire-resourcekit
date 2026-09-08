using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit;

public abstract class ResourceKitSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, ResourceKitSourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	// Generated attribute files: EmbeddedAttribute, HostKitAttribute, ResourceDefinitionAttribute.
	public static readonly int ExpectedGeneratedFileCount = 3;

	public static readonly int ExpectedFileCountPlusGen = ExpectedGeneratedFileCount + 1;
}
