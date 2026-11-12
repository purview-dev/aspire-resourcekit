using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	static void EmitResourceKits(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Emitting resource kits for host kit: {context.HostKit.HostKitType}");

		foreach (var resourceKitGroup in context.ResourceKits)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (resourceKitGroup.Items.IsEmpty)
			{
				context.Info(
					"No resource kits found for group: {0}",
					resourceKitGroup.Namespace ?? "<global-namespace>"
				);
				continue;
			}

			var resourceKitForNS = resourceKitGroup.Items.AsImmutableArray().FirstOrDefault();

			using var resourceNs = context.Writer.BlockNamespaceScope(resourceKitForNS.ResourceKitType);
			context.Debug(
				$"Processing resource kit group: {resourceKitForNS.ResourceKitType.Namespace ?? "<global-namespace>"}",
				1
			);

			foreach (var resourceKit in resourceKitGroup.Items)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.Debug($"Processing resource kit: {resourceKit.ResourceKitType.Name}", 2);

				var baseClass = resourceKit.HasExplicitBaseType
					? default
					: context.HostKit.ResourceKitBaseType.MakeGeneric(resourceKit.AspireResourceType);

				// Generate the resource kit class
				using (
					context
						.Writer.XmlSummary(
							$"Represents a resource kit for {XmlSee(resourceKit.AspireResourceType)} />."
						)
						.ClassScope(
							new(resourceKit.ResourceKitType, resourceKit.Accessibility)
							{
								IsPartial = true,
								BaseType = baseClass,
							}
						)
				)
				{
					var parent = context.ParentOf(resourceKit);

					// Write the constructor. A child receives its parent instance directly; the relationship is
					// fixed here and never changes after construction.
					List<ParameterDeclarationOptions> constructorParameters =
					[
						new("hostKit", context.HostKit.HostKitType),
					];

					if (parent is { } parentModel)
						constructorParameters.Add(new("parent", parentModel.ResourceKitType));

					constructorParameters.Add(
						context.HostKit.ShouldGenerateOptions
							? new("options", resourceKit.OptionsType)
							: new("name", TypeLibrary.System.String.MakeNullable(context.Writer))
					);

					context
						.Writer.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
						.Constructor(
							new(resourceKit.ResourceKitType, TypeDeclarationAccessibility.Public)
							{
								Parameters = [.. constructorParameters],
								Initializer = context.HostKit.ShouldGenerateOptions
									? "base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
									: "base(hostKit, name)",
							},
							body =>
							{
								if (parent is not null)
								{
									body.Assignment(
											"Parent",
											$"parent ?? throw new {TypeLibrary.System.ArgumentNullException}(nameof(parent))"
										)
										.Assignment("ParentKit", "Parent");
								}

								if (context.HostKit.ShouldGenerateOptions)
									body.Assignment("Options", "options").Assignment("IsEnabled", "options.IsEnabled");
							}
						);

					if (parent is { } parentKit)
					{
						context
							.Writer.XmlSummary(
								$"Gets the <see cref=\"{parentKit.ResourceKitType}\" /> resource kit that owns this kit."
							)
							.Property(
								new(
									"Parent",
									parentKit.ResourceKitType,
									LeastAccessible(resourceKit.Accessibility, parentKit.Accessibility)
								)
							);
					}

					// Write the Options property if the host kit has options
					if (context.HostKit.ShouldGenerateOptions)
					{
						context
							.Writer.XmlSummary("Gets the Resource Kit options.")
							.Property("Options", resourceKit.OptionsType, TypeDeclarationAccessibility.Public);
					}

					if (context.HostKit.ShouldGenerateOptions)
					{
						GenerateResourceKitOptionsClass(context, resourceKit, cancellationToken);
					}

					// Surface the execution mode from the bound options (or the attribute) so the base class gates Build.
					ModeOverride(
						context.Writer,
						context.HostKit.ShouldGenerateOptions ? "Options.Mode" : ModeExpression(resourceKit.Mode)
					);

					// Surface the declared dashboard URLs, as bound from options when the host kit has them.
					if (!resourceKit.Urls.IsEmpty)
					{
						context
							.Writer.NewLine()
							.XmlInheritDoc()
							.Property(
								new(
									"Urls",
									TypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(
										TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitUrl
									),
									TypeDeclarationAccessibility.Protected
								)
								{
									IsOverride = true,
									ExpressionBody = UrlsExpression(
										resourceKit.Urls,
										context.HostKit.ShouldGenerateOptions
									),
								}
							);
					}

					GenerateChildResourceKitProperties(context, resourceKit, cancellationToken);
				}
			}

			resourceNs.Dispose();
		}
	}

	// The host kit constructs children, so the setter is assembly-internal rather than private. The generated
	// host and every generated kit live in the consuming compilation.
	static void GenerateChildResourceKitProperties(
		OutputContext context,
		ResourceKitModel resourceKit,
		CancellationToken cancellationToken
	)
	{
		foreach (var child in context.ChildrenOf(resourceKit))
		{
			cancellationToken.ThrowIfCancellationRequested();

			context
				.Writer.XmlSummary($"Gets the <see cref=\"{child.ResourceKitType}\" /> child resource instance.")
				.XmlException(
					TypeLibrary.System.InvalidOperationException,
					"Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set."
				)
				.XmlException(TypeLibrary.System.ArgumentNullException, "Thrown if the resource is set to null.");

			context.Writer.Property(
				new(
					child.PropertyName,
					child.ResourceKitType,
					LeastAccessible(resourceKit.Accessibility, child.Accessibility)
				)
				{
					HasGetter = true,
					HasSetter = true,
					SetterAccessibility = TypeDeclarationAccessibility.Internal,
				},
				writeGetterBody =>
				{
					var message = $"The '{child.PropertyName}' resource has not been initialized. Call Build first.";
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
							$"The '{child.PropertyName}' resource has already been initialized."
						);

					writeSetterBody.NewLine().Assignment("field", "value");
				}
			);
		}
	}

	static void GenerateResourceKitOptionsClass(
		OutputContext context,
		ResourceKitModel resourceKit,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Resource Kit Options class: {resourceKit.OptionsType.Name}", 3);

		context.Writer.XmlSummary(
			$"Typed settings for <see cref=\"{resourceKit.OptionsType}\" />.",
			context.ParentOf(resourceKit) is null
				? $"Can be accessed by resolving <see cref=\"{context.HostKit.OptionsType}.{resourceKit.PropertyName}\" />."
				: "Nested inside its parent resource kit's options."
		);

		using (
			context.Writer.ClassScope(
				new(resourceKit.OptionsType, TypeDeclarationAccessibility.Public) { IsSealed = true, IsPartial = true }
			)
		)
		{
			context
				.Writer.XmlSummary("Gets or sets the logical name used to register the resource.")
				.Property(
					new("Name", TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
					{
						HasSetter = true,
						Initializer = GeneratedText.QuoteLiteral(resourceKit.ResourceName),
						Attributes =
						[
							new(TypeLibrary.System.ComponentModel.DataAnnotations.RequiredAttribute)
							{
								Arguments = [new(false) { Name = "AllowEmptyStrings", IsPropertyAssignment = true }],
							},
						],
					}
				);

			context
				.Writer.XmlSummary("Gets or sets whether the resource is enabled.")
				.Property(
					new("IsEnabled", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
					{
						HasSetter = true,
						Initializer = "true",
					}
				);

			context
				.Writer.NewLine()
				.XmlSummary("Gets or sets the execution mode in which this resource participates.")
				.Property(
					new(
						"Mode",
						TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode,
						TypeDeclarationAccessibility.Public
					)
					{
						HasSetter = true,
						Initializer = ModeExpression(resourceKit.Mode),
					}
				);

			foreach (var child in context.ChildrenOf(resourceKit))
			{
				cancellationToken.ThrowIfCancellationRequested();

				context
					.Writer.NewLine()
					.XmlSummary(
						$"Gets or sets options for the <see cref=\"{child.ResourceKitType}\" /> child resource."
					)
					.Property(
						new(
							child.PropertyName,
							child.OptionsType,
							LeastAccessible(resourceKit.Accessibility, child.Accessibility)
						)
						{
							HasGetter = true,
							IsInitOnly = true,
							Initializer = "new()",
						}
					);
			}

			if (!resourceKit.Urls.IsEmpty)
			{
				context
					.Writer.NewLine()
					.XmlSummary("Gets or sets the dashboard URLs declared for the resource.")
					.Property(
						new("Urls", resourceKit.OptionsType.Nested("UrlsOptions"), TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "new()",
						}
					);

				GenerateUrlsOptionsClass(context.Writer, resourceKit.Urls);
			}
		}
	}
}
