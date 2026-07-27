using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct TypeValueObject
{
	public TypeValueObject(string typeName, string? @namespace)
	{
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
		Namespace = @namespace;
	}

	public TypeValueObject(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		TypeName = typeSymbol.Name;
		Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();
	}

	public TypeValueObject(SpecialType specialType)
		: this(specialType.ToString(), null)
	{
		if (!TypeHelpers.TryGetKeyword(specialType, out var keyword))
		{
			throw new ArgumentException(
				$"The provided special type '{specialType}' is not a recognized C# keyword type.",
				nameof(specialType)
			);
		}

		TypeName = keyword!;
	}

	public string TypeName { get; init; }

	public string? Namespace { get; init; }

	public string SymbolFullName => IsGlobalNamespace ? TypeName : $"{Namespace}.{TypeName}";

	public string RenderFullName
	{
		get
		{
			var result = IsGlobalNamespace ? TypeName : $"global::{Namespace}.{RenderTypeName}";
			return TypeHelpers.IsAttribute(TypeName) ? $"[{result}]" : result;
		}
	}

	public string RenderTypeName =>
		TypeHelpers.IsAttribute(TypeName)
			? TypeName.Substring(0, TypeName.Length - TypeHelpers.AttributeSuffix.Length)
			: TypeName;

	public bool IsGlobalNamespace => Namespace is null;

	public override string ToString() => RenderFullName;

	public static implicit operator string(TypeValueObject typeValueObject) => typeValueObject.RenderFullName;

	public TypeValueObject MakeGeneric(params TypeValueObject[] typeArguments)
	{
		if (typeArguments.Length == 0)
			throw new ArgumentException("At least one type argument must be provided.", nameof(typeArguments));

		string typeArgs = string.Join(", ", typeArguments.Select(arg => arg.RenderFullName));
		string fullTypeName = $"{TypeName}<{typeArgs}>";
		return new TypeValueObject(fullTypeName, Namespace);
	}

	public static readonly TypeValueObject Empty;
}
