using System.Collections.Immutable;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	static void EmitHostKit(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Info($"Generating {context.HostKit.HostKitType.MetadataFullName}...");

		using var nsScope = context.Writer.BlockNamespaceScope(context.HostKit.HostKitType);

		var primaryConstructorParameters = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();
		primaryConstructorParameters.Add(
			new(
				"onBuilt",
				TypeLibrary
					.System.Action.MakeGeneric(
						context.HostKit.HostKitType,
						TypeLibrary.Aspire.Hosting.IDistributedApplicationBuilder
					)
					.MakeNullable(context.Writer)
			)
		);
		primaryConstructorParameters.Add(
			new(
				"onConfigured",
				TypeLibrary.System.Action.MakeGeneric(context.HostKit.HostKitType).MakeNullable(context.Writer)
			)
		);

		if (context.HostKit.ShouldGenerateOptions)
			primaryConstructorParameters.Add(new("options", context.HostKit.OptionsType.MakeNullable(context.Writer)));

		using (
			context
				.Writer.XmlSummary("Represents the generated Host Kit and composes all discovered Resources Kits")
				.ClassScope(
					new(context.HostKit.HostKitType, context.HostKit.Accessibility)
					{
						IsPartial = true,
						BaseType = TypeLibrary.Purview.Aspire.ResourceKit.HostKitBase.MakeGeneric(
							context.HostKit.HostKitType
						),
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
					.Property(
						new("Options", context.HostKit.OptionsType, TypeDeclarationAccessibility.Public)
						{
							HasGetter = true,
							IsInitOnly = true,
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

		using (context.Writer.BlockNamespaceScope(context.Model.HostKit.Value.ResourceKitBaseType.Namespace))
		{
			context
				.Writer.XmlSummary("Represents a typed base class for all generated Resource Kits for the Host Kit.")
				.Class(
					new(context.Model.HostKit.Value.ResourceKitBaseType.Name, context.Model.HostKit.Value.Accessibility)
					{
						IsPartial = true,
						IsAbstract = true,
						BaseType = TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase.MakeGeneric(
							context.Model.HostKit.Value.HostKitType,
							"TResource"
						),
						GenericTypes =
						[
							new("TResource")
							{
								Constraints =
								[
									.. new[] { $"class, {TypeLibrary.Aspire.Hosting.ApplicationModel.IResource}" },
								],
							},
						],
					},
					body =>
						body.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
							.Constructor(
								new(
									context.Model.HostKit.Value.ResourceKitBaseType,
									TypeDeclarationAccessibility.Protected
								)
								{
									Parameters =
									[
										new("hostKit", context.Model.HostKit.Value.HostKitType),
										new("name", TypeLibrary.System.String.MakeNullable(context.Writer)),
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
				.Model.ResourceKits.AsImmutableArray()
				.SelectMany(r => r.Items.AsImmutableArray())
				.Where(m => m.ShouldProcess)
				.Select(m => m.Value)
		)
		{
			cancellationToken.ThrowIfCancellationRequested();

			context.Debug($"Generating properties for resource kit: {resourceKit.ResourceKitType.Name}", 2);

			context
				.Writer.XmlSummary($"Gets the <see cref=\"{resourceKit.ResourceKitType}\" /> resource instance.")
				.XmlException(
					TypeLibrary.System.InvalidOperationException,
					"Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set."
				)
				.XmlException(TypeLibrary.System.ArgumentNullException, "Thrown if the resource is set to null.");

			context.Writer.Property(
				new(resourceKit.PropertyName, resourceKit.ResourceKitType, TypeDeclarationAccessibility.Public)
				{
					HasGetter = true,
					HasSetter = true,
					SetterAccessibility = TypeDeclarationAccessibility.Private,
				},
				writeGetterBody =>
				{
					var message =
						$"The '{resourceKit.PropertyName}' resource has not been initialized. Call Build first.";
					writeGetterBody.Return(
						$"field ?? throw new {TypeLibrary.System.InvalidOperationException}({message.StringLiteral()})"
					);
				},
				writeSetterBody =>
				{
					writeSetterBody
						.MethodCallOn(TypeLibrary.System.ArgumentNullException, "ThrowIfNull", ["value"])
						.NewLine();

					using (writeSetterBody.IfBlockScope("field is not null"))
						writeSetterBody.Throw(
							TypeLibrary.System.InvalidOperationException,
							$"The '{resourceKit.PropertyName}' resource has already been initialized."
						);

					writeSetterBody.NewLine().Assignment("field", "value");
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
			context.Writer.MethodScope(
				new("Build", TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					Parameters = [new("builder", TypeLibrary.Aspire.Hosting.IDistributedApplicationBuilder)],
				}
			)
		)
		{
			context.Writer.MethodCallOn(TypeLibrary.System.ArgumentNullException, "ThrowIfNull", ["builder"]).NewLine();

			foreach (
				var resourceKit in context.ResourceKits.AsImmutableArray().SelectMany(r => r.Items.AsImmutableArray())
			)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.Writer.Comment($"Creating {resourceKit.ResourceKitType.Name} Resource Kit.");
				if (context.HostKit.ShouldGenerateOptions)
				{
					context.Writer.Assignment(
						resourceKit.PropertyName,
						new ObjectCreationOptions(["this", $"Options.{resourceKit.PropertyName}"])
					);
				}
				else
				{
					context.Writer.Assignment(
						resourceKit.PropertyName,
						new ObjectCreationOptions(["this", resourceKit.ResourceName.StringLiteral()])
					);
				}

				context.Writer.NewLine();
			}

			if (!context.Model.HasResourceKits)
				context.Writer.Comment("No resource definitions were discovered for this host app.");
			else
			{
				context.Writer.Comment("Register the discovered app resources with the base class.");
				foreach (
					var resourceKit in context
						.ResourceKits.AsImmutableArray()
						.SelectMany(r => r.Items.AsImmutableArray())
				)
				{
					cancellationToken.ThrowIfCancellationRequested();
					context.Writer.MethodCall("AddResource", resourceKit.PropertyName);
				}
			}

			context
				.Writer.NewLine()
				.Comment("Now the additional post-build func builder")
				.MethodCallOn("onBuilt", "Invoke", ["this", "builder"], nullConditional: true);

			context
				.Writer.NewLine()
				.Comment(
					"Now that we've populated all of the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.MethodCallOn("base", "Build", ["builder"]);
		}
	}

	static void GenerateConfigureMethod(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Configure method for host kit: {context.HostKit.HostKitType.Name}");

		context.Writer.XmlInheritDoc();
		using (context.Writer.MethodScope(new("Configure", TypeDeclarationAccessibility.Public) { IsOverride = true }))
		{
			context
				.Writer.NewLine()
				.Comment("Call the base classes Configure method first...")
				.MethodCallOn("base", "Configure", [], nullConditional: false);

			context
				.Writer.NewLine()
				.Comment("Now the additional post-configure func builder")
				.MethodCallOn("onConfigured", "Invoke", ["this"], nullConditional: true);
		}
	}

	static void GenerateHostKitOptionsClass(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Host Kit Options class: {context.HostKit.OptionsType.Name}");

		context.Writer.NewLine().XmlSummary($"Typed settings for ${CodeWriter.XmlSee(context.HostKit.OptionsType)}.");

		using (
			context.Writer.ClassScope(
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
				.Field(
					new("SectionName", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
					{
						IsConst = true,
						Initializer = GeneratedText.QuoteLiteral(context.HostKit.HostKitType.Name),
					}
				);

			if (context.HasResourceKits)
			{
				foreach (
					var resourceKit in context
						.ResourceKits.AsImmutableArray()
						.SelectMany(r => r.Items.AsImmutableArray())
				)
				{
					cancellationToken.ThrowIfCancellationRequested();

					context
						.Writer.NewLine()
						.XmlSummary(
							$"Gets or sets options for {CodeWriter.XmlSee(resourceKit.ResourceKitType)}.",
							$"{CodeWriter.XmlSee(resourceKit.OptionsType)} for specific configuration options."
						)
						.Property(
							new(resourceKit.PropertyName, resourceKit.OptionsType, TypeDeclarationAccessibility.Public)
							{
								HasGetter = true,
								IsInitOnly = true,
								Initializer = "new()",
							}
						);
				}
			}
		}
	}
}
