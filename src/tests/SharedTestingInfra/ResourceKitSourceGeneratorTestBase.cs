using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit;

public abstract class ResourceKitSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, ResourceKitSourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	// +1 is for the EmbeddedAttribute
	public static readonly int ExpectedGeneratedFileCount =
		TypeLibrary.Purview.Aspire.ResourceKit.GetTypes().Length + 1;

	public static readonly int ExpectedFileCountPlusGen = ExpectedGeneratedFileCount + 1;
}
