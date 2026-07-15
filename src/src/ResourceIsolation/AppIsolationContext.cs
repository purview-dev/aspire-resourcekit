using System.Diagnostics;

namespace Purview.Aspire.ResourceIsolation;

[DebuggerDisplay("{Mode} | Prefix={Settings.NamePrefix} | Suffix={Settings.NameSuffix}")]
public sealed class AppIsolationContext(AppRunMode mode, IsolationSettings settings)
{
	public AppRunMode Mode { get; } = mode;

	public IsolationSettings Settings { get; } = settings;

	public bool IsPublishMode => Mode == AppRunMode.Publishing;

	public bool IsRunningMode => Mode == AppRunMode.Running;

	public bool IsLocalMode => Mode == AppRunMode.Local;

	public static AppIsolationContext Create(IDistributedApplicationBuilder builder, IsolationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(settings);

		var mode =
			settings.ForceMode
			?? (
				builder.ExecutionContext.IsPublishMode ? AppRunMode.Publishing
				: settings.IsRunning ? AppRunMode.Running
				: AppRunMode.Local
			);

		return new AppIsolationContext(mode, settings);
	}

	public string ResolveName(string resourceKey, string defaultName)
	{
		if (Settings.ResourceNameOverrides.TryGetValue(resourceKey, out var overrideName))
			return overrideName;

		var prefix = string.IsNullOrWhiteSpace(Settings.NamePrefix) ? string.Empty : $"{Settings.NamePrefix.Trim()}-";
		var suffix = string.IsNullOrWhiteSpace(Settings.NameSuffix) ? string.Empty : $"-{Settings.NameSuffix.Trim()}";

		return $"{prefix}{defaultName}{suffix}";
	}
}
