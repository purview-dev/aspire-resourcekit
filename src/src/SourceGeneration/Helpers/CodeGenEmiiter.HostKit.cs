using System.Collections.Immutable;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	static void EmitHostKit(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Info($"Generating {context.HostKit.HostKitType.MetadataFullName}...");

		using var nsScope = context.Writer.WriteBlockNamespaceScope(context.HostKit.HostKitType.Namespace);

		var primaryConstructorParameters = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();
		primaryConstructorParameters.Add(
			new(
				"onBuilt",
				TypeLibrary
					.Action.MakeGeneric(context.HostKit.HostKitType, TypeLibrary.IDistributedApplicationBuilder)
					.MakeNullable()
			)
		);
		primaryConstructorParameters.Add(
			new("onConfigured", TypeLibrary.Action.MakeGeneric(context.HostKit.HostKitType).MakeNullable())
		);

		if (context.HostKit.ShouldGenerateOptions)
		{
			primaryConstructorParameters.Add(new("options", context.HostKit.OptionsType.MakeNullable()));
		}

		using (
			context
				.Writer.XmlSummary("Represents the generated Host Kit and composes all discovered Resources Kits")
				.WriteClassScope(
					new(context.HostKit.HostKitType)
					{
						IsPartial = true,
						BaseType = TypeLibrary.HostKitBase.MakeGeneric(context.HostKit.HostKitType),
						PrimaryConstructorParameters = primaryConstructorParameters.ToImmutable(),
						ConstructorParametersOnSeparateLines = true,
					}
				)
		)
		{
			// Write the Options property if the host kit has options
			if (context.HostKit.ShouldGenerateOptions)
			{
				context
					.Writer.XmlSummary($"Gets the Host Kit options <see cref=\"{context.HostKit.OptionsType}\"/>.")
					.WriteProperty(
						new("Options", context.HostKit.OptionsType, TypeDeclarationAccessibility.Public)
						{
							HasGetter = true,
							Initializer = "options",
						}
					);
			}

			// Write the Resource Kit properties
			if (context.Model.HasResourceKits)
			{
				GenerateResourceKitProperties(context, cancellationToken);
			}
			else
			{
				context.Writer.Comment(
					"Note: No resources were discovered for this host kit. The Resources property will be an empty list."
				);
			}

			// Write the Build method
			GenerateBuildMethod(context, cancellationToken);

			// Write the Configure method
			GenerateConfigureMethod(context, cancellationToken);

			if (context.Model.HostKit.Value.ShouldGenerateOptions)
			{
				// Generate the options class.
				GenerateHostKitOptionsClass(context, cancellationToken);
			}
		}
	}

	static void EmitResourceKitBase(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug(
			$"Generating Resource Kit base class for host kit: {context.Model.HostKit.Value.HostKitType.Name}"
		);

		using (context.Writer.WriteBlockNamespaceScope(context.Model.HostKit.Value.ResourceKitBaseType.Namespace))
		{
			context
				.Writer.XmlSummary("Represents a typed base class for all generated Resource Kits for the Host Kit.")
				.WriteClass(
					new(context.Model.HostKit.Value.ResourceKitBaseType.Name, context.Model.HostKit.Value.Accessibility)
					{
						IsPartial = true,
						IsAbstract = true,
						BaseType = TypeLibrary.ResourceKitBase.MakeGeneric(
							context.Model.HostKit.Value.HostKitType,
							"TResource"
						),
						GenericTypes =
						[
							new("TResource") { Constraints = [.. new[] { $"class, {TypeLibrary.IResource}" }] },
						],
					},
					body =>
						body.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
							.WriteConstructor(
								new(
									context.Model.HostKit.Value.ResourceKitBaseType,
									TypeDeclarationAccessibility.Protected
								)
								{
									Parameters =
									[
										new("hostKit", context.Model.HostKit.Value.HostKitType),
										new("name", PurviewTypeLibrary.System.String.MakeNullable()),
									],
									Initializer = $"base(hostKit, name)",
								},
								body => body.Comment("Empty")
							)
				);
		}
	}

	static void GenerateResourceKitProperties(OutputContext context, CancellationToken cancellationToken)
	{
		context.Debug(
			$"Processing {context.ResourceKitCount} resource kits for {context.HostKit.HostKitType.MetadataFullName}...",
			1
		);

		foreach (
			var resourceKit in context
				.Model.ResourceKits.SelectMany(r => r.Value)
				.Where(m => m.IsSuccess)
				.Select(m => m.Value)
		)
		{
			cancellationToken.ThrowIfCancellationRequested();

			context.Debug($"Generating properties for resource kit: {resourceKit.ResourceKitType.Name}", 2);

			context
				.Writer.XmlSummary($"Gets the <see cref=\"{resourceKit.ResourceKitType}\" /> resource instance.")
				.XmlException(
					TypeLibrary.InvalidOperationException,
					"Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set."
				)
				.XmlException(TypeLibrary.ArgumentNullException, "Thrown if the resource is set to null.");

			context.Writer.WriteProperty(
				new(resourceKit.PropertyName, resourceKit.ResourceKitType, TypeDeclarationAccessibility.Public)
				{
					HasGetter = true,
					HasSetter = true,
					SetterAccessibility = TypeDeclarationAccessibility.Private,
				},
				writeGetterBody =>
					writeGetterBody.WriteLine(
						$"return field ?? throw new {TypeLibrary.InvalidOperationException}(\"The '{resourceKit.PropertyName}' resource has not been initialized. Call Build first.\");"
					),
				writeSetterBody =>
				{
					writeSetterBody
						.WriteLine(TypeLibrary.ArgumentNullException.StaticMember("ThrowIfNull(value);"))
						.NewLine();

					using (writeSetterBody.OpenBlockScope("if (field is not null)"))
						writeSetterBody.WriteThrow(
							TypeLibrary.InvalidOperationException,
							$"The '{resourceKit.PropertyName}' resource has already been initialized."
						);

					writeSetterBody.NewLine().WriteAssignment("field", "value");
				}
			);
		}
	}

	static void GenerateBuildMethod(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Build method for host kit: {context.HostKit.HostKitType.Name}");

		context.Writer.XmlInheritDoc();
		using (
			context.Writer.WriteMethodScope(
				new("Build", TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					Parameters = [new("builder", TypeLibrary.IDistributedApplicationBuilder)],
				}
			)
		)
		{
			context.Writer.WriteLine(TypeLibrary.ArgumentNullException.StaticMember("ThrowIfNull(builder);")).NewLine();

			foreach (var resourceKit in context.ResourceKits.SelectMany(r => r.Value))
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.Writer.Comment($"Creating {resourceKit.ResourceKitType.Name} Resource Kit.");
				if (context.HostKit.ShouldGenerateOptions)
				{
					context.Writer.WriteInvocationLine(
						$"{resourceKit.PropertyName} = new",
						[$"this", $"Options.{resourceKit.PropertyName}"]
					);
				}
				else
				{
					context.Writer.WriteInvocationLine(
						$"{resourceKit.PropertyName} = new",
						[$"this", GeneratedText.QuoteLiteral(resourceKit.ResourceName)]
					);
				}

				context.Writer.NewLine();
			}

			if (!context.Model.HasResourceKits)
				context.Writer.Comment("No resource definitions were discovered for this host app.");
			else
			{
				context.Writer.Comment("Register the discovered app resources with the base class.");
				foreach (var resourceKit in context.ResourceKits.SelectMany(r => r.Value))
				{
					cancellationToken.ThrowIfCancellationRequested();
					context.Writer.WriteInvocationLine("AddResource", [$"{resourceKit.PropertyName}"]);
				}
			}

			context
				.Writer.NewLine()
				.Comment("Now the additional post-build func builder")
				.WriteInvocationLine("onBuilt?.Invoke", ["this", "builder"]);

			context
				.Writer.NewLine()
				.Comment(
					"Now that we've populated all of the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.WriteInvocationLine("base.Build", ["builder"]);
		}
	}

	static void GenerateConfigureMethod(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Configure method for host kit: {context.HostKit.HostKitType.Name}");

		context.Writer.XmlInheritDoc();
		using (
			context.Writer.WriteMethodScope(new("Configure", TypeDeclarationAccessibility.Public) { IsOverride = true })
		)
		{
			context
				.Writer.NewLine()
				.Comment("Call the base classes Configure method first...")
				.WriteInvocationLine("base.Configure", []);

			context
				.Writer.NewLine()
				.Comment("Now the additional post-configure func builder")
				.WriteInvocationLine("onConfigured?.Invoke", ["this"]);
		}
	}

	static void GenerateHostKitOptionsClass(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Host Kit Options class: {context.HostKit.OptionsType.Name}");

		context.Writer.NewLine().XmlSummary($"Typed settings for ${CodeWriter.XmlSee(context.HostKit.OptionsType)}.");

		using (
			context.Writer.WriteClassScope(
				new(context.HostKit.OptionsType.Name, TypeDeclarationAccessibility.Public)
				{
					IsSealed = true,
					IsPartial = true,
				}
			)
		)
		{
			context
				.Writer.XmlSummary("Configuration section name for host kit options.")
				.WriteField(
					new("SectionName", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
					{
						IsConst = true,
						Initializer = GeneratedText.QuoteLiteral(context.HostKit.HostKitType.Name),
					}
				);

			if (context.HasResourceKits)
			{
				foreach (var resourceKit in context.ResourceKits.SelectMany(r => r.Value))
				{
					cancellationToken.ThrowIfCancellationRequested();

					context
						.Writer.NewLine()
						.XmlSummary(
							$"Gets or sets options for {CodeWriter.XmlSee(resourceKit.ResourceKitType)}.",
							$"{CodeWriter.XmlSee(resourceKit.OptionsType)} for specific configuration options."
						)
						.WriteProperty(
							new(resourceKit.PropertyName, resourceKit.OptionsType, TypeDeclarationAccessibility.Public)
							{
								HasGetter = true,
								Initializer = "new()",
							}
						);
				}
			}
		}
	}
}
