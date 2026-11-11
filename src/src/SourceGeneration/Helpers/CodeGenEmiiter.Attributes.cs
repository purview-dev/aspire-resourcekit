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
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.Constructor(new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute), static ctor =>
							ctor.Comment("Empty")
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
							static ctor =>
								ctor.Assignment("Name", "name").Assignment("GenerateOptions", "generateOptions")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("generateOptions", "Whether the Host App generates IOptions support.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters = [new("generateOptions", TypeLibrary.System.Boolean)],
							},
							static ctor => ctor.Assignment("GenerateOptions", "generateOptions")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("mode", "The execution mode in which the host kit participates.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters = [new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode)],
							},
							static ctor => ctor.Assignment("Mode", "mode")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("generateOptions", "Whether the Host App generates IOptions support.")
						.XmlParam("mode", "The execution mode in which the host kit participates.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters =
								[
									new("generateOptions", TypeLibrary.System.Boolean),
									new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
								],
							},
							static ctor =>
								ctor.Assignment("GenerateOptions", "generateOptions").Assignment("Mode", "mode")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the HostKitAttribute class.")
						.XmlParam("name", "Optional logical host app name used for generated type naming.")
						.XmlParam("generateOptions", "Whether the Host App generates IOptions support.")
						.XmlParam("mode", "The execution mode in which the host kit participates.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute)
							{
								Parameters =
								[
									new("name", TypeLibrary.System.String),
									new("generateOptions", TypeLibrary.System.Boolean),
									new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
								],
							},
							static ctor =>
								ctor.Assignment("Name", "name")
									.Assignment("GenerateOptions", "generateOptions")
									.Assignment("Mode", "mode")
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

					AddModeProperty(attributeBody);
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
			static attributeBody =>
			{
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.Constructor(new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute), static ctor =>
						ctor.Comment("Empty")
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
						static ctor => ctor.Assignment("Name", "name").Assignment("PropertyName", "propertyName")
					);
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters = [new("name", TypeLibrary.System.String)],
						},
						static ctor => ctor.Assignment("Name", "name")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters = [new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode)],
						},
						static ctor => ctor.Assignment("Mode", "mode")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
							],
						},
						static ctor => ctor.Assignment("Name", "name").Assignment("Mode", "mode")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("propertyName", "Generated host-app property name for this resource.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("propertyName", TypeLibrary.System.String),
								new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
							],
						},
						static ctor =>
							ctor.Assignment("Name", "name")
								.Assignment("PropertyName", "propertyName")
								.Assignment("Mode", "mode")
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

				AddModeProperty(attributeBody);
			}
		);

		writer.AttributeClass(
			new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute)
			{
				BaseType = TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
				GenericTypes = [new("TResource") { Constraints = ["class"] }],
			},
			AttributeTargets.Class,
			static attributeBody =>
			{
				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.Constructor(new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute), static ctor =>
						ctor.Comment("Empty")
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
						static ctor => ctor.Comment("Empty")
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
						static ctor => ctor.Comment("Empty")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters = [new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode)],
							Initializer = "base(mode)",
						},
						static ctor => ctor.Comment("Empty")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
							],
							Initializer = "base(name, mode)",
						},
						static ctor => ctor.Comment("Empty")
					);

				attributeBody
					.XmlSummary("Initializes a new instance of the ResourceDefinitionAttribute class.")
					.XmlParam("name", "Logical resource name used in Aspire resource registration.")
					.XmlParam("propertyName", "Generated host-app property name for this resource.")
					.XmlParam("mode", "The execution mode in which this resource participates.")
					.Constructor(
						new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("propertyName", TypeLibrary.System.String),
								new("mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode),
							],
							Initializer = "base(name, propertyName, mode)",
						},
						static ctor => ctor.Comment("Empty")
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

	static void AddModeProperty(CodeWriter attributeBody)
	{
		attributeBody
			.XmlSummary(
				"Gets or sets the execution mode in which the host kit or resource kit participates.",
				$"Defaults to <see cref=\"{TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode}.All\" />."
			)
			.Property(
				new("Mode", TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode, TypeDeclarationAccessibility.Public)
				{
					Initializer = $"{TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode}.All",
					IsInitOnly = true,
				}
			);
	}
}
