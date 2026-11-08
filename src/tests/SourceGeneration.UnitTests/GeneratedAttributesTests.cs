using System.Reflection;

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
		// Arrange
		// Nothing to arrange: the attribute sources come from post-initialization output, so an empty
		// compilation is enough to produce them.

		// Act
		using var result = await GenerateAsync(EmptySource, cancellationToken);

		// Assert
		await Assert.That(result.GetGeneratedTree("HostKitAttribute.g.cs")).IsNotNull();
		await Assert
			.That(result.Generated())
			.HasGeneratedClass(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute);
	}

	[Test]
	public async Task Generate_GivenEmptySource_GeneratesResourceDefinitionAttributeSource(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Nothing to arrange: an empty compilation is enough for the generator's attribute output.

		// Act
		using var result = await GenerateAsync(EmptySource, cancellationToken);

		// Assert
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
		// Arrange
		// Nothing to arrange: an empty compilation is enough for the generator's attribute output.

		// Act
		using var result = await GenerateAsync(EmptySource, cancellationToken);

		// Assert
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
		// Arrange
		// Nothing to arrange: the option requests a compiled assembly, which is the artefact under test.

		// Act
		using var result = await GenerateAsync(EmptySource, cancellationToken);

		// Assert
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
		// Arrange
		// Nothing to arrange: an empty compilation is enough for the generator's attribute output.

		// Act
		using var result = await GenerateAsync(EmptySource, cancellationToken);

		// Assert
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
		// Arrange
		// Nothing to arrange: the option requests a compiled assembly, so the attributes can be reflected over.

		// Act
		using var result = await GenerateAsync(
			EmptySource,
			ResourceKitSourceGeneratorTestOptions.Compile,
			cancellationToken
		);

		// Assert
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
		// Arrange
		var attributeTypes = new[]
		{
			TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute,
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
			TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute,
		};

		// Act
		using var result = await GenerateAsync(
			EmptySource,
			ResourceKitSourceGeneratorTestOptions.Compile,
			cancellationToken
		);

		// Assert
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		foreach (var attributeType in attributeTypes)
		{
			var type = assembly.GetType(attributeType.MetadataFullName);
			await Assert.That(type).IsNotNull();

			var usage = type!.GetCustomAttribute<AttributeUsageAttribute>();
			await Assert.That(usage).IsNotNull();
			await Assert.That(usage!.ValidOn.HasFlag(AttributeTargets.Class)).IsTrue();
			await Assert.That(usage.AllowMultiple).IsFalse();
			await Assert.That(usage.Inherited).IsFalse();
		}
	}
}
