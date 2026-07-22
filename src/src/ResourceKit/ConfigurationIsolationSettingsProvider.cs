using Microsoft.Extensions.Configuration;

namespace Purview.Aspire.ResourceKit;

public sealed class ConfigurationIsolationSettingsProvider(
	IConfiguration configuration,
	string sectionName = IsolationSettings.DefaultSectionName
) : IIsolationSettingsProvider
{
	public IsolationSettings Load()
	{
		var section = configuration.GetSection(sectionName);
		var settings = new IsolationSettings
		{
			NamePrefix = section["NamePrefix"],
			NameSuffix = section["NameSuffix"],
			IsRunning = bool.TryParse(section["IsRunning"], out var isRunning) && isRunning,
		};

		if (Enum.TryParse<AppRunMode>(section["Mode"], true, out var parsedMode))
			settings.ForceMode = parsedMode;

		foreach (var disabled in section.GetSection("DisabledResources").GetChildren())
		{
			if (!string.IsNullOrWhiteSpace(disabled.Value))
				settings.DisabledResources.Add(disabled.Value);
		}

		foreach (var overrideEntry in section.GetSection("ResourceNameOverrides").GetChildren())
		{
			if (!string.IsNullOrWhiteSpace(overrideEntry.Value))
				settings.ResourceNameOverrides[overrideEntry.Key] = overrideEntry.Value;
		}

		return settings;
	}
}
