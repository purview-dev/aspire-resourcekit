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

		var notNull = context.SystemCANotNull is null ? null : $"{TypeHelpers.NotNullAttribute} ";
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
							w.MultiLineParameters($"{notNull}this {TypeHelpers.IDistributedApplicationBuilder} builder")
					)
				)
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					if (hostKit.ShouldGenerateOptions)
					{
						List<TypeValueObject> optionTypes =
						[
							hostKit.HostKitOptionsType,
							.. hostKit.ResourceKits.Select(r => r.ResourceKitOptionsType),
						];
						foreach (var optionType in optionTypes)
						{
							writer.WriteLine($"builder.Services.AddOptions<{optionType}>()");
							writer
								.Indent()
								.WriteLine($".BindConfiguration({optionType}.SectionName)")
								.WriteLine(".ValidateOnStart();")
								.Unindent();
						}

						writer.NewLine();
						writer
							.WriteLine(
								$"var hostKitOptions = new (builder.Configuration.GetSection({hostKit.HostKitOptionsType}.SectionName).Get<{hostKit.HostKitOptionsType}>() ?? new {hostKit.HostKitOptionsType}());"
							)
							.NewLine();
					}

					writer.Write($"{hostKit.HostKitType} hostKit = new (");
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
