namespace Purview.Aspire.ResourceKit;

public static class TestHelper
{
	public static string GenerateAspireResource(TypeIdentity? typeIdentity = null)
	{
		var resourceIdentity =
			typeIdentity == null || typeIdentity == TypeIdentity.Empty
				? TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource
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
							new("Name", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
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
		$"protected override IResourceBuilder<{aspireResource ?? TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource}> BuildResource({TypeLibrary.Aspire.Hosting.IDistributedApplicationBuilder} builder) => throw new global::System.NotImplementedException();";

	public static IEnumerable<string> GenerateSources(
		TypeIdentity? hostKit = null,
		TypeIdentity? hostKitBase = null,
		bool generateOptions = true,
		TypeIdentity? resourceKit = null,
		TypeIdentity? resourceKitBase = null,
		TypeIdentity? aspireResource = null
	)
	{
		yield return GenerateHostKit(hostKit, hostKitBase: hostKitBase, generateOptions: generateOptions);
		yield return GenerateResourceKit(resourceKit, aspireResource: aspireResource, resourceKitBase: resourceKitBase);
	}

	public static string GenerateHostKit(
		TypeIdentity? hostKit = null,
		TypeIdentity? hostKitBase = null,
		bool generateOptions = true
	)
	{
		hostKit ??= TestingTypeLibrary.Testing.HostKitNamespace.DefaultHostKitType;

		var writer = CodeWriter.CreateTestWriter();

		writer
			.FileScopedNamespace(hostKit)
			.Class(
				new(hostKit, TypeDeclarationAccessibility.Public)
				{
					BaseType = hostKitBase,
					IsPartial = true,
					Attributes =
					[
						new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
						{
							Arguments = [new(generateOptions, "GenerateOptions", true)],
						},
					],
				}
			);

		return writer.ToString();
	}

	public static string GenerateResourceKit(
		TypeIdentity? resourceKit = null,
		TypeIdentity? aspireResource = null,
		TypeIdentity? resourceKitBase = null
	)
	{
		resourceKit ??= TestingTypeLibrary.Testing.ResourceKitNamespace.DefaultResourceKitType;
		aspireResource ??= TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource;

		var writer = CodeWriter.CreateTestWriter();
		AttributeDeclarationOptions resourceDefinitionAttribute = new(
			resourceKitBase is null
				? TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute.MakeGeneric(aspireResource.Value)
				: TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute
		);

		writer
			.FileScopedNamespace(resourceKit)
			.Class(
				new(resourceKit, TypeDeclarationAccessibility.Public)
				{
					BaseType = resourceKitBase,
					IsPartial = true,
					Attributes = [resourceDefinitionAttribute],
				},
				bodyWriter =>
					bodyWriter.Method(
						"BuildResource",
						TestingTypeLibrary.Aspire.Hosting.ApplicationModel.IResourceBuilder.MakeGeneric(
							aspireResource.Value
						),
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
						writer => writer.Throw(TypeIdentity.Create<NotImplementedException>())
					)
			);

		return writer.ToString();
	}
}
