var builder = DistributedApplication.CreateBuilder(args);
if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Example.Pre17.AppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit();

var app = builder.Build();

await app.RunAsync();
