namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct TypeValueObject(string TypeName, string Namespace)
{
	const string AttributeSuffix = nameof(Attribute);

	public string SymbolFullName => Namespace + '.' + TypeName;

	public string RenderFullName => "global::" + Namespace + "." + RenderTypeName;

	public string RenderTypeName =>
		IsAttribute(TypeName) ? TypeName.Substring(0, TypeName.Length - AttributeSuffix.Length) : TypeName;

	public override string ToString() => RenderFullName;

	static bool IsAttribute(string typeName) =>
		typeName.Length > AttributeSuffix.Length && typeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);
}
