using System.Reflection;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies the post-initialization attribute sources emitted by <see cref="HostKitGenerator"/>
/// and that a compilation containing only those attributes can be emitted and reflected over.
/// </summary>
public class GeneratedAttributesTests : IncrementalSourceGeneratorTestBase<HostKitGenerator>
{
	const string EmptySource =
		@"
namespace Testing
{
	public class Empty { }
}
";

	static SyntaxTree? GetGeneratedTree(GeneratorDriverRunResult result, string filePathSuffix) =>
		result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith(filePathSuffix, StringComparison.Ordinal));

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesHostKitAttributeSource(CancellationToken cancellationToken)
	{
		var (result, _) = await GenerateAsync(EmptySource, cancellationToken);

		var tree = GetGeneratedTree(result, "HostKitAttribute.g.cs");
		tree = await Assert.That(tree).IsNotNull();

		var syntaxTree = await tree.GetTextAsync(cancellationToken);
		var text = syntaxTree.ToString();

		await Assert.That(text).Contains("class HostKitAttribute");
		await Assert.That(text).Contains("string? Name");
		await Assert.That(text).Contains("AttributeTargets.Class");
		await Assert.That(text).Contains("AllowMultiple = false");
		await Assert.That(text).Contains("Inherited = false");
	}

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesResourceDefinitionAttributeSource(
		CancellationToken cancellationToken
	)
	{
		var (result, _) = await GenerateAsync(EmptySource, cancellationToken);

		var tree = GetGeneratedTree(result, "ResourceDefinitionAttribute.g.cs");
		tree = await Assert.That(tree).IsNotNull();

		var syntaxTree = await tree.GetTextAsync(cancellationToken);
		var text = syntaxTree.ToString();

		await Assert.That(text).Contains("class ResourceDefinitionAttribute");
		await Assert.That(text).Contains("class ResourceDefinitionAttribute<TResource>");
		await Assert.That(text).Contains("class ResourceDefinitionAttribute<TResource> : ResourceDefinitionAttribute");
		await Assert.That(text).Contains("string? Name");
		await Assert.That(text).Contains("string? PropertyName");
		await Assert.That(text).Contains("bool GenerateOptions");
	}

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesEmbeddedAttributeSource(CancellationToken cancellationToken)
	{
		var (result, _) = await GenerateAsync(EmptySource, cancellationToken);

		var tree = GetGeneratedTree(result, "EmbeddedAttribute.cs");
		tree = await Assert.That(tree).IsNotNull();

		var syntaxTree = await tree.GetTextAsync(cancellationToken);
		var text = syntaxTree.ToString();

		await Assert.That(text).Contains("class EmbeddedAttribute");
		await Assert.That(text).Contains("namespace Microsoft.CodeAnalysis");
	}

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesEmbeddedAttributeSourceAsPartialClass(
		CancellationToken cancellationToken
	)
	{
		var (result, _) = await GenerateAsync(EmptySource, cancellationToken);

		var tree = GetGeneratedTree(result, "EmbeddedAttribute.cs");
		tree = await Assert.That(tree).IsNotNull();

		var syntaxTree = await tree.GetTextAsync(cancellationToken);
		var text = syntaxTree.ToString();

		await Assert.That(text).Contains("partial class EmbeddedAttribute");
	}

	[Test]
	public async Task Generate_GivenEmptySource_AttributesAreEmittedIntoResourceKitNamespace(
		CancellationToken cancellationToken
	)
	{
		var (result, _) = await GenerateAsync(EmptySource, cancellationToken);

		var hostAppTree = GetGeneratedTree(result, "HostKitAttribute.g.cs");
		var appResourceTree = GetGeneratedTree(result, "ResourceDefinitionAttribute.g.cs");

		hostAppTree = await Assert.That(hostAppTree).IsNotNull();
		appResourceTree = await Assert.That(appResourceTree).IsNotNull();

		var hostApp = hostAppTree.ToString();
		var appResource = appResourceTree.ToString();

		await Assert.That(hostApp).Contains("namespace Purview.Aspire.ResourceKit");
		await Assert.That(appResource).Contains("namespace Purview.Aspire.ResourceKit");
	}

	[Test]
	public async Task Compile_GivenEmptySource_ProducesAssemblyWithGeneratedAttributes(
		CancellationToken cancellationToken
	)
	{
		var assembly = await CompileToAssemblyAsync(EmptySource, cancellationToken);

		await Assert.That(assembly.GetType(TypeHelpers.HostKitAttribute.SymbolFullName)).IsNotNull();
		await Assert.That(assembly.GetType(TypeHelpers.ResourceDefinitionAttribute.SymbolFullName)).IsNotNull();
		await Assert.That(assembly.GetType(TypeHelpers.GenericResourceDefinitionAttribute.SymbolFullName)).IsNotNull();
	}

	[Test]
	public async Task Compile_GivenEmptySource_HostKitAttributeHasExpectedMembers(CancellationToken cancellationToken)
	{
		var assembly = await CompileToAssemblyAsync(EmptySource, cancellationToken);
		var type = assembly.GetType(TypeHelpers.HostKitAttribute.SymbolFullName)!;

		var nameProp = type.GetProperty("Name");
		await Assert.That(nameProp).IsNotNull();
		await Assert.That(nameProp!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
	}

	[Test]
	public async Task Compile_GivenEmptySource_ResourceDefinitionAttributeHasExpectedMembers(
		CancellationToken cancellationToken
	)
	{
		var assembly = await CompileToAssemblyAsync(EmptySource, cancellationToken);
		var type = assembly.GetType(TypeHelpers.ResourceDefinitionAttribute.SymbolFullName)!;

		await Assert.That(type.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert.That(type.GetProperty("PropertyName")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert.That(type.GetProperty("GenerateOptions")!.PropertyType.FullName).IsEqualTo(typeof(bool).FullName);

		var genericType = assembly.GetType(TypeHelpers.GenericResourceDefinitionAttribute.SymbolFullName)!;
		await Assert.That(genericType.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert
			.That(genericType.GetProperty("PropertyName")!.PropertyType.FullName)
			.IsEqualTo(typeof(string).FullName);
		await Assert
			.That(genericType.GetProperty("GenerateOptions")!.PropertyType.FullName)
			.IsEqualTo(typeof(bool).FullName);
	}

	[Test]
	public async Task Compile_GivenEmptySource_AttributesHaveExpectedAttributeUsage(CancellationToken cancellationToken)
	{
		var assembly = await CompileToAssemblyAsync(EmptySource, cancellationToken);

		foreach (
			var fullName in new[]
			{
				TypeHelpers.HostKitAttribute.SymbolFullName,
				TypeHelpers.ResourceDefinitionAttribute.SymbolFullName,
				TypeHelpers.GenericResourceDefinitionAttribute.SymbolFullName,
			}
		)
		{
			var type = assembly.GetType(fullName)!;
			var usage = type.GetCustomAttribute<AttributeUsageAttribute>();

			await Assert.That(usage).IsNotNull();
			await Assert.That(usage!.ValidOn.HasFlag(AttributeTargets.Class)).IsTrue();
			await Assert.That(usage.AllowMultiple).IsFalse();
			await Assert.That(usage.Inherited).IsFalse();
		}
	}
}
