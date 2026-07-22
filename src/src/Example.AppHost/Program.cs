using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

var builder = DistributedApplication.CreateBuilder(args);
if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.AppHost v{AssemblyInfo.Version}";

builder.Services.AddSingleton<IEnvironmentTagProvider, ConfigurationEnvironmentTagProvider>();
builder.Services.AddSingleton<HostAppBuildMetadata>();

builder.AddExampleHostApp();

var app = builder.Build();
await app.RunAsync();
