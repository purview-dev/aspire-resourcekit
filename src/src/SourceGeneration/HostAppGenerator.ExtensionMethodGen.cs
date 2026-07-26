using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostAppGenerator
{
	static void GenerateExtensionMethod(
		CodeWriter writer,
		(
			string hostAppTypeName,
			string hostAppTypeDisplay,
			string optionsClassName,
			string? notNull,
			bool generateOptions
		) args
	)
	{
		(
			string hostAppTypeName,
			string hostAppTypeDisplay,
			string optionsClassName,
			string? notNull,
			bool generateOptions
		) = args;

		var extensionMethodName = $"Add{hostAppTypeName}ResourceKit";

		using (writer.Block(TypeHelpers.IDistributedApplicationBuilder.Namespace))
		{
			using (writer.Block($"internal static class {hostAppTypeName}BuilderExtensions"))
			{
				writer.WriteXmlComment("Adds and configures the generated ResourceKit host app.");
				writer.WriteLine("/// <param name=\"builder\">The distributed application builder.</param>");
				writer.WriteLine("/// <returns>The same builder instance for chaining.</returns>");
				using (
					writer.Block(
						$"public static {TypeHelpers.IDistributedApplicationBuilder} {extensionMethodName}(",
						additionalParts: w =>
							w.MultiLineParameters($"{notNull}this {TypeHelpers.IDistributedApplicationBuilder} builder")
					)
				)
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					if (generateOptions)
					{
						writer.WriteLine($"builder.Services.AddOptions<{optionsClassName}>()");
						writer
							.Indent()
							.WriteLine($".BindConfiguration({optionsClassName}.SectionName)")
							.WriteLine(".ValidateOnStart();")
							.Unindent();
						writer.NewLine();
						writer.WriteLine(
							$"{hostAppTypeDisplay} hostApp = new (builder.Configuration.GetSection({optionsClassName}.SectionName).Get<{optionsClassName}>() ?? new {optionsClassName}());"
						);
					}
					else
					{
						writer.WriteLine($"{hostAppTypeDisplay} hostApp = new ();");
					}

					foreach (
						var (_, _, _, _, generateResourceOptions, _, resourceOptionsTypeDisplay, _, _) in resourceInfo
					)
					{
						if (!generateResourceOptions)
							continue;

						writer.NewLine();
						writer.WriteLine($"builder.Services.AddOptions<{resourceOptionsTypeDisplay}>()");
						writer
							.Indent()
							.WriteLine($".BindConfiguration({resourceOptionsTypeDisplay}.SectionName)")
							.WriteLine(".ValidateOnStart();")
							.Unindent();
					}

					writer.NewLine();
					writer.WriteLine("hostApp.Build(builder);");
					writer.WriteLine("hostApp.Configure();");
					writer.NewLine();
					writer.WriteLine("builder.Services.AddSingleton(hostApp);");
					writer.NewLine();

					writer.WriteLine("return builder;");
				}
			}
		}
	}
}
