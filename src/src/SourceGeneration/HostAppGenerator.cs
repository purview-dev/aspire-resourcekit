using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Models;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Templates;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration;

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

			postInitContext.AddEmbeddedAttributeDefinition();
			_logger?.Debug("- EmbeddedAttribute", 1);

			foreach (var resourceName in TypeHelpers.GeneratedTypes)
			{
				_logger?.Debug($"- {resourceName}", 1);
				postInitContext.AddSource(resourceName + ".g.cs", EmbeddedResources.LoadTemplate(resourceName));
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
					var baseClassName = $"{descriptor.Name ?? descriptor.Symbol.Name}AppResourceBase";
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
				var source = BuildHostAppSource(generatedModel);
				var fileName = $"{hostAppSymbol.Name}.AppResources.g.cs";

				sourceProductionContext.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
			}
		);
	}

	static string BuildHostAppSource(GeneratedHostAppModel model)
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
		var baseClassName = $"{hostAppDescriptor.Name ?? hostAppTypeName}AppResourceBase";
		var optionsClassName = $"{hostAppTypeName}AppOptions";
		var extensionMethodName = $"Add{hostAppTypeName}";
		var resourceInfo =
			new List<(
				TargetSymbolDescriptor Descriptor,
				string ResourceName,
				string PropertyName,
				string VariableName
			)>();
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
			writer.WriteLine("// <auto-generated />");
			writer.WriteLine("#nullable enable");
			writer.WriteLine("using global::Microsoft.Extensions.DependencyInjection;");
			writer.WriteLine("using global::Microsoft.Extensions.DependencyInjection.Extensions;");
			writer.WriteLine("using global::System;");
			writer.WriteLine("using global::System.Linq;");
			writer.NewLine();
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
			writer.WriteLine(
				$"{baseAccessibility} abstract class {baseClassName}<TResource> : global::Purview.Aspire.ResourceIsolation.HostAppResource<{hostAppTypeDisplay}, TResource>, global::Purview.Aspire.ResourceIsolation.IHostAppResource<{hostAppTypeDisplay}> where TResource : class, global::Aspire.Hosting.ApplicationModel.IResource"
			);
			using (writer.Block())
			{
				writer.WriteLine(
					"protected override bool IsResourceEnabled(global::Aspire.Hosting.IDistributedApplicationBuilder builder, global::System.IServiceProvider services)"
				);
				using (writer.Block())
				{
					writer.WriteLine($"var options = services.GetService<{optionsClassName}>();");
					writer.WriteLine("if (options is not null && options.IsResourceDisabled(Name))");
					writer.Indent();
					writer.WriteLine("return false;");
					writer.Unindent();
					writer.WriteLine("return base.IsResourceEnabled(builder, services);");
				}
			}

			writer.NewLine();
			// --- Host app partial class ---
			var accessPrefix = string.IsNullOrEmpty(hostAccessibility) ? string.Empty : hostAccessibility + " ";
			writer.WriteLine($"{accessPrefix}partial class {hostAppTypeName}");
			// (accessibility prefix handled above)
			using (writer.Block())
			{
				if (resourceInfo.Count == 0)
				{
					writer.WriteLine("// No app resources were discovered for this host app.");
					writer.NewLine();
				}
				else
				{
					foreach (var (desc, _, propName, _) in resourceInfo)
						writer.WriteLine(
							$"public {desc.Symbol.ToDisplayString(FullyQualifiedFormat)} {propName} {{ get; private set; }} = default!;"
						);
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
							$"var {varName} = ActivatorUtilities.GetServiceOrCreateInstance<{desc.Symbol.ToDisplayString(FullyQualifiedFormat)}>(serviceProvider);"
						);
						writer.WriteLine($"{propName} = {varName};");
					}
				}

				writer.NewLine();
				// Build method
				writer.WriteLine(
					"public void Build(global::Aspire.Hosting.IDistributedApplicationBuilder builder, global::System.IServiceProvider serviceProvider)"
				);
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(serviceProvider);");
					writer.NewLine();
					if (resourceInfo.Count == 0)
						writer.WriteLine("// No app resources were discovered for this host app.");
					else
						foreach (var (_, _, propName, _) in resourceInfo)
							writer.WriteLine($"{propName}.BuildResource(builder, serviceProvider);");
				}

				writer.NewLine();
				// Configure method
				writer.WriteLine("public void Configure(global::System.IServiceProvider serviceProvider)");
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(serviceProvider);");
					writer.NewLine();
					if (resourceInfo.Count == 0)
						writer.WriteLine("// No app resources were discovered for this host app.");
					else
						foreach (var (_, _, propName, _) in resourceInfo)
							writer.WriteLine($"{propName}.ConfigureResource(this, serviceProvider);");
				}
			}

			writer.NewLine();
			// --- AppOptions class ---
			writer.WriteLine($"{baseAccessibility} sealed class {optionsClassName}");
			using (writer.Block())
			{
				writer.WriteLine(
					"public global::System.Collections.Generic.HashSet<string> DisabledResources { get; } = new(global::System.StringComparer.Ordinal);"
				);
				writer.NewLine();
				writer.WriteLine("public global::System.Func<string, bool>? IsResourceEnabledPredicate { get; set; }");
				writer.NewLine();
				writer.WriteLine("public bool IsResourceDisabled(string resourceName)");
				using (writer.Block())
				{
					writer.WriteLine("if (DisabledResources.Contains(resourceName))");
					writer.Indent();
					writer.WriteLine("return true;");
					writer.Unindent();
					writer.WriteLine("if (IsResourceEnabledPredicate is not null)");
					writer.Indent();
					writer.WriteLine("return !IsResourceEnabledPredicate(resourceName);");
					writer.Unindent();
					writer.WriteLine("return false;");
				}
			}

			writer.NewLine();
			// --- Builder extensions ---
			writer.WriteLine($"internal static class {hostAppTypeName}BuilderExtensions");
			using (writer.Block())
			{
				writer.WriteLine(
					$"public static global::Aspire.Hosting.IDistributedApplicationBuilder {extensionMethodName}(this global::Aspire.Hosting.IDistributedApplicationBuilder builder, global::System.Action<global::Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureServices = null, global::System.Action<{optionsClassName}>? configureOptions = null, global::{TypeHelpers.FullServiceLifetimeName}? hostAppLifetime = null, global::{TypeHelpers.FullServiceLifetimeName}? resourceLifetimeOverride = null)"
				);
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();
					writer.WriteLine("configureServices?.Invoke(builder.Services);");
					writer.NewLine();
					writer.WriteLine($"builder.Services.TryAddSingleton<{optionsClassName}>();");
					writer.WriteLine(
						$"builder.Services.TryAdd(new ServiceDescriptor(typeof({hostAppTypeDisplay}), typeof({hostAppTypeDisplay}), global::{TypeHelpers.FullServiceLifetimeName}.{hostAppDescriptor.ServiceLifetime}));"
					);
					foreach (var (desc, _, _, _) in resourceInfo)
						writer.WriteLine(
							$"builder.Services.TryAdd(new ServiceDescriptor(typeof({desc.Symbol.ToDisplayString(FullyQualifiedFormat)}), typeof({desc.Symbol.ToDisplayString(FullyQualifiedFormat)}), resourceLifetimeOverride ?? global::{TypeHelpers.FullServiceLifetimeName}.{desc.ServiceLifetime}));"
						);
					writer.NewLine();
					writer.WriteLine("using var serviceProvider = builder.Services.BuildServiceProvider();");
					writer.WriteLine($"var options = serviceProvider.GetRequiredService<{optionsClassName}>();");
					writer.WriteLine("configureOptions?.Invoke(options);");
					writer.NewLine();
					writer.WriteLine(
						$"var hostApp = ActivatorUtilities.GetServiceOrCreateInstance<{hostAppTypeDisplay}>(serviceProvider);"
					);
					writer.WriteLine(
						"// Initialize the host app with the service provider. We do this hear to allow the user to"
					);
					writer.WriteLine("// add other services via construction if they need to...");
					writer.WriteLine("hostApp.Initialize(serviceProvider);");
					writer.WriteLine("// Build first to allow the assembly of all the required parts.");
					writer.WriteLine("hostApp.Build(builder, serviceProvider);");
					writer.WriteLine("// Then configure once all of the building is done across all resources.");
					writer.WriteLine("hostApp.Configure(serviceProvider);");
					writer.NewLine();
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
