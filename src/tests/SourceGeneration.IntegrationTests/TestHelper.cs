using System.Globalization;
using System.Text;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

static class TestHelper
{
	public const string DefaultHostKitType = "TestingHostKit";

	public const string DefaultHostKitNamespace = "Testing.HostKitNamespace";

	public const string DefaultResourceKitType = "TestingResourceKit";

	public const string DefaultResourceKitNamespace = "Testing.ResourceKitNamespace";

	public const string DefaultAspireResource = "TestingAspireResource";

	public static string GenerateBuildResource(string aspireResource = DefaultAspireResource) =>
		$"protected override IResourceBuilder<{aspireResource}> BuildResource(IDistributedApplicationBuilder builder) => throw new NotImplementedException();";

	public static string GenerateSource(HostKitInfo? hostKit, params ResourceKitInfo[] resourceKits)
	{
		StringBuilder builder = new();

		if (hostKit is not null)
		{
			if (hostKit.HostKitNamespace is not null)
			{
				builder
					.AppendLine(CultureInfo.InvariantCulture, $"namespace {hostKit.HostKitNamespace}")
					.Append('{')
					.AppendLine();
			}

			builder.AppendLine(
				CultureInfo.InvariantCulture,
				$"{hostKit.Accessibility} class {hostKit.HostKitType} {{ }}"
			);

			if (hostKit.HostKitNamespace is not null)
			{
				builder.Append('}').AppendLine();
			}
		}

		foreach (var resourceKit in resourceKits)
		{
			if (resourceKit.ResourceKitNamespace is not null)
			{
				builder
					.AppendLine(CultureInfo.InvariantCulture, $"namespace {resourceKit.ResourceKitNamespace}")
					.Append('{')
					.AppendLine();
			}

			var baseClass = "";
			if (resourceKit.BaseClass is not null)
			{
				baseClass = $" : {resourceKit.BaseClass}<";
				if (hostKit is not null)
					baseClass += hostKit.HostKitType;

				baseClass += ">";
			}

			builder.AppendLine(
				CultureInfo.InvariantCulture,
				$"{resourceKit.Accessibility} class {resourceKit.ResourceKitType}{baseClass}"
			);
			builder.Append('{').AppendLine();

			builder
				.Append("protected override IResourceBuilder<")
				.Append(resourceKit.AspireResource)
				.AppendLine("> BuildResource(IDistributedResourceBuilder app) => throw new NotImplementedException();");

			builder.Append('}').AppendLine();

			if (resourceKit.ResourceKitNamespace is not null)
			{
				builder.Append('}').AppendLine();
			}
		}

		return builder.ToString();
	}
}

sealed record HostKitInfo(
	string HostKitType = TestHelper.DefaultHostKitType,
	string? HostKitNamespace = TestHelper.DefaultHostKitNamespace,
	string? BaseClass = null,
	string Accessibility = "public"
)
{
	public static HostKitInfo Default = new(
		HostKitType: TestHelper.DefaultHostKitType,
		HostKitNamespace: TestHelper.DefaultHostKitNamespace,
		BaseClass: null,
		Accessibility: "public"
	);
}

sealed record ResourceKitInfo(
	string ResourceKitType = TestHelper.DefaultResourceKitType,
	string AspireResource = TestHelper.DefaultAspireResource,
	string? BaseClass = null,
	string? ResourceKitNamespace = TestHelper.DefaultResourceKitNamespace,
	string Accessibility = "public"
)
{
	public static ResourceKitInfo Default = new(
		ResourceKitType: TestHelper.DefaultResourceKitType,
		AspireResource: TestHelper.DefaultAspireResource,
		BaseClass: TypeHelpers.ResourceKitBase.SymbolFullName,
		ResourceKitNamespace: TestHelper.DefaultResourceKitNamespace,
		Accessibility: "public"
	);
}
