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
| Postgres | `postgres` (+ child kit `db`) | `AzurePostgresFlexibleServerResource` |
| Azure Storage | `azure-storage` (+ child kit `blob`) | `AzureStorageResource` |
| VNET (publish-only) | `vnet` (+ child kits `snet-web`, `snet-data`, `snet-private-endpoint`) | `AzureVirtualNetworkResource` |
| Redis | `redis` | `AzureManagedRedisResource` |
| Key Vault | `kv` | `AzureKeyVaultResource` |
| API | `api` | `ProjectResource` (`Projects.Example_Service`) |
| Publish marker | `publish-marker` | `ParameterResource` |

The Postgres database and storage blob are separate child kits, so their builders come from the parent:

```csharp
[ResourceDefinition<AzurePostgresFlexibleServerDatabaseResource>(
    Platform.ResourceKits.PostgresDb,
    PropertyName = "Database",
    Parent = typeof(PostgresKit))]
sealed partial class PostgresDatabaseKit
{
    protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.AddDatabase(Name);
}
```

The VNET/subnet hierarchy is publish-only (`ResourceKitMode.Publish`) so the run-mode example does not need
Azure provisioning, but it is the issue #57 shape:

```csharp
[ResourceDefinition<AzureVirtualNetworkResource>(
    Platform.ResourceKits.Vnet,
    ResourceKitMode.Publish)]
[InGroup<DataGroup>]
sealed partial class VnetKit
{
    protected override IResourceBuilder<AzureVirtualNetworkResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            builder.AddAzureVirtualNetwork(Name);
}

[ResourceDefinition<AzureSubnetResource>(
    Platform.ResourceKits.SubnetWeb,
    PropertyName = "Web",
    Parent = typeof(VnetKit))]
sealed partial class SubnetWebKit
{
    protected override IResourceBuilder<AzureSubnetResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.AddSubnet(Name, "10.0.1.0/24");
}
```

See [Parent and child resource kits](Hierarchies.md).

Key Vault and the publish marker are publish-only. The generated AppHost declares that on the
attribute (`ResourceKitMode.Publish`), and the manual AppHost overrides the `Mode` property:

```csharp
// Example.AppHost (generated)
[ResourceDefinition<AzureKeyVaultResource>(Platform.ResourceKits.KeyVault, ResourceKitMode.Publish)]
sealed partial class KeyVaultKit;

// Example.ManualAppHost (manual)
protected override ResourceKitMode Mode => ResourceKitMode.Publish;
```

See [Enablement](Enablement.md) for the full mode list.

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
[InGroup<ServicesGroup>]
[ResourceUrl("Health", Platform.EndpointsDefinitions.Health, DisplayText = "Health")]
[EndpointUrl("https", DisplayText = "API")]
sealed partial class ExampleAPIKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

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

		if (HostKit.Postgres.IsEnabled && HostKit.Postgres.Database.IsEnabled)
			ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);

		if (HostKit.AzureStorage.IsEnabled && HostKit.AzureStorage.Blobs.IsEnabled)
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

		ResourceBuilder.WithReference(HostKit.PostgresDatabase).WaitFor(HostKit.PostgresDatabase);
		// ...
	}
}
```

A manual child passes its parent to the base constructor and exposes it as a typed property:

```csharp
sealed partial class PostgresDatabaseKit(ExampleHostKit hostKit, PostgresKit parent)
	: ResourceKitBase<ExampleHostKit, AzurePostgresFlexibleServerDatabaseResource>(
		hostKit,
		parent,
		Platform.ResourceKits.PostgresDb)
{
	public PostgresKit Parent { get; } = parent;

	protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>
		BuildResource(IDistributedApplicationBuilder builder) =>
			Parent.AddDatabase(Name);
}
```

The host creates it after the parent and registers it; `HostKitBase` validates the graph and builds the
parent first.

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
