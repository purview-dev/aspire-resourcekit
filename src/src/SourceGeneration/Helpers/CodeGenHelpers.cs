using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class CodeGenHelpers
{
	public const string CodeGenReplacementToken = "{{CodeGen}}";
	public const string NonClassCodeGenReplacementToken = "{{NonClassCodeGen}}";

	const string EmbedAttributesHashDefineName = "PURVIEW_ASPIRE_RESOURCEKIT_ATTRIBUTES";

	const string GeneratedCodeConstant = "System.CodeDom.Compiler.GeneratedCodeAttribute(\"{0}\", \"{1}\")";
	const string ConditionalConstant = "System.Diagnostics.ConditionalAttribute(\"{0}\")";
	const string CompilerGeneratedConstant = "System.Runtime.CompilerServices.CompilerGenerated";

	const string EmbeddedConstant = "Microsoft.CodeAnalysis.EmbeddedAttribute";
	const string ExcludeFromCodeCoverageConstant = "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

	static readonly Lazy<string> GeneratedCodeAttribute = new(() =>
		string.Format(
			CultureInfo.InvariantCulture,
			GeneratedCodeConstant,
			AssemblyInfo.RootNamespace,
			AssemblyInfo.Version
		)
	);

	static readonly Lazy<string> ConditionalAttribute = new(() =>
		string.Format(CultureInfo.InvariantCulture, ConditionalConstant, EmbedAttributesHashDefineName)
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

				string result = string.Empty;
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

				string result = string.Empty;
				foreach (var attr in NonClassGenAttributes.Value)
					result += $"{t}{Global(attr)}\n";

				return result;
			}
		);

	public static HostKitInfo BuildHostKit(
		TargetSymbolDescriptor hostKitSymbol,
		ImmutableArray<TargetSymbolDescriptor> resourceKitSymbols
	)
	{
		TypeValueObject hostKitType = new(hostKitSymbol.Symbol);

		var hostKitOptionsNamespace =
			hostKitType.Namespace + (hostKitType.IsGlobalNamespace ? null : '.') + hostKitType.TypeName;

		var hostKitOptionsType = hostKitSymbol.GenerateOptions
			? new TypeValueObject(
				$"{hostKitType.TypeName}{TypeHelpers.OptionsBaseClassSuffix}",
				hostKitOptionsNamespace
			)
			: TypeValueObject.Empty;
		TypeValueObject hostKitResourceKitBaseType = new(
			$"{hostKitType.TypeName}{TypeHelpers.ResourceKitBaseClassSuffix}",
			hostKitType.Namespace
		);
		var resourceKits = resourceKitSymbols
			.Select(r =>
			{
				TypeValueObject resourceKitType = new(r.Symbol);

				var resourceKitOptionsNamespace =
					resourceKitType.Namespace
					+ (resourceKitType.IsGlobalNamespace ? null : '.')
					+ resourceKitType.TypeName;
				var resourceKitOptionsType = hostKitSymbol.GenerateOptions
					? new TypeValueObject(
						$"{resourceKitType.TypeName}{TypeHelpers.OptionsBaseClassSuffix}",
						resourceKitOptionsNamespace
					)
					: TypeValueObject.Empty;

				TypeValueObject aspireResourceType = new(r.AspireResourceTypeSymbol!);
				var accessibilityModifier = GetAccessibilityKeyword(r.Symbol.DeclaredAccessibility);
				var resourceName = r.Name;
				var propertyName = r.PropertyName;
				if (string.IsNullOrWhiteSpace(propertyName))
					propertyName = TrimSuffix(r.Symbol.Name);

				return new ResourceKitInfo(
					r,
					resourceKitType,
					resourceKitOptionsType,
					aspireResourceType,
					accessibilityModifier,
					r.PropertyName!,
					resourceName,
					TypeHelpers.HasExplicitBaseType(r)
				);
			})
			.ToImmutableArray();

		var accessibilityModifier = GetAccessibilityKeyword(hostKitSymbol.Symbol.DeclaredAccessibility);

		return new(
			hostKitSymbol,
			hostKitType,
			hostKitOptionsType,
			hostKitResourceKitBaseType,
			accessibilityModifier,
			hostKitSymbol.ExtensionName ?? "AddAspireResourceKit",
			resourceKits
		);
	}

	static string GetAccessibilityKeyword(Accessibility accessibility)
	{
		return accessibility switch
		{
			Accessibility.Public => "public ",
			Accessibility.Internal => "internal ",
			Accessibility.Private => "private ",
			Accessibility.Protected => "protected ",
			Accessibility.ProtectedAndInternal => "private protected ",
			Accessibility.ProtectedOrInternal => "protected internal ",
			_ => string.Empty,
		};
	}

	public static string TrimSuffix(string typeName) =>
		typeName.EndsWith("AppResource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "AppResource".Length)
		: typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName.EndsWith("Kit", StringComparison.Ordinal) ? typeName.Substring(0, typeName.Length - "Kit".Length)
		: typeName;
}
