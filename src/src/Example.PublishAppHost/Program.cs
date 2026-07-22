var builder = DistributedApplication.CreateBuilder(args);

var settingsProvider = new ConfigurationIsolationSettingsProvider(builder.Configuration, "ResourceKit");
var context = AppIsolationContext.Create(builder, settingsProvider.Load());
IsolatedResourceCollection appModel = new(context);

DelegateIsolatedResource<ParameterResource> publishOnlyToggle = new(
	resourceKey: "publish-toggle",
	defaultName: "publish-toggle",
	build: (appBuilder, _, name) => appBuilder.AddParameter(name, "on", secret: false),
	isEnabled: ctx => ctx.IsPublishMode
);

DelegateIsolatedResource<ProjectResource> api = new(
	resourceKey: "api",
	defaultName: "publish-api",
	build: (appBuilder, _, name) => appBuilder.AddProject<Projects.Example_Service>(name),
	configure: (_, _, resource) =>
	{
		if (publishOnlyToggle.IsEnabled)
			resource.WithEnvironment("PUBLISH_TOGGLE", publishOnlyToggle.ResourceBuilder);
	}
);

appModel.Add(publishOnlyToggle);
appModel.Add(api);
appModel.Initialize(builder);

await builder.Build().RunAsync();
