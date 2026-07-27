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
			.Write(hostKitInfo.HostKitType.TypeName);

		if (hostKitInfo.ShouldGenerateOptions)
			writer.Write('(').Write(hostKitInfo.HostKitOptionsType).Write(" options)");

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
				GenerateHostKitResourceKitProperties(writer, hostKitInfo, context, cancellationToken);
			}
			else
			{
				writer.Comment(
					"Note: No app resources were discovered for this host app. The Resources property will be an empty list."
				);
			}

			// Write the Build method
			GenerateHostAppBuildMethod(writer, hostKitInfo, notNull, cancellationToken);

			if (hostKitInfo.ShouldGenerateOptions)
			{
				// Generate the options class.
				GenerateHostKitOptionsClass(writer, hostKitInfo, cancellationToken);
			}
		}

		GenerateHostKitResourceKitBase(writer, hostKitInfo, cancellationToken);

		hostKitNamespace?.Dispose();
	}

	static void GenerateHostKitResourceKitBase(
		CodeWriter writer,
		HostKitInfo hostKitInfo,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		writer
			.NewLine()
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

	static void GenerateHostKitResourceKitProperties(
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

	static void GenerateHostAppBuildMethod(
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
					var optionsVarName = ToCamelCase(resourceKit.ResourceKitOptionsType.TypeName);
					using (
						writer.Indented(
							$"var {optionsVarName} = builder.Configuration.GetSection({resourceKit.ResourceKitOptionsType}.SectionName)"
						)
					)
					{
						writer.WriteLine($".Get<{resourceKit.ResourceKitOptionsType}>() ?? new();");
					}

					writer.WriteLine($"{resourceKit.PropertyName} = new(this, {optionsVarName}, \"{resourceName}\");");
				}
				else
				{
					writer.WriteLine($"{resourceKit.PropertyName} = new(this, \"{resourceName}\");");
				}

				writer.NewLine();
			}

			if (hostKitInfo.ShouldGenerateOptions && hostKitInfo.HasResourceKits)
			{
				writer.Comment(
					"Set the enabled/ disabled state for each resource kit based on",
					"the owning Host Kit's Options property."
				);
				foreach (var resourceKit in hostKitInfo.ResourceKits)
				{
					writer.WriteLine(
						$"{resourceKit.PropertyName}.IsEnabled = !Options.IsResourceDisabled({resourceKit.PropertyName}.Name);"
					);
				}

				writer.NewLine();
			}

			if (!hostKitInfo.HasResourceKits)
				writer.Comment("No app resources were discovered for this host app.");
			else
			{
				writer
					.Comment("Provide the list of app resources to the base class.")
					.WriteLine("Resources = [")
					.Indent()
					.MultiLineItems([.. hostKitInfo.ResourceKits.Select(r => r.PropertyName)])
					.Unindent()
					.WriteLine("];");
			}

			writer
				.NewLine()
				.Comment(
					"Now that we've populated the resources, call the base classes",
					"Build method to register the app resources with the builder."
				)
				.WriteLine("base.Build(builder);");
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

			writer
				.NewLine()
				.WriteXmlSummary(
					"Gets the Resource Kit names that should be disabled.",
					$"<para>This is based on the <see cref=\"{TypeHelpers.IResourceKit}{{THostKit}}.Name\" /></para>"
				)
				.WriteLine(
					"public global::System.Collections.Generic.HashSet<string> DisabledResources { get; } = new(global::System.StringComparer.Ordinal);"
				)
				.NewLine();

			writer
				.WriteXmlSummary("Gets or sets an optional predicate used to decide whether resources are enabled.")
				.WriteLine("public global::System.Func<string, bool>? IsResourceEnabledPredicate { get; set; }")
				.NewLine();

			writer
				.WriteXmlSummary("Determines whether a resource should be considered disabled.")
				.WriteXml(
					"<param name=\"resourceName\">The logical resource name.</param>",
					"<returns><see langword=\"true\"/> when disabled; otherwise <see langword=\"false\"/>.</returns>"
				);

			using (writer.Block("public bool IsResourceDisabled(string resourceName)"))
			{
				using (writer.Block("if (DisabledResources.Contains(resourceName))", separator: null))
					writer.WriteLine("return true;");

				using (writer.Block("if (IsResourceEnabledPredicate is not null)", separator: null))
					writer.WriteLine("return !IsResourceEnabledPredicate(resourceName);");

				writer.WriteLine("return false;");
			}
		}
	}
}
