using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

public record class DriverRunResult(
	GeneratorDriverRunResult Result,
	Compilation OutputCompilation,
	Assembly? Assembly,
	IEnumerable<SyntaxTree> SyntaxTrees,
	IEnumerable<SyntaxTree> NonAttributeSyntaxTrees
)
{
	public string GetSource() => NonAttributeSyntaxTrees.First().GetText().ToString();

	public async Task<string> GetSourceAsync(CancellationToken cancellationToken) =>
		(await NonAttributeSyntaxTrees.First().GetTextAsync(cancellationToken)).ToString();

	public SyntaxTree? GetGeneratedTree(string filePathSuffix) =>
		SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith(filePathSuffix, StringComparison.Ordinal));
}
