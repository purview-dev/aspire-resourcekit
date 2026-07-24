# Purview.Aspire.ResourceKit

Testable Aspire AppHost resource composition with runtime isolation modes:

- `Local`
- `Running`
- `Publishing`

You can selectively enable/disable resources and isolate names using prefix/suffix/overrides from config.

## Generated App Resource model

The package supports an AppModel style for composing Aspire AppHost resources:

- Mark your host app with `[HostApp]` (exactly one required; more than one is an error).
- Mark each resource with `[AppResource]` (optionally specifying `Name`, `PropertyName`, and `ServiceLifetime`).
- Implement resources by inheriting the generated `{HostAppClassName}AppResourceBase<TResource>`.
- Register everything with `builder.Add{HostAppClassName}()`.

### Attributes

```csharp
[HostApp(Name = "MyApp", ServiceLifetime = AppServiceLifetime.Singleton)]
sealed partial class MyAppHost { }

[ResourceDefinition("azure-storage", PropertyName = "Storage", ServiceLifetime = AppServiceLifetime.Singleton)]
sealed partial class AzureStorageAppResource : MyAppHostAppResourceBase<AzureStorageResource> { }
```

- **`HostAppAttribute`** — marks the single host app class. Optional `Name` overrides the generated base-class name (default: `{ClassName}AppResourceBase`). Optional `ServiceLifetime` controls the DI lifetime.
- **`ResourceDefinitionAttribute`** — marks an app resource. Optional `Name` sets the resource name (default: derived from the class name, minus any trailing `Resource`/`AppResource` suffix). Optional `PropertyName` overrides the generated host-app property name (default: PascalCase of the sanitized `Name`). Optional `ServiceLifetime` controls the DI lifetime.
- **`AppServiceLifetime`** — enum (`Singleton`, `Scoped`, `Transient`) used on both attributes; mapped to `Microsoft.Extensions.DependencyInjection.ServiceLifetime` in the generated registration.

### Generated base class

The source generator emits an abstract base class in the host app's namespace:

```csharp
public abstract class MyAppHostAppResourceBase<TResource>
    : HostAppResource<MyAppHost, TResource>, IHostAppResource<MyAppHost>
    where TResource : class, IResource
{
    protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder, IServiceProvider services)
    {
        var options = services.GetService<MyAppHostAppOptions>();
        if (options is not null && options.IsResourceDisabled(Name))
            return false;
        return base.IsResourceEnabled(builder, services);
    }
}
```

### Generated host app members

The source generator augments the host app partial class with:

- **Resource properties** — `public {ResourceType} {PropertyName} { get; private set; } = default!;` for each app resource.
- **`Initialise(IServiceProvider)`** — resolves each app resource from DI and assigns it to the corresponding property.
- **`Build(IDistributedApplicationBuilder, IServiceProvider)`** — calls `BuildResource` on each resource (consulting `IsResourceEnabled`).
- **`Configure(IServiceProvider)`** — calls `ConfigureResource` on each resource, allowing cross-resource wiring (e.g. `WithReference`, `WaitFor`).

### Generated options class

```csharp
public sealed class MyAppHostAppOptions
{
    public HashSet<string> DisabledResources { get; } = new(StringComparer.Ordinal);
    public Func<string, bool>? IsResourceEnabledPredicate { get; set; }
    public bool IsResourceDisabled(string resourceName) { ... }
}
```

Use this to programmatically enable/disable resources at startup.

### Builder extension

```csharp
public static IDistributedApplicationBuilder AddMyAppHost(
    this IDistributedApplicationBuilder builder,
    Action<IServiceCollection>? configureServices = null,
    Action<MyAppHostAppOptions>? configureOptions = null,
    AppServiceLifetime? hostAppLifetime = null,
    AppServiceLifetime? resourceLifetimeOverride = null)
```

- `configureServices` — register custom dependencies (e.g. `IEnvironmentTagProvider`, configuration services).
- `configureOptions` — programmatically disable resources or set an enable predicate.
- `hostAppLifetime` / `resourceLifetimeOverride` — override the DI lifetimes declared on the attributes.

### Execution order

Inside `Add{HostApp}()`, before `DistributedApplication.Build()`:

1. **`Initialise(IServiceProvider)`** — resolve all `[AppResource]` classes from DI (ctor-injected deps satisfied) and assign to host-app properties.
2. **`Build(IDistributedApplicationBuilder, IServiceProvider)`** — for each resource: consult `IsResourceEnabled` (DI/options/config-driven) then run the user's `Build` (e.g. `builder.AddAzureStorage(Name)`), populating `ResourceBuilder` and sub-builders.
3. **`Configure(IServiceProvider)`** — for each resource: run the user's `Configure` to wire cross-references (`WithReference`, `WaitFor`) now that all sub-builders are populated.

### Diagnostics

| ID   | Severity | Description |
|------|----------|-------------|
| SG0001 | Error | Class must be `partial` |
| SG0002 | Info   | No app resources defined for the host app |
| SG0003 | Warning | No host app defined (but app resources exist) |
| SG0004 | Error  | Multiple `[HostApp]` classes defined |
| SG0005 | Error  | Duplicate resource property name |
| SG0006 | Error  | App resource must derive from `{HostApp}AppResourceBase<T>` |
| SG0007 | Error  | Resource name could not be derived and no `Name` was specified |
| SG0008 | Error  | Explicit `PropertyName` is not a valid C# identifier |
