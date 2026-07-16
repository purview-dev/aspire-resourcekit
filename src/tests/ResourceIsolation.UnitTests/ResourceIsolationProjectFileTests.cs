using System.Xml.Linq;

namespace Purview.Aspire.ResourceIsolation;

public sealed class ResourceIsolationProjectFileTests
{
	[Test]
	public async Task ResourceIsolationProject_ReferencesSourceGeneratorAsAnalyzer()
	{
		var document = XDocument.Load(GetResourceIsolationProjectPath());
		var projectReference = document
			.Descendants("ProjectReference")
			.SingleOrDefault(x => (string?)x.Attribute("Include") == "..\\SourceGeneration\\SourceGeneration.csproj");

		await Assert.That(projectReference).IsNotNull();
		await Assert.That((string?)projectReference!.Attribute("OutputItemType")).IsEqualTo("Analyzer");
		await Assert.That((string?)projectReference.Attribute("ReferenceOutputAssembly")).IsEqualTo("false");
		await Assert.That((string?)projectReference.Attribute("PrivateAssets")).IsEqualTo("all");
	}

	[Test]
	public async Task ResourceIsolationProject_PackagesAnalyzerAndPropsAssets()
	{
		var document = XDocument.Load(GetResourceIsolationProjectPath());
		var includeAnalyzerTarget = document
			.Descendants("Target")
			.SingleOrDefault(x => (string?)x.Attribute("Name") == "IncludeAnalyzerInPackage");

		await Assert.That(includeAnalyzerTarget).IsNotNull();
		await Assert.That((string?)includeAnalyzerTarget!.Attribute("BeforeTargets")).IsEqualTo("_GetPackageFiles");

		var noneItems = includeAnalyzerTarget.Descendants("None").ToArray();
		await Assert.That(noneItems.Any(x => (string?)x.Attribute("PackagePath") == "analyzers/dotnet/cs")).IsTrue();
		await Assert.That(
			noneItems.Any(x =>
				((string?)x.Attribute("Include") ?? string.Empty)
					.Contains("build\\Purview.Aspire.ResourceIsolation.props", StringComparison.Ordinal)
				&& (string?)x.Attribute("PackagePath") == "build"
			)
		).IsTrue();
	}

	static string GetResourceIsolationProjectPath() => Path.GetFullPath(
		Path.Combine(AppContext.BaseDirectory, "../../../../../src/ResourceIsolation/ResourceIsolation.csproj")
	);
}
