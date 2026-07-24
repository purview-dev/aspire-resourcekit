using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

				var mixedUsageResourceSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
				foreach (var group in resourceDescriptors.GroupBy(r => r.Symbol, SymbolEqualityComparer.Default))
				{
					if (group.Key is null)
						continue;

					var hasGeneric = group.Any(r => r.IsGenericResourceDefinition);
					var hasNonGeneric = group.Any(r => !r.IsGenericResourceDefinition);
					if (!hasGeneric || !hasNonGeneric)
						continue;

					mixedUsageResourceSymbols.Add(group.Key);
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.MixedResourceDefinitionAttributesNotSupported,
							group.First().Declaration.Identifier.GetLocation(),
							group.Key.Name
						)
					);
				}

				resourceDescriptors = [
					.. resourceDescriptors.Where(resource => !mixedUsageResourceSymbols.Contains(resource.Symbol)),
				];

				List<TargetSymbolDescriptor> validResourceDescriptors = [];

				// Validate app resources: derive names, check uniqueness and base type.
				if (hostAppSymbol is not null && resourceDescriptors.Count > 0)
				{
					var descriptor = model.HostApp.Value!;
					var baseClassName = $"{descriptor.Name ?? descriptor.Symbol.Name}{TypeHelpers.BaseClassSuffix}";
					var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);

					foreach (var resource in resourceDescriptors)
					{
						var resourceSymbol = resource.Symbol;
						var hasExplicitBaseType = HasExplicitBaseType(resource);

						if (resource.IsGenericResourceDefinition && hasExplicitBaseType)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.GenericResourceDefinitionCannotHaveExplicitBase,
									resource.Declaration.Identifier.GetLocation(),
									resourceSymbol.Name
								)
							);
							continue;
						}

						if (!resource.IsGenericResourceDefinition && !hasExplicitBaseType)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.NonGenericResourceDefinitionRequiresExplicitBase,
									resource.Declaration.Identifier.GetLocation(),
									resourceSymbol.Name,
									baseClassName
								)
							);
							continue;
						}

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

						// Derive the property name from the resource type name when not explicitly overridden.
						var propertyName = resource.PropertyName ?? BuildPropertyNameFromTypeName(resourceSymbol.Name);
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

						// Check that resources with an explicit base derive from the expected generated base (SG0006).
						// If no explicit base was declared, a generated partial will provide the host-specific base.
						if (hasExplicitBaseType && !IsDerivedFromExpectedBase(resource, baseClassName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.ResourceMustDeriveFromBase,
									resource.Declaration.Identifier.GetLocation(),
									resourceSymbol.Name,
									baseClassName
								)
							);
							continue;
						}

						validResourceDescriptors.Add(resource);
					}
				}

				if (validResourceDescriptors.Count == 0)
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
				var generatedModel = new GeneratedHostAppModel(hostAppDescriptor, [.. validResourceDescriptors]);
				var source = BuildHostAppSource(generatedModel, model.GenerationContext);
				var fileName = $"{hostAppSymbol.Name}.AppResourceKit.g.cs";

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
		var baseClassName = $"{hostAppDescriptor.Name ?? hostAppTypeName}{TypeHelpers.BaseClassSuffix}";
		var optionsClassName = $"{hostAppTypeName}Options";
		var generateOptions = hostAppDescriptor.GenerateOptions;
		var extensionMethodName = $"Add{hostAppTypeName}ResourceKit";
		var notNull = generationContext.SystemCANotNull is null ? null : $"[{TypeHelpers.NotNullAttribute}] ";

		var resourceInfo =
			new List<(
				TargetSymbolDescriptor Descriptor,
				string ResourceName,
				string PropertyName,
				string ParameterName,
				bool GenerateOptions,
				string OptionsClassName,
				bool HasExplicitBaseType,
				string? GenericResourceTypeName
			)>();

		generationContext.Logger?.Info($"Generating {hostAppType}...");

		var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (var resource in model.Resources)
		{
			var resourceName = resource.Name ?? DeriveResourceName(resource.Symbol.Name);
			if (string.IsNullOrWhiteSpace(resourceName))
				continue;

			var propertyName = resource.PropertyName ?? BuildPropertyNameFromTypeName(resource.Symbol.Name);
			if (!IsValidIdentifier(propertyName) || !seenPropertyNames.Add(propertyName))
				continue;

			var hasExplicitBaseType = HasExplicitBaseType(resource);

			resourceInfo.Add(
				(
					resource,
					resourceName,
					propertyName,
					ToCamelCase(propertyName),
					resource.GenerateOptions,
					$"{resource.Symbol.Name}Options",
					hasExplicitBaseType,
					resource.GenericResourceTypeName
				)
			);
		}

		CodeWriter writer = new();
		string source;
		using (writer.Begin())
		{
			writer
				.WriteLine("// <auto-generated />")
				.NewLine()
				.WriteLine("#nullable enable")
				.NewLine()
				.WriteLine("using global::Microsoft.Extensions.Configuration;")
				.WriteLine("using global::Microsoft.Extensions.DependencyInjection;")
				.WriteLine("using global::Microsoft.Extensions.Options;")
				.WriteLine("using global::System;")
				.NewLine();

			// --- Per-resource option constructor partials in each resource namespace ---
			if (resourceInfo.Any(r => r.GenerateOptions || !r.HasExplicitBaseType))
			{
				string? currentNs = null;
				IDisposable? nsScope = null;
				foreach (
					var (
						desc,
						_,
						_,
						_,
						generateResourceOptions,
						resourceOptionsClassName,
						hasExplicitBaseType,
						genericResourceTypeName
					) in resourceInfo
				)
				{
					if (!generateResourceOptions && hasExplicitBaseType)
						continue;

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

					var resourceAccessibility = GetAccessibilityKeyword(resSymbol.DeclaredAccessibility);
					var resourceAccessPrefix = string.IsNullOrEmpty(resourceAccessibility)
						? string.Empty
						: resourceAccessibility + " ";
					var resourceOptionsType = hostNamespace is null
						? resourceOptionsClassName
						: $"global::{hostNamespace}.{resourceOptionsClassName}";
					string? generatedBaseType = null;
					if (!hasExplicitBaseType && !string.IsNullOrWhiteSpace(genericResourceTypeName))
					{
						var resourceTypeArgument = genericResourceTypeName;
						generatedBaseType = GetGeneratedResourceBaseType(
							hostNamespace,
							baseClassName,
							resourceTypeArgument!
						);
					}

					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Represents a generated app resource implementation.");
					writer.WriteLine("/// </summary>");
					if (generatedBaseType is null)
						writer.WriteLine($"{resourceAccessPrefix}partial class {resSymbol.Name}");
					else
						writer.WriteLine(
							$"{resourceAccessPrefix}partial class {resSymbol.Name} : {generatedBaseType}"
						);
					using (writer.Block())
					{
						if (generateResourceOptions)
						{
							writer.WriteLine("/// <summary>");
							writer.WriteLine(
								"/// Initializes a new instance of the generated resource using bound options."
							);
							writer.WriteLine("/// </summary>");
							writer.WriteLine(
								"/// <param name=\"options\">The resource options bound from configuration.</param>"
							);
							using (
								writer.Block($"public {resSymbol.Name}({resourceOptionsType} options) : base(options)")
							)
							{
								writer
									.WriteLine("global::System.ArgumentNullException.ThrowIfNull(options);")
									.NewLine()
									.WriteLine("Options = options;");
							}

							writer.NewLine();
							writer.WriteLine("/// <summary>");
							writer.WriteLine("/// Gets the strongly typed options used to initialize this resource.");
							writer.WriteLine("/// </summary>");
							writer.NewLine().WriteLine($"{resourceOptionsType} Options {{ get; init; }}");
						}
					}

					writer.NewLine();
				}

				nsScope?.Dispose();
				writer.NewLine();
			}

			// --- Host app namespace block (base class + partial + options + extensions) ---
			IDisposable? hostNs = hostNamespace is not null ? writer.Block($"namespace {hostNamespace}") : null;

			var resourceOptionsBaseClassName = $"{hostAppTypeName}ResourceOptionsBase";
			if (resourceInfo.Any(r => r.GenerateOptions))
			{
				using (writer.Block($"{baseAccessibility} abstract partial class {resourceOptionsBaseClassName}"))
				{
					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Gets or sets the logical resource name.");
					writer.WriteLine("/// </summary>");
					writer.WriteLine("public string? Name { get; set; }");
				}

				writer.NewLine();

				foreach (
					var (
						desc,
						resourceName,
						propertyName,
						_,
						generateResourceOptions,
						resourceOptionsClassName,
						_,
						_
					) in resourceInfo
				)
				{
					if (!generateResourceOptions)
						continue;

					var resourceAccessibility = GetAccessibilityKeyword(desc.Symbol.DeclaredAccessibility);
					var resourceAccessPrefix = string.IsNullOrEmpty(resourceAccessibility)
						? string.Empty
						: resourceAccessibility + " ";

					using (
						writer.Block(
							$"{resourceAccessPrefix}sealed partial class {resourceOptionsClassName} : {resourceOptionsBaseClassName}"
						)
					)
					{
						writer.WriteLine("/// <summary>");
						writer.WriteLine("/// Configuration section name for this resource options type.");
						writer.WriteLine("/// </summary>");
						writer
							.WriteIndent()
							.Write("public const string SectionName = ")
							.Quote(propertyName)
							.Write(";")
							.NewLine()
							.NewLine()
							.WriteLine("/// <summary>")
							.WriteLine("/// Initializes a new options instance with the default generated resource name.")
							.WriteLine("/// </summary>")
							.WriteLine($"public {resourceOptionsClassName}() => Name = \"{resourceName}\";");
					}

					writer.NewLine();
				}
			}

			// --- Generated base class ---
			writer.WriteLine("/// <summary>");
			writer.WriteLine("/// Base class for generated resources associated with this host app.");
			writer.WriteLine("/// </summary>");
			writer.WriteLine("/// <typeparam name=\"TResource\">The Aspire resource type.</typeparam>");
			using (
				writer.Block(
					$"{baseAccessibility} abstract class {baseClassName}<TResource> : ",
					additionalParts: w =>
						w.NewLine()
							.Indent()
							.MultiLine(
								$"{TypeHelpers.ResourceKitBase}<{hostAppTypeDisplay}, TResource>",
								$"where TResource : class, {TypeHelpers.IResource}"
							)
							.Unindent()
				)
			)
			{
				if (resourceInfo.Any(r => r.GenerateOptions))
				{
					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Initializes a new instance of the generated resource base type.");
					writer.WriteLine("/// </summary>");
					writer.WriteLine($"protected {baseClassName}() {{ }}");
					writer.NewLine();
					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Initializes a new instance of the generated resource base type using options.");
					writer.WriteLine("/// </summary>");
					writer.WriteLine("/// <param name=\"options\">The generated resource options.</param>");
					writer.WriteLine($"protected {baseClassName}({resourceOptionsBaseClassName} options)");
					using (writer.Block())
					{
						writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(options);");
						writer.WriteLine("Name = options.Name ?? Name;");
					}
				}
			}

			writer.NewLine();
			// --- Host app partial class ---
			var accessPrefix = string.IsNullOrEmpty(hostAccessibility) ? string.Empty : hostAccessibility + " ";
			if (generateOptions)
			{
				writer.WriteLine("/// <summary>");
				writer.WriteLine("/// Represents the generated host app and composes all discovered resources.");
				writer.WriteLine("/// </summary>");
				writer.WriteLine(
					$"{accessPrefix}partial class {hostAppTypeName}({optionsClassName} hostAppOptions) : {TypeHelpers.HostAppBase}<{hostAppTypeDisplay}>"
				);
			}
			else
			{
				writer.WriteLine("/// <summary>");
				writer.WriteLine("/// Represents the generated host app and composes all discovered resources.");
				writer.WriteLine("/// </summary>");
				writer.WriteLine(
					$"{accessPrefix}partial class {hostAppTypeName} : {TypeHelpers.HostAppBase}<{hostAppTypeDisplay}>"
				);
			}

			// (accessibility prefix handled above)
			using (writer.Block())
			{
				if (resourceInfo.Count == 0)
					writer.WriteLine("// No app resources were discovered for this host app.");
				else
				{
					foreach (var (desc, _, propName, _, _, _, _, _) in resourceInfo)
					{
						writer.WriteLine("/// <summary>");
						writer.WriteLine($"/// Gets the '{propName}' resource instance.");
						writer.WriteLine("/// </summary>");
						writer.WriteLine($"public {desc.Symbol.ToDisplayString(FullyQualifiedFormat)} {propName}");
						using (writer.Block())
						{
							writer.WriteLine(
								$"get => field ?? throw new global::System.InvalidOperationException(\"The '{propName}' resource has not been initialized. Call Build first.\");"
							);
							writer.WriteLine("private set");
							using (writer.Block())
							{
								writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(value);");
								writer.WriteLine(
									$"if (field is not null) throw new global::System.InvalidOperationException(\"The '{propName}' resource has already been initialized.\");"
								);
								writer.WriteLine("field = value;");
							}
						}

						writer.NewLine();
					}
				}

				writer.NewLine();
				// Build method
				writer.WriteLine("/// <summary>");
				writer.WriteLine("/// Builds and initializes all generated resources for this host app.");
				writer.WriteLine("/// </summary>");
				writer.WriteLine("/// <param name=\"builder\">The distributed application builder.</param>");
				writer.WriteLine(
					$"public override void Build({notNull}{TypeHelpers.IDistributedApplicationBuilder} builder)"
				);
				using (writer.Block())
				{
					writer.WriteLine("global::System.ArgumentNullException.ThrowIfNull(builder);");
					writer.NewLine();

					foreach (
						var (
							desc,
							_,
							propName,
							_,
							generateResourceOptions,
							resourceOptionsClassName,
							_,
							_
						) in resourceInfo
					)
					{
						if (generateResourceOptions)
						{
							writer.WriteLine(
								$"var {ToCamelCase(desc.Symbol.Name)}Options = builder.Configuration.GetSection({resourceOptionsClassName}.SectionName).Get<{resourceOptionsClassName}>() ?? new {resourceOptionsClassName}();"
							);
							writer.WriteLine($"{propName} = new({ToCamelCase(desc.Symbol.Name)}Options);");
						}
						else
						{
							writer.WriteLine($"{propName} = new() {{ Name = \"{desc.Name}\" }};");
						}

						writer.NewLine();
					}

					if (generateOptions && resourceInfo.Count > 0)
					{
						writer.WriteLine("// Set the enabled/ disabled state for each app resource.");
						foreach (var (_, _, propName, _, _, _, _, _) in resourceInfo)
						{
							writer.WriteLine(
								$"{propName}.IsEnabled = !hostAppOptions.IsResourceDisabled({propName}.Name);"
							);
						}

						writer.NewLine();
					}

					if (resourceInfo.Count == 0)
						writer.WriteLine("// No app resources were discovered for this host app.");
					else
					{
						writer
							.NewLine()
							.WriteLine("// Provide the list of app resources to the base class.")
							.WriteLine("Resources = [")
							.Indent()
							.MultiLineItems([.. resourceInfo.Select(r => r.PropertyName)])
							.Unindent()
							.WriteLine("];")
							.NewLine();
					}

					writer
						.WriteLine(
							"// Call the base class Build method to register the app resources with the builder."
						)
						.WriteLine("base.Build(builder);");
				}
			}

			writer.NewLine();
			// --- AppOptions class ---
			if (generateOptions)
			{
				using (writer.Block($"{baseAccessibility} sealed partial class {optionsClassName}"))
				{
					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Configuration section name for host app options.");
					writer.WriteLine("/// </summary>");
					writer
						.WriteIndent()
						.Write("public const string SectionName = ")
						.Quote(hostAppTypeName)
						.Write(";")
						.NewLine()
						.NewLine()
						.WriteLine("/// <summary>")
						.WriteLine("/// Gets the set of resource names that should be disabled.")
						.WriteLine("/// </summary>")
						.WriteLine(
							"public global::System.Collections.Generic.HashSet<string> DisabledResources { get; } = new(global::System.StringComparer.Ordinal);"
						)
						.NewLine();

					writer
						.WriteLine("/// <summary>")
						.WriteLine("/// Gets or sets an optional predicate used to decide whether resources are enabled.")
						.WriteLine("/// </summary>")
						.WriteLine("public global::System.Func<string, bool>? IsResourceEnabledPredicate { get; set; }")
						.NewLine();

					writer.WriteLine("/// <summary>");
					writer.WriteLine("/// Determines whether a resource should be considered disabled.");
					writer.WriteLine("/// </summary>");
					writer.WriteLine("/// <param name=\"resourceName\">The logical resource name.</param>");
					writer.WriteLine("/// <returns><see langword=\"true\"/> when disabled; otherwise <see langword=\"false\"/>.</returns>");
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

			writer.NewLine();
			// --- Builder extensions ---
			using (writer.Block($"internal static class {hostAppTypeName}BuilderExtensions"))
			{
				writer.WriteLine("/// <summary>");
				writer.WriteLine("/// Adds and configures the generated ResourceKit host app.");
				writer.WriteLine("/// </summary>");
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
						var (_, _, _, _, generateResourceOptions, resourceOptionsClassName, _, _) in resourceInfo
					)
					{
						if (!generateResourceOptions)
							continue;

						writer.NewLine();
						writer.WriteLine($"builder.Services.AddOptions<{resourceOptionsClassName}>()");
						writer
							.Indent()
							.WriteLine($".BindConfiguration({resourceOptionsClassName}.SectionName)")
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

	static bool HasExplicitBaseType(TargetSymbolDescriptor descriptor) =>
		descriptor.Declaration.BaseList is { Types.Count: > 0 };

	static bool IsDerivedFromExpectedBase(TargetSymbolDescriptor descriptor, string expectedBaseName)
	{
		if (
			descriptor.Symbol.BaseType is not null
			&& string.Equals(descriptor.Symbol.BaseType.Name, expectedBaseName, StringComparison.Ordinal)
		)
			return true;

		var declaredBaseTypes = descriptor.Declaration.BaseList?.Types;
		if (declaredBaseTypes is null)
			return false;

		foreach (var baseType in declaredBaseTypes)
		{
			if (string.Equals(GetUnqualifiedTypeName(baseType.Type), expectedBaseName, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	static string GetGeneratedResourceBaseType(
		string? hostNamespace,
		string baseClassName,
		string resourceTypeArgument
	) =>
		hostNamespace is null
			? $"{baseClassName}<{resourceTypeArgument}>"
			: $"global::{hostNamespace}.{baseClassName}<{resourceTypeArgument}>";

	static string GetUnqualifiedTypeName(TypeSyntax typeSyntax) =>
		typeSyntax switch
		{
			IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => GetUnqualifiedTypeName(qualifiedName.Right),
			AliasQualifiedNameSyntax aliasQualifiedName => GetUnqualifiedTypeName(aliasQualifiedName.Name),
			NullableTypeSyntax nullableType => GetUnqualifiedTypeName(nullableType.ElementType),
			_ => typeSyntax.ToString(),
		};

	static string TrimSuffix(string typeName) =>
		typeName.EndsWith("AppResource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "AppResource".Length)
		: typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName.EndsWith("Kit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Kit".Length)
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

	static string BuildPropertyNameFromTypeName(string typeName)
	{
		var trimmed = TrimSuffix(typeName);
		return string.IsNullOrEmpty(trimmed) ? "Resource" : trimmed;
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
