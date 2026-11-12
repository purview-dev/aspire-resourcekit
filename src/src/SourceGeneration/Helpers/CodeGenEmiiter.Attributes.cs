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

	static SourceText ResourceGroupAttributes()
	{
		var writer = CreateWriter();

		writer
			.XmlSummary(
				"Marks a class as a named group of resource kits. Kits join it with InGroupAttribute{TGroup}.",
				"The generator derives the class from ResourceKitGroup{THostKit} and binds its options."
			)
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute)
				{
					IsSealed = false,
				},
				AttributeTargets.Class,
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the ResourceGroupAttribute class.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute),
							static ctor => ctor.Comment("Empty")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the ResourceGroupAttribute class.")
						.XmlParam(
							"name",
							"The group name, which also names its dashboard node (suffixed with -group if the name is taken)."
						)
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute)
							{
								Parameters = [new("name", TypeLibrary.System.String)],
							},
							static ctor => ctor.Assignment("Name", "name")
						);

					attributeBody
						.XmlSummary(
							"Gets or sets the group name, which also names its dashboard node (suffixed with -group if the name is taken).",
							"Defaults to the class name without a 'ResourceGroup' or 'Group' suffix."
						)
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
						.XmlSummary("Gets or sets the parent group, another class marked with ResourceGroupAttribute.")
						.Property(
							new(
								"Parent",
								TypeLibrary.System.Type.MakeNullable(attributeBody),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					attributeBody
						.XmlSummary("Gets or sets a value indicating whether the group is shown as a dashboard node.")
						.Property(
							new("ShowInDashboard", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								Initializer = "true",
								IsInitOnly = true,
							}
						);

					AddModeProperty(attributeBody);
				}
			);

		writer
			.XmlSummary(
				"Marks a class as a named group of resource kits, nested under the resource group TParent.",
				"Equivalent to ResourceGroupAttribute with Parent = typeof(TParent); do not set Parent as well."
			)
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceGroupAttribute)
				{
					BaseType = TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute,
					GenericTypes = [new("TParent") { Constraints = ["class"] }],
				},
				AttributeTargets.Class,
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the ResourceGroupAttribute class.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute),
							static ctor => ctor.Comment("Empty")
						);

					attributeBody
						.XmlSummary("Initializes a new instance of the ResourceGroupAttribute class.")
						.XmlParam(
							"name",
							"The group name, which also names its dashboard node (suffixed with -group if the name is taken)."
						)
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute)
							{
								Parameters = [new("name", TypeLibrary.System.String)],
								Initializer = "base(name)",
							},
							static ctor => ctor.Comment("Empty")
						);
				}
			);

		writer
			.XmlSummary("Adds a resource kit to a resource group.")
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.InGroupAttribute)
				{
					IsSealed = false,
					IsAbstract = true,
				},
				AttributeTargets.Class,
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the InGroupAttribute class.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.InGroupAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Protected,
							},
							static ctor => ctor.Comment("Empty")
						);

					attributeBody
						.XmlSummary(
							"Gets or sets a value indicating whether this is the kit's primary group, which decides where the kit is shown in the dashboard.",
							"Defaults to the first group declared."
						)
						.Property(
							new("Primary", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
							}
						);
				},
				allowMultiple: true
			);

		writer
			.XmlSummary("Adds a resource kit to the resource group TGroup.")
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.GenericInGroupAttribute)
				{
					BaseType = TypeLibrary.Purview.Aspire.ResourceKit.InGroupAttribute,
					GenericTypes = [new("TGroup") { Constraints = ["class"] }],
				},
				AttributeTargets.Class,
				static attributeBody =>
					attributeBody
						.XmlSummary("Initializes a new instance of the InGroupAttribute class.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.InGroupAttribute),
							static ctor => ctor.Comment("Empty")
						),
				allowMultiple: true
			);

		return writer;
	}

	static SourceText ResourceUrlAttributes()
	{
		var writer = CreateWriter();

		writer
			.XmlSummary(
				"Declares a named URL the dashboard shows for a resource kit or resource group.",
				"A relative URL is resolved against an endpoint of the resource; an absolute URL is shown as-is."
			)
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.ResourceUrlAttribute),
				AttributeTargets.Class,
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the ResourceUrlAttribute class.")
						.XmlParam("name", "The URL name, which is also its generated options property.")
						.XmlParam("url", "The absolute URL, or a path relative to the resource's endpoint.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.ResourceUrlAttribute)
							{
								Parameters =
								[
									new("name", TypeLibrary.System.String),
									new("url", TypeLibrary.System.String),
								],
							},
							static ctor => ctor.Assignment("Name", "name").Assignment("Url", "url")
						);

					attributeBody
						.XmlSummary("Gets the URL name, which is also its generated options property.")
						.Property(
							new("Name", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
							{
								HasSetter = false,
							}
						);

					attributeBody
						.XmlSummary("Gets the absolute URL, or a path relative to the resource's endpoint.")
						.Property(
							new("Url", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
							{
								HasSetter = false,
							}
						);

					AddDisplayTextProperty(attributeBody);

					attributeBody
						.XmlSummary(
							"Gets or sets the endpoint a relative URL is resolved against.",
							"Defaults to the https endpoint, then the http endpoint, then the first endpoint."
						)
						.Property(
							new(
								"Endpoint",
								TypeLibrary.System.String.MakeNullable(attributeBody),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					AddDisplayLocationProperty(attributeBody);
				},
				allowMultiple: true
			);

		writer
			.XmlSummary("Customises the URL the dashboard shows for one of a resource kit's endpoints.")
			.AttributeClass(
				new TypeDeclarationOptions(TypeLibrary.Purview.Aspire.ResourceKit.EndpointUrlAttribute),
				AttributeTargets.Class,
				static attributeBody =>
				{
					attributeBody
						.XmlSummary("Initializes a new instance of the EndpointUrlAttribute class.")
						.XmlParam("endpoint", "The endpoint name; it also names the generated options property.")
						.Constructor(
							new(TypeLibrary.Purview.Aspire.ResourceKit.EndpointUrlAttribute)
							{
								Parameters = [new("endpoint", TypeLibrary.System.String)],
							},
							static ctor => ctor.Assignment("Endpoint", "endpoint")
						);

					attributeBody
						.XmlSummary("Gets the endpoint name.")
						.Property(
							new("Endpoint", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
							{
								HasSetter = false,
							}
						);

					AddDisplayTextProperty(attributeBody);
					AddDisplayLocationProperty(attributeBody);
				},
				allowMultiple: true
			);

		return writer;
	}

	static void AddDisplayTextProperty(CodeWriter attributeBody) =>
		attributeBody
			.XmlSummary("Gets or sets the text the dashboard shows for the URL.")
			.Property(
				new(
					"DisplayText",
					TypeLibrary.System.String.MakeNullable(attributeBody),
					TypeDeclarationAccessibility.Public
				)
				{
					IsInitOnly = true,
				}
			);

	static void AddDisplayLocationProperty(CodeWriter attributeBody) =>
		attributeBody
			.XmlSummary("Gets or sets where the dashboard shows the URL.")
			.Property(
				new(
					"DisplayLocation",
					TypeLibrary.Aspire.Hosting.ApplicationModel.UrlDisplayLocation,
					TypeDeclarationAccessibility.Public
				)
				{
					Initializer = $"{TypeLibrary.Aspire.Hosting.ApplicationModel.UrlDisplayLocation}.SummaryAndDetails",
					IsInitOnly = true,
				}
			);

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
