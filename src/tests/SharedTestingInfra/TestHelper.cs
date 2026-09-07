namespace Purview.Aspire.ResourceKit;

public static class TestHelper
{
	public const string DefaultHostKitType = "TestingHostKit";

	public const string DefaultHostKitNamespace = "Testing.HostKitNamespace";

	public const string DefaultResourceKitType = "TestingResourceKit";

	public const string DefaultResourceKitNamespace = "Testing.ResourceKitNamespace";

	public static string GenerateAspireResource(TypeIdentity? typeIdentity = null)
	{
		var resourceIdentity =
			typeIdentity == null || typeIdentity == TypeIdentity.Empty
				? TypeLibrary.DefaultAspireResource
				: typeIdentity.Value;

		var writer = CodeWriter.CreateTestWriter();

		using (writer.BlockNamespaceScope(typeIdentity))
		{
			writer.Class(
				new(resourceIdentity, TypeDeclarationAccessibility.Public)
				{
					Interfaces = [TypeLibrary.Aspire.Hosting.ApplicationModel.IResource],
				},
				bodyWriter =>
					bodyWriter
						.Property(
							new("Name", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
							{
								ExpressionBody = $"\"{resourceIdentity.Name}\"",
							}
						)
						.Property(
							new(
								"Annotations",
								TypeLibrary.Aspire.Hosting.ApplicationModel.ResourceAnnotations,
								TypeDeclarationAccessibility.Public
							)
							{
								ExpressionBody = "[]",
							}
						)
			);
		}

		return writer.ToString();
	}

	public static string GenerateBuildResourceMethod(TypeIdentity? aspireResource = null) =>
		$"protected override IResourceBuilder<{aspireResource ?? TypeLibrary.DefaultAspireResource}> BuildResource({TypeLibrary.Aspire.Hosting.IDistributedApplicationBuilder} builder) => throw new global::System.NotImplementedException();";

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
		AttributeDeclarationOptions hostKitAttribute = new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
		{
			Arguments =
			[
				new AttributeArgumentOptions(generateOptions) { Name = "GenerateOptions", IsPropertyAssignment = true },
			],
		};
#pragma warning restore CA1308 // Normalize strings to uppercase

		writer
			.FileScopedNamespace(namespaceName)
			.Class(
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
		aspireResource ??= TypeLibrary.DefaultAspireResource;

		var writer = CodeWriter.CreateTestWriter();

		var baseType = baseClass is null
			? null
			: new TypeIdentity(baseClass, null).MakeGeneric(aspireResource.Value).AsTypeReference();
		AttributeDeclarationOptions resourceDefinitionAttribute = new(
			baseClass is null
				? TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute.MakeGeneric(aspireResource.Value)
				: TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute
		);

		writer
			.FileScopedNamespace(namespaceName)
			.Class(
				new(resourceKitName, TypeDeclarationAccessibility.Public)
				{
					BaseType = baseType,
					IsPartial = true,
					Attributes = [resourceDefinitionAttribute],
				},
				bodyWriter =>
					bodyWriter.Method(
						"BuildResource",
						TypeLibrary.IResourceBuilder.MakeGeneric(aspireResource.Value),
						TypeDeclarationAccessibility.Protected,
						methodWriter =>
							methodWriter with
							{
								IsOverride = true,
								Parameters =
								[
									new("builder", TypeLibrary.Aspire.Hosting.IDistributedApplicationBuilder),
								],
							},
						writer => writer.Throw("System.NotImplementedException")
					)
			);

		return writer.ToString();
	}
}
