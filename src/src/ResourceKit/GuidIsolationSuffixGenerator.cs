namespace Purview.Aspire.ResourceKit;

public sealed class GuidIsolationSuffixGenerator : IIsolationSuffixGenerator
{
	public string CreateSuffix() => Guid.NewGuid().ToString("N")[..8];
}
