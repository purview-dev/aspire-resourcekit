# Examples: Generated vs Manual

The repository ships two example AppHosts that compose the same set of resources in two different
styles:

- `src/src/Example.AppHost` — the **source-generated** pattern (attributes, no hand-written wiring).
- `src/src/Example.ManualAppHost` — the **manual** pattern (hand-written `HostKitBase` and
  `ResourceKitBase` composition, no generator).

Both hosts build the same logical resources through `Purview.Aspire.ResourceKit.Example` constants in
`Example.ServiceDefaults/Platform.cs`:

| Resource | Name | Aspire type |
| --- | --- | --- |
| Postgres | `postgres` (+ `db` database) | `AzurePostgresFlexibleServerResource` |
| Azure Storage | `azure-storage` (+ `blob`) | `AzureStorageResource` |
| Redis | `redis` | `AzureManagedRedisResource` |
| Key Vault | `kv` | `AzureKeyVaultResource` |
| API | `api` | `ProjectResource` (`Projects.Example_Service`) |
| Publish marker | `publish-marker` | `ParameterResource` |

Key Vault and the publish marker are publish-only (`IsResourceEnabled` returns
`builder.ExecutionContext.IsPublishMode`).

## Generated pattern (`Example.AppHost`)

`AppHost.cs` registers everything with a single generated call:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
var app = builder.Build();
await app.RunAsync();
```

The host kit is a one-line attribute declaration:

```csharp
[HostKit]
sealed partial class ExampleHostKit;
```

Each resource is a `[ResourceDefinition<TResource>]` partial class that supplies the overrides. For
example, the API kit also wires dependencies in `ConfigureResource()` and can now forward a populated
options object to a destination resource:

```csharp
[ResourceDefinition<Projects.Example_Service>(Platform.ResourceKits.API)]
sealed partial class ExampleAPIKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name).WithUrl("/health", "Health");

	protected override void ConfigureResource()
	{
		if (HostKit.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment(Options.PublishEnvironmentVariableName, HostKit.PublishMarker);

		ResourceBuilder.WithEnvironment(
			OptionsHelper.Environment(
				new DemoServiceEnvironmentOptions
				{
					Service = new()
					{
						Name = Name,
						Enabled = true,
						Labels =
						{
							["region"] = "west",
						},
					},
					Replicas = [1, 2, 3],
					Routes =
					{
						["health"] = "/health",
					},
				}
			)
			.Override(static o => o.Service.Name = "api")
			.Ignore(static o => o.Service.Labels)
			.Build()
		);

		ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
		ResourceBuilder.WithReference(HostKit.AzureStorage.Blobs).WaitFor(HostKit.AzureStorage.Blobs);

		if (HostKit.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(HostKit.KeyVault).WaitFor(HostKit.KeyVault);

		if (HostKit.Redis.IsEnabled)
			ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
	}

	partial class ExampleAPIKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
	}

	sealed class DemoServiceEnvironmentOptions
	{
		public DemoServiceOptions Service { get; set; } = new();

		public List<int> Replicas { get; set; } = [];

		public Dictionary<string, string> Routes { get; set; } = [];
	}

	sealed class DemoServiceOptions
	{
		public string Name { get; set; } = string.Empty;

		public bool Enabled { get; set; }

		public Dictionary<string, string> Labels { get; set; } = [];
	}
}
```

The generator emits the host kit members, options, `ResourceKitBase<TResource>` base classes, and the
extension method. See [Generated Output](Generated-Output.md).

## Manual pattern (`Example.ManualAppHost`)

`AppHost.cs` registers a host kit instance directly via the generic extension:

```csharp
builder.AddAspireResourceKit<ExampleHostKit>();
```

The host kit is hand-written and composes the resource kits:

```csharp
sealed class ExampleHostKit : HostKitBase<ExampleHostKit>
{
	public AzureStorageKit AzureStorage { get; init; }
	public ExampleAPIKit ExampleAPI { get; init; }
	// ...

	public ExampleHostKit()
	{
		AzureStorage = new(this);
		ExampleAPI = new(this);
		// ...
		AddResource(AzureStorage);
		AddResource(ExampleAPI);
		// ...
	}
}
```

Each resource kit is a hand-written partial class deriving from the runtime base, with a primary
constructor that takes the host kit:

```csharp
sealed partial class ExampleAPIKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, ProjectResource>(hostKit, Platform.ResourceKits.API)
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource()
	{
		if (HostKit.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", HostKit.PublishMarker);

		ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
		// ...
	}
}
```

## Choosing between the two

| Consideration | Generated | Manual |
| --- | --- | --- |
| Boilerplate | Minimal (attributes + overrides) | Explicit (base classes, constructors, `AddResource`) |
| Options | Generated, typed, bindable | You manage options yourself |
| Diagnostics | Analyzer checks model rules | No generator diagnostics |
| Testability | Both test well | Lifecycle is directly unit-testable |
| Best for | Most AppHosts | Learning the model, or when you want full control |

Both patterns produce the same runtime behavior: instantiate resources, `Build` enabled resources,
`Configure` enabled resources.

## See also

- [Getting Started](Getting-Started.md)
- [Attributes Reference](Attributes-Reference.md)
- [Generated Output](Generated-Output.md)
- [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md)
