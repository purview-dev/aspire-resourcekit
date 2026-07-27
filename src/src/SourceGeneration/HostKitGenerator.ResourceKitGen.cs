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

		foreach (var resourceKitGroup in hostKit.ResourceKits.GroupBy(r => r.ResourceKitType.Namespace))
		{
			var resourceNs = resourceKitGroup.Key is null ? null : writer.Block($"namespace {resourceKitGroup.Key}");

			foreach (var resourceKit in resourceKitGroup)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var suffix = resourceKit.HasExplicitBaseType ? null : $" : {hostKit.HostKitResourceKitBaseType}";

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
						writer.Write($"{resourceKit.ResourceKitOptionsType} options, ");

					writer.Write("string? name)").NewLine().Indent().WriteLine(": base(hostKit, name)").Unindent();

					using (writer.Block())
					{
						if (hostKit.ShouldGenerateOptions)
						{
							using (writer.Block("if (options == null)"))
								writer.WriteLine("throw new global::System.ArgumentNullException(nameof(options));");

							writer.NewLine().WriteLine("Options = options;");
						}
					}

					// Write the Options property if the host kit has options
					if (hostKit.ShouldGenerateOptions)
					{
						writer
							.NewLine()
							.WriteXmlSummary("Gets the Resource Kit options.")
							.WriteLine($"public {resourceKit.ResourceKitOptionsType} Options {{ get; }} = options;");
					}

					if (hostKit.ShouldGenerateOptions)
						GenerateResourceKitOptionsClass(writer.NewLine(), resourceKit, cancellationToken);
				}
			}

			resourceNs?.Dispose();
		}
	}

	static void GenerateResourceKitOptionsClass(
		CodeWriter writer,
		ResourceKitInfo resourceKit,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		using (writer.Block($"public sealed partial class {resourceKit.ResourceKitOptionsType.TypeName}"))
		{
			writer.Write("public const string SectionName = ").Quote(resourceKit.PropertyName).WriteLine(";").NewLine();

			writer.WriteLine("public string? Name { get; set; }");
		}
	}
}
