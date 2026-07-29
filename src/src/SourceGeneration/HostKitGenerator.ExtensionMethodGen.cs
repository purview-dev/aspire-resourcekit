using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildExtensionMethod(
		CodeWriter writer,
		HostKitInfo hostKit,
		GenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Logger?.Debug($"Generating extension method for host kit: {hostKit.HostKitType.TypeName}");

		using (writer.NewLine().Block($"namespace {TypeHelpers.IDistributedApplicationBuilder.Namespace}"))
		{
			using (
				writer.Block(
					$"{hostKit.AccessibilityModifier}static class {hostKit.HostKitType.TypeName}BuilderExtensions"
				)
			)
			{
				writer.WriteXmlSummary("Adds and configures the generated ResourceKit host app.");
				writer.WriteXml("<param name=\"builder\">The distributed application builder.</param>");
				writer.WriteXml("<returns>The same builder instance for chaining.</returns>");
				using (
					writer.Block(
						$"public static {TypeHelpers.IDistributedApplicationBuilder} {hostKit.ExtensionMethodName}(",
						additionalParts: w =>
							w.MultiLineParameters($"this {TypeHelpers.IDistributedApplicationBuilder} builder")
					)
				)
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					if (hostKit.ShouldGenerateOptions)
					{
						writer
							.Comment(
								"Bind the host kit options from configuration, or create a new instance if not found."
							)
							.WriteLine($"builder.Services.AddOptions<{hostKit.HostKitOptionsType}>()")
							.Indent()
							.WriteLine($".BindConfiguration({hostKit.HostKitOptionsType}.SectionName)")
							.WriteLine(".ValidateOnStart();")
							.Unindent()
							.NewLine();

						writer
							.WriteLine(
								$"var hostKitOptions = builder.Configuration.GetSection({hostKit.HostKitOptionsType}.SectionName).Get<{hostKit.HostKitOptionsType}>() ?? new();"
							)
							.NewLine();
					}

					writer
						.Comment("Create an instance of the generated host kit and configure it.")
						.Write($"{hostKit.HostKitType} hostKit = new (");
					if (hostKit.ShouldGenerateOptions)
						writer.Write("hostKitOptions");
					writer.WriteLine(");");

					writer
						.NewLine()
						.WriteLine("hostKit.Build(builder);")
						.WriteLine("hostKit.Configure();")
						.NewLine()
						.WriteLine("builder.Services.AddSingleton(hostKit);")
						.NewLine();

					writer.WriteLine("return builder;");
				}
			}
		}
	}
}
