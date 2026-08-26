using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	static void EmitExtensionClass(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating extension class for host kit: {context.HostKit.HostKitType.Name}");

		using (context.Writer.WriteBlockNamespaceScope(TypeLibrary.IDistributedApplicationBuilder))
		{
			AttributeDeclarationOptions editorBrowsable = new(TypeLibrary.EditorBrowsableAttribute)
			{
				Arguments = [new(TypeLibrary.EditorBrowsableState.StaticMember("Never"))],
			};

			context
				.Writer.XmlSummary($"Extension methods for {CodeWriter.XmlSee(context.HostKit.HostKitType)}.")
				.WriteClass(
					new($"{context.HostKit.HostKitType.Name}BuilderExtensions", context.HostKit.Accessibility)
					{
						IsStatic = true,
						Attributes = [editorBrowsable],
					},
					body => BuildExtensionMethod(body, context)
				);
		}
	}

	static void BuildExtensionMethod(CodeWriter writer, OutputContext context)
	{
		writer.XmlSummary($"Adds and configures <see cref=\"{context.HostKit.HostKitType}\"/>.");
		writer
			.XmlParam("builder",
				$"The <see cref=\"{TypeLibrary.IDistributedApplicationBuilder}\"/> to add the host kit to."
			)
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

		if (context.HostKit.ShouldGenerateOptions)
		{
			writer.XmlParam(
				"configureOptions",
				$"An optional action that provides access to the <see cref=\"{CodeWriter.XmlText(TypeLibrary.OptionsBuilder.MakeGeneric("TOptions"))}\"/> for additional configuration."
			);
		}

		writer.XmlReturn("The same builder instance for chaining.");

		List<ParameterDeclarationOptions> parameters =
		[
			new("builder", TypeLibrary.IDistributedApplicationBuilder) {
				IsThis = true
			},
			new(
				"onBuilt",
				TypeLibrary
					.Action.MakeGeneric(context.HostKit.HostKitType, TypeLibrary.IDistributedApplicationBuilder)
					.MakeNullable()
			)
			{
				DefaultValue = "null",
			},
			new("onConfigured", TypeLibrary.Action.MakeGeneric(context.HostKit.HostKitType).MakeNullable())
			{
				DefaultValue = "null",
			},
		];

		if (context.HostKit.ShouldGenerateOptions)
		{
			parameters.Add(
				new(
					"configureOptions",
					TypeLibrary
						.Action.MakeGeneric(TypeLibrary.OptionsBuilder.MakeGeneric(context.HostKit.OptionsType))
						.MakeNullable()
				)
				{
					DefaultValue = "null",
				}
			);
		}

		using (
			writer.WriteMethodScope(
				new(
					context.HostKit.ExtensionMethodName,
					TypeLibrary.IDistributedApplicationBuilder,
					TypeDeclarationAccessibility.Public
				)
				{
					IsStatic = true,
					Parameters = [.. parameters],
				}
			)
		)
		{
			if (context.HostKit.ShouldGenerateOptions)
			{
				writer
					.Comment("Bind the host kit options from configuration, or create a new instance if not found.")
					.Write($"var optionsBuilder = builder.Services.AddOptions<{context.HostKit.OptionsType}>")
					.WriteArgumentList([], terminate: false)
					.NewLine()
					.Indented(w =>
						w.WriteInvocationLine(".BindConfiguration", [$"{context.HostKit.OptionsType}.SectionName"])
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
						[$"{context.HostKit.OptionsType}.SectionName"],
						terminate: false
					)
					.NewLine()
					.Indented(w =>
						w.WriteInvocation($".Get<{context.HostKit.OptionsType}>", [], terminate: false)
							.Write(" ?? new();")
							.NewLine()
					);
			}

			writer.Comment("Create an instance of the generated host kit and configure it.");
			writer.Write($"{context.HostKit.HostKitType} hostKit = ");
			writer.WriteInvocation(
				"new",
				context.HostKit.ShouldGenerateOptions
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
