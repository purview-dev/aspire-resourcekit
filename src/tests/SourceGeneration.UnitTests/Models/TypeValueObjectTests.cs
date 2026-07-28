using System.ComponentModel;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

public class TypeValueObjectTests
{
	[Test]
	[MethodDataSource(nameof(GetFullNameTestData))]
	public async Task SymbolFullName_GivenTypeValueObject_GeneratesCorrectSymbolFullName(
		string typeName,
		string @namespace,
		string excepation
	)
	{
		// Arrange
		TypeValueObject sut = new(typeName, @namespace);

		// Act
		var result = sut.SymbolFullName;

		// Assert
		await Assert.That(result).IsEqualTo(excepation);
	}

	[Test]
	[MethodDataSource(nameof(GetRenderFullNameTestData))]
	public async Task RenderFullName_GivenTypeValueObject_GeneratesCorrectRenderFullName(
		string typeName,
		string @namespace,
		string excepation
	)
	{
		// Arrange
		TypeValueObject sut = new(typeName, @namespace);

		// Act
		var result = sut.RenderFullName;

		// Assert
		await Assert.That(result).IsEqualTo(excepation);
	}

	[Test]
	[MethodDataSource(nameof(GetAttributeSymbolFullNameTestData))]
	public async Task SymbolFullName_GivenTypeValueObjectIsAttribute_GeneratesCorrectRenderFullNameWithSuffix(
		string typeName,
		string @namespace,
		string excepation
	)
	{
		// Arrange
		TypeValueObject sut = new(typeName, @namespace);

		// Act
		var result = sut.SymbolFullName;

		// Assert
		await Assert.That(result).IsEqualTo(excepation);
	}

	[Test]
	[MethodDataSource(nameof(GetAttributeRenderFullNameTestData))]
	public async Task RenderFullName_GivenTypeValueObjectIsAttribute_GeneratesCorrectRenderFullNameWithoutSuffix(
		string typeName,
		string @namespace,
		string exception
	)
	{
		// Arrange
		TypeValueObject sut = new(typeName, @namespace);

		// Act
		var result = sut.RenderFullName;

		// Assert
		await Assert.That(result).IsEqualTo(exception);
	}

	public static IEnumerable<Func<(string Type, string Namespace, string Expectation)>> GetFullNameTestData()
	{
		foreach (var type in GetTestTypes())
			yield return () => FromType(type);
	}

	public static IEnumerable<Func<(string Type, string Namespace, string Expectation)>> GetRenderFullNameTestData()
	{
		foreach (var type in GetTestTypes())
			yield return () => FromType(type, s => "global::" + s);
	}

	public static IEnumerable<
		Func<(string Type, string Namespace, string Expectation)>
	> GetAttributeSymbolFullNameTestData()
	{
		foreach (var type in GetTestTypes(useAttributes: true))
			yield return () => FromType(type);
	}

	public static IEnumerable<
		Func<(string Type, string Namespace, string Expectation)>
	> GetAttributeRenderFullNameTestData()
	{
		foreach (var type in GetTestTypes(useAttributes: true))
			yield return () => FromType(type, s => $"[global::{s[..^nameof(Attribute).Length]}]");
	}

	static IEnumerable<Type> GetTestTypes(bool useAttributes = false)
	{
		if (useAttributes)
		{
			yield return typeof(AttributeProviderAttribute);
			yield return typeof(BindableAttribute);
			yield return typeof(InstanceMethodDataSourceAttribute);
			yield return typeof(Microsoft.CodeAnalysis.EmbeddedAttribute);
		}
		else
		{
			yield return typeof(string);
			yield return typeof(HttpResponseMessage);
			yield return typeof(CodeWriter);
			yield return typeof(Microsoft.Extensions.DependencyModel.DependencyContext);
		}
	}

	static (string Type, string Namespace, string Expectation) FromType(
		Type type,
		Func<string, string>? expectation = null
	) => (type.Name!, type.Namespace!, expectation?.Invoke(type.FullName!) ?? type.FullName!);
}
