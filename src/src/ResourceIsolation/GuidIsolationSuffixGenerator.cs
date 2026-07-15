namespace Purview.Aspire.ResourceIsolation;

public sealed class GuidIsolationSuffixGenerator : IIsolationSuffixGenerator
{
	public string CreateSuffix() => Guid.NewGuid().ToString("N")[..8];
}
