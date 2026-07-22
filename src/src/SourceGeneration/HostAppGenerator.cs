using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;
using Purview.Aspire.ResourceKit.SourceGeneration.Templates;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed partial class HostAppGenerator : IIncrementalGenerator, ILogSupport
{
	GenerationLogger? _logger;

	static readonly SymbolDisplayFormat FullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(postInitContext =>
		{
			_logger?.Debug("Adding attributes:");
			_logger?.Debug($"- {TypeHelpers.EmbeddedAttribute.TypeName}", 1);

			postInitContext.AddEmbeddedAttributeDefinition();

			foreach (var resourceType in TypeHelpers.GeneratedTypes)
			{
				_logger?.Debug($"- {resourceType.TypeName}", 1);
				postInitContext.AddSource(
					resourceType.SymbolFullName + ".g.cs",
					EmbeddedResources.LoadTemplate(resourceType.TypeName)
				);
			}
		});

		// Collect all of the host app types and host resource types.
		var valueProviders = SourceGenHelpers.GetGeneratorValueProviders(context, _logger);

		context.RegisterSourceOutput(
			valueProviders,
			static (sourceProductionContext, model) =>
			{
				if (!model.IsSourceGeneratorEnabled)
				{
					model.GenerationContext.Logger?.Debug("Source generator disabled.");
					return;
				}

				model.GenerationContext.Logger?.Debug("Source generator enabled, processing...");

				List<DiagnosticInfo> diagnostics = [];
				if (model.HostApp.HasDiagnostics)
					diagnostics.AddRange(model.HostApp.Diagnostics);
				if (!model.Diagnostics.IsDefaultOrEmpty)
					diagnostics.AddRange(model.Diagnostics);

				// Collect any diagnostics from the app resource results.
				foreach (var resourceResult in model.AppResources)
				{
					if (resourceResult.HasDiagnostics)
						diagnostics.AddRange(resourceResult.Diagnostics);
				}

				// Resolve the host app symbol (if any).
				var hostAppSymbol = model.HostApp.IsSuccess ? model.HostApp.Value!.Symbol : null;

				// Resolve the app resource descriptors. All [AppResource] classes
				// attach to the single [HostApp]; the generated base class is not
				// visible during source generation, so interface-based filtering
				// is not possible.
				List<TargetSymbolDescriptor> resourceDescriptors;
				if (hostAppSymbol is not null)
				{
					resourceDescriptors = [];
					foreach (var resourceResult in model.AppResources)
					{
						if (!resourceResult.IsSuccess)
							continue;

						resourceDescriptors.Add(resourceResult.Value!);
					}
				}
				else
				{
					model.GenerationContext.Logger?.Debug("No host app found");
					resourceDescriptors = [];
				}

				// Validate app resources: derive names, check uniqueness and base type.
				if (hostAppSymbol is not null && resourceDescriptors.Count > 0)
				{
					var descriptor = model.HostApp.Value!;
					var baseClassName = $"{descriptor.Name ?? descriptor.Symbol.Name}ResourceBase";
					var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);

					foreach (var resource in resourceDescriptors)
					{
						var resourceSymbol = resource.Symbol;

						// Derive the resource name (from attribute or type name).
						var resourceName = resource.Name ?? DeriveResourceName(resourceSymbol.Name);
						if (string.IsNullOrWhiteSpace(resourceName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.ResourceNameNotDerivable,
									resource.Declaration.Identifier.GetLocation(),
									resourceSymbol.Name
								)
							);
							continue;
						}

						// Derive the property name.
						var propertyName = resource.PropertyName ?? BuildPropertyNameFromResourceName(resourceName);
						if (!IsValidIdentifier(propertyName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.InvalidPropertyName,
									resource.Declaration.Identifier.GetLocation(),
									propertyName
								)
							);
							continue;
						}

						// Check for duplicate property names (SG0005).
						if (!seenPropertyNames.Add(propertyName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.DuplicateResourcePropertyName,
									resource.Declaration.Identifier.GetLocation(),
									propertyName
								)
							);
							continue;
						}

						// Check that the resource derives from the expected base (SG0006).
						if (
							resourceSymbol.BaseType is null
							|| !string.Equals(resourceSymbol.BaseType.Name, baseClassName, StringComparison.Ordinal)
						)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.ResourceMustDeriveFromBase,
									resource.Declaration.Identifier.GetLocation(),
									resourceSymbol.Name,
									baseClassName
								)
							);
						}
					}
				}

				if (resourceDescriptors.Count == 0)
				{
					if (!model.HostApp.IsEmpty)
					{
						diagnostics.Add(
							GeneratorDiagnostics.Create(GeneratorDiagnostics.NoAppResourcesDefined, hostAppSymbol)
						);
					}
				}
				else if (model.HostApp.IsEmpty)
					diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.NoHostAppInfoDefined));

				// Report any diagnostics that were collected during the generation process.
				if (diagnostics.Count > 0)
					ReportDiagnostics(sourceProductionContext, diagnostics, model.GenerationContext.Logger);

				// If any fatal diagnostics were reported, do not generate any source code.
				if (model.HostApp.IsFatal || model.AppResources.Any(s => s.IsFatal))
					return;

				// We only support a single host app - if none was found, there is nothing to generate.
				if (hostAppSymbol is null)
					return;

				var hostAppDescriptor = model.HostApp.Value!;
				var generatedModel = new GeneratedHostAppModel(hostAppDescriptor, [.. resourceDescriptors]);
				var source = BuildHostAppSource(generatedModel, model.GenerationContext);
				var fileName = $"{hostAppSymbol.Name}.AppResources.g.cs";

				sourceProductionContext.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
			}
		);
	}

	static string BuildHostAppSource(GeneratedHostAppModel model, GenerationContext generationContext)
	{
		var hostAppDescriptor = model.HostApp;
		var hostAppType = hostAppDescriptor.Symbol;
		var hostNamespace = hostAppType.ContainingNamespace.IsGlobalNamespace
			? null
			: hostAppType.ContainingNamespace.ToDisplayString();
		var hostAppTypeDisplay = hostAppType.ToDisplayString(FullyQualifiedFormat);
		var hostAppTypeName = hostAppType.Name;
		var hostAccessibility = GetAccessibilityKeyword(hostAppType.DeclaredAccessibility);
		var baseAccessibility = string.IsNullOrEmpty(hostAccessibility) ? "internal" : hostAccessibility;
		var baseClassName = $"{hostAppDescriptor.Name ?? hostAppTypeName}ResourceBase";
		var optionsClassName = $"{hostAppTypeName}Options";
		var extensionMethodName = $"Add{hostAppTypeName}";
		var resourceInfo =
			new List<(
				TargetSymbolDescriptor Descriptor,
				string ResourceName,
				string PropertyName,
				string VariableName
			)>();

		generationContext.Logger?.Info($"Generating {hostAppType}...");

		var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (var resource in model.Resources)
		{
			var resourceName = resource.Name ?? DeriveResourceName(resource.Symbol.Name);
			if (string.IsNullOrWhiteSpace(resourceName))
				continue;
			var propertyName = resource.PropertyName ?? BuildPropertyNameFromResourceName(resourceName);
			if (!IsValidIdentifier(propertyName) || !seenPropertyNames.Add(propertyName))
				continue;
			resourceInfo.Add((resource, resourceName, propertyName, ToCamelCase(propertyName)));
		}

		var writer = new CodeWriter();
		string source;
		using (writer.Begin())
		{
			writer
				.WriteLine("// <auto-generated />")
				.NewLine()
				.WriteLine("#nullable enable")
				.NewLine()
				.WriteLine("using global::Microsoft.Extensions.Options;")
				.WriteLine("using global::Microsoft.Extensions.DependencyInjection;")
				.WriteLine("using global::Microsoft.Extensions.DependencyInjection.Extensions;")
				.WriteLine("using global::System;")
				.WriteLine("using global::System.Linq;")
				.NewLine();

			// --- Per-resource partials (Name override) in block-scoped namespaces ---
			string? currentNs = null;
			IDisposable? nsScope = null;
			foreach (var (desc, resName, _, _) in resourceInfo)
			{
				var resSymbol = desc.Symbol;
				var resNs = resSymbol.ContainingNamespace.IsGlobalNamespace
					? null
					: resSymbol.ContainingNamespace.ToDisplayString();
				if (resNs != currentNs)
				{
					nsScope?.Dispose();
					nsScope = resNs is not null ? writer.Block($"namespace {resNs}") : null;
					currentNs = resNs;
				}

				writer.WriteLine($"partial class {resSymbol.Name}");

				using (writer.Block())
					writer.WriteLine($"public override string Name => \"{resName}\";");

				writer.NewLine();
			}

			nsScope?.Dispose();
			writer.NewLine();
			// --- Host app namespace block (base class + partial + options + extensions) ---
			IDisposable? hostNs = hostNamespace is not null ? writer.Block($"namespace {hostNamespace}") : null;
			// --- Generated base class ---
			using (
				writer.Block(
					$"{baseAccessibility} abstract class {baseClassName}<TResource> : ",
					additionalParts: w =>
						w.NewLine()
							.Indent()
							.MultiLine(
								$"{TypeHelpers.HostAppResource}<{hostAppTypeDisplay}, TResource>,",
								$"{TypeHelpers.IHostAppResource}<{hostAppTypeDisplay}>",
								$"where TResource : class, global::Aspire.Hosting.ApplicationModel.IResource"
							)
							.Unindent()
				)
			) { }

			writer.NewLine();
			// --- Host app partial class ---
			var accessPrefix = string.IsNullOrEmpty(hostAccessibility) ? string.Empty : hostAccessibility + " ";
			writer.WriteLine($"{accessPrefix}partial class {hostAppTypeName}");

			// (accessibility prefix handled above)
			using (writer.Block())
			{
				writer.WriteLine($"IOptions<{optionsClassName}> _appHostOptions = default!;").NewLine();

				if (resourceInfo.Count == 0)
				{
					writer.WriteLine("// No app resources were discovered for this host app.");
					writer.NewLine();
				}
				else
				{
					foreach (var (desc, _, propName, _) in resourceInfo)
					{
						writer
							.WriteLine(
								$"public {desc.Symbol.ToDisplayString(FullyQualifiedFormat)} {propName} {{ get; private set; }} = default!;"
							)
							.NewLine();
					}

					writer.NewLine();
				}

				// Initialise method
				writer.WriteLine("public void Initialize(global::System.IServiceProvider serviceProvider)");
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(serviceProvider);");
					writer.NewLine();
					foreach (var (desc, _, propName, varName) in resourceInfo)
					{
						writer.WriteLine(
							$"{propName} = ActivatorUtilities.GetServiceOrCreateInstance<{desc.Symbol.ToDisplayString(FullyQualifiedFormat)}>(serviceProvider);"
						);
					}

					writer
						.WriteLine()
						.WriteLine(
							$"_appHostOptions = ActivatorUtilities.GetServiceOrCreateInstance<IOptions<{optionsClassName}>>(serviceProvider);"
						)
						.NewLine();

					foreach (var (desc, _, propName, varName) in resourceInfo)
					{
						writer.WriteLine(
							$"{propName}.IsEnabled = !_appHostOptions.Value.IsResourceDisabled({propName}.Name);"
						);
					}
				}

				writer.NewLine();
				// Build method
				writer.WriteLine("public void Build(global::Aspire.Hosting.IDistributedApplicationBuilder builder)");
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					if (resourceInfo.Count == 0)
						writer.WriteLine("// No app resources were discovered for this host app.");
					else
						foreach (var (_, _, propName, _) in resourceInfo)
							writer.WriteLine($"{propName}.Build(builder);");
				}

				writer.NewLine();
				// Configure method
				writer.WriteLine("public void Configure()");
				using (writer.Block())
				{
					if (resourceInfo.Count == 0)
						writer.WriteLine("// No app resources were discovered for this host app.");
					else
						foreach (var (_, _, propName, _) in resourceInfo)
							writer.WriteLine($"{propName}.Configure(this);");
				}
			}

			writer.NewLine();
			// --- AppOptions class ---
			using (writer.Block($"{baseAccessibility} sealed partial class {optionsClassName}"))
			{
				writer
					.WriteIndent()
					.Write("public const string SectionName = ")
					.Quote(hostAppTypeName)
					.Write(";")
					.NewLine()
					.NewLine()
					.WriteLine(
						"public global::System.Collections.Generic.HashSet<string> DisabledResources { get; } = new(global::System.StringComparer.Ordinal);"
					)
					.NewLine();

				writer
					.WriteLine("public global::System.Func<string, bool>? IsResourceEnabledPredicate { get; set; }")
					.NewLine();

				using (writer.Block("public bool IsResourceDisabled(string resourceName)"))
				{
					using (writer.Block("if (DisabledResources.Contains(resourceName))", seperator: null))
						writer.WriteLine("return true;");

					using (writer.Block("if (IsResourceEnabledPredicate is not null)", seperator: null))
						writer.WriteLine("return !IsResourceEnabledPredicate(resourceName);");

					writer.WriteLine("return false;");
				}
			}

			writer.NewLine();
			// --- Builder extensions ---
			using (writer.Block($"internal static class {hostAppTypeName}BuilderExtensions"))
			{
				using (
					writer.Block(
						$"public static global::Aspire.Hosting.IDistributedApplicationBuilder {extensionMethodName}(",
						additionalParts: w =>
							w.NewLine()
								.Indent()
								.MultiLine(
									"this global::Aspire.Hosting.IDistributedApplicationBuilder builder, ",
									$"global::System.Action<{optionsClassName}>? configureOptions = null, ",
									$"{TypeHelpers.ServiceLifetime}? hostAppLifetimeOverride = null, ",
									$"{TypeHelpers.ServiceLifetime}? resourceLifetimeOverride = null)"
								)
								.Unindent()
					)
				)
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					writer.WriteLine(
						$"builder.Services.AddOptions<{optionsClassName}>().BindConfiguration({optionsClassName}.SectionName);"
					);
					writer.WriteLine(
						$"builder.Services.TryAdd(new ServiceDescriptor(typeof({hostAppTypeDisplay}), typeof({hostAppTypeDisplay}), hostAppLifetimeOverride ?? {TypeHelpers.ServiceLifetime}.{hostAppDescriptor.ServiceLifetime}));"
					);

					// Register each of the resources.
					foreach (var (desc, _, _, _) in resourceInfo)
					{
						writer.WriteLine(
							$"builder.Services.TryAdd(new ServiceDescriptor(typeof({desc.Symbol.ToDisplayString(FullyQualifiedFormat)}), typeof({desc.Symbol.ToDisplayString(FullyQualifiedFormat)}), resourceLifetimeOverride ?? {TypeHelpers.ServiceLifetime}.{desc.ServiceLifetime}));"
						);
					}

					writer.NewLine();

					//writer.WriteLine(
					//	$"var hostApp = ActivatorUtilities.GetServiceOrCreateInstance<{hostAppTypeDisplay}>(serviceProvider);"
					//);
					//writer.WriteLine(
					//	"// Initialize the host app with the service provider. We do this hear to allow the user to"
					//);
					//writer.WriteLine("// add other services via construction if they need to...");
					//writer.WriteLine("hostApp.Initialize(serviceProvider);");
					//writer.WriteLine("// Build first to allow the assembly of all the required parts.");
					//writer.WriteLine("hostApp.Build(builder, serviceProvider);");
					//writer.WriteLine("// Then configure once all of the building is done across all resources.");
					//writer.WriteLine("hostApp.Configure(serviceProvider);");
					//writer.NewLine();

					writer.WriteLine("return builder;");
				}
			}

			hostNs?.Dispose();
			source = writer.ToString();
		}

		return source;
	}

	static string GetAccessibilityKeyword(Accessibility accessibility)
	{
		return accessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			_ => string.Empty,
		};
	}

	static string TrimSuffix(string typeName) =>
		typeName.EndsWith("AppResource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "AppResource".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName;

	static string ToCamelCase(string value)
	{
		return string.IsNullOrEmpty(value) ? value
			: value.Length == 1 ? char.ToLowerInvariant(value[0]).ToString()
			: char.ToLowerInvariant(value[0]) + value.Substring(1);
	}

	static string SanitizeForFileName(string typeName)
	{
		var sb = new StringBuilder(typeName.Length);
		foreach (var c in typeName)
		{
			if (c is '_' or '.' or ':' or '<' or '>' or ',' or ' ' or '`' or '{' or '}')
				sb.Append('_');
			else
				sb.Append(c);
		}

		return sb.ToString();
	}

	static string DeriveResourceName(string typeName) => TrimSuffix(typeName);

	static string BuildPropertyNameFromResourceName(string name)
	{
		var sb = new StringBuilder(name.Length);
		var capitalizeNext = true;
		foreach (var c in name)
		{
			if (char.IsLetterOrDigit(c) || c == '_')
			{
				sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
				capitalizeNext = false;
			}
			else
				capitalizeNext = true;
		}

		var result = sb.ToString();
		return string.IsNullOrEmpty(result) ? "Resource" : result;
	}

	static bool IsValidIdentifier(string name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		if (!char.IsLetter(name[0]) && name[0] != '_')
			return false;
		for (var i = 1; i < name.Length; i++)
		{
			if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
				return false;
		}

		return true;
	}
}
