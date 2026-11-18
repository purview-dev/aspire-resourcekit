var builder = DistributedApplication.CreateBuilder(args);
if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.AppHost17 v{AssemblyInfo.Version}";

builder.AddAspireResourceKit();

var app = builder.Build();

await app.RunAsync();
