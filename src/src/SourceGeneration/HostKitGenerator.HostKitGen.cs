using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	static void BuildHostKit(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		GenerationContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var notNull = (string?)null; //context.SystemCANotNull is null ? null : $"{TypeHelpers.NotNullAttribute} ";

		context.Logger?.Info($"Generating {hostKitInfo.HostKitType.SymbolFullName}...");

		var hostKitNamespace = hostKitInfo.HostKitType.IsGlobalNamespace
			? null
			: writer.Block($"namespace {hostKitInfo.HostKitType.Namespace}");

		writer
			.WriteXmlSummary("Represents the generated Host Kit and composes all discovered Resources Kits")
			.Write("partial class ")
			.Write(hostKitInfo.HostKitType.TypeName)
			.Write('(')
			.NewLine()
			.Indent();

		writer
			.Write(TypeHelpers.Action.MakeGeneric(hostKitInfo.HostKitType, TypeHelpers.IDistributedApplicationBuilder))
			.WriteLine("? onBuilt, ");

		writer.Write(TypeHelpers.Action.MakeGeneric(hostKitInfo.HostKitType)).Write("? onConfigured");

		if (hostKitInfo.ShouldGenerateOptions)
			writer.WriteLine(", ").Write(hostKitInfo.HostKitOptionsType).WriteLine(" options");

		writer.Unindent().Write(')');
		writer.Write(" : ").WriteLine(TypeHelpers.HostKitBase.MakeGeneric(hostKitInfo.HostKitType));

		using (writer.Block())
		{
			// Write the Options property if the host kit has options
			if (hostKitInfo.ShouldGenerateOptions)
			{
				writer
					.WriteXmlSummary("Gets the Host Kit options.")
					.WriteLine($"public {hostKitInfo.HostKitOptionsType} Options {{ get; }} = options;")
					.NewLine();
			}

			// Write the Resource Kit properties
			if (hostKitInfo.HasResourceKits)
			{
				GenerateResourceKitProperties(writer, hostKitInfo, context, cancellationToken);
			}
			else
			{
				writer.Comment(
					"Note: No app resources were discovered for this host app. The Resources property will be an empty list."
				);
			}

			// Write the Build method
			GenerateBuildMethod(writer, hostKitInfo, notNull, cancellationToken);

			// Write the Configure method
			GenerateConfigureMethod(writer, cancellationToken);

			if (hostKitInfo.ShouldGenerateOptions)
			{
				// Generate the options class.
				GenerateHostKitOptionsClass(writer, hostKitInfo, cancellationToken);
			}
		}

		hostKitNamespace?.Dispose();

		GenerateResourceKitBase(writer, hostKitInfo, cancellationToken);
	}

	static void GenerateResourceKitBase(CodeWriter writer, HostKitInfo hostKitInfo, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		writer.NewLine();
		using (writer.Block($"namespace {hostKitInfo.HostKitResourceKitBaseType.Namespace}"))
		{
			writer
				.WriteXmlSummary("Represents a typed base class for all generated Resource Kits for the Host Kit.")
				.Write(hostKitInfo.AccessibilityModifier)
				.Write("abstract partial class ")
				.Write(hostKitInfo.HostKitResourceKitBaseType.TypeName)
				.Write("<TResource>")
				.NewLine()
				.Indent()
				.Write(": ")
				.WriteLine(TypeHelpers.ResourceKitBase.MakeGeneric(hostKitInfo.HostKitType, "TResource"))
				.WriteLine($"where TResource : class, {TypeHelpers.IResource}")
				.Unindent();

			using (writer.Block())
			{
				// Write the constructor
				writer
					.WriteXmlSummary("Initializes a new instance of the Host Kit Resource Kit base class.")
					.Write("protected ")
					.Write(hostKitInfo.HostKitResourceKitBaseType.TypeName)
					.Write('(')
					.Write(hostKitInfo.HostKitType)
					.Write(" hostKit, ")
					.Write("string? name)")
					.NewLine()
					.Indent()
					.WriteLine(": base(hostKit, name)")
					.Unindent();

				using (writer.Block())
				{
					// Empty constructor body
				}
			}
		}
	}

	static void GenerateResourceKitProperties(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		GenerationContext context,
		CancellationToken cancellationToken
	)
	{
		context.Logger?.Debug(
			$"Processing {hostKitInfo.ResourceKits.Length} resource kits for {hostKitInfo.HostKitType.SymbolFullName}...",
			1
		);

		foreach (var resourceKit in hostKitInfo.ResourceKits)
		{
			cancellationToken.ThrowIfCancellationRequested();

			context.Logger?.Debug($"Generating properties for resource kit: {resourceKit.ResourceKitType.TypeName}", 2);

			writer
				.WriteXmlSummary($"Gets the <see cref=\"{resourceKit.ResourceKitType}\" /> resource instance.")
				.WriteXml(
					"<exception cref=\"System.InvalidOperationException\">Thrown if the resource has not been initialized on get, or if the resource has already been initialized on set.</exception>"
				)
				.WriteXml(
					"<exception cref=\"System.ArgumentNullException\">Thrown if the resource is set to null.</exception>"
				);

			if (resourceKit.PropertyName is null)
				throw new Exception("UH OH");

			using (writer.Block($"public {resourceKit.ResourceKitType} {resourceKit.PropertyName}"))
			{
				writer.WriteLine(
					$"get => field ?? throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has not been initialized. Call Build first.\");"
				);
				using (writer.Block("private set"))
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(value);").NewLine();
					using (writer.Block("if (field is not null)"))
					{
						writer.WriteLine(
							$"throw new global::System.InvalidOperationException(\"The '{resourceKit.PropertyName}' resource has already been initialized.\");"
						);
					}

					writer.NewLine().WriteLine("field = value;");
				}
			}

			writer.NewLine();
		}
	}

	static void GenerateBuildMethod(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		string? notNull,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		writer.WriteXml("<inheritdoc />");
		using (
			writer.Block($"public override void Build({notNull}{TypeHelpers.IDistributedApplicationBuilder} builder)")
		)
		{
			writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);").NewLine();

			foreach (var resourceKit in hostKitInfo.ResourceKits)
			{
				cancellationToken.ThrowIfCancellationRequested();

				writer.Comment($"Creating {resourceKit.ResourceKitType.TypeName} Resource Kit.");
				var resourceName =
					resourceKit.ResourceName ?? CodeGenHelpers.TrimSuffix(resourceKit.ResourceKitType.TypeName);
				if (hostKitInfo.ShouldGenerateOptions)
				{
					writer.WriteLine($"{resourceKit.PropertyName} = new(this, Options.{resourceKit.PropertyName});");
				}
				else
				{
					writer.WriteLine($"{resourceKit.PropertyName} = new(this, \"{resourceName}\");");
				}

				writer.NewLine();
			}

			if (!hostKitInfo.HasResourceKits)
				writer.Comment("No app resources were discovered for this host app.");
			else
			{
				writer.Comment("Register the discovered app resources with the base class.");
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();
					writer.WriteLine($"AddResource({resourceKit.PropertyName});");
				}
			}

			writer
				.NewLine()
				.Comment("Now the additional post-build func builder")
				.WriteLine("onBuilt?.Invoke(this, builder);");

			writer
				.NewLine()
				.Comment(
					"Now that we've populated all of the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.WriteLine("base.Build(builder);");
		}
	}

	static void GenerateConfigureMethod(CodeWriter writer, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		writer.WriteXml("<inheritdoc />");
		using (writer.Block($"public override void Configure()"))
		{
			writer.NewLine().Comment("Call the base classes Configure method first...").WriteLine("base.Configure();");

			writer
				.NewLine()
				.Comment("Now the additional post-configure func builder")
				.WriteLine("onConfigured?.Invoke(this);");
		}
	}

	static void GenerateHostKitOptionsClass(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		writer.NewLine();
		using (writer.Block($"public sealed partial class {hostKitInfo.HostKitOptionsType.TypeName}"))
		{
			writer.WriteXmlSummary("Configuration section name for host kit options.");
			writer
				.Write("public const string SectionName = ")
				.Quote(hostKitInfo.HostKitType.TypeName)
				.Write(";")
				.NewLine();

			if (hostKitInfo.HasResourceKits)
			{
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					cancellationToken.ThrowIfCancellationRequested();

					writer
						.NewLine()
						.WriteXmlSummary(
							$"Gets or sets options for the {resourceKit.ResourceKitType.TypeName} resource kit."
						)
						.WriteLine(
							$"public {resourceKit.ResourceKitOptionsType} {resourceKit.PropertyName} {{ get; set; }} = new();"
						);
				}
			}

			writer.NewLine();
		}
	}
}
