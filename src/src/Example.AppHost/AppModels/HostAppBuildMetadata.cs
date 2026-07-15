using Microsoft.Extensions.Configuration;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

sealed record HostAppBuildMetadata(string EnvironmentTag);

interface IEnvironmentTagProvider
{
	string GetTag();
}

sealed class ConfigurationEnvironmentTagProvider(IConfiguration configuration) : IEnvironmentTagProvider
{
	public string GetTag() =>
		configuration["HostApp:EnvironmentTag"]
		?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
		?? "local";
}
