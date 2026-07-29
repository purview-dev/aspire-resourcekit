# Getting started with Purview.Aspire.ResourceKit

This guide walks through a minimal host + resource setup using source generation.

> [!TIP]
> For a quick jump across lifecycle docs, see **Lifecycle quick links** in the root [`README.md`](../README.md).

## 1) Add the package

Install the `Purview.Aspire.ResourceKit` package in your AppHost project.

## 2) Define a host kit

Create a partial class and annotate it with `[HostKit]`.

```csharp
using Purview.Aspire.ResourceKit;

[HostKit]
partial class ShopHostKit;
```

## 3) Define one or more resources

Create a partial resource class per resource and annotate it with `[ResourceDefinition]`.

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[ResourceDefinition<ProjectResource>("api")]
partial class ApiResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

### Choose an attribute style

You have two valid styles:

- **Generic style**: `[ResourceDefinition<TResource>(...)]`
  - Preferred for most cases.
  - Do not declare an explicit base type.
- **Non-generic style**: `[ResourceDefinition(...)]`
  - Requires an explicit compatible base type that provides the resource type.

Example non-generic style:

```csharp
[ResourceDefinition("api")]
partial class ApiResourceKit : ShopHostKitResourceBase<ProjectResource>
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

Avoid mixing both styles on the same class.

## 3.1) Extend generated typed options (HostKit + ResourceKit)

ResourceKit generates typed options as nested `sealed partial` classes, so you can extend them with your own settings.

### Extend resource options

Add a nested partial options class inside your resource kit:

```csharp
[ResourceDefinition<ProjectResource>("api")]
sealed partial class ApiResourceKit
{
  protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
    => builder.AddProject<Projects.Example_Service>(Name);

  partial class ApiResourceKitOptions
  {
    public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
  }
}
```

Then consume it from the generated `Options` property inside the resource kit.

### Extend host options

Add a nested partial options class inside your host kit:

```csharp
[HostKit]
partial class ShopHostKit
{
  public sealed partial class ShopHostKitOptions
  {
    public bool EnablePreviewResources { get; set; }
  }
}
```

Then consume it via `HostKit.Options` from your resource kits.

> Tip: keep custom option members `public` with `get; set;` so configuration binding can populate them.

## 4) Register generated wiring in AppHost

Call the generated extension method from your AppHost entry point.

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
```

> The method name can be customized with `HostKitAttribute.ExtensionMethodName`.

## 5) Understand Build vs Configure (before adding dependencies)

ResourceKit has two lifecycle pairs:

- `Build` / `BuildResource(...)` builds the resource itself.
- `Configure` / `ConfigureResource()` wires resources together after build.

Think of it as:

- **Build phase**: create each resource.
- **Configure phase**: connect the created resources.

### How enablement is decided

Before `BuildResource(...)` runs, ResourceKit checks whether the resource should participate:

- `IsEnabled` is the current toggle value.
- `IsResourceEnabled(builder)` is called during `Build` to compute the effective runtime state.
- If disabled, both build and configure are skipped for that resource.

Use `IsResourceEnabled(builder)` when enablement should react to runtime conditions.

```csharp
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
{
  // Example: allow config + environment based behavior.
  var isEnabledInConfig = IsEnabled;
  var isProd = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production";

  return isEnabledInConfig && isProd;
}
```

## 6) Understand the runtime order

ResourceKit executes resources in a predictable sequence:

1. Instantiate resources from options
2. Build enabled resources
3. Configure enabled resources

This makes dependency flow explicit and easier to reason about.

## 7) Expand incrementally

A common progression:

- Start with one resource kit class
- Add more resource kit classes as AppHost grows
- Use generated options to toggle resources for local/dev/test scenarios

For configuration details, continue with [Configuration and options](configuration.md).
