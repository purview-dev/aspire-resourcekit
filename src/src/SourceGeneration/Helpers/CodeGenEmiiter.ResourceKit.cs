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

			if (resourceKitGroup.Value.IsDefaultOrEmpty)
			{
				context.Info("No resource kits found for group: {0}", resourceKitGroup.Key ?? "<global-namespace>");
				continue;
			}

			var resourceKitForNS = resourceKitGroup.Value.FirstOrDefault();

			using var resourceNs = context.Writer.WriteBlockNamespaceScope(resourceKitForNS.ResourceKitType);
			context.Debug(
				$"Processing resource kit group: {resourceKitForNS.ResourceKitType.Namespace ?? "<global-namespace>"}",
				1
			);

			foreach (var resourceKit in resourceKitGroup.Value)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.Debug($"Processing resource kit: {resourceKit.ResourceKitType.Name}", 2);

				var baseClass = resourceKit.HasExplicitBaseType
					? default
					: context.HostKit.ResourceKitBaseType.MakeGeneric(resourceKit.AspireResourceType);

				// Generate the resource kit class
				using (
					context.Writer.WriteClassScope(
						new(resourceKit.ResourceKitType) { IsPartial = true, BaseType = baseClass }
					)
				)
				{
					// Write the constructor
					context
						.Writer.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
						.WriteConstructor(
							new(resourceKit.ResourceKitType, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("hostKit", context.HostKit.HostKitType),
									context.HostKit.ShouldGenerateOptions
										? new("options", resourceKit.OptionsType)
										: new("name", PurviewTypeLibrary.System.String.MakeNullable()),
								],
								Initializer = context.HostKit.ShouldGenerateOptions
									? "base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
									: "base(hostKit, name)",
							},
							body =>
							{
								if (context.HostKit.ShouldGenerateOptions)
									body.WriteLine("Options = options;").WriteLine("IsEnabled = options.IsEnabled;");
							}
						);

					// Write the Options property if the host kit has options
					if (context.HostKit.ShouldGenerateOptions)
					{
						context
							.Writer.XmlSummary("Gets the Resource Kit options.")
							.WriteProperty(
								new("Options", resourceKit.OptionsType, TypeDeclarationAccessibility.Public)
							);
					}

					if (context.HostKit.ShouldGenerateOptions)
					{
						GenerateResourceKitOptionsClass(context, resourceKit, cancellationToken);
					}
				}
			}

			resourceNs.Dispose();
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
			$"Can be accessed by resolving <see cref=\"{context.HostKit.OptionsType}.{resourceKit.PropertyName}\" />."
		);

		using (
			context.Writer.WriteClassScope(
				new(resourceKit.OptionsType, TypeDeclarationAccessibility.Public) { IsSealed = true, IsPartial = true }
			)
		)
		{
			context
				.Writer.XmlSummary("Gets or sets the logical name used to register the resource.")
				.WriteProperty(
					new("Name", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
					{
						Initializer = GeneratedText.QuoteLiteral(resourceKit.ResourceName),
						Attributes =
						[
							new(TypeLibrary.RequiredAttribute)
							{
								Arguments = [new(false) { Name = "AllowEmptyStrings", IsPropertyAssignment = true }],
							},
						],
					}
				);

			context
				.Writer.XmlSummary("Gets or sets whether the resource is enabled.")
				.WriteProperty(
					new("IsEnabled", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
					{
						Initializer = "true",
					}
				);
		}
	}
}
