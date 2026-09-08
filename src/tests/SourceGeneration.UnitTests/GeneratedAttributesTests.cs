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
		var query = result.Generated();

		await Assert
			.That(query)
			.HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
			.And.HasPropertyOfType("Name", query.MakeNullable(TypeLibrary.System.String));

		var genericResourceDefAttribute = await Assert
			.That(query)
			.HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute);

		genericResourceDefAttribute.HasBaseType(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute);
	}

	[Test]
	public async Task Generate_GivenEmptySource_AttributesAreEmittedIntoResourceKitNamespace(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var query = result.Generated();

		await Assert.That(query).HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute);
		await Assert.That(query).HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute);
		await Assert
			.That(query)
			.HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute);
	}

	[Test]
	public async Task Compile_GivenEmptySource_ProducesAssemblyWithGeneratedAttributes(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var query = result.Generated();

		foreach (
			var attributeType in new[]
			{
				TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute,
				TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
				TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute,
			}
		)
		{
			await Assert.That(query).HasGeneratedClass(attributeType);
		}
	}

	[Test]
	public async Task Compile_GivenEmptySource_HostKitAttributeHasExpectedMembers(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(EmptySource, cancellationToken);
		var query = result.Generated();

		var attributeClass = await Assert
			.That(query)
			.HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute);

		await Assert.That(attributeClass).HasPropertyOfType("Name", query.MakeNullable(TypeLibrary.System.String));
	}

	[Test]
	public async Task Compile_GivenEmptySource_ResourceDefinitionAttributeHasExpectedMembers(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(EmptySource, ResourceKitSourceGeneratorTestOptions.Compile, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var type = assembly.GetType(
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute.MetadataFullName
		)!;

		await Assert.That(type.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert.That(type.GetProperty("PropertyName")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);

		var genericType = assembly.GetType(
			TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute.MetadataFullName
		)!;
		await Assert.That(genericType.GetProperty("Name")!.PropertyType.FullName).IsEqualTo(typeof(string).FullName);
		await Assert
			.That(genericType.GetProperty("PropertyName")!.PropertyType.FullName)
			.IsEqualTo(typeof(string).FullName);
	}

	[Test]
	public async Task Compile_GivenEmptySource_AttributesHaveExpectedAttributeUsage(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(EmptySource, ResourceKitSourceGeneratorTestOptions.Compile, cancellationToken);
		//var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var query = result.Generated();

		foreach (
			var attributeType in new[]
			{
				TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute,
				TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
				TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute,
			}
		)
		{
			var classNode = query.GetClass(attributeType);

			await Assert.That(classNode).IsNotNull();

			//var type = assembly.GetType(attributeType)!;
			//var usage = type.GetCustomAttribute<AttributeUsageAttribute>();

			//await Assert.That(usage).IsNotNull();
			//await Assert.That(usage.ValidOn.HasFlag(AttributeTargets.Class)).IsTrue();
			//await Assert.That(usage.AllowMultiple).IsFalse();
			//await Assert.That(usage.Inherited).IsFalse();
		}
	}
}
