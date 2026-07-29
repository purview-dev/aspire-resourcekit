using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildResourceKits(
		CodeWriter writer,
		HostKitInfo hostKit,
		GenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Logger?.Debug($"Building resource kits for host kit: {hostKit.HostKitType}");

		foreach (var resourceKitGroup in hostKit.ResourceKits.GroupBy(r => r.ResourceKitType.Namespace))
		{
			var resourceNs = resourceKitGroup.Key is null ? null : writer.Block($"namespace {resourceKitGroup.Key}");

			context.Logger?.Debug($"Processing resource kit group: {resourceKitGroup.Key ?? "<global-namespace>"}", 1);

			foreach (var resourceKit in resourceKitGroup)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context.Logger?.Debug($"Processing resource kit: {resourceKit.ResourceKitType.TypeName}", 2);

				var suffix = resourceKit.HasExplicitBaseType
					? null
					: $" : {hostKit.HostKitResourceKitBaseType.MakeGeneric(resourceKit.AspireResourceType)}";

				writer.NewLine();

				// Generate the resource kit class
				using (writer.Block($"partial class {resourceKit.ResourceKitType.TypeName}{suffix}"))
				{
					// Write the constructor
					writer
						.WriteXmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
						.Write("public ")
						.Write(resourceKit.ResourceKitType.TypeName)
						.Write('(')
						.Write(hostKit.HostKitType)
						.Write(" hostKit, ");

					if (hostKit.ShouldGenerateOptions)
					{
						writer
							.Write($"{resourceKit.ResourceKitOptionsType} options)")
							.NewLine()
							.Indent()
							.WriteLine(
								": base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
							)
							.Unindent();
					}
					else
					{
						writer.Write("string? name)").NewLine().Indent().WriteLine(": base(hostKit, name)").Unindent();
					}

					using (writer.Block())
					{
						if (hostKit.ShouldGenerateOptions)
						{
							writer
								.NewLine()
								.WriteLine("Options = options;")
								.WriteLine("IsEnabled = options.IsEnabled;");
						}
					}

					// Write the Options property if the host kit has options
					if (hostKit.ShouldGenerateOptions)
					{
						writer
							.NewLine()
							.WriteXmlSummary("Gets the Resource Kit options.")
							.WriteLine($"public {resourceKit.ResourceKitOptionsType} Options {{ get; }}");
					}

					if (hostKit.ShouldGenerateOptions)
						GenerateResourceKitOptionsClass(writer.NewLine(), resourceKit, context, cancellationToken);
				}
			}

			resourceNs?.Dispose();
		}
	}

	static void GenerateResourceKitOptionsClass(
		CodeWriter writer,
		ResourceKitInfo resourceKit,
		GenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Logger?.Debug(
			$"Generating Resource Kit Options class: {resourceKit.ResourceKitOptionsType.TypeName}",
			3
		);

		using (writer.Block($"public sealed partial class {resourceKit.ResourceKitOptionsType.TypeName}"))
		{
			var defaultResourceName = resourceKit.ResourceName ?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);

			writer
				.WriteXmlSummary("Gets or sets the logical name used to register the resource.")
				.WriteLine("[global::System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]")
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
