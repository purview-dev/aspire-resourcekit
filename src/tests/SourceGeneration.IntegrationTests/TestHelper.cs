using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

static class TestHelper
{
	public const string DefaultHostKitType = "TestingHostKit";

	public const string DefaultHostKitNamespace = "Testing.HostKitNamespace";

	public const string DefaultResourceKitType = "TestingResourceKit";

	public const string DefaultResourceKitNamespace = "Testing.ResourceKitNamespace";

	public static TypeValueObject DefaultAspireResource = new(
		nameof(DefaultAspireResource),
		"Testing.AspireResourceNamespace"
	);

	public static string GenerateBuildResource(TypeValueObject? aspireResource = null) =>
		$"protected override IResourceBuilder<{aspireResource ?? DefaultAspireResource}> BuildResource({TypeLibrary.IDistributedApplicationBuilder} builder) => throw new global::System.NotImplementedException();";

	public static IEnumerable<string> GenerateSources(
		string hostKitName = DefaultHostKitType,
		string? hostKitNamespace = DefaultHostKitNamespace,
		string? hostKitBaseClass = null,
		bool generateOptions = true,
		string resourceKitName = DefaultResourceKitType,
		string? resourceKitNamespace = DefaultResourceKitNamespace,
		string? resourceKitBaseClass = null,
		TypeValueObject? aspireResource = null
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
		var hostKitAttribute = new AttributeDeclarationOptions(TypeLibrary.HostKitAttribute)
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
				new TypeDeclarationOptions(hostKitName)
				{
					BaseType = baseClass,
					Accessibility = TypeDeclarationAccessibility.Public,
					IsPartial = true,
					Attributes = [hostKitAttribute],
				},
				bodyWriter => bodyWriter.Comment("Empty")
			);

		return writer.ToString();
	}

	public static string GenerateResourceKit(
		string resourceKitName = DefaultResourceKitType,
		TypeValueObject? aspireResource = null,
		string? baseClass = null,
		string? namespaceName = DefaultHostKitNamespace
	)
	{
		aspireResource ??= DefaultAspireResource;

		var writer = CodeWriter.CreateTestWriter();

		var baseType = baseClass is null ? null : $"{baseClass}<{aspireResource}>";
		AttributeDeclarationOptions resourceDefinitionAttribute = new(
			baseClass is null
				? TypeLibrary.ResourceDefinitionAttribute.MakeGeneric(aspireResource)
				: TypeLibrary.ResourceDefinitionAttribute
		);

		writer
			.WriteFileScopedNamespace(namespaceName)
			.WriteClass(
				new(resourceKitName)
				{
					BaseType = baseType,
					Accessibility = TypeDeclarationAccessibility.Public,
					IsPartial = true,
					Attributes = [resourceDefinitionAttribute],
				},
				bodyWriter => bodyWriter.WriteLine(GenerateBuildResource(aspireResource))
			);

		return writer.ToString();
	}
}
