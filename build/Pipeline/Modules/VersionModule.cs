using System.Text.Json;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.Pipeline.Modules;

[ModuleCategory("Build")]
public class VersionModule : Module<string>
{
	protected override async Task<string?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var packageJsonPath = Path.Combine(Environment.CurrentDirectory, "package.json");

		if (!File.Exists(packageJsonPath))
		{
			throw new FileNotFoundException($"Could not find package.json at {packageJsonPath}");
		}

		var packageJson = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);

		using var document = JsonDocument.Parse(packageJson);
		var version = document.RootElement.GetProperty("version").GetString();

		if (string.IsNullOrWhiteSpace(version))
		{
			throw new InvalidOperationException(
				"The version field in package.json is missing or empty."
			);
		}

		context.Summary.KeyValue("Version", "Package version", version);
		return version;
	}
}
