using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

static class TestHelper
{
	public const string DefaultHostKitType = "TestingHostKit";

	public const string DefaultHostKitNamespace = "Testing.HostKitNamespace";

	public const string DefaultResourceKitType = "TestingResourceKit";

	public const string DefaultResourceKitNamespace = "Testing.ResourceKitNamespace";

	public static TypeIdentity DefaultAspireResource = TypeIdentity.Create<DefaultAspireResource>();

	public static string GenerateAspireResource(TypeIdentity? typeIdentity = null)
	{
		var resourceIdentity =
			typeIdentity == null || typeIdentity == TypeIdentity.Empty ? DefaultAspireResource : typeIdentity.Value;

		var writer = CodeWriter.CreateTestWriter();

		using (writer.WriteBlockNamespaceScope(typeIdentity))
		{
			writer.WriteClass(
				new(resourceIdentity, TypeDeclarationAccessibility.Public) { Interfaces = [TypeLibrary.IResource] },
				bodyWriter =>
					bodyWriter
						.WriteProperty(
							new("Name", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
							{
								ExpressionBody = $"\"{resourceIdentity.Name}\"",
							}
						)
						.WriteProperty(
							new("Annotations", TypeLibrary.ResourceAnnotations, TypeDeclarationAccessibility.Public)
							{
								ExpressionBody = "[]",
							}
						)
			);
		}

		return writer.ToString();
	}

	public static string GenerateBuildResourceMethod(TypeIdentity? aspireResource = null) =>
		$"protected override IResourceBuilder<{aspireResource ?? DefaultAspireResource}> BuildResource({TypeLibrary.IDistributedApplicationBuilder} builder) => throw new global::System.NotImplementedException();";

	public static IEnumerable<string> GenerateSources(
		string hostKitName = DefaultHostKitType,
		string? hostKitNamespace = DefaultHostKitNamespace,
		string? hostKitBaseClass = null,
		bool generateOptions = true,
		string resourceKitName = DefaultResourceKitType,
		string? resourceKitNamespace = DefaultResourceKitNamespace,
		string? resourceKitBaseClass = null,
		TypeIdentity? aspireResource = null
	)
	{
		yield return GenerateHostKit(
			hostKitName,
			baseClass: hostKitBaseClass,
			namespaceName: hostKitNamespace,
			generateOptions: generateOptions
		);
		yield return GenerateResourceKit(
			resourceKitName,
			aspireResource: aspireResource,
			baseClass: resourceKitBaseClass,
			namespaceName: resourceKitNamespace
		);
	}

	public static string GenerateHostKit(
		string hostKitName = DefaultHostKitType,
		string? baseClass = null,
		string? namespaceName = DefaultHostKitNamespace,
		bool generateOptions = true
	)
	{
		var writer = CodeWriter.CreateTestWriter();

#pragma warning disable CA1308 // Normalize strings to uppercase
		AttributeDeclarationOptions hostKitAttribute = new(TypeLibrary.HostKitAttribute)
		{
			Arguments =
			[
				new AttributeArgumentOptions(generateOptions) { Name = "GenerateOptions", IsPropertyAssignment = true },
			],
		};
#pragma warning restore CA1308 // Normalize strings to uppercase

		writer
			.WriteFileScopedNamespace(namespaceName)
			.WriteClass(
				new(hostKitName, TypeDeclarationAccessibility.Public)
				{
					BaseType = baseClass is null ? null : new TypeIdentity(baseClass, null).AsTypeReference(),
					IsPartial = true,
					Attributes = [hostKitAttribute],
				},
				bodyWriter => bodyWriter.Comment("Empty")
			);

		return writer.ToString();
	}

	public static string GenerateResourceKit(
		string resourceKitName = DefaultResourceKitType,
		TypeIdentity? aspireResource = null,
		string? baseClass = null,
		string? namespaceName = DefaultHostKitNamespace
	)
	{
		aspireResource ??= DefaultAspireResource;

		var writer = CodeWriter.CreateTestWriter();

		var baseType = baseClass is null
			? null
			: new TypeIdentity(baseClass, null).MakeGeneric(aspireResource.Value).AsTypeReference();
		AttributeDeclarationOptions resourceDefinitionAttribute = new(
			baseClass is null
				? TypeLibrary.ResourceDefinitionAttribute.MakeGeneric(aspireResource.Value)
				: TypeLibrary.ResourceDefinitionAttribute
		);

		writer
			.WriteFileScopedNamespace(namespaceName)
			.WriteClass(
				new(resourceKitName, TypeDeclarationAccessibility.Public)
				{
					BaseType = baseType,
					IsPartial = true,
					Attributes = [resourceDefinitionAttribute],
				},
				bodyWriter => bodyWriter.WriteLine(GenerateBuildResourceMethod(aspireResource))
			);

		return writer.ToString();
	}
}
