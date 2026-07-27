using System.Reflection;
using System.Text;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Templates;

static class EmbeddedResources
{
	static readonly Assembly OwnerAssembly = typeof(EmbeddedResources).Assembly;

	public static string LoadTemplate(string name)
	{
		var resourceName = $"{AssemblyInfo.RootNamespace}.Templates.Sources.{name}.cs";

		var resourceStream = OwnerAssembly.GetManifestResourceStream(resourceName);
		if (resourceStream is null)
		{
			var existingResources = OwnerAssembly.GetManifestResourceNames();
			throw new ArgumentException(
				$"Could not find embedded resource {resourceName}. Available: {string.Join(", ", existingResources)}"
			);
		}

		string template;
		using (StreamReader reader = new(resourceStream, Encoding.UTF8))
			template = reader.ReadToEnd();

		return SourceGenHelpers.AddCodeGen(template);
	}
}
