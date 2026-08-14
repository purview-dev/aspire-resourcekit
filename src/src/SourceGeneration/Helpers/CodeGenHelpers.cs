using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class CodeGenHelpers
{
	public const string CodeGenReplacementToken = "//{{CodeGen}}";
	public const string NonClassCodeGenReplacementToken = "//{{NonClassCodeGen}}";

	const string EmbedAttributesHashDefineName = "PURVIEW_ASPIRE_RESOURCEKIT_ATTRIBUTES";

	const string GeneratedCodeConstant =
		"System.CodeDom.Compiler.GeneratedCodeAttribute(\"{0}\", \"{1}\")";
	const string ConditionalConstant = "System.Diagnostics.ConditionalAttribute(\"{0}\")";
	const string CompilerGeneratedConstant = "System.Runtime.CompilerServices.CompilerGenerated";

	const string EmbeddedConstant = "Microsoft.CodeAnalysis.EmbeddedAttribute";
	const string ExcludeFromCodeCoverageConstant =
		"System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

	public const string DefaultExtensionMethodName = "AddAspireResourceKit";

	static readonly Lazy<string> GeneratedCodeAttribute = new(() =>
		string.Format(
			CultureInfo.InvariantCulture,
			GeneratedCodeConstant,
			AssemblyInfo.RootNamespace,
			AssemblyInfo.Version
		)
	);

	static readonly Lazy<string> ConditionalAttribute = new(() =>
		string.Format(
			CultureInfo.InvariantCulture,
			ConditionalConstant,
			EmbedAttributesHashDefineName
		)
	);

	static readonly Lazy<string[]> GenAttributes = new(() =>
		[
			EmbeddedConstant,
			ExcludeFromCodeCoverageConstant,
			ConditionalAttribute.Value,
			CompilerGeneratedConstant,
			GeneratedCodeAttribute.Value,
		]
	);

	static readonly Lazy<string[]> NonClassGenAttributes = new(() =>
		[EmbeddedConstant, CompilerGeneratedConstant, GeneratedCodeAttribute.Value]
	);

	static readonly ConcurrentDictionary<int, string> GeneratedCodeAttributesByTabs = new();
	static readonly ConcurrentDictionary<int, string> NonClassGeneratedCodeAttributesByTabs = new();

	static string Global(string attribute) => $"[global::{attribute}]";

	public static string GetGeneratedCodeAttribute(int tabs = 0) =>
		GeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(_ => '\t'));

				var result = string.Empty;
				foreach (var attr in GenAttributes.Value)
					result += $"{t}{Global(attr)}\n";

				return result;
			}
		);

	public static string GetNonClassGeneratedCodeAttribute(int tabs = 0) =>
		NonClassGeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(_ => '\t'));

				var result = string.Empty;
				foreach (var attr in NonClassGenAttributes.Value)
					result += $"{t}{Global(attr)}\n";

				return result;
			}
		);

	public static HostKitInfo BuildHostKit(
		KitTargetDescriptor hostKitSymbol,
		ImmutableArray<KitTargetDescriptor> resourceKitSymbols
	)
	{
		TypeValueObject hostKitType = new(hostKitSymbol.Target.Symbol);

		var hostKitOptionsNamespace =
			hostKitType.Namespace
			+ (hostKitType.IsGlobalNamespace ? null : '.')
			+ hostKitType.TypeName;

		var hostKitOptionsType = hostKitSymbol.GenerateOptions
			? new TypeValueObject(
				$"{hostKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}",
				hostKitOptionsNamespace
			)
			: TypeValueObject.Empty;
		var resourceKits = resourceKitSymbols
			.Select(r =>
			{
				TypeValueObject resourceKitType = new(r.Target.Symbol);

				var resourceKitOptionsNamespace =
					resourceKitType.Namespace
					+ (resourceKitType.IsGlobalNamespace ? null : '.')
					+ resourceKitType.TypeName;
				var resourceKitOptionsType = hostKitSymbol.GenerateOptions
					? new TypeValueObject(
						$"{resourceKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}",
						resourceKitOptionsNamespace
					)
					: TypeValueObject.Empty;

				TypeValueObject aspireResourceType = new(r.AspireResourceTypeSymbol!);
				var resourceName = r.Name;
				var propertyName = r.PropertyName;
				if (string.IsNullOrWhiteSpace(propertyName))
					propertyName = TrimSuffix(r.Target.Symbol.Name);

				return new ResourceKitInfo(
					r,
					resourceKitType,
					resourceKitOptionsType,
					aspireResourceType,
					r.Target.Symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!.Value,
					r.PropertyName!,
					resourceName,
					TypeHelpers.HasExplicitBaseType(r.Target)
				);
			})
			.ToImmutableArray();

		return new(
			hostKitSymbol,
			hostKitType,
			hostKitOptionsType,
			TypeLibrary.ResourceKitBase,
			hostKitSymbol
				.Target.Symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!
				.Value,
			hostKitSymbol.ExtensionName ?? DefaultExtensionMethodName,
			resourceKits
		);
	}

	public static string TrimSuffix(string typeName) =>
		typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName.EndsWith("Kit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Kit".Length)
		: typeName;
}
