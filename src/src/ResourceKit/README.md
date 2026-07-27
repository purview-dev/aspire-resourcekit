# Purview.Aspire.ResourceKit

`Purview.Aspire.ResourceKit` provides a source-generator-powered model for composing Aspire AppHost resources as strongly typed classes.

It is designed to make resource setup:

- easier to test,
- easier to navigate in IntelliSense,
- and more maintainable as your AppHost grows.

## Generated App Resource model

The package supports an AppModel style for composing Aspire AppHost resources:

- Mark your host app with `[HostApp]` (exactly one per compilation).
- Mark each resource with `[ResourceDefinition]`.
- Register everything with `builder.Add{HostAppClassName}ResourceKit()`.

### Attributes

```csharp
[HostApp(Name = "MyApp")]
sealed partial class MyAppHost { }

[ResourceDefinition<AzureStorageResource>("azure-storage", PropertyName = "Storage")]
sealed partial class AzureStorageAppResource
```

- **`HostKitAttribute`** — marks the host app class. Optional `Name` overrides generated type naming. `GenerateOptions` controls whether host app options are generated.
- **`ResourceDefinitionAttribute`** — marks a resource class. Optional `Name` sets the logical resource name. Optional `PropertyName` overrides the generated host-app property name. `GenerateOptions` controls per-resource options generation.

### Generated base class

The source generator emits an abstract base class in the host app namespace:

```csharp
 abstract class MyAppHostResourceBase<TResource>
    : ResourceKitBase<MyAppHost, TResource>
    where TResource : class, IResource
{
    // Your resource classes inherit this generated base type.
}
```

### Generated host app members

The source generator augments the host app partial class with:

- **Resource properties** — one property per `[ResourceDefinition]` class.
- **`Build(IDistributedApplicationBuilder)` override** — initializes resource instances, applies options-based enable/disable behavior, and builds resources.
- **`Configure()`** — inherited flow that configures all enabled resources after build.

### Generated options class

```csharp
public sealed class MyAppHostOptions
{
    public HashSet<string> DisabledResources { get; } = new(StringComparer.Ordinal);
    public Func<string, bool>? IsResourceEnabledPredicate { get; set; }
    public bool IsResourceDisabled(string resourceName) { ... }
}
```

Use this to programmatically enable/disable resources at startup when options generation is enabled.

### Builder extension

```csharp
public static IDistributedApplicationBuilder AddMyAppHostResourceKit(
    this IDistributedApplicationBuilder builder)
```

The generated extension builds/configures the host app and registers it as a singleton in DI.

### Execution order

Inside `Add{HostApp}ResourceKit()`, before `DistributedApplication.Build()`:

1. **Instantiate resource classes** (using generated options where enabled).
2. **Apply enable/disable state** from host options.
3. **Call `Build`** for each enabled resource.
4. **Call `Configure`** for each enabled resource.

### Diagnostics

| ID | Severity | Description |
| - | - | - |
| SG0001 | Error | Class must be `partial` |
| SG0002 | Info | No app resources defined for the host app |
| SG0003 | Warning | No host app defined (but app resources exist) |
| SG0004 | Error | Multiple `[HostApp]` classes defined |
| SG0005 | Error | Duplicate resource property name |
| SG0006 | Error | App resource must derive from `{HostApp}ResourceBase<T>` |
| SG0007 | Error | Resource name could not be derived and no `Name` was specified |
| SG0008 | Error | Explicit `PropertyName` is not a valid C# identifier |

## Minimal end-to-end sample

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[HostApp]
sealed partial class ShopHost;

[ResourceDefinition<ProjectResource>("api")]
sealed partial class ApiResource
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}

var builder = DistributedApplication.CreateBuilder(args);
builder.AddShopHostResourceKit();
```

## Configuration reference

When options are enabled:

- Host options section: `{HostAppClassName}`
- Resource options section: generated property name (or explicit `PropertyName`)

You can disable resource `api` via config by adding it to `DisabledResources`.
