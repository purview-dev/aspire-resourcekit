// Every test class in this project starts an AppHost, and an AppHost allocates each project resource's
// target port by probing it and then releasing it before the project binds it. Two AppHosts starting at
// the same time can therefore be handed the same port, and the second api process dies with "address
// already in use" - which is how the release pipeline failed while a PR on the same commit passed.
//
// Serializing the fixtures removes the race inside one test process. The net10.0 and net11.0 test apps
// are still started in parallel by 'dotnet test', so the hook below gives each target framework its own
// non-overlapping Aspire port range as well.
[assembly: NotInParallel]

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Isolates the Aspire port allocations of the two target-framework test apps this project produces.
/// </summary>
static class TestProcessConfiguration
{
	internal const string PortRangeVariable = "ASPIRE_PROXYLESS_ENDPOINT_PORT_RANGE";

	[Before(TestSession)]
	public static void ConfigureAspirePortRange()
	{
		var major = TargetFrameworkMajorVersion();
		var start = 10000 + ((Math.Max(major, 10) - 10) * 10000);
		var end = Math.Min(start + 9999, 60000);
		Environment.SetEnvironmentVariable(PortRangeVariable, $"{start}-{end}");
	}

	static int TargetFrameworkMajorVersion()
	{
		// AppContext.TargetFrameworkName is the framework the test app was compiled for, not the runtime
		// it may have rolled forward onto, so net10.0 and net11.0 cannot collapse onto one range.
		const string marker = "Version=v";
		var name = AppContext.TargetFrameworkName;
		var index = name?.IndexOf(marker, StringComparison.Ordinal) ?? -1;

		return index >= 0 && int.TryParse(name.AsSpan(index + marker.Length, 2), out var major) ? major : 10;
	}
}
