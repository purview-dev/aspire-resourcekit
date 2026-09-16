# Lifecycle: Build vs Configure

ResourceKit executes resources in a predictable sequence so dependency flow stays explicit and easy to
reason about.

## Runtime order

When the generated extension method is invoked, ResourceKit performs:

1. Instantiate resource kits from options.
2. `Build` each enabled resource.
3. `Configure` each enabled resource.

This happens before `DistributedApplication.Build()` completes.

## The two lifecycle pairs

- `Build` / `BuildResource(...)`: **construct this resource** (`AddProject`, `AddRedis`,
  `AddAzureStorage`, and so on).
- `Configure` / `ConfigureResource()`: **attach resources to each other** after construction
  (references, bindings, cross-resource wiring).

The separation keeps creation and cross-resource wiring explicit and deterministic.

## How the generated host kit drives the lifecycle

The generated host kit overrides `Build` and `Configure`:

- `Build` creates each discovered resource kit from its options, registers all of them with the base
  class via `AddResource`, invokes an optional `onBuilt` callback, and then calls `base.Build(builder)`,
  which runs `BuildResource(...)` for each resource.
- `Configure` calls `base.Configure()` first (running each resource's `ConfigureResource()`), then
  invokes an optional `onConfigured` callback.

The extension method signature gives you hooks for extra wiring:

```csharp
builder.AddAspireResourceKit(
    onBuilt: (hostKit, builder) => { /* after resources are created, before Configure */ },
    onConfigured: hostKit => { /* after all resources are configured */ },
    configureOptions: optionsBuilder => { /* additional options configuration */ }
);
```

`HostKitBase<THostKit>` (the runtime base of the generated host kit) enforces that resources cannot be
added after `Build` seals the resource list, keeping the lifecycle single-run and deterministic.

## Build vs Configure per resource

Each resource kit implements `IResourceKit<THostKit>`:

- `Build(IDistributedApplicationBuilder builder)` calls your `BuildResource(...)` override to
  **construct** the resource and store the resulting `IResourceBuilder<TResource>` in
  `ResourceBuilder`.
- `Configure()` calls your `ConfigureResource()` override to **attach** resources to each other after
  construction.

A common Configure example that connects an API to its dependencies:

```csharp
protected override void ConfigureResource()
{
    ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
    ResourceBuilder.WithReference(HostKit.AzureStorage.Blobs).WaitFor(HostKit.AzureStorage.Blobs);

    if (HostKit.Redis.IsEnabled)
        ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
}
```

Because `ConfigureResource()` runs after every resource is built, you can safely reach other resources
through the host kit's generated properties.

## Enablement gates both phases

Before `BuildResource(...)` runs, ResourceKit checks whether the resource should participate:

- If `IsEnabled` is `false`, both `BuildResource(...)` and `ConfigureResource()` are skipped.
- During `Build`, `IsResourceEnabled(builder)` is evaluated (only when `IsEnabled` is already `true`)
  and its result is assigned back to `IsEnabled`.

See [Enablement](Enablement.md) for the full model.

## See also

- [Generated Output](Generated-Output.md)
- [Enablement](Enablement.md)