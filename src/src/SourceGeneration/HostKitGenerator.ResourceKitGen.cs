using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildResourceKits(
		HostKitInfo hostKit,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Building resource kits for host kit: {hostKit.HostKitType}");

		foreach (var resourceKitGroup in hostKit.ResourceKits.GroupBy(r => r.ResourceKitType.Namespace))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var resourceNs = resourceKitGroup.Key is null
				? default
				: context.CodeWriter.WriteBlockNamespaceScope(resourceKitGroup.Key);

			logger?.Debug($"Processing resource kit group: {resourceKitGroup.Key ?? "<global-namespace>"}", 1);

			foreach (var resourceKit in resourceKitGroup)
			{
				cancellationToken.ThrowIfCancellationRequested();

				logger?.Debug($"Processing resource kit: {resourceKit.ResourceKitType.TypeName}", 2);

				var baseClass = resourceKit.HasExplicitBaseType
					? default
					: new TypeReferenceOptions(
						hostKit.HostKitResourceKitBaseType.MakeGeneric(resourceKit.AspireResourceType).RenderFullName
					);

				// Generate the resource kit class
				using (
					context.CodeWriter.WriteClassScope(
						new(resourceKit.ResourceKitType.TypeName) { IsPartial = true, BaseType = baseClass }
					)
				)
				{
					// Write the constructor
					context
						.CodeWriter.XmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
						.WriteConstructor(
							new(resourceKit.ResourceKitType.TypeName)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters =
								[
									new("hostKit", hostKit.HostKitType),
									hostKit.ShouldGenerateOptions
										? new("options", resourceKit.ResourceKitOptionsType)
										: new("name", "string") { IsNullable = true },
								],
								Initializer = hostKit.ShouldGenerateOptions
									? "base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
									: "base(hostKit, name)",
							},
							body =>
							{
								if (hostKit.ShouldGenerateOptions)
								{
									body.WriteLine("Options = options;").WriteLine("IsEnabled = options.IsEnabled;");
								}
							}
						);

					// Write the Options property if the host kit has options
					if (hostKit.ShouldGenerateOptions)
					{
						context
							.CodeWriter.XmlSummary("Gets the Resource Kit options.")
							.WriteProperty(
								new("Options", resourceKit.ResourceKitOptionsType)
								{
									Accessibility = TypeDeclarationAccessibility.Public,
									HasGetter = true,
								}
							);
					}

					if (hostKit.ShouldGenerateOptions)
					{
						GenerateResourceKitOptionsClass(
							context.CodeWriter,
							hostKit,
							resourceKit,
							logger,
							cancellationToken
						);
					}
				}
			}

			resourceNs.Dispose();
		}
	}

	static void GenerateResourceKitOptionsClass(
		CodeWriter writer,
		HostKitInfo hostKit,
		ResourceKitInfo resourceKit,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Resource Kit Options class: {resourceKit.ResourceKitOptionsType.TypeName}", 3);

		writer.XmlSummary(
			$"Typed settings for <see cref=\"{resourceKit.ResourceKitOptionsType}\" />.",
			$"Can be accessed by resolving <see cref=\"{hostKit.HostKitOptionsType}.{resourceKit.PropertyName}\" />."
		);

		using (
			writer.WriteClassScope(
				new(resourceKit.ResourceKitOptionsType.TypeName)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsSealed = true,
					IsPartial = true,
				}
			)
		)
		{
			var defaultResourceName =
				resourceKit.ResourceName ?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);

			writer
				.XmlSummary("Gets or sets the logical name used to register the resource.")
				.WriteProperty(
					new("Name", "string")
					{
						Accessibility = TypeDeclarationAccessibility.Public,
						HasGetter = true,
						HasSetter = true,
						Initializer = GeneratedText.QuoteLiteral(defaultResourceName),
						Attributes =
						[
							new(TypeLibrary.RequiredAttribute)
							{
								Arguments = [new(false) { Name = "AllowEmptyStrings", IsPropertyAssignment = true }],
							},
						],
					}
				);

			writer
				.XmlSummary("Gets or sets whether the resource is enabled.")
				.WriteProperty(
					new("IsEnabled", "bool")
					{
						Accessibility = TypeDeclarationAccessibility.Public,
						HasGetter = true,
						HasSetter = true,
						Initializer = "true",
					}
				);
		}
	}
}
