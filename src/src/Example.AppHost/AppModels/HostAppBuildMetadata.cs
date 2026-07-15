using Microsoft.Extensions.Configuration;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

sealed class HostAppBuildMetadata(IEnvironmentTagProvider tagProvider)
{
	public string EnvironmentTag => tagProvider.GetTag();
}

interface IEnvironmentTagProvider
{
	string GetTag();
}

sealed class ConfigurationEnvironmentTagProvider(IConfiguration configuration) : IEnvironmentTagProvider
{
	public string GetTag() =>
		configuration["HostApp:EnvironmentTag"]
		?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
		?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
		?? "Local";
}
