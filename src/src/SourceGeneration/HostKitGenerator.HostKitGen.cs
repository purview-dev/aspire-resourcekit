using System.Collections.Immutable;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildHostKit(
		HostKitInfo hostKitInfo,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Info($"Generating {hostKitInfo.HostKitType.SymbolFullName}...");

		using var nsScope = context.CodeWriter.WriteBlockNamespace(
			hostKitInfo.HostKitType.Namespace
		);

		var primaryConstructorParameters = ImmutableArray.CreateBuilder<string>();
		primaryConstructorParameters.Add(
			$"{TypeLibrary.Action.MakeGeneric(hostKitInfo.HostKitType, TypeLibrary.IDistributedApplicationBuilder)}? onBuilt"
		);
		primaryConstructorParameters.Add(
			$"{TypeLibrary.Action.MakeGeneric(hostKitInfo.HostKitType)}? onConfigured"
		);

		if (hostKitInfo.ShouldGenerateOptions)
			primaryConstructorParameters.Add($"{hostKitInfo.HostKitOptionsType}? options");

		using (
			context
				.CodeWriter.WriteXmlSummary(
					"Represents the generated Host Kit and composes all discovered Resources Kits"
				)
				.WriteClass(
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
					.CodeWriter.WriteXmlSummary("Gets the Host Kit options.")
					.WriteLine(
						$"public {hostKitInfo.HostKitOptionsType} Options {{ get; }} = options;"
					)
					.NewLine();
			}

			// Write the Resource Kit properties
			if (hostKitInfo.HasResourceKits)
			{
				GenerateResourceKitProperties(
					context.CodeWriter,
					hostKitInfo,
					logger,
					cancellationToken
				);
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
		HostKitInfo hostKitInfo,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug(
			$"Generating Resource Kit base class for host kit: {hostKitInfo.HostKitType.TypeName}"
		);

		context.CodeWriter.NewLine();
		using (
			context.CodeWriter.Block(
				$"namespace {hostKitInfo.HostKitResourceKitBaseType.Namespace}"
			)
		)
		{
			context
				.CodeWriter.WriteXmlSummary(
					"Represents a typed base class for all generated Resource Kits for the Host Kit."
				)
				.Write(hostKitInfo.AccessibilityModifier)
				.Write("abstract partial class ")
				.Write(hostKitInfo.HostKitResourceKitBaseType.TypeName)
				.Write("<TResource>")
				.NewLine()
				.Indent()
				.Write(": ")
				.WriteLine(
					TypeLibrary.ResourceKitBase.MakeGeneric(hostKitInfo.HostKitType, "TResource")
				)
				.WriteLine($"where TResource : class, {TypeLibrary.IResource}")
				.Unindent();

			using (context.CodeWriter.Block())
			{
				// Write the constructor
				context
					.CodeWriter.WriteXmlSummary(
						"Initializes a new instance of the Host Kit Resource Kit base class."
					)
					.Write("protected ")
					.Write(hostKitInfo.HostKitResourceKitBaseType.TypeName)
					.Write('(')
					.Write(hostKitInfo.HostKitType)
					.Write(" hostKit, ")
					.Write("string? name)")
					.NewLine()
					.Indent()
					.WriteLine(": base(hostKit, name)")
					.Unindent();

				using (context.CodeWriter.Block())
				{
					// Empty constructor body
				}
			}
		}
	}

	static void GenerateResourceKitProperties(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		logger?.Debug(
			$"Processing {hostKitInfo.ResourceKits.Length} resource kits for {hostKitInfo.HostKitType.SymbolFullName}...",
			1
		);

		foreach (var resourceKit in hostKitInfo.ResourceKits)
		{
			cancellationToken.ThrowIfCancellationRequested();

			logger?.Debug(
				$"Generating properties for resource kit: {resourceKit.ResourceKitType.TypeName}",
				2
			);

			writer
				.WriteXmlSummary(
					$"Gets the <see cref=\"{resourceKit.ResourceKitType}\" /> resource instance."
				)
				.WriteXml(
					"<exception cref=\"System.InvalidOperationException\">Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set.</exception>"
				)
				.WriteXml(
					"<exception cref=\"System.ArgumentNullException\">Thrown if the resource is set to null.</exception>"
				);

			if (resourceKit.PropertyName is null)
				throw new Exception("UH OH");

			using (writer.Block($"public {resourceKit.ResourceKitType} {resourceKit.PropertyName}"))
			{
				writer.WriteLine(
					$"get => field ?? throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has not been initialized. Call Build first.\");"
				);
				using (writer.Block("private set"))
				{
					writer
						.WriteLine("global::System.ArgumentNullException.ThrowIfNull(value);")
						.NewLine();
					using (writer.Block("if (field is not null)"))
					{
						writer.WriteLine(
							$"throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has already been initialized.\");"
						);
					}

					writer.NewLine().WriteLine("field = value;");
				}
			}

			writer.NewLine();
		}
	}

	static void GenerateBuildMethod(
		HostKitInfo hostKitInfo,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Build method for host kit: {hostKitInfo.HostKitType.TypeName}");

		context.CodeWriter.WriteXml("<inheritdoc />");
		using (
			context.CodeWriter.Block(
				$"public override void Build({TypeLibrary.IDistributedApplicationBuilder} builder)"
			)
		)
		{
			context
				.CodeWriter.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);")
				.NewLine();

			foreach (var resourceKit in hostKitInfo.ResourceKits)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.CodeWriter.Comment(
					$"Creating {resourceKit.ResourceKitType.TypeName} Resource Kit."
				);
				var resourceName =
					resourceKit.ResourceName
					?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);
				if (hostKitInfo.ShouldGenerateOptions)
				{
					context.CodeWriter.WriteLine(
						$"{resourceKit.PropertyName} = new(this, Options.{resourceKit.PropertyName});"
					);
				}
				else
				{
					context.CodeWriter.WriteLine(
						$"{resourceKit.PropertyName} = new(this, \"{resourceName}\");"
					);
				}

				context.CodeWriter.NewLine();
			}

			if (!hostKitInfo.HasResourceKits)
				context.CodeWriter.Comment("No app resources were discovered for this host app.");
			else
			{
				context.CodeWriter.Comment(
					"Register the discovered app resources with the base class."
				);
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();
					context.CodeWriter.WriteLine($"AddResource({resourceKit.PropertyName});");
				}
			}

			context
				.CodeWriter.NewLine()
				.Comment("Now the additional post-build func builder")
				.WriteLine("onBuilt?.Invoke(this, builder);");

			context
				.CodeWriter.NewLine()
				.Comment(
					"Now that we've populated all of the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.WriteLine("base.Build(builder);");
		}
	}

	static void GenerateConfigureMethod(
		HostKitInfo hostKit,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating Configure method for host kit: {hostKit.HostKitType.TypeName}");

		context.CodeWriter.NewLine().WriteXml("<inheritdoc />");
		using (context.CodeWriter.Block($"public override void Configure()"))
		{
			context
				.CodeWriter.NewLine()
				.Comment("Call the base classes Configure method first...")
				.WriteLine("base.Configure();");

			context
				.CodeWriter.NewLine()
				.Comment("Now the additional post-configure func builder")
				.WriteLine("onConfigured?.Invoke(this);");
		}
	}

	static void GenerateHostKitOptionsClass(
		HostKitInfo hostKitInfo,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug(
			$"Generating Host Kit Options class: {hostKitInfo.HostKitOptionsType.TypeName}"
		);

		context
			.CodeWriter.NewLine()
			.WriteXmlSummary(
				$"Typed settings for <see cref=\"{hostKitInfo.HostKitOptionsType}\" />."
			);
		using (
			context.CodeWriter.Block(
				$"public sealed partial class {hostKitInfo.HostKitOptionsType.TypeName}"
			)
		)
		{
			context.CodeWriter.WriteXmlSummary("Configuration section name for host kit options.");
			context
				.CodeWriter.Write("public const string SectionName = ")
				.Quote(hostKitInfo.HostKitType.TypeName)
				.Write(";")
				.NewLine();

			if (hostKitInfo.HasResourceKits)
			{
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();

					context
						.CodeWriter.NewLine()
						.WriteXmlSummary(
							$"Gets or sets options for <see cref=\"{resourceKit.ResourceKitType}\" />.",
							$"<see cref=\"{resourceKit.ResourceKitOptionsType}\" /> for specific configuration options."
						)
						.WriteLine(
							$"public {resourceKit.ResourceKitOptionsType} {resourceKit.PropertyName} {{ get; set; }} = new();"
						);
				}
			}

			context.CodeWriter.NewLine();
		}
	}
}
