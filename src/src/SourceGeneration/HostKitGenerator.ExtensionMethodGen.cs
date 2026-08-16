using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildExtensionClass(
		HostKitInfo hostKit,
		KitGenerationContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating extension class for host kit: {hostKit.HostKitType.TypeName}");

		using (context.CodeWriter.WriteBlockNamespaceScope(TypeLibrary.IDistributedApplicationBuilder.Namespace))
		{
			AttributeDeclarationOptions editorBrowsable = new(TypeLibrary.EditorBrowsableAttribute)
			{
				Arguments = [new($"{TypeLibrary.EditorBrowsableState}.Never")],
			};

			context
				.CodeWriter.XmlSummary($"Extension methods for <see cref=\"{hostKit.HostKitType}\"/>.")
				.WriteClass(
					new($"{hostKit.HostKitType.TypeName}BuilderExtensions")
					{
						Accessibility = hostKit.AccessibilityModifier,
						IsStatic = true,
						Attributes = [editorBrowsable],
					},
					body =>
					{
						using (body.OpenBlockScope($"extension({TypeLibrary.IDistributedApplicationBuilder} builder)"))
							BuildExtensionMethod(body, hostKit);
					}
				);
		}
	}

	static void BuildExtensionMethod(CodeWriter writer, HostKitInfo hostKit)
	{
		writer.XmlSummary($"Adds and configures <see cref=\"{hostKit.HostKitType}\"/>.");
		writer
			.XmlParam(
				"onBuilt",
				"<para>",
				$"An optional action to invoke after the host kit is built (post <see cref=\"{TypeLibrary.IHostKit}.Build({TypeLibrary.IDistributedApplicationBuilder})\"/>).",
				"</para>",
				"<para>",
				"Allows for additional customization/ additions to the host kit before it is configured.",
				"</para>"
			)
			.XmlParam(
				"onConfigured",
				$"An optional action to invoke after the host kit is configured (post <see cref=\"{TypeLibrary.IHostKit}.Configure\"/>)."
			);

		if (hostKit.ShouldGenerateOptions)
		{
			writer.XmlParam(
				"configureOptions",
				$"An optional action that provides access to the <see cref=\"{TypeLibrary.OptionsBuilder.MakeGenericXml("TOptions")}\"/> for additional configuration."
			);
		}

		writer.XmlReturn("The same builder instance for chaining.");

		List<ParameterDeclarationOptions> parameters =
		[
			new(
				"onBuilt",
				new(TypeLibrary.Action.MakeGeneric(hostKit.HostKitType, TypeLibrary.IDistributedApplicationBuilder))
				{
					IsNullable = true,
				}
			)
			{
				DefaultValue = "null",
			},
			new("onConfigured", TypeLibrary.Action.MakeGeneric(hostKit.HostKitType))
			{
				DefaultValue = "null",
				IsNullable = true,
			},
		];

		if (hostKit.ShouldGenerateOptions)
		{
			parameters.Add(
				new(
					"configureOptions",
					TypeLibrary.Action.MakeGeneric(TypeLibrary.OptionsBuilder.MakeGeneric(hostKit.HostKitOptionsType))
				)
				{
					DefaultValue = "null",
					IsNullable = true,
				}
			);
		}

		using (
			writer.WriteMethodScope(
				new(hostKit.ExtensionMethodName, TypeLibrary.IDistributedApplicationBuilder)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					Parameters = [.. parameters],
				}
			)
		)
		{
			if (hostKit.ShouldGenerateOptions)
			{
				writer
					.Comment("Bind the host kit options from configuration, or create a new instance if not found.")
					.Write($"var optionsBuilder = builder.Services.AddOptions<{hostKit.HostKitOptionsType}>")
					.WriteArgumentList([], terminate: false)
					.NewLine()
					.Indented(w =>
						w.WriteInvocationLine(".BindConfiguration", [$"{hostKit.HostKitOptionsType}.SectionName"])
					);

				writer
					.NewLine()
					.WriteInvocationLine("configureOptions?.Invoke", ["optionsBuilder"])
					.WriteInvocationLine("optionsBuilder.ValidateOnStart", [])
					.NewLine();

				writer
					.Write("var hostKitOptions = ")
					.WriteInvocation(
						"builder.Configuration.GetSection",
						[$"{hostKit.HostKitOptionsType}.SectionName"],
						terminate: false
					)
					.NewLine()
					.Indented(w =>
						w.WriteInvocation($".Get<{hostKit.HostKitOptionsType}>", [], terminate: false)
							.Write(" ?? new();")
							.NewLine()
					);
			}

			writer.Comment("Create an instance of the generated host kit and configure it.");
			writer.Write($"{hostKit.HostKitType} hostKit = ");
			writer.WriteInvocation(
				"new",
				hostKit.ShouldGenerateOptions
					? ["onBuilt", "onConfigured", "hostKitOptions"]
					: ["onBuilt", "onConfigured"],
				terminate: false
			);
			writer.WriteLine(";");

			writer
				.NewLine()
				.WriteInvocationLine("hostKit.Build", ["builder"])
				.WriteInvocationLine("hostKit.Configure", [])
				.NewLine()
				.WriteInvocationLine("builder.Services.AddSingleton", ["hostKit"])
				.NewLine();

			writer.WriteLine("return builder;");
		}
	}
}
