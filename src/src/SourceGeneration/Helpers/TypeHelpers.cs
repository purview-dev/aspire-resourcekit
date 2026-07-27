using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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

	public static bool IsAttribute(string typeName) =>
		typeName.Length > AttributeSuffix.Length && typeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);

	// Generated type information...
	public const string ResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public const string ResourceKitBaseClassSuffix = "ResourceKitBase";

	public const string OptionsBaseClassSuffix = "Options";

	public static readonly TypeValueObject HostKitAttribute = new(nameof(HostKitAttribute), ResourceKitNamespace);

	public static readonly TypeValueObject ResourceDefinitionAttribute = new(
		nameof(ResourceDefinitionAttribute),
		ResourceKitNamespace
	);

	public static readonly TypeValueObject GenericResourceDefinitionAttribute = new(
		"ResourceDefinitionAttribute`1",
		ResourceKitNamespace
	);

	// Library types
	public static readonly TypeValueObject HostKitBase = new(nameof(HostKitBase), ResourceKitNamespace);

	public static readonly TypeValueObject ResourceKitBase = new(nameof(ResourceKitBase), ResourceKitNamespace);

	public static readonly TypeValueObject IResourceKit = new(nameof(IResourceKit), ResourceKitNamespace);

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
