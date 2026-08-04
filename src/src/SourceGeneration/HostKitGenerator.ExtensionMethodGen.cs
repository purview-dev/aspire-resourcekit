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
				using (writer.Block($"extension({TypeHelpers.IDistributedApplicationBuilder} builder)"))
				{
					writer.WriteXmlSummary($"Adds and configures <see cref=\"{hostKit.HostKitType}\"/>.");
					writer.WriteXml(
						$"<param name=\"onBuilt\">An optional action to invoke after the host app is built (post <see cref=\"{hostKit.HostKitType}.Build({TypeHelpers.IDistributedApplicationBuilder})\"/>).</param>"
					);
					writer.WriteXml(
						$"<param name=\"onConfigured\">An optional action to invoke after the host app is configured (post <see cref=\"{TypeHelpers.IHostKit}.Configure\"/>).</param>"
					);
					writer.WriteXml("<returns>The same builder instance for chaining.</returns>");
					using (
						writer.Block(
							$"public {TypeHelpers.IDistributedApplicationBuilder} {hostKit.ExtensionMethodName}(",
							additionalParts: w =>
								w.MultiLineParameters(
									$"global::System.Action<{hostKit.HostKitType}>? onBuilt = null",
									$"global::System.Action<{hostKit.HostKitType}>? onConfigured = null"
								)
						)
					)
					{
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
								.WriteLine($"var hostKitOptions = builder.Configuration.GetSection(")
								.Indent()
								.Indent()
								.WriteLine($"{hostKit.HostKitOptionsType}.SectionName")
								.Unindent()
								.WriteLine(")")
								.WriteLine($".Get<{hostKit.HostKitOptionsType}>()")
								.WriteLine($"?? new();")
								.Unindent()
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
							.WriteLine("onBuilt?.Invoke(hostKit);")
							.NewLine()
							.WriteLine("hostKit.Configure();")
							.WriteLine("onConfigured?.Invoke(hostKit);")
							.NewLine()
							.WriteLine("builder.Services.AddSingleton(hostKit);")
							.NewLine();

						writer.WriteLine("return builder;");
					}
				}
			}
		}
	}
}
