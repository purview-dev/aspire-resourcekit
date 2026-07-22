namespace Purview.Aspire.ResourceKit;

public sealed class IsolationSettings
{
	public const string DefaultSectionName = "Aspire:Isolation";

	public AppRunMode? ForceMode { get; set; }

	public bool IsRunning { get; set; }

	public string? NamePrefix { get; set; }

	public string? NameSuffix { get; set; }

	public HashSet<string> DisabledResources { get; } = [with(StringComparer.OrdinalIgnoreCase)];

	public Dictionary<string, string> ResourceNameOverrides { get; } = [with(StringComparer.OrdinalIgnoreCase)];

	public static IsolationSettings CreateScoped(
		string? namePrefix = null,
		IIsolationSuffixGenerator? suffixGenerator = null
	)
	{
		suffixGenerator ??= new GuidIsolationSuffixGenerator();
		return new IsolationSettings { NamePrefix = namePrefix, NameSuffix = suffixGenerator.CreateSuffix() };
	}

	public bool IsResourceDisabled(string resourceKey) => DisabledResources.Contains(resourceKey);
}
