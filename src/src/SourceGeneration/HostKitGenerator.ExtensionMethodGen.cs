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
					BuildExtensionMethod(writer, hostKit);
			}
		}
	}

	static void BuildExtensionMethod(CodeWriter writer, HostKitInfo hostKit)
	{
		writer.WriteXmlSummary($"Adds and configures <see cref=\"{hostKit.HostKitType}\"/>.");
		writer
			.WriteXml(
				$"<param name=\"onBuilt\">",
				"<para>",
				$"An optional action to invoke after the host kit is built (post <see cref=\"{TypeHelpers.IHostKit}.Build({TypeHelpers.IDistributedApplicationBuilder})\"/>).",
				"</para>",
				"<para>",
				"Allows for additional customisation/ additions to the host kit before it is configured.",
				"</para>",
				"</param>"
			)
			.WriteXml(
				$"<param name=\"onConfigured\">An optional action to invoke after the host kit is configured (post <see cref=\"{TypeHelpers.IHostKit}.Configure\"/>).</param>"
			)
			.WriteXml(
				$"<param name=\"configureOptions\">An optional action that provides access to the <see cref=\"{TypeHelpers.OptionsBuilder.MakeGenericXml("TOptions")}\"/> for additional configuration.</param>"
			)
			.WriteXml("<returns>The same builder instance for chaining.</returns>");

		List<string> parameters =
		[
			$"{TypeHelpers.Action.MakeGeneric(hostKit.HostKitType, TypeHelpers.IDistributedApplicationBuilder)}? onBuilt = null",
			$"{TypeHelpers.Action.MakeGeneric(hostKit.HostKitType)}? onConfigured = null",
		];

		if (hostKit.ShouldGenerateOptions)
		{
			parameters.Add(
				$"{TypeHelpers.Action.MakeGeneric(TypeHelpers.OptionsBuilder.MakeGeneric(hostKit.HostKitOptionsType))}? configureOptions = null"
			);
		}

		using (
			writer.Block(
				$"public {TypeHelpers.IDistributedApplicationBuilder} {hostKit.ExtensionMethodName}(",
				additionalParts: w => w.MultiLineParameters([.. parameters])
			)
		)
		{
			if (hostKit.ShouldGenerateOptions)
			{
				writer
					.Comment("Bind the host kit options from configuration, or create a new instance if not found.")
					.WriteLine($"var optionsBuilder = builder.Services.AddOptions<{hostKit.HostKitOptionsType}>()")
					.Indent()
					.WriteLine($".BindConfiguration({hostKit.HostKitOptionsType}.SectionName);")
					.Unindent()
					.NewLine()
					.WriteLine("configureOptions?.Invoke(optionsBuilder);")
					.NewLine()
					.WriteLine("optionsBuilder.ValidateOnStart();")
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
				.Write($"{hostKit.HostKitType} hostKit = new (onBuilt, onConfigured");
			if (hostKit.ShouldGenerateOptions)
				writer.Write(", hostKitOptions");
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
