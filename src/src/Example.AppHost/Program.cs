using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

var builder = DistributedApplication.CreateBuilder(args);
if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.AppHost v{AssemblyInfo.Version}";

builder.AddHostAppResources(static services =>
{
	services.AddSingleton<IEnvironmentTagProvider, ConfigurationEnvironmentTagProvider>();
	services.AddSingleton(new HostAppBuildMetadata("example-apphost"));
});

var app = builder.Build();
await app.RunAsync();
