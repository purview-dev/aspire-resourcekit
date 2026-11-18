using Purview.Aspire.ResourceKit.Example.Pre17.ManualAppHost.AppModels;

var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.Pre17.ManualAppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit<ExampleHostKit>();

var app = builder.Build();

await app.RunAsync();
