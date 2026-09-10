using Microsoft.CodeAnalysis.Text;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	static SourceText HostKitAttribute()
	{
		var writer = CreateWriter();

		return writer
			.XmlSummary("Marks the single host application type used by ResourceKit source generation.")
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute),
				AttributeTargets.Class,
				attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute),
							ctor => ctor.Comment("Empty")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("name", "Optional logical host app name used for generated type naming.")
						.XmlParam("generateOptions", "Whether the Host App generates IOptions support.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters =
								[
									new("name", TypeLibrary.System.String),
									new("generateOptions", TypeLibrary.System.Boolean),
								],
							},
							ctor => ctor.Assignment("Name", "name").Assignment("GenerateOptions", "generateOptions")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("generateOptions", "Whether the Host App generates IOptions support.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters = [new("generateOptions", TypeLibrary.System.Boolean)],
							},
							ctor => ctor.Assignment("GenerateOptions", "generateOptions")
						);

					attributeBody
						.XmlSummary("Gets or sets an optional logical host app name used in generated type names.")
						.Property(
							new(
								"Name",
								TypeLibrary.System.String.MakeNullable(attributeBody),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					attributeBody
						.XmlSummary(
							"Gets or sets an optional logical host app extension method name used in generated type names."
						)
						.Property(
							new(
								"ExtensionMethodName",
								TypeLibrary.System.String.MakeNullable(attributeBody),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					attributeBody
						.XmlSummary(
							"Gets or sets a value indicating whether host app options types should be generated."
						)
						.Property(
							new("GenerateOptions", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								Initializer = "true",
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText ResourceKitDefinitionAttribute()
	{
		var writer = CreateWriter();

		writer.AttributeClass(
			new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
			{
				IsSealed = false,
			},
			AttributeTargets.Class,
			attributeBody =>
			{
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute),
						ctor => ctor.Comment("Empty")
					);
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("propertyName", "Generated host-app property name for this resource.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("propertyName", TypeLibrary.System.String),
							],
						},
						ctor => ctor.Assignment("Name", "name").Assignment("PropertyName", "propertyName")
					);
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters = [new("name", TypeLibrary.System.String)],
						},
						ctor => ctor.Assignment("Name", "name")
					);

				attributeBody
					.XmlComment("Gets or sets the logical resource name used in Aspire resource registration.")
					.Property(
						new(
							"Name",
							TypeLibrary.System.String.MakeNullable(attributeBody),
							TypeDeclarationAccessibility.Public
						)
						{
							IsInitOnly = true,
						}
					);

				attributeBody
					.XmlSummary("Gets or sets the generated host-app property name for this resource.")
					.Property(
						new(
							"PropertyName",
							TypeLibrary.System.String.MakeNullable(attributeBody),
							TypeDeclarationAccessibility.Public
						)
						{
							IsInitOnly = true,
						}
					);
			}
		);

		writer.AttributeClass(
			new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute)
			{
				BaseType = TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
				GenericTypes = [new("TResource") { Constraints = ["class"] }],
			},
			AttributeTargets.Class,
			attributeBody =>
			{
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute),
						ctor => ctor.Comment("Empty")
					);
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("propertyName", "Generated host-app property name for this resource.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("propertyName", TypeLibrary.System.String),
							],
							Initializer = "base(name, propertyName)",
						},
						ctor => ctor.Comment("Empty")
					);
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters = [new("name", TypeLibrary.System.String)],
							Initializer = "base(name)",
						},
						ctor => ctor.Comment("Empty")
					);
			}
		);

		return writer;
	}

	static CodeWriter CreateWriter()
	{
		CodeWriter writer = new(GenerationSettings.Create<HostKitGenerator>());

		return writer.AutoGeneratedHeader().FileScopedNamespace(TypeLibrary.Purview.Aspire.ResourceKit.Namespace);
	}
}
