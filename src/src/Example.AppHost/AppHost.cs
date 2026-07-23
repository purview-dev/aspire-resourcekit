using Purview.Aspire.ResourceKit.Example.AppHost.AppModels;

var builder = DistributedApplication.CreateBuilder(args);
if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.AppHost v{AssemblyInfo.Version}";

builder.AddExampleHostAppResourceKit();

var app = builder.Build();
await app.RunAsync();
