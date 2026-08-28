using System.Reflection;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies the post-initialization attribute sources emitted by <see cref="HostKitGenerator"/>
/// and that a compilation containing only those attributes can be emitted and reflected over.
/// </summary>
public sealed class GeneratedAttributesTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string EmptySource =
		@"
namespace Testing
{
	public class Empty { }
}
";

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesHostKitAttributeSource(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);

		var tree = result.GetGeneratedTree("HostKitAttribute.g.cs");
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
		var result = await GenerateAsync(EmptySource, cancellationToken);

		var tree = result.GetGeneratedTree("ResourceDefinitionAttribute.g.cs");
		tree = await Assert.That(tree).IsNotNull();

		var syntaxTree = await tree.GetTextAsync(cancellationToken);
		var text = syntaxTree.ToString();

		await Assert.That(text).Contains("class ResourceDefinitionAttribute");
		await Assert.That(text).Contains("class ResourceDefinitionAttribute<TResource>");
		await Assert.That(text).Contains("class ResourceDefinitionAttribute<TResource> : ResourceDefinitionAttribute");
		await Assert.That(text).Contains("string? Name");
		await Assert.That(text).Contains("string? PropertyName");
	}

	[Test]
	public async Task Generate_GivenEmptySource_AttributesAreEmittedIntoResourceKitNamespace(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);

		var hostAppTree = result.GetGeneratedTree("HostKitAttribute.g.cs");
		var appResourceTree = result.GetGeneratedTree("ResourceDefinitionAttribute.g.cs");

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
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		await Assert.That(assembly.GetType(TypeLibrary.HostKitAttribute.MetadataFullName)).IsNotNull();
		await Assert.That(assembly.GetType(TypeLibrary.ResourceDefinitionAttribute.MetadataFullName)).IsNotNull();
		await Assert
			.That(assembly.GetType(TypeLibrary.GenericResourceDefinitionAttribute.MetadataFullName))
			.IsNotNull();
	}

	[Test]
	public async Task Compile_GivenEmptySource_HostKitAttributeHasExpectedMembers(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var type = assembly.GetType(TypeLibrary.HostKitAttribute.MetadataFullName)!;

		var nameProp = type.GetProperty("Name");
		await Assert.That(nameProp).IsNotNull();
		await Assert.That(nameProp!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
	}

	[Test]
	public async Task Compile_GivenEmptySource_ResourceDefinitionAttributeHasExpectedMembers(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var type = assembly.GetType(TypeLibrary.ResourceDefinitionAttribute.MetadataFullName)!;

		await Assert.That(type.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert.That(type.GetProperty("PropertyName")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);

		var genericType = assembly.GetType(TypeLibrary.GenericResourceDefinitionAttribute.MetadataFullName)!;
		await Assert.That(genericType.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert
			.That(genericType.GetProperty("PropertyName")!.PropertyType.FullName)
			.IsEqualTo(typeof(string).FullName);
	}

	[Test]
	public async Task Compile_GivenEmptySource_AttributesHaveExpectedAttributeUsage(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		foreach (
			var fullName in new[]
			{
				TypeLibrary.HostKitAttribute.MetadataFullName,
				TypeLibrary.ResourceDefinitionAttribute.MetadataFullName,
				TypeLibrary.GenericResourceDefinitionAttribute.MetadataFullName,
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
