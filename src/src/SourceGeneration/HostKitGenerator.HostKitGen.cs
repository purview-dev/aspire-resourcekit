using System.Collections.Immutable;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildHostKit(
		HostKitGenerationModel hostKitInfo,
		KitGenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Info($"Generating {hostKitInfo.HostKitType.MetadataFullName}...");

		using var nsScope = context.CodeWriter.WriteBlockNamespaceScope(hostKitInfo.HostKitType.Namespace);

		var primaryConstructorParameters = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();
		primaryConstructorParameters.Add(
			new(
				"onBuilt",
				TypeLibrary.Action.MakeGeneric(hostKitInfo.HostKitType, TypeLibrary.IDistributedApplicationBuilder).MakeNullable()
			)
		);
		primaryConstructorParameters.Add(
			new("onConfigured", TypeLibrary.Action.MakeGeneric(hostKitInfo.HostKitType).MakeNullable())
		);

		if (hostKitInfo.ShouldGenerateOptions)
		{
			primaryConstructorParameters.Add(new("options", hostKitInfo.HostKitOptionsType) { IsNullable = true });
		}

		using (
			context
				.CodeWriter.XmlSummary("Represents the generated Host Kit and composes all discovered Resources Kits")
				.WriteClassScope(
					new TypeDeclarationOptions(hostKitInfo.HostKitType.TypeName)
					{
						IsPartial = true,
						BaseType = TypeLibrary.HostKitBase.MakeGeneric(hostKitInfo.HostKitType),
						PrimaryConstructorParameters = primaryConstructorParameters.ToImmutable(),
						ConstructorParametersOnSeparateLines = true,
					}
				)
		)
		{
			// Write the Options property if the host kit has options
			if (hostKitInfo.ShouldGenerateOptions)
			{
				context
					.CodeWriter.XmlSummary(
						$"Gets the Host Kit options <see cref=\"{hostKitInfo.HostKitOptionsType}\"/>."
					)
					.WriteProperty(
						new("Options", hostKitInfo.HostKitOptionsType, TypeDeclarationAccessibility.Public)
						{
							HasGetter = true,
							Initializer = "options",
						}
					);
			}

			// Write the Resource Kit properties
			if (hostKitInfo.HasResourceKits)
			{
				GenerateResourceKitProperties(context.CodeWriter, hostKitInfo, logger, cancellationToken);
			}
			else
			{
				context.CodeWriter.Comment(
					"Note: No app resources were discovered for this host app. The Resources property will be an empty list."
				);
			}

			// Write the Build method
			GenerateBuildMethod(hostKitInfo, context, logger, cancellationToken);

			// Write the Configure method
			GenerateConfigureMethod(hostKitInfo, context, logger, cancellationToken);

			if (hostKitInfo.ShouldGenerateOptions)
			{
				// Generate the options class.
				GenerateHostKitOptionsClass(hostKitInfo, context, logger, cancellationToken);
			}
		}
	}

	static void BuildResourceKitBase(
		HostKitGenerationModel hostKitInfo,
		KitGenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Resource Kit base class for host kit: {hostKitInfo.HostKitType.TypeName}");

		using (context.CodeWriter.WriteBlockNamespaceScope(hostKitInfo.ResourceKitBaseType.Namespace))
		{
			context
				.CodeWriter.XmlSummary(
					"Represents a typed base class for all generated Resource Kits for the Host Kit."
				)
				.WriteClass(
					new TypeDeclarationOptions(hostKitInfo.ResourceKitBaseType.TypeName)
					{
						IsPartial = true,
						IsAbstract = true,
						Accessibility = hostKitInfo.AccessibilityModifier,
						BaseType = TypeLibrary.ResourceKitBase.MakeGeneric(hostKitInfo.HostKitType, "TResource"),
						GenericTypes =
						[
							new GenericTypeParameterOptions("TResource")
							{
								Constraints = [.. new[] { $"class, {TypeLibrary.IResource}" }],
							},
						],
					},
					body =>
						body.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
							.WriteConstructor(
								new ConstructorDeclarationOptions(hostKitInfo.ResourceKitBaseType)
								{
									Accessibility = TypeDeclarationAccessibility.Protected,
									Parameters = [new("hostKit", hostKitInfo.HostKitType), new("name", PurviewTypeLibrary.System.String.MakeNullable())],
									Initializer = $"base(hostKit, name)",
								},
								body => body.Comment("Empty")
							)
				);
		}
	}

	static void GenerateResourceKitProperties(
		CodeWriter writer,
		HostKitGenerationModel hostKitInfo,
		CancellationToken cancellationToken
	)
	{
		logger?.Debug(
			$"Processing {hostKitInfo.ResourceKits.Count} resource kits for {hostKitInfo.HostKitType.MetadataFullName}...",
			1
		);

		foreach (var resourceKit in hostKitInfo.ResourceKits)
		{
			cancellationToken.ThrowIfCancellationRequested();

			logger?.Debug($"Generating properties for resource kit: {resourceKit.ResourceKitType.TypeName}", 2);

			writer
				.XmlSummary($"Gets the <see cref=\"{resourceKit.ResourceKitType}\" /> resource instance.")
				.Xml(
					"<exception cref=\"global::System.InvalidOperationException\">Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set.</exception>"
				)
				.Xml(
					"<exception cref=\"global::System.ArgumentNullException\">Thrown if the resource is set to null.</exception>"
				);

			writer.WriteProperty(
				new(resourceKit.PropertyName, resourceKit.ResourceKitType)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					HasGetter = true,
					HasSetter = true,
					SetterAccessibility = TypeDeclarationAccessibility.Private,
				},
				writeGetterBody =>
					writeGetterBody.WriteLine(
						$"return field ?? throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has not been initialized. Call Build first.\");"
					),
				writeSetterBody =>
				{
					writeSetterBody.WriteLine("global::System.ArgumentNullException.ThrowIfNull(value);").NewLine();

					using (writeSetterBody.OpenBlockScope("if (field is not null)"))
						writeSetterBody.WriteLine(
							$"throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has already been initialized.\");"
						);

					writeSetterBody.NewLine().WriteLine("field = value;");
				}
			);
		}
	}

	static void GenerateBuildMethod(
		HostKitGenerationModel hostKitInfo,
		KitGenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Build method for host kit: {hostKitInfo.HostKitType.TypeName}");

		context.CodeWriter.Xml("<inheritdoc />");
		using (
			context.CodeWriter.WriteMethodScope(
				new("Build", "void")
				{
					IsOverride = true,
					Accessibility = TypeDeclarationAccessibility.Public,
					Parameters = [new("builder", TypeLibrary.IDistributedApplicationBuilder)],
				}
			)
		)
		{
			context.CodeWriter.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);").NewLine();

			foreach (var resourceKit in hostKitInfo.ResourceKits)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.CodeWriter.Comment($"Creating {resourceKit.ResourceKitType.TypeName} Resource Kit.");
				var resourceName =
					resourceKit.ResourceName ?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);
				if (hostKitInfo.ShouldGenerateOptions)
				{
					context.CodeWriter.WriteInvocationLine(
						$"{resourceKit.PropertyName} = new",
						[$"this", $"Options.{resourceKit.PropertyName}"]
					);
				}
				else
				{
					context.CodeWriter.WriteInvocationLine(
						$"{resourceKit.PropertyName} = new",
						[$"this", GeneratedText.QuoteLiteral(resourceName)]
					);
				}

				context.CodeWriter.NewLine();
			}

			if (!hostKitInfo.HasResourceKits)
				context.CodeWriter.Comment("No app resources were discovered for this host app.");
			else
			{
				context.CodeWriter.Comment("Register the discovered app resources with the base class.");
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();
					context.CodeWriter.WriteInvocationLine("AddResource", [$"{resourceKit.PropertyName}"]);
				}
			}

			context
				.CodeWriter.NewLine()
				.Comment("Now the additional post-build func builder")
				.WriteInvocationLine("onBuilt?.Invoke", ["this", "builder"]);

			context
				.CodeWriter.NewLine()
				.Comment(
					"Now that we've populated all of the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.WriteInvocationLine("base.Build", ["builder"]);
		}
	}

	static void GenerateConfigureMethod(
		HostKitGenerationModel hostKit,
		KitGenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Configure method for host kit: {hostKit.HostKitType.TypeName}");

		context.CodeWriter.Xml("<inheritdoc />");
		using (
			context.CodeWriter.WriteMethodScope(
				new("Configure", "void") { Accessibility = TypeDeclarationAccessibility.Public, IsOverride = true }
			)
		)
		{
			context
				.CodeWriter.NewLine()
				.Comment("Call the base classes Configure method first...")
				.WriteInvocationLine("base.Configure", []);

			context
				.CodeWriter.NewLine()
				.Comment("Now the additional post-configure func builder")
				.WriteInvocationLine("onConfigured?.Invoke", ["this"]);
		}
	}

	static void GenerateHostKitOptionsClass(
		HostKitGenerationModel hostKitInfo,
		KitGenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Host Kit Options class: {hostKitInfo.HostKitOptionsType.TypeName}");

		context
			.CodeWriter.NewLine()
			.XmlSummary($"Typed settings for <see cref=\"{hostKitInfo.HostKitOptionsType}\" />.");
		using (
			context.CodeWriter.WriteClassScope(
				new(hostKitInfo.HostKitOptionsType.TypeName)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsSealed = true,
					IsPartial = true,
				}
			)
		)
		{
			context
				.CodeWriter.XmlSummary("Configuration section name for host kit options.")
				.WriteField(
					new("SectionName", "string")
					{
						IsConst = true,
						Accessibility = TypeDeclarationAccessibility.Public,
						Initializer = GeneratedText.QuoteLiteral(hostKitInfo.HostKitType.TypeName),
					}
				);

			if (hostKitInfo.HasResourceKits)
			{
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();

					context
						.CodeWriter.NewLine()
						.XmlSummary(
							$"Gets or sets options for <see cref=\"{resourceKit.ResourceKitType}\" />.",
							$"<see cref=\"{resourceKit.ResourceKitOptionsType}\" /> for specific configuration options."
						)
						.WriteProperty(
							new(resourceKit.PropertyName, resourceKit.ResourceKitOptionsType)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								HasGetter = true,
								HasSetter = true,
								Initializer = "new()",
							}
						);
				}
			}
		}
	}
}
