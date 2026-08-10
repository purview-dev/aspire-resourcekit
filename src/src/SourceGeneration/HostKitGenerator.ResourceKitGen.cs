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

		if (!hostKit.ResourceKits.IsDefaultOrEmpty)
			context.CodeWriter.NewLine();

		var isFirst = true;
		foreach (
			var resourceKitGroup in hostKit.ResourceKits.GroupBy(r => r.ResourceKitType.Namespace)
		)
		{
			var resourceNs = resourceKitGroup.Key is null
				? default
				: context.CodeWriter.Block($"namespace {resourceKitGroup.Key}");

			logger?.Debug(
				$"Processing resource kit group: {resourceKitGroup.Key ?? "<global-namespace>"}",
				1
			);

			foreach (var resourceKit in resourceKitGroup)
			{
				cancellationToken.ThrowIfCancellationRequested();

				logger?.Debug(
					$"Processing resource kit: {resourceKit.ResourceKitType.TypeName}",
					2
				);

				var suffix = resourceKit.HasExplicitBaseType
					? null
					: $" : {hostKit.HostKitResourceKitBaseType.MakeGeneric(resourceKit.AspireResourceType)}";

				if (isFirst)
					isFirst = false;
				else
					context.CodeWriter.NewLine();

				// Generate the resource kit class
				using (
					context.CodeWriter.Block(
						$"partial class {resourceKit.ResourceKitType.TypeName}{suffix}"
					)
				)
				{
					// Write the constructor
					context
						.CodeWriter.WriteXmlSummary(
							"Initializes a new instance of the Host Kit Resource Kit base class."
						)
						.Write("public ")
						.Write(resourceKit.ResourceKitType.TypeName)
						.Write('(')
						.Write(hostKit.HostKitType)
						.Write(" hostKit, ");

					if (hostKit.ShouldGenerateOptions)
					{
						context
							.CodeWriter.Write($"{resourceKit.ResourceKitOptionsType} options)")
							.NewLine()
							.Indent()
							.WriteLine(
								": base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
							)
							.Unindent();
					}
					else
					{
						context
							.CodeWriter.Write("string? name)")
							.NewLine()
							.Indent()
							.WriteLine(": base(hostKit, name)")
							.Unindent();
					}

					using (context.CodeWriter.Block())
					{
						if (hostKit.ShouldGenerateOptions)
						{
							context
								.CodeWriter.NewLine()
								.WriteLine("Options = options;")
								.WriteLine("IsEnabled = options.IsEnabled;");
						}
					}

					// Write the Options property if the host kit has options
					if (hostKit.ShouldGenerateOptions)
					{
						context
							.CodeWriter.NewLine()
							.WriteXmlSummary("Gets the Resource Kit options.")
							.WriteLine(
								$"public {resourceKit.ResourceKitOptionsType} Options {{ get; }}"
							);
					}

					if (hostKit.ShouldGenerateOptions)
						GenerateResourceKitOptionsClass(
							context.CodeWriter.NewLine(),
							hostKit,
							resourceKit,
							logger,
							cancellationToken
						);
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

		logger?.Debug(
			$"Generating Resource Kit Options class: {resourceKit.ResourceKitOptionsType.TypeName}",
			3
		);

		writer.WriteXmlSummary(
			$"Typed settings for <see cref=\"{resourceKit.ResourceKitOptionsType}\" />.",
			$"Can be accessed by resolving <see cref=\"{hostKit.HostKitOptionsType}.{resourceKit.PropertyName}\" />."
		);

		using (
			writer.Block(
				$"public sealed partial class {resourceKit.ResourceKitOptionsType.TypeName}"
			)
		)
		{
			var defaultResourceName =
				resourceKit.ResourceName
				?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);

			writer
				.WriteXmlSummary("Gets or sets the logical name used to register the resource.")
				.WriteLine(
					"[global::System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]"
				)
				.Write("public string Name { get; set; } = ")
				.Quote(defaultResourceName)
				.WriteLine(";")
				.NewLine();

			writer
				.WriteXmlSummary("Gets or sets whether the resource is enabled.")
				.WriteLine("public bool IsEnabled { get; set; } = true;");
		}
	}
}
