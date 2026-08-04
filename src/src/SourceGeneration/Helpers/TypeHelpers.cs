using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class TypeHelpers
{
	public const string AttributeSuffix = nameof(Attribute);

	static readonly (string Keyword, SpecialType SpecialType)[] Map =
	[
		("bool", SpecialType.System_Boolean),
		("byte", SpecialType.System_Byte),
		("sbyte", SpecialType.System_SByte),
		("char", SpecialType.System_Char),
		("decimal", SpecialType.System_Decimal),
		("double", SpecialType.System_Double),
		("float", SpecialType.System_Single),
		("int", SpecialType.System_Int32),
		("uint", SpecialType.System_UInt32),
		("long", SpecialType.System_Int64),
		("ulong", SpecialType.System_UInt64),
		("short", SpecialType.System_Int16),
		("ushort", SpecialType.System_UInt16),
		("string", SpecialType.System_String),
		("object", SpecialType.System_Object),
		("void", SpecialType.System_Void),
		("nint", SpecialType.System_IntPtr),
		("nuint", SpecialType.System_UIntPtr),
	];

	static readonly ImmutableDictionary<string, SpecialType> KeywordToSpecialType = Map.ToImmutableDictionary(
		m => m.Keyword,
		m => m.SpecialType,
		StringComparer.Ordinal
	);

	static readonly ImmutableDictionary<SpecialType, string> SpecialTypeToKeyword = Map.ToImmutableDictionary(
		m => m.SpecialType,
		m => m.Keyword
	);

	public static bool TryGetSpecialType(string keyword, out SpecialType specialType) =>
		KeywordToSpecialType.TryGetValue(keyword, out specialType);

	public static bool TryGetKeyword(SpecialType specialType, out string? keyword) =>
		SpecialTypeToKeyword.TryGetValue(specialType, out keyword);

	public static bool IsKeywordType(ITypeSymbol type) => SpecialTypeToKeyword.ContainsKey(type.SpecialType);

	public static bool IsKeywordType(string keyword) => KeywordToSpecialType.ContainsKey(keyword);

	public static bool IsAttribute(string typeName)
	{
		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		return typeName.Length > AttributeSuffix.Length && typeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);
	}

	public static string GetTypeName(string typeName)
	{
		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		if (IsAttribute(typeName))
			typeName = typeName.Substring(0, typeName.Length - AttributeSuffix.Length);

		return typeName;
	}

	public static bool HasExplicitBaseType(TargetSymbolDescriptor descriptor) =>
		descriptor.Declaration.BaseList is { Types.Count: > 0 };

	public static bool IsDerivedFromExpectedBase(TargetSymbolDescriptor descriptor, TypeValueObject expectedBase)
	{
		if (descriptor.Symbol.BaseType is not null)
		{
			TypeValueObject baseType = new(descriptor.Symbol.BaseType);
			if (baseType == expectedBase)
				return true;
		}

		var declaredBaseTypes = descriptor.Declaration.BaseList?.Types;
		if (declaredBaseTypes is null)
			return false;

		foreach (var baseType in declaredBaseTypes)
		{
			if (
				string.Equals(
					GetUnqualifiedTypeName(baseType.Type),
					expectedBase.SymbolFullName,
					StringComparison.Ordinal
				)
			)
				return true;
		}

		return false;
	}

	public static string GetUnqualifiedTypeName(TypeSyntax typeSyntax) =>
		typeSyntax switch
		{
			IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => GetUnqualifiedTypeName(qualifiedName.Right),
			AliasQualifiedNameSyntax aliasQualifiedName => GetUnqualifiedTypeName(aliasQualifiedName.Name),
			NullableTypeSyntax nullableType => GetUnqualifiedTypeName(nullableType.ElementType),
			_ => typeSyntax.ToString(),
		};

	public static string DeriveResourceName(string typeName) => CodeGenHelpers.TrimSuffix(typeName);

	public static bool IsValidIdentifier(string? name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		if (!char.IsLetter(name![0]) && name[0] != '_')
			return false;
		for (var i = 1; i < name.Length; i++)
		{
			if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
				return false;
		}

		return true;
	}

	// Generated type information...
	public const string PurviewAspireResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public const string OptionsBaseClassSuffix = "Options";

	public static readonly TypeValueObject HostKitAttribute = new(
		nameof(HostKitAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject ResourceDefinitionAttribute = new(
		nameof(ResourceDefinitionAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject GenericResourceDefinitionAttribute = new(
		"ResourceDefinitionAttribute`1",
		PurviewAspireResourceKitNamespace
	);

	// Library types
	public static readonly TypeValueObject IHostKit = new(nameof(IHostKit), PurviewAspireResourceKitNamespace);

	public static readonly TypeValueObject HostKitBase = new(nameof(HostKitBase), PurviewAspireResourceKitNamespace);

	public static readonly TypeValueObject ResourceKitBase = new(
		nameof(ResourceKitBase),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject IResourceKit = new(nameof(IResourceKit), PurviewAspireResourceKitNamespace);

	// Other required types
	// Required for DI
	public static readonly TypeValueObject IServiceCollection = new(
		nameof(IServiceCollection),
		"Microsoft.Extensions.DependencyInjection"
	);

	public static readonly TypeValueObject ConfigurationBinder = new(
		nameof(ConfigurationBinder),
		"Microsoft.Extensions.Configuration"
	);

	// Aspire types.
	public static readonly TypeValueObject IResource = new(nameof(IResource), "Aspire.Hosting.ApplicationModel");

	public static readonly TypeValueObject IDistributedApplicationBuilder = new(
		nameof(IDistributedApplicationBuilder),
		"Aspire.Hosting"
	);

	// Other useful types
	public static readonly TypeValueObject NotNullAttribute = new(
		nameof(NotNullAttribute),
		"System.Diagnostics.CodeAnalysis"
	);
	public static readonly TypeValueObject EmbeddedAttribute = new(nameof(EmbeddedAttribute), "Microsoft.CodeAnalysis");

	// Generated attributes (make sure this is after they're all initialized!)
	public static readonly TypeValueObject[] GeneratedTypes = [HostKitAttribute, ResourceDefinitionAttribute];
}
